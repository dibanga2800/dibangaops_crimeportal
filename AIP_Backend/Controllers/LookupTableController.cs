using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LookupTableController : ControllerBase
    {
        private readonly ILookupTableService _lookupTableService;
        private readonly ILogger<LookupTableController> _logger;

        public LookupTableController(ILookupTableService lookupTableService, ILogger<LookupTableController> logger)
        {
            _lookupTableService = lookupTableService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<LookupTableListResponseDto>>>> GetAll()
        {
            try
            {
                var lookupTables = await _lookupTableService.GetAllAsync();
                return Ok(new ApiResponseDto<IEnumerable<LookupTableListResponseDto>>
                {
                    Success = true,
                    Message = "Lookup tables retrieved successfully",
                    Data = lookupTables
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all lookup tables");
                return StatusCode(500, new ApiResponseDto<IEnumerable<LookupTableListResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving lookup tables",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<LookupTableListResponseDto>>>> GetByCategory(string category)
        {
            try
            {
                var lookupTables = await _lookupTableService.GetByCategoryAsync(category);
                return Ok(new ApiResponseDto<IEnumerable<LookupTableListResponseDto>>
                {
                    Success = true,
                    Message = $"Lookup tables for category '{category}' retrieved successfully",
                    Data = lookupTables
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lookup tables for category: {Category}", category);
                return StatusCode(500, new ApiResponseDto<IEnumerable<LookupTableListResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving lookup tables",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponseDto<IEnumerable<string>>>> GetCategories()
        {
            try
            {
                var categories = await _lookupTableService.GetCategoriesAsync();
                return Ok(new ApiResponseDto<IEnumerable<string>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new ApiResponseDto<IEnumerable<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving categories",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("{lookupId}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<LookupTableResponseDto>>> GetById(int lookupId)
        {
            try
            {
                var lookupTable = await _lookupTableService.GetByIdAsync(lookupId);
                if (lookupTable == null)
                {
                    return NotFound(new ApiResponseDto<LookupTableResponseDto>
                    {
                        Success = false,
                        Message = "Lookup table not found"
                    });
                }

                return Ok(new ApiResponseDto<LookupTableResponseDto>
                {
                    Success = true,
                    Message = "Lookup table retrieved successfully",
                    Data = lookupTable
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lookup table with ID: {LookupId}", lookupId);
                return StatusCode(500, new ApiResponseDto<LookupTableResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the lookup table",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<LookupTableResponseDto>>> Create(LookupTableCreateRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ApiResponseDto<LookupTableResponseDto>
                    {
                        Success = false,
                        Message = "User ID not found in token"
                    });
                }

                var created = await _lookupTableService.CreateAsync(request, userId);
                return CreatedAtAction(nameof(GetById), new { lookupId = created.LookupId }, new ApiResponseDto<LookupTableResponseDto>
                {
                    Success = true,
                    Message = "Lookup table created successfully",
                    Data = created
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lookup table");
                return StatusCode(500, new ApiResponseDto<LookupTableResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the lookup table",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("{lookupId}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<LookupTableResponseDto>>> Update(int lookupId, LookupTableUpdateRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ApiResponseDto<LookupTableResponseDto>
                    {
                        Success = false,
                        Message = "User ID not found in token"
                    });
                }

                var updated = await _lookupTableService.UpdateAsync(lookupId, request, userId);
                if (updated == null)
                {
                    return NotFound(new ApiResponseDto<LookupTableResponseDto>
                    {
                        Success = false,
                        Message = "Lookup table not found"
                    });
                }

                return Ok(new ApiResponseDto<LookupTableResponseDto>
                {
                    Success = true,
                    Message = "Lookup table updated successfully",
                    Data = updated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lookup table with ID: {LookupId}", lookupId);
                return StatusCode(500, new ApiResponseDto<LookupTableResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the lookup table",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("{lookupId}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<bool>>> Delete(int lookupId)
        {
            try
            {
                var deleted = await _lookupTableService.DeleteAsync(lookupId);
                if (!deleted)
                {
                    return NotFound(new ApiResponseDto<bool>
                    {
                        Success = false,
                        Message = "Lookup table not found"
                    });
                }

                return Ok(new ApiResponseDto<bool>
                {
                    Success = true,
                    Message = "Lookup table deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lookup table with ID: {LookupId}", lookupId);
                return StatusCode(500, new ApiResponseDto<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the lookup table",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("test")]
        public async Task<ActionResult<ApiResponseDto<object>>> TestData()
        {
            try
            {
                var categories = await _lookupTableService.GetCategoriesAsync();
                var counties = await _lookupTableService.GetByCategoryAsync("Counties");
                var regions = await _lookupTableService.GetByCategoryAsync("Regions");
                var positions = await _lookupTableService.GetByCategoryAsync("Positions");
                
                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Test data retrieved successfully",
                    Data = new
                    {
                        TotalCategories = categories.Count(),
                        Categories = categories.ToList(),
                        CountiesCount = counties.Count(),
                        RegionsCount = regions.Count(),
                        PositionsCount = positions.Count(),
                        SampleCounties = counties.Take(3).Select(c => new { c.Value, c.Description }).ToList(),
                        SampleRegions = regions.Take(3).Select(c => new { c.Value, c.Description }).ToList(),
                        SamplePositions = positions.Take(3).Select(c => new { c.Value, c.Description }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test endpoint");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while testing data",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
