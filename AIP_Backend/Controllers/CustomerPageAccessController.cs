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
        private readonly ILogger<CustomerPageAccessController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CustomerPageAccessController(
            ICustomerPageAccessService customerPageAccessService,
            ILogger<CustomerPageAccessController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _customerPageAccessService = customerPageAccessService;
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
                    bool hasAccess = false;
                    
                    if (isOfficer)
                    {
                        // Officers can access any of their assigned customers
                        var assignedCustomerIds = user.CustomerIds;
                        var assignedCustomerIdsClaim = User.FindFirst("AssignedCustomerIds")?.Value;
                        
                        _logger.LogInformation("Officer access check: AssignedCustomerIds from DB={DbIds}, from JWT={JwtIds}, checking CustomerId={CustomerId}", 
                            string.Join(",", assignedCustomerIds), assignedCustomerIdsClaim, customerId);
                        
                        hasAccess = assignedCustomerIds.Contains(customerId);
                        
                        if (!hasAccess && !string.IsNullOrEmpty(assignedCustomerIdsClaim))
                        {
                            // Try parsing from JWT claim
                            try
                            {
                                var jwtCustomerIds = assignedCustomerIdsClaim.Split(',')
                                    .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : -1)
                                    .Where(id => id > 0)
                                    .ToList();
                                hasAccess = jwtCustomerIds.Contains(customerId);
                                _logger.LogInformation("JWT claim check: ParsedIds={ParsedIds}, HasAccess={HasAccess}", 
                                    string.Join(",", jwtCustomerIds), hasAccess);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse AssignedCustomerIds from JWT claim");
                            }
                        }
                        
                        if (!hasAccess)
                        {
                            // Fallback: Check UserCustomerAssignments table
                            var assignments = await _context.UserCustomerAssignments
                                .Where(uca => uca.UserId == user.Id)
                                .Select(uca => uca.CustomerId)
                                .ToListAsync();
                            hasAccess = assignments.Contains(customerId);
                            _logger.LogInformation("UserCustomerAssignments check: Assignments={Assignments}, HasAccess={HasAccess}", 
                                string.Join(",", assignments), hasAccess);
                        }
                    }
                    else if (isCustomerRole)
                    {
                        // Customer users can only access their own customer
                        if (user.CustomerId.HasValue)
                        {
                            hasAccess = user.CustomerId.Value == customerId;
                        }
                        
                        if (!hasAccess)
                        {
                            // Fallback: Check AssignedCustomerIds
                            var customerIds = user.CustomerIds;
                            hasAccess = customerIds.Count == 1 && customerIds.Contains(customerId);
                        }
                    }
                    
                    if (!hasAccess)
                    {
                        _logger.LogWarning("User {UserId} ({Role}) attempted to access customer {CustomerId} without permission", 
                            userId, userRole, customerId);
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

