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
        private readonly ILoginProtectionService _loginProtectionService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            ApplicationDbContext context,
            IEmailService emailService,
            ILoginProtectionService loginProtectionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
            _loginProtectionService = loginProtectionService;
        }

        /// <summary>
        /// Complete two-factor login using email code.
        /// </summary>
        [HttpPost("2fa/complete")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> CompleteTwoFactorLogin([FromBody] CompleteTwoFactorLoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("2FA completion attempt for email {Email}", request.Email);

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

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("2FA completion failed: user not found for email {Email}", request.Email);
                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Invalid code or email"
                    });
                }

                if (!user.TwoFactorEnabled)
                {
                    _logger.LogWarning("2FA completion failed: 2FA not enabled for user {UserId}", user.Id);
                    return BadRequest(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Two-factor authentication is not enabled for this account."
                    });
                }

                if (string.IsNullOrEmpty(user.PendingTwoFactorCode) || !user.PendingTwoFactorExpiryUtc.HasValue)
                {
                    _logger.LogWarning("2FA completion failed: no pending code for user {UserId}", user.Id);
                    return BadRequest(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "No active verification code. Please log in again."
                    });
                }

                if (DateTime.UtcNow > user.PendingTwoFactorExpiryUtc.Value || !string.Equals(user.PendingTwoFactorCode, request.Code))
                {
                    _logger.LogWarning("2FA completion failed: invalid or expired code for user {UserId}", user.Id);
                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Invalid or expired verification code."
                    });
                }

                // Clear pending code
                user.PendingTwoFactorCode = null;
                user.PendingTwoFactorExpiryUtc = null;

                var roles = (await _userManager.GetRolesAsync(user))
                    .Select(r => r.Trim().ToLowerInvariant())
                    .ToList();

                var accessToken = _jwtService.GenerateAccessToken(user, roles);
                var refreshToken = _jwtService.GenerateRefreshToken();
                await _userManager.UpdateAsync(user);

                var userResponse = await BuildUserResponseAsync(user, roles);

                var response = new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                    User = userResponse,
                    RequiresTwoFactor = false,
                    TwoFactorMethods = Array.Empty<string>()
                };

                return Ok(new ApiResponseDto<LoginResponseDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing two-factor login for email {Email}", request.Email);
                return StatusCode(500, new ApiResponseDto<LoginResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while completing two-factor authentication. Please try again."
                });
            }
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

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

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
                        Message = "Account is deactivated. Please contact admin."
                    });
                }

                // Check centralised login protection (user + IP level) before verifying password
                var protectionResult = await _loginProtectionService.CheckPreLoginAsync(
                    user,
                    request.Email.Trim().ToLowerInvariant(),
                    clientIp);

                if (protectionResult.IsBlocked)
                {
                    if (protectionResult.IsUserLockedOut)
                    {
                        _logger.LogWarning(
                            "Login blocked for locked-out user {UserId}. LockoutUntilUtc={LockoutUntilUtc}",
                            user.Id,
                            protectionResult.LockoutUntilUtc);

                        // Use a generic message to avoid leaking enumeration data
                        return Unauthorized(new ApiResponseDto<LoginResponseDto>
                        {
                            Success = false,
                            Message = protectionResult.LockoutUntilUtc.HasValue
                                ? $"Account is temporarily locked. Please try again after {protectionResult.LockoutUntilUtc.Value:yyyy-MM-dd HH:mm:ss} UTC"
                                : "Too many failed attempts. Please try again later."
                        });
                    }

                    if (protectionResult.IsIpThrottled)
                    {
                        _logger.LogWarning(
                            "Login blocked due to IP throttling. IP={IpAddress}, Email={Email}",
                            clientIp,
                            request.Email);

                        // Use 429 (Too Many Requests) semantics for IP-level throttling
                        return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponseDto<LoginResponseDto>
                        {
                            Success = false,
                            Message = "Too many login attempts from your network. Please wait a few minutes and try again."
                        });
                    }
                }

                // Verify password
                _logger.LogInformation("Verifying password for user {Email} with password length: {PasswordLength}", 
                    request.Email, request.Password?.Length ?? 0);

                var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password ?? "", lockoutOnFailure: false);
                if (!passwordResult.Succeeded)
                {
                    _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);

                    await _loginProtectionService.RegisterLoginFailureAsync(
                        user,
                        request.Email.Trim().ToLowerInvariant(),
                        clientIp);

                    // Small, uniform delay to make automated guessing less efficient without harming UX
                    await Task.Delay(250);

                    return Unauthorized(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Successful login → reset counters in protection service
                await _loginProtectionService.RegisterLoginSuccessAsync(
                    user,
                    request.Email.Trim().ToLowerInvariant(),
                    clientIp);

                // Get user roles - normalize to lowercase for consistency
                var roles = (await _userManager.GetRolesAsync(user))
                    .Select(r => r.Trim().ToLowerInvariant())
                    .ToList();

                // Update user login information
                user.LastLoginAt = DateTime.UtcNow;
                user.LoginAttempts = 0;
                user.LockoutUntil = null;

                // If 2FA is enabled, start email-based 2FA flow instead of issuing tokens
                if (user.TwoFactorEnabled)
                {
                    var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
                    user.PendingTwoFactorCode = code;
                    user.PendingTwoFactorExpiryUtc = DateTime.UtcNow.AddMinutes(10);
                    await _userManager.UpdateAsync(user);

                    if (user.EmailNotificationsEnabled && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        try
                        {
                            var subject = "Your Crime Portal login code";
                            var body = $@"<html><body>
<h2>Login Verification Code</h2>
<p>Hello {user.FirstName} {user.LastName},</p>
<p>Use the following code to complete your login:</p>
<h1 style=""font-size: 32px; letter-spacing: 4px;"">{code}</h1>
<p>This code expires in 10 minutes.</p>
<p>If you did not try to sign in, you can ignore this email.</p>
</body></html>";

                            await _emailService.SendEmailAsync(user.Email!, subject, body, isHtml: true, fromName: "Crime Portal");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Error sending 2FA email for user {UserId}", user.Id);
                        }
                    }

                    var userResponseForStep1 = await BuildUserResponseAsync(user, roles);

                    var twoFactorState = new LoginResponseDto
                    {
                        Success = true,
                        Message = "Two-factor authentication required",
                        RequiresTwoFactor = true,
                        TwoFactorMethods = new[] { "email" },
                        User = userResponseForStep1
                    };

                    _logger.LogInformation("2FA required for user {UserId}", user.Id);

                    return Ok(new ApiResponseDto<LoginResponseDto>
                    {
                        Success = true,
                        Message = "Two-factor authentication required",
                        Data = twoFactorState
                    });
                }

                // 2FA not enabled → generate tokens as before
                var accessToken = _jwtService.GenerateAccessToken(user, roles);
                var refreshToken = _jwtService.GenerateRefreshToken();
                await _userManager.UpdateAsync(user);

                var isCustomerUser = user.CustomerId.HasValue && user.CustomerId.Value > 0;
                
                // For customer users: Use direct CustomerId field (foreign key to Customer table)
                // For AdvantageOne users: Use AssignedCustomerIds (JSON array for multiple customers)
                int? customerId = null;
                int[] customerAssignmentsForResponse = Array.Empty<int>();
                
                if (isCustomerUser)
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
                    User = userResponse,
                    RequiresTwoFactor = false,
                    TwoFactorMethods = Array.Empty<string>()
                };

                _logger.LogInformation("Login successful for user {UserId}", user.Id);

                // Fire-and-forget login alert email (do not block login if it fails)
                if (user.EmailNotificationsEnabled && user.LoginAlertsEnabled && !string.IsNullOrWhiteSpace(user.Email))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
                            var subject = "New Login to Crime Portal";
                            var body = $@"<html><body>
