#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using AIPBackend.Models;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SiteController : ControllerBase
    {
        private readonly ILogger<SiteController> _logger;
        private readonly ISiteService _siteService;

        public SiteController(ILogger<SiteController> logger, ISiteService siteService)
        {
            _logger = logger;
            _siteService = siteService;
        }

        /// <summary>
        /// Get all sites with optional filtering
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<List<SiteDto>>>> GetSites(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? customerId = null,
            [FromQuery] int? regionId = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get sites request by user {CurrentUserId}", currentUserId);

                var result = await _siteService.GetSitesAsync(page, pageSize, search, customerId, regionId);

                return Ok(new ApiResponseDto<List<SiteDto>>
                {
                    Success = true,
                    Message = "Sites retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites");
                return StatusCode(500, new ApiResponseDto<List<SiteDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sites. Please try again."
                });
            }
        }

        /// <summary>
        /// Get a specific site by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<SiteDto>>> GetSite(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get site request for ID {SiteId} by user {CurrentUserId}", id, currentUserId);

                var site = await _siteService.GetSiteByIdAsync(id);
                if (site == null)
                {
                    return NotFound(new ApiResponseDto<SiteDto>
                    {
                        Success = false,
                        Message = "Site not found"
                    });
                }

                return Ok(new ApiResponseDto<SiteDto>
                {
                    Success = true,
                    Message = "Site retrieved successfully",
                    Data = site
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site {SiteId}", id);
                return StatusCode(500, new ApiResponseDto<SiteDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the site. Please try again."
                });
            }
        }

        /// <summary>
        /// Get sites by customer ID
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<List<SiteDto>>>> GetSitesByCustomer(int customerId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get sites by customer request for customer {CustomerId} by user {CurrentUserId}", customerId, currentUserId);

                var sites = await _siteService.GetSitesByCustomerAsync(customerId);

                return Ok(new ApiResponseDto<List<SiteDto>>
                {
                    Success = true,
                    Message = "Sites retrieved successfully",
                    Data = sites
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites for customer {CustomerId}", customerId);
                return StatusCode(500, new ApiResponseDto<List<SiteDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sites. Please try again."
                });
            }
        }

        /// <summary>
        /// Get sites by region ID
        /// </summary>
        [HttpGet("region/{regionId}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<List<SiteDto>>>> GetSitesByRegion(int regionId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get sites by region request for region {RegionId} by user {CurrentUserId}", regionId, currentUserId);

                var sites = await _siteService.GetSitesByRegionAsync(regionId);

                return Ok(new ApiResponseDto<List<SiteDto>>
                {
                    Success = true,
                    Message = "Sites retrieved successfully",
                    Data = sites
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites for region {RegionId}", regionId);
                return StatusCode(500, new ApiResponseDto<List<SiteDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sites. Please try again."
                });
            }
        }

        /// <summary>
        /// Create a new site
        /// </summary>
        [HttpPost]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<SiteDto>>> CreateSite([FromBody] SiteCreateRequestDto createSiteDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Create site request by user {CurrentUserId}", currentUserId);

                var result = await _siteService.CreateSiteAsync(createSiteDto, currentUserId ?? "system");

                return CreatedAtAction(nameof(GetSite), new { id = result.SiteID }, new ApiResponseDto<SiteDto>
                {
                    Success = true,
                    Message = "Site created successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site");
                return StatusCode(500, new ApiResponseDto<SiteDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the site. Please try again."
                });
            }
        }

        /// <summary>
        /// Update an existing site
        /// </summary>
        [HttpPut("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<SiteDto>>> UpdateSite(int id, [FromBody] SiteUpdateRequestDto updateSiteDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update site request for ID {SiteId} by user {CurrentUserId}", id, currentUserId);

                var result = await _siteService.UpdateSiteAsync(id, updateSiteDto, currentUserId ?? "system");
                if (result == null)
                {
                    return NotFound(new ApiResponseDto<SiteDto>
                    {
                        Success = false,
                        Message = "Site not found"
                    });
                }

                return Ok(new ApiResponseDto<SiteDto>
                {
                    Success = true,
                    Message = "Site updated successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site {SiteId}", id);
                return StatusCode(500, new ApiResponseDto<SiteDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the site. Please try again."
                });
            }
        }

        /// <summary>
        /// Delete a site
        /// </summary>
        [HttpDelete("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<object>>> DeleteSite(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Delete site request for ID {SiteId} by user {CurrentUserId}", id, currentUserId);

                var success = await _siteService.DeleteSiteAsync(id);
                if (!success)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Site not found"
                    });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Site deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site {SiteId}", id);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the site. Please try again."
                });
            }
        }
    }
}
