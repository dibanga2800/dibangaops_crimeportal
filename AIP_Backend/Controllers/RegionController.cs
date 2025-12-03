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
    public class RegionController : ControllerBase
    {
        private readonly ILogger<RegionController> _logger;
        private readonly IRegionService _regionService;

        public RegionController(ILogger<RegionController> logger, IRegionService regionService)
        {
            _logger = logger;
            _regionService = regionService;
        }

        /// <summary>
        /// Get all regions with optional filtering
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<List<RegionDto>>>> GetRegions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? customerId = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get regions request by user {CurrentUserId}", currentUserId);

                var result = await _regionService.GetRegionsAsync(page, pageSize, search, customerId);

                return Ok(new ApiResponseDto<List<RegionDto>>
                {
                    Success = true,
                    Message = "Regions retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regions");
                return StatusCode(500, new ApiResponseDto<List<RegionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving regions. Please try again."
                });
            }
        }

        /// <summary>
        /// Get a specific region by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<RegionDto>>> GetRegion(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get region request for ID {RegionId} by user {CurrentUserId}", id, currentUserId);

                var region = await _regionService.GetRegionByIdAsync(id);
                if (region == null)
                {
                    return NotFound(new ApiResponseDto<RegionDto>
                    {
                        Success = false,
                        Message = "Region not found"
                    });
                }

                return Ok(new ApiResponseDto<RegionDto>
                {
                    Success = true,
                    Message = "Region retrieved successfully",
                    Data = region
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving region {RegionId}", id);
                return StatusCode(500, new ApiResponseDto<RegionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the region. Please try again."
                });
            }
        }

        /// <summary>
        /// Get regions by customer ID
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<List<RegionDto>>>> GetRegionsByCustomer(int customerId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get regions by customer request for customer {CustomerId} by user {CurrentUserId}", customerId, currentUserId);

                var regions = await _regionService.GetRegionsByCustomerAsync(customerId);

                return Ok(new ApiResponseDto<List<RegionDto>>
                {
                    Success = true,
                    Message = "Regions retrieved successfully",
                    Data = regions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regions for customer {CustomerId}", customerId);
                return StatusCode(500, new ApiResponseDto<List<RegionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving regions. Please try again."
                });
            }
        }

        /// <summary>
        /// Create a new region
        /// </summary>
        [HttpPost]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<RegionDto>>> CreateRegion([FromBody] RegionCreateRequestDto createRegionDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Create region request by user {CurrentUserId}", currentUserId);

                var result = await _regionService.CreateRegionAsync(createRegionDto, currentUserId ?? "system");

                return CreatedAtAction(nameof(GetRegion), new { id = result.RegionID }, new ApiResponseDto<RegionDto>
                {
                    Success = true,
                    Message = "Region created successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region");
                return StatusCode(500, new ApiResponseDto<RegionDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the region. Please try again."
                });
            }
        }

        /// <summary>
        /// Update an existing region
        /// </summary>
        [HttpPut("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<RegionDto>>> UpdateRegion(int id, [FromBody] RegionUpdateRequestDto updateRegionDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update region request for ID {RegionId} by user {CurrentUserId}", id, currentUserId);

                var result = await _regionService.UpdateRegionAsync(id, updateRegionDto, currentUserId ?? "system");
                if (result == null)
                {
                    return NotFound(new ApiResponseDto<RegionDto>
                    {
                        Success = false,
                        Message = "Region not found"
                    });
                }

                return Ok(new ApiResponseDto<RegionDto>
                {
                    Success = true,
                    Message = "Region updated successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating region {RegionId}", id);
                return StatusCode(500, new ApiResponseDto<RegionDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the region. Please try again."
                });
            }
        }

        /// <summary>
        /// Delete a region
        /// </summary>
        [HttpDelete("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<object>>> DeleteRegion(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Delete region request for ID {RegionId} by user {CurrentUserId}", id, currentUserId);

                var success = await _regionService.DeleteRegionAsync(id);
                if (!success)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Region not found"
                    });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Region deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting region {RegionId}", id);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the region. Please try again."
                });
            }
        }
    }
}