<h2>New Login Detected</h2>
<p>Hello {user.FirstName} {user.LastName},</p>
<p>Your account ({user.Email}) was just used to sign in to Crime Portal.</p>
<ul>
  <li><strong>Time (UTC):</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</li>
  <li><strong>IP Address:</strong> {ip}</li>
  <li><strong>Username:</strong> {user.UserName}</li>
  <li><strong>Email:</strong> {user.Email}</li>
</ul>
<p>If you recognise this activity, no further action is required.</p>
<p>If you do <strong>not</strong> recognise this login, please change your password immediately and contact an administrator.</p>
</body></html>";

                            var sent = await _emailService.SendEmailAsync(user.Email!, subject, body, isHtml: true, fromName: "Crime Portal");
                            if (!sent)
                            {
                                _logger.LogWarning("Login alert email failed to send for user {UserId}", user.Id);
                            }
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Error sending login alert email for user {UserId}", user.Id);
                        }
                    });
                }

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
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<RefreshTokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                _logger.LogInformation("Refresh token request received");

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

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

                // Validate refresh token (basic format)
                if (!_jwtService.ValidateRefreshToken(request.RefreshToken))
                {
                    _logger.LogWarning("Invalid refresh token provided");

                    // Feed IP-based protection even when we cannot resolve a user
                    await _loginProtectionService.RegisterLoginFailureAsync(
                        null,
                        "refresh-token",
                        clientIp);

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

                    await _loginProtectionService.RegisterLoginFailureAsync(
                        null,
                        "refresh-token",
                        clientIp);

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

                    await _loginProtectionService.RegisterLoginFailureAsync(
                        null,
                        "refresh-token",
                        clientIp);

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

                    await _loginProtectionService.RegisterLoginFailureAsync(
                        null,
                        "refresh-token",
                        clientIp);

                    return Unauthorized(new ApiResponseDto<RefreshTokenResponseDto>
                    {
                        Success = false,
                        Message = "User not found or inactive"
                    });
                }

                // Apply the same lockout / IP throttling rules used for login
                var normalizedIdentifier = (user.Email ?? user.UserName ?? user.Id).Trim().ToLowerInvariant();
                var protectionResult = await _loginProtectionService.CheckPreLoginAsync(
                    user,
                    normalizedIdentifier,
                    clientIp);

                if (protectionResult.IsBlocked)
                {
                    if (protectionResult.IsUserLockedOut)
                    {
                        _logger.LogWarning(
                            "Refresh token blocked for locked-out user {UserId}. LockoutUntilUtc={LockoutUntilUtc}",
                            user.Id,
                            protectionResult.LockoutUntilUtc);

                        return Unauthorized(new ApiResponseDto<RefreshTokenResponseDto>
                        {
                            Success = false,
                            Message = protectionResult.LockoutUntilUtc.HasValue
                                ? $"Account is temporarily locked. Please try again after {protectionResult.LockoutUntilUtc.Value:yyyy-MM-dd HH:mm:ss} UTC"
                                : "Too many failed attempts. Please try again later."
                        });
                    }

                    if (protectionResult.IsIpThrottled)
                    {
                        _logger.LogWarning(
                            "Refresh token blocked due to IP throttling. IP={IpAddress}, UserId={UserId}",
                            clientIp,
                            user.Id);

                        return StatusCode(StatusCodes.Status429TooManyRequests, new ApiResponseDto<RefreshTokenResponseDto>
                        {
                            Success = false,
                            Message = "Too many token requests from your network. Please wait a few minutes and try again."
                        });
                    }
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                await _loginProtectionService.RegisterLoginSuccessAsync(
                    user,
                    normalizedIdentifier,
                    clientIp);

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
        /// Update the current user's own profile. Any authenticated user can update their own profile.
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<UserResponseDto>>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto<UserResponseDto>
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

                if (!string.IsNullOrWhiteSpace(request.FirstName))
                    user.FirstName = request.FirstName.Trim();
                if (!string.IsNullOrWhiteSpace(request.LastName))
                    user.LastName = request.LastName.Trim();
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    var existing = await _userManager.FindByEmailAsync(request.Email);
                    if (existing != null && existing.Id != userId)
                    {
                        return BadRequest(new ApiResponseDto<UserResponseDto>
                        {
                            Success = false,
                            Message = "Email is already in use by another account"
                        });
                    }
                    user.Email = request.Email;
                }
                if (request.PhoneNumber != null)
                    user.PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
                if (request.JobTitle != null)
                    user.JobTitle = string.IsNullOrEmpty(request.JobTitle) ? null : request.JobTitle.Trim();

                if (request.ClearProfilePicture == true)
                    user.ProfilePicture = null;
                else if (request.ProfilePicture != null)
                    user.ProfilePicture = request.ProfilePicture;

                if (request.TwoFactorEnabled.HasValue)
                    user.TwoFactorEnabled = request.TwoFactorEnabled.Value;
                if (request.EmailNotificationsEnabled.HasValue)
                    user.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
                if (request.LoginAlertsEnabled.HasValue)
                    user.LoginAlertsEnabled = request.LoginAlertsEnabled.Value;

                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = userId;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponseDto<UserResponseDto>
                    {
                        Success = false,
                        Message = "Failed to update profile",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userResponse = await BuildUserResponseAsync(user, roles);

                return Ok(new ApiResponseDto<UserResponseDto>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = userResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                var message = "An error occurred while updating your profile. Please try again.";
#if DEBUG
                message += $" ({ex.Message})";
#endif
                return StatusCode(500, new ApiResponseDto<UserResponseDto>
                {
                    Success = false,
                    Message = message
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

                var isCustomerUser = user.CustomerId.HasValue && user.CustomerId.Value > 0;
                
                // For customer users: Use direct CustomerId field (foreign key to Customer table)
                // For AdvantageOne users: Use AssignedCustomerIds (JSON array for multiple customers)
                int? customerId = null;
                int[] customerAssignmentsForResponse = Array.Empty<int>();
                
                if (isCustomerUser)
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
                    // Store / site assignments
                    PrimarySiteId = user.PrimarySiteId,
                    AssignedSiteIds = user.SiteIds.ToArray(),
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

            var isCustomerUser = user.CustomerId.HasValue && user.CustomerId.Value > 0;

            int? customerId = null;
            int[] customerAssignmentsForResponse = Array.Empty<int>();

            if (isCustomerUser)
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
                ProfilePicture = user.ProfilePicture,
                Signature = user.Signature ?? "",
                SignatureCode = user.SignatureCode ?? "",
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                EmailNotificationsEnabled = user.EmailNotificationsEnabled,
                LoginAlertsEnabled = user.LoginAlertsEnabled,
                LastLoginAt = user.LastLoginAt,
                LoginAttempts = user.LoginAttempts,
                LockoutUntil = user.LockoutUntil,
                Role = user.Role ?? "",
                PageAccessRole = user.PageAccessRole ?? "",
                Roles = normalizedRoleList.ToArray(),
                AssignedCustomerIds = customerAssignmentsForResponse,
                CustomerId = customerId,
                // Store / site assignments
                PrimarySiteId = user.PrimarySiteId,
                AssignedSiteIds = user.SiteIds.ToArray(),
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy ?? "",
                UpdatedAt = user.UpdatedAt,
                UpdatedBy = user.UpdatedBy ?? ""
            };
        }
    }
}
