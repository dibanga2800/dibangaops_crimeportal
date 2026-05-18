using AIPBackend.Exceptions;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AIPBackend.Data;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/customer-page-access")]
    [Authorize] // Allow all authenticated users - access control managed through Settings page
    public class CustomerPageAccessController : ControllerBase
    {
        private readonly ICustomerPageAccessService _customerPageAccessService;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<CustomerPageAccessController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CustomerPageAccessController(
            ICustomerPageAccessService customerPageAccessService,
            IUserContextService userContextService,
            ILogger<CustomerPageAccessController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _customerPageAccessService = customerPageAccessService;
            _userContextService = userContextService;
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("{customerId:int}")]
        public async Task<ActionResult<ApiResponseDto<CustomerPageAccessResponseDto>>> GetCustomerPageAccess(int customerId)
        {
            try
            {
                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto<CustomerPageAccessResponseDto>
                    {
                        Success = false,
                        Message = "User not authenticated."
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new ApiResponseDto<CustomerPageAccessResponseDto>
                    {
                        Success = false,
                        Message = "User not found."
                    });
                }

                // Get role from JWT claim first (most reliable), fallback to user.Role
                var jwtRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var dbRole = user.Role;
                var userRole = (jwtRole ?? dbRole ?? string.Empty).ToLowerInvariant();
                
                _logger.LogInformation("CustomerPageAccess authorization: UserId={UserId}, Role={Role}, JwtRole={JwtRole}, DbRole={DbRole}, CustomerId={CustomerId}", 
                    userId, userRole, jwtRole, dbRole, customerId);
                
                // Use case-insensitive comparison to handle any case variations
                var isAdministrator = string.Equals(userRole, "administrator", StringComparison.OrdinalIgnoreCase);
                var isOfficer = string.Equals(userRole, "store", StringComparison.OrdinalIgnoreCase) 
                    || string.Equals(userRole, "manager", StringComparison.OrdinalIgnoreCase);
                var isCustomerRole = string.Equals(userRole, "store", StringComparison.OrdinalIgnoreCase) 
                    || string.Equals(userRole, "manager", StringComparison.OrdinalIgnoreCase);
                
                _logger.LogInformation("Role check results: isAdministrator={IsAdmin}, isOfficer={IsOfficer}, isCustomerRole={IsCustomer}", 
                    isAdministrator, isOfficer, isCustomerRole);
                
                if (!isAdministrator)
                {
                    try
                    {
                        _userContextService.EnsureCanAccessCustomer(customerId);
                    }
                    catch (ForbiddenAccessException)
                    {
                        _logger.LogWarning(
                            "User {UserId} ({Role}) attempted to access customer {CustomerId} without permission",
                            userId,
                            userRole,
                            customerId);
                        return StatusCode(403, new ApiResponseDto<CustomerPageAccessResponseDto>
                        {
                            Success = false,
                            Message = "You do not have permission to access this customer's page assignments."
                        });
                    }
                }
                
                var response = await _customerPageAccessService.GetCustomerPageAccessAsync(customerId);
                return Ok(new ApiResponseDto<CustomerPageAccessResponseDto>
                {
                    Success = true,
                    Message = "Customer page access retrieved successfully",
                    Data = response
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Customer page access not found for customer {CustomerId}", customerId);
                return NotFound(new ApiResponseDto<CustomerPageAccessResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer page access for customer {CustomerId}", customerId);
                return StatusCode(500, new ApiResponseDto<CustomerPageAccessResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer page access."
                });
            }
        }

        [HttpPut("{customerId:int}")]
        public async Task<ActionResult<ApiResponseDto<CustomerPageAccessResponseDto>>> UpdateCustomerPageAccess(
            int customerId,
            [FromBody] UpdateCustomerPageAccessRequestDto request)
        {
            try
            {
                // Authorization: Only Administrators can update customer page assignments
                // Access control is managed through Settings page, but updates require admin privileges
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.ToLowerInvariant() ?? string.Empty;
                var isAdministrator = userRole == "administrator";
                
                if (!isAdministrator)
                {
                    _logger.LogWarning("Non-admin user attempted to update customer page assignments");
                    return StatusCode(403, new ApiResponseDto<CustomerPageAccessResponseDto>
                    {
                        Success = false,
                        Message = "Only administrators can update customer page assignments."
                    });
                }
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<CustomerPageAccessResponseDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                if (request.CustomerId == 0)
                {
                    request.CustomerId = customerId;
                }
                else if (request.CustomerId != customerId)
                {
                    return BadRequest(new ApiResponseDto<CustomerPageAccessResponseDto>
                    {
                        Success = false,
                        Message = "Customer ID in the URL does not match the request body."
                    });
                }

                var currentUserId = User?.Identity?.Name ?? "System";
                var response = await _customerPageAccessService.UpdateCustomerPageAccessAsync(request, currentUserId);

                return Ok(new ApiResponseDto<CustomerPageAccessResponseDto>
                {
                    Success = true,
                    Message = "Customer page access updated successfully",
                    Data = response
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Customer page access update failed for customer {CustomerId}", customerId);
                return NotFound(new ApiResponseDto<CustomerPageAccessResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer page access for customer {CustomerId}", customerId);
                return StatusCode(500, new ApiResponseDto<CustomerPageAccessResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating customer page access."
                });
            }
        }
    }
}

