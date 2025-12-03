using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PageAccessController : ControllerBase
    {
        private readonly IPageAccessService _pageAccessService;
        private readonly ILogger<PageAccessController> _logger;
        private readonly IWebHostEnvironment _environment;

        public PageAccessController(IPageAccessService pageAccessService, ILogger<PageAccessController> logger, IWebHostEnvironment environment)
        {
            _pageAccessService = pageAccessService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get page access settings (used by frontend)
        /// This endpoint is accessible without authentication to allow initial page load
        /// </summary>
        [HttpGet("settings")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDto<PageAccessSettingsDto>>> GetPageAccessSettings()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get page access settings request by user {CurrentUserId}", currentUserId);

                var settings = await _pageAccessService.GetPageAccessSettingsAsync();

                return Ok(new ApiResponseDto<PageAccessSettingsDto>
                {
                    Success = true,
                    Message = "Page access settings retrieved successfully",
                    Data = settings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page access settings");
                return StatusCode(500, new ApiResponseDto<PageAccessSettingsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving page access settings. Please try again."
                });
            }
        }

        /// <summary>
        /// Update page access settings
        /// Requires Administrator role
        /// </summary>
        [HttpPut("settings")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<PageAccessSettingsDto>>> UpdatePageAccessSettings([FromBody] UpdatePageAccessSettingsRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update page access settings request by user {CurrentUserId}", currentUserId);

                var settings = await _pageAccessService.UpdatePageAccessSettingsAsync(request, currentUserId ?? "System");

                return Ok(new ApiResponseDto<PageAccessSettingsDto>
                {
                    Success = true,
                    Message = "Page access settings updated successfully",
                    Data = settings
                });
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Invalid argument when updating page access settings: {Message}", argEx.Message);
                return BadRequest(new ApiResponseDto<PageAccessSettingsDto>
                {
                    Success = false,
                    Message = $"Invalid request: {argEx.Message}"
                });
            }
            catch (InvalidOperationException invalidOpEx)
            {
                // This is thrown when no valid pages are found
                _logger.LogError(invalidOpEx, "Invalid operation when updating page access settings: {Message}", invalidOpEx.Message);
                return BadRequest(new ApiResponseDto<PageAccessSettingsDto>
                {
                    Success = false,
                    Message = invalidOpEx.Message,
                    Errors = new List<string> { invalidOpEx.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page access settings: {ExceptionType} - {Message}", 
                    ex.GetType().Name, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                // In development, return detailed error; in production, return generic message
                var isDevelopment = _environment.IsDevelopment();
                var errorMessage = isDevelopment 
                    ? $"Error: {ex.GetType().Name} - {ex.Message}" 
                    : "An error occurred while updating page access settings. Please try again.";
                
                return StatusCode(500, new ApiResponseDto<PageAccessSettingsDto>
                {
                    Success = false,
                    Message = errorMessage,
                    Errors = isDevelopment ? new List<string> { ex.Message, ex.StackTrace ?? "" } : new List<string>()
                });
            }
        }

        /// <summary>
        /// Diagnostic endpoint to check what pages exist and can be matched
        /// </summary>
        [HttpGet("diagnostic/pages")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult> GetPagesDiagnostic([FromQuery] string? search = null)
        {
            try
            {
                var pages = await _pageAccessService.GetAllPagesAsync();
                
                var result = new
                {
                    totalPages = pages.Count,
                    activePages = pages.Count(p => p.IsActive),
                    pages = pages.Select(p => new
                    {
                        pageId = p.PageId,
                        title = p.Title,
                        isActive = p.IsActive,
                        category = p.Category
                    }).ToList()
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pages diagnostic");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all pages
        /// </summary>
        [HttpGet("pages")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<List<PageAccessDto>>>> GetAllPages()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get all pages request by user {CurrentUserId}", currentUserId);

                var pages = await _pageAccessService.GetAllPagesAsync();

                return Ok(new ApiResponseDto<List<PageAccessDto>>
                {
                    Success = true,
                    Message = "Pages retrieved successfully",
                    Data = pages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pages");
                return StatusCode(500, new ApiResponseDto<List<PageAccessDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving pages. Please try again."
                });
            }
        }

        /// <summary>
        /// Get page by ID
        /// </summary>
        [HttpGet("pages/{id}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<PageAccessDto>>> GetPageById(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get page by ID {PageId} request by user {CurrentUserId}", id, currentUserId);

                var page = await _pageAccessService.GetPageByIdAsync(id);
                if (page == null)
                {
                    return NotFound(new ApiResponseDto<PageAccessDto>
                    {
                        Success = false,
                        Message = "Page not found"
                    });
                }

                return Ok(new ApiResponseDto<PageAccessDto>
                {
                    Success = true,
                    Message = "Page retrieved successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page with ID {PageId}", id);
                return StatusCode(500, new ApiResponseDto<PageAccessDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the page. Please try again."
                });
            }
        }

        /// <summary>
        /// Get page by PageId
        /// </summary>
        [HttpGet("pages/by-pageid/{pageId}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<PageAccessDto>>> GetPageByPageId(string pageId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get page by PageId {PageId} request by user {CurrentUserId}", pageId, currentUserId);

                var page = await _pageAccessService.GetPageByPageIdAsync(pageId);
                if (page == null)
                {
                    return NotFound(new ApiResponseDto<PageAccessDto>
                    {
                        Success = false,
                        Message = "Page not found"
                    });
                }

                return Ok(new ApiResponseDto<PageAccessDto>
                {
                    Success = true,
                    Message = "Page retrieved successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page with PageId {PageId}", pageId);
                return StatusCode(500, new ApiResponseDto<PageAccessDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the page. Please try again."
                });
            }
        }

        /// <summary>
        /// Create new page
        /// </summary>
        [HttpPost("pages")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<PageAccessDto>>> CreatePage([FromBody] CreatePageAccessRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Create page request by user {CurrentUserId}", currentUserId);

                var page = await _pageAccessService.CreatePageAsync(request, currentUserId ?? "System");

                if (page == null)
                {
                    return StatusCode(500, new ApiResponseDto<PageAccessDto>
                    {
                        Success = false,
                        Message = "Failed to create page"
                    });
                }

                return Ok(new ApiResponseDto<PageAccessDto>
                {
                    Success = true,
                    Message = "Page created successfully",
                    Data = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page");
                return StatusCode(500, new ApiResponseDto<PageAccessDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the page. Please try again."
                });
            }
        }

        /// <summary>
        /// Update page
        /// </summary>
        [HttpPut("pages/{id}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<PageAccessDto>>> UpdatePage(int id, [FromBody] CreatePageAccessRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update page {PageId} request by user {CurrentUserId}", id, currentUserId);

                var page = await _pageAccessService.UpdatePageAsync(id, request, currentUserId ?? "System");

                if (page == null)
                {
                    return StatusCode(500, new ApiResponseDto<PageAccessDto>
                    {
                        Success = false,
                        Message = "Failed to update page"
                    });
                }

                return Ok(new ApiResponseDto<PageAccessDto>
                {
                    Success = true,
                    Message = "Page updated successfully",
                    Data = page
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Page not found: {PageId}", id);
                return NotFound(new ApiResponseDto<PageAccessDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page {PageId}", id);
                return StatusCode(500, new ApiResponseDto<PageAccessDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the page. Please try again."
                });
            }
        }

        /// <summary>
        /// Delete page
        /// </summary>
        [HttpDelete("pages/{id}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<object>>> DeletePage(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Delete page {PageId} request by user {CurrentUserId}", id, currentUserId);

                var result = await _pageAccessService.DeletePageAsync(id, currentUserId ?? "System");
                if (!result)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Page not found"
                    });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Page deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page {PageId}", id);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the page. Please try again."
                });
            }
        }

        /// <summary>
        /// Get role page access
        /// </summary>
        [HttpGet("roles/{roleName}/access")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<List<RolePageAccessDto>>>> GetRolePageAccess(string roleName)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get role page access for role {RoleName} by user {CurrentUserId}", roleName, currentUserId);

                var roleAccess = await _pageAccessService.GetRolePageAccessAsync(roleName);

                return Ok(new ApiResponseDto<List<RolePageAccessDto>>
                {
                    Success = true,
                    Message = "Role page access retrieved successfully",
                    Data = roleAccess
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role page access for role {RoleName}", roleName);
                return StatusCode(500, new ApiResponseDto<List<RolePageAccessDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving role page access. Please try again."
                });
            }
        }

        /// <summary>
        /// Update role page access
        /// </summary>
        [HttpPut("roles/{roleName}/access")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateRolePageAccess(string roleName, [FromBody] UpdatePageAccessRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update role page access for role {RoleName} by user {CurrentUserId}", roleName, currentUserId);

                // Ensure the role name in the request matches the URL parameter
                request.RoleName = roleName;

                var result = await _pageAccessService.UpdateRolePageAccessAsync(request, currentUserId ?? "System");

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Role page access updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role page access for role {RoleName}", roleName);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while updating role page access. Please try again."
                });
            }
        }

        /// <summary>
        /// Check user access to a specific page
        /// </summary>
        [HttpGet("check-access")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<bool>>> CheckUserAccess([FromQuery] string pagePath)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Check user access for user {CurrentUserId} to page {PagePath}", currentUserId, pagePath);

                if (string.IsNullOrEmpty(pagePath))
                {
                    return BadRequest(new ApiResponseDto<bool>
                    {
                        Success = false,
                        Message = "Page path is required"
                    });
                }

                var hasAccess = await _pageAccessService.CheckUserAccessAsync(currentUserId ?? "System", pagePath);

                return Ok(new ApiResponseDto<bool>
                {
                    Success = true,
                    Message = "Access check completed",
                    Data = hasAccess
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user access for user {CurrentUserId} to page {PagePath}", 
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value, pagePath);
                return StatusCode(500, new ApiResponseDto<bool>
                {
                    Success = false,
                    Message = "An error occurred while checking access. Please try again."
                });
            }
        }

        /// <summary>
        /// Sync pages from frontend definitions
        /// </summary>
        [HttpPost("sync-pages")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<SyncResultDto>>> SyncPagesFromDefinitions([FromBody] SyncPagesRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _logger.LogInformation("Sync pages request by user {CurrentUserId} for {PageCount} pages", currentUserId, request.Pages.Count);

                if (request.Pages == null || !request.Pages.Any())
                {
                    return BadRequest(new ApiResponseDto<SyncResultDto>
                    {
                        Success = false,
                        Message = "Pages list is required and cannot be empty"
                    });
                }

                var result = await _pageAccessService.SyncPagesFromDefinitionsAsync(request, currentUserId);

                return Ok(new ApiResponseDto<SyncResultDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing pages from definitions");
                return StatusCode(500, new ApiResponseDto<SyncResultDto>
                {
                    Success = false,
                    Message = "An error occurred while syncing pages. Please try again."
                });
            }
        }

        /// <summary>
        /// Get page access statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<PageAccessStatisticsDto>>> GetPageAccessStatistics()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get page access statistics request by user {CurrentUserId}", currentUserId);

                var statistics = await _pageAccessService.GetPageAccessStatisticsAsync();

                return Ok(new ApiResponseDto<PageAccessStatisticsDto>
                {
                    Success = true,
                    Message = "Page access statistics retrieved successfully",
                    Data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page access statistics");
                return StatusCode(500, new ApiResponseDto<PageAccessStatisticsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving page access statistics. Please try again."
                });
            }
        }

        /// <summary>
        /// Initialize default page access settings
        /// </summary>
        [HttpPost("initialize")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<object>>> InitializeDefaultPageAccess()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("=== MANUAL INITIALIZATION REQUEST by user {CurrentUserId} ===", currentUserId);

                var result = await _pageAccessService.InitializeDefaultPageAccessAsync(currentUserId ?? "System");

                _logger.LogInformation("=== INITIALIZATION RESULT: {Result} ===", result);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Default page access settings initialized successfully. Result: {result}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR during manual initialization ===");
                _logger.LogError("Exception: {Exception}", ex.ToString());
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while initializing default page access settings. Please try again."
                });
            }
        }
    }
}
