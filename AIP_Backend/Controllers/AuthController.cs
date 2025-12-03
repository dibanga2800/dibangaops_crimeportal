using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }


        /// <summary>
        /// Authenticate user and return JWT tokens
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Login attempt for user {Email}", request.Email);

                // Validate request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed: Inactive user {UserId}", user.Id);
                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Account is deactivated. Please contact administrator."
                    });
                }

                // Check if user is locked out
                if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Login failed: User {UserId} is locked out until {LockoutUntil}", user.Id, user.LockoutUntil);
                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = $"Account is temporarily locked. Please try again after {user.LockoutUntil.Value:yyyy-MM-dd HH:mm:ss} UTC"
                    });
                }

                // Verify password
                _logger.LogInformation("Verifying password for user {Email} with password length: {PasswordLength}", 
                    request.Email, request.Password?.Length ?? 0);

                var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password ?? "", lockoutOnFailure: true);
                if (!passwordResult.Succeeded)
                {
                    _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Get user roles - normalize to lowercase for consistency
                var roles = (await _userManager.GetRolesAsync(user))
                    .Select(r => r.Trim().ToLowerInvariant())
                    .ToList();

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user, roles);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Update user login information
                user.LastLoginAt = DateTime.UtcNow;
                user.LoginAttempts = 0;
                user.LockoutUntil = null;
                await _userManager.UpdateAsync(user);

                // Determine if user is a customer role (handle both PascalCase and lowercase)
                var normalizedRole = (user.Role ?? "").Trim().ToLowerInvariant();
                var isCustomerRole = normalizedRole == "customersitemanager" || normalizedRole == "customerhomanager";
                
                // For customer users: Use direct CustomerId field (foreign key to Customer table)
                // For AdvantageOne users: Use AssignedCustomerIds (JSON array for multiple customers)
                int? customerId = null;
                int[] customerAssignmentsForResponse = Array.Empty<int>();
                
                if (isCustomerRole)
                {
                    // Priority 1: Use direct CustomerId field (most reliable - direct foreign key)
                    if (user.CustomerId.HasValue && user.CustomerId.Value > 0)
                    {
                        customerId = user.CustomerId.Value;
                        _logger.LogInformation("Setting CustomerId {CustomerId} for customer user {UserId} from direct CustomerId field", customerId, user.Id);
                    }
                    // Priority 2: Fallback to AssignedCustomerIds JSON array (first ID)
                    else
                    {
                        var customerIdsFromUserField = user.CustomerIds;
                        if (customerIdsFromUserField.Count > 0)
                        {
                            customerId = customerIdsFromUserField[0];
                            _logger.LogInformation("Setting CustomerId {CustomerId} for customer user {UserId} from AssignedCustomerIds field", customerId, user.Id);
                        }
                        // Priority 3: Fallback to UserCustomerAssignments table
                        else
                        {
                            var customerAssignmentsFromTable = await _context.UserCustomerAssignments
                                .Where(uca => uca.UserId == user.Id)
                                .Select(uca => uca.CustomerId)
                                .ToArrayAsync();
                            
                            if (customerAssignmentsFromTable.Length > 0)
                            {
                                customerId = customerAssignmentsFromTable[0];
                                _logger.LogInformation("Setting CustomerId {CustomerId} for customer user {UserId} from UserCustomerAssignments table", customerId, user.Id);
                            }
                            else
                            {
                                _logger.LogWarning("No customerId found for customer user {UserId}. User has no CustomerId, AssignedCustomerIds, or UserCustomerAssignments.", user.Id);
                            }
                        }
                    }
                }
                else
                {
                    // For AdvantageOne users: Get all assigned customer IDs
                    var customerIdsFromUserField = user.CustomerIds;
                    customerAssignmentsForResponse = customerIdsFromUserField.Count > 0 
                        ? customerIdsFromUserField.ToArray()
                        : await _context.UserCustomerAssignments
                            .Where(uca => uca.UserId == user.Id)
                            .Select(uca => uca.CustomerId)
                            .ToArrayAsync();
                }

                var userResponse = await BuildUserResponseAsync(user, roles);

                var response = new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                    User = userResponse
                };

                _logger.LogInformation("Login successful for user {UserId}", user.Id);

                return Ok(new ApiResponseDto<LoginResponseDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", request.Email);
                return StatusCode(500, new ApiResponseDto<LoginResponseDto>
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again."
                });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<RefreshTokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                _logger.LogInformation("Refresh token request received");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<RefreshTokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                // Validate refresh token
                if (!_jwtService.ValidateRefreshToken(request.RefreshToken))
                {
                    _logger.LogWarning("Invalid refresh token provided");
                    return Unauthorized(new ApiResponseDto<RefreshTokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    });
                }

                // Get user from expired access token (if available in Authorization header)
                string? userId = null;
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var expiredToken = authHeader.Substring("Bearer ".Length);
                    try
                    {
                        var principal = _jwtService.GetPrincipalFromExpiredToken(expiredToken);
                        userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    }
                    catch
                    {
                        // Token is invalid or expired, which is expected
                    }
                }

                // If we couldn't get userId from expired token, we need to implement
                // a way to store and retrieve refresh tokens with their associated users
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Could not determine user for refresh token");
                    return Unauthorized(new ApiResponseDto<RefreshTokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    });
                }

                // Validate refresh token for user
                if (!await _jwtService.IsRefreshTokenValidAsync(request.RefreshToken, userId))
                {
                    _logger.LogWarning("Invalid refresh token for user {UserId}", userId);
                    return Unauthorized(new ApiResponseDto<RefreshTokenResponseDto>
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    });
                }

                // Get user
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("User {UserId} not found or inactive", userId);
                    return Unauthorized(new ApiResponseDto<RefreshTokenResponseDto>
                    {
                        Success = false,
                        Message = "User not found or inactive"
                    });
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                var response = new RefreshTokenResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["Jwt:AccessTokenExpirationMinutes"]))
                };

                _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);

                return Ok(new ApiResponseDto<RefreshTokenResponseDto>
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ApiResponseDto<RefreshTokenResponseDto>
                {
                    Success = false,
                    Message = "An error occurred during token refresh. Please try again."
                });
            }
        }

        /// <summary>
        /// Logout user and invalidate refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public Task<ActionResult<ApiResponseDto<LogoutResponseDto>>> Logout([FromBody] LogoutRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Logout request for user {UserId}", userId ?? "Unknown");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Task.FromResult<ActionResult<ApiResponseDto<LogoutResponseDto>>>(BadRequest(new ApiResponseDto<LogoutResponseDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    }));
                }

                // In a real application, you would invalidate the refresh token in the database
                // For now, we'll just return success
                // TODO: Implement refresh token invalidation

                var response = new LogoutResponseDto
                {
                    Success = true,
                    Message = "Logout successful"
                };

                _logger.LogInformation("Logout successful for user {UserId}", userId ?? "Unknown");

                return Task.FromResult<ActionResult<ApiResponseDto<LogoutResponseDto>>>(Ok(new ApiResponseDto<LogoutResponseDto>
                {
                    Success = true,
                    Message = "Logout successful",
                    Data = response
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Task.FromResult<ActionResult<ApiResponseDto<LogoutResponseDto>>>(StatusCode(500, new ApiResponseDto<LogoutResponseDto>
                {
                    Success = false,
                    Message = "An error occurred during logout. Please try again."
                }));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<object>>> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Change password request for user {UserId}", userId ?? "Unknown");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid user identifier"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                _logger.LogInformation("Attempting to change password for user {UserId}. Current password length: {CurrentLength}, New password length: {NewLength}", 
                    userId, request.CurrentPassword?.Length ?? 0, request.NewPassword?.Length ?? 0);

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword ?? "", request.NewPassword ?? "");
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Password change failed for user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Failed to change password",
                        Errors = errors
                    });
                }

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during password change. Please try again."
                });
            }
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<object>>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                _logger.LogInformation("Forgot password request for email {Email}", request.Email);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    // Don't reveal that the user doesn't exist
                    _logger.LogInformation("Forgot password request for non-existent email {Email}", request.Email);
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = true,
                        Message = "If the email address exists in our system, you will receive a password reset link."
                    });
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Create reset link
                var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(request.Email)}";
                
                // Send password reset email
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(request.Email, resetLink);
                    _logger.LogInformation("Password reset email sent to {Email}", request.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send password reset email to {Email}", request.Email);
                    // Don't fail the request if email fails
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "If the email address exists in our system, you will receive a password reset link."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during password reset request. Please try again."
                });
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<object>>> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                _logger.LogInformation("Reset password request for email {Email}", request.Email);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid reset token or email"
                    });
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Failed to reset password",
                        Errors = errors
                    });
                }

                _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Password reset successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during password reset. Please try again."
                });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<UserResponseDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get current user request for user {UserId}", userId ?? "Unknown");

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ApiResponseDto<UserResponseDto>
                    {
                        Success = false,
                        Message = "Invalid user identifier"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponseDto<UserResponseDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);

                // Determine if user is a customer role (handle both PascalCase and lowercase)
                var normalizedRole = (user.Role ?? "").Trim().ToLowerInvariant();
                var isCustomerRole = normalizedRole == "customersitemanager" || normalizedRole == "customerhomanager";
                
                // For customer users: Use direct CustomerId field (foreign key to Customer table)
                // For AdvantageOne users: Use AssignedCustomerIds (JSON array for multiple customers)
                int? customerId = null;
                int[] customerAssignmentsForResponse = Array.Empty<int>();
                
                if (isCustomerRole)
                {
                    // Priority 1: Use direct CustomerId field (most reliable - direct foreign key)
                    if (user.CustomerId.HasValue && user.CustomerId.Value > 0)
                    {
                        customerId = user.CustomerId.Value;
                    }
                    // Priority 2: Fallback to AssignedCustomerIds JSON array (first ID)
                    else
                    {
                        var customerIdsFromUserField = user.CustomerIds;
                        if (customerIdsFromUserField.Count > 0)
                        {
                            customerId = customerIdsFromUserField[0];
                        }
                        // Priority 3: Fallback to UserCustomerAssignments table
                        else
                        {
                            var customerAssignmentsFromTable = await _context.UserCustomerAssignments
                                .Where(uca => uca.UserId == user.Id)
                                .Select(uca => uca.CustomerId)
                                .ToArrayAsync();
                            
                            if (customerAssignmentsFromTable.Length > 0)
                            {
                                customerId = customerAssignmentsFromTable[0];
                            }
                        }
                    }
                }
                else
                {
                    // For AdvantageOne users: Get all assigned customer IDs
                    var customerIdsFromUserField = user.CustomerIds;
                    customerAssignmentsForResponse = customerIdsFromUserField.Count > 0 
                        ? customerIdsFromUserField.ToArray()
                        : await _context.UserCustomerAssignments
                            .Where(uca => uca.UserId == user.Id)
                            .Select(uca => uca.CustomerId)
                            .ToArrayAsync();
                }

                var userResponse = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    JobTitle = user.JobTitle,
                    Signature = user.Signature,
                    SignatureCode = user.SignatureCode,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLoginAt = user.LastLoginAt,
                    LoginAttempts = user.LoginAttempts,
                    LockoutUntil = user.LockoutUntil,
                    Role = user.Role ?? "",
                    PageAccessRole = user.PageAccessRole ?? "",
                    Roles = roles.ToArray(),
                    AssignedCustomerIds = customerAssignmentsForResponse, // Set assigned customer IDs (for AdvantageOne users)
                    CustomerId = customerId, // Set customer ID (for customer users)
                    CreatedAt = user.CreatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedAt = user.UpdatedAt,
                    UpdatedBy = user.UpdatedBy
                };

                return Ok(new ApiResponseDto<UserResponseDto>
                {
                    Success = true,
                    Message = "User information retrieved successfully",
                    Data = userResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponseDto<UserResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user information. Please try again."
                });
            }
        }
        private async Task<UserResponseDto> BuildUserResponseAsync(ApplicationUser user, IList<string>? roles = null)
        {
            var normalizedRoles = roles ?? await _userManager.GetRolesAsync(user);
            var normalizedRoleList = normalizedRoles
                .Select(r => r.Trim().ToLowerInvariant())
                .ToList();

            var normalizedRole = (user.Role ?? "").Trim().ToLowerInvariant();
            var isCustomerRole = normalizedRole == "customersitemanager" || normalizedRole == "customerhomanager";

            int? customerId = null;
            int[] customerAssignmentsForResponse = Array.Empty<int>();

            if (isCustomerRole)
            {
                if (user.CustomerId.HasValue && user.CustomerId.Value > 0)
                {
                    customerId = user.CustomerId.Value;
                }
                else
                {
                    var customerIdsFromUserField = user.CustomerIds;
                    if (customerIdsFromUserField.Count > 0)
                    {
                        customerId = customerIdsFromUserField[0];
                    }
                    else
                    {
                        var customerAssignmentsFromTable = await _context.UserCustomerAssignments
                            .Where(uca => uca.UserId == user.Id)
                            .Select(uca => uca.CustomerId)
                            .ToArrayAsync();

                        if (customerAssignmentsFromTable.Length > 0)
                        {
                            customerId = customerAssignmentsFromTable[0];
                        }
                    }
                }
            }
            else
            {
                var customerIdsFromUserField = user.CustomerIds;
                customerAssignmentsForResponse = customerIdsFromUserField.Count > 0
                    ? customerIdsFromUserField.ToArray()
                    : await _context.UserCustomerAssignments
                        .Where(uca => uca.UserId == user.Id)
                        .Select(uca => uca.CustomerId)
                        .ToArrayAsync();
            }

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                JobTitle = user.JobTitle ?? "",
                Signature = user.Signature ?? "",
                SignatureCode = user.SignatureCode ?? "",
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLoginAt = user.LastLoginAt,
                LoginAttempts = user.LoginAttempts,
                LockoutUntil = user.LockoutUntil,
                Role = user.Role ?? "",
                PageAccessRole = user.PageAccessRole ?? "",
                Roles = normalizedRoleList.ToArray(),
                AssignedCustomerIds = customerAssignmentsForResponse,
                CustomerId = customerId,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy ?? "",
                UpdatedAt = user.UpdatedAt,
                UpdatedBy = user.UpdatedBy ?? ""
            };
        }
    }
}
