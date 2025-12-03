#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<List<StockItemDto>>>> List()
        {
            var items = await _stockService.GetAllAsync();
            return Ok(new ApiResponseDto<List<StockItemDto>>
            {
                Success = true,
                Message = "Stock items retrieved",
                Data = items
            });
        }

        [HttpGet("{id}")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<StockItemDto>>> Get(int id)
        {
            var item = await _stockService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new ApiResponseDto<StockItemDto>
                {
                    Success = false,
                    Message = "Stock item not found"
                });
            }
            return Ok(new ApiResponseDto<StockItemDto>
            {
                Success = true,
                Message = "Stock item retrieved",
                Data = item
            });
        }

        [HttpPost]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<StockItemDto>>> Create([FromBody] StockCreateRequestDto dto)
        {
            var created = await _stockService.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new ApiResponseDto<StockItemDto>
            {
                Success = true,
                Message = "Stock item created",
                Data = created
            });
        }

        [HttpPut("{id}")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<StockItemDto>>> Update(int id, [FromBody] StockUpdateRequestDto dto)
        {
            var updated = await _stockService.UpdateAsync(id, dto);
            if (updated == null)
            {
                return NotFound(new ApiResponseDto<StockItemDto>
                {
                    Success = false,
                    Message = "Stock item not found"
                });
            }
            return Ok(new ApiResponseDto<StockItemDto>
            {
                Success = true,
                Message = "Stock item updated",
                Data = updated
            });
        }

        [HttpDelete("{id}")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<object>>> Delete(int id)
        {
            var ok = await _stockService.DeleteAsync(id);
            if (!ok)
            {
                return NotFound(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Stock item not found"
                });
            }
            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Stock item deleted"
            });
        }

        [HttpPost("check-low-stock")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<object>>> CheckLowStock()
        {
            try
            {
                await _stockService.CheckAndNotifyLowStockAsync();
                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Low stock check completed and notifications sent"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking low stock");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Error checking low stock"
                });
            }
        }

        [HttpPost("{id}/issue")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<StockItemDto>>> IssueStock(int id, [FromBody] StockIssueRequestDto dto)
        {
            try
            {
                var issued = await _stockService.IssueStockAsync(id, dto);
                if (issued == null)
                {
                    return BadRequest(new ApiResponseDto<StockItemDto>
                    {
                        Success = false,
                        Message = "Failed to issue stock. Item not found or insufficient stock available."
                    });
                }

                return Ok(new ApiResponseDto<StockItemDto>
                {
                    Success = true,
                    Message = $"Successfully issued {dto.QuantityToIssue} units of stock",
                    Data = issued
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error issuing stock for item {Id}", id);
                return StatusCode(500, new ApiResponseDto<StockItemDto>
                {
                    Success = false,
                    Message = "Error issuing stock"
                });
            }
        }

        [HttpPost("{id}/add")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<StockItemDto>>> AddStock(int id, [FromBody] StockAddRequestDto dto)
        {
            try
            {
                var updated = await _stockService.AddStockAsync(id, dto);
                if (updated == null)
                {
                    return BadRequest(new ApiResponseDto<StockItemDto>
                    {
                        Success = false,
                        Message = "Failed to add stock. Item not found."
                    });
                }

                return Ok(new ApiResponseDto<StockItemDto>
                {
                    Success = true,
                    Message = $"Successfully added {dto.QuantityToAdd} units of stock",
                    Data = updated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stock for item {Id}", id);
                return StatusCode(500, new ApiResponseDto<StockItemDto>
                {
                    Success = false,
                    Message = "Error adding stock"
                });
            }
        }

        [HttpGet("low-stock")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<List<StockItemDto>>>> GetLowStockItems()
        {
            try
            {
                var lowStockItems = await _stockService.GetLowStockItemsAsync();
                return Ok(new ApiResponseDto<List<StockItemDto>>
                {
                    Success = true,
                    Message = $"Found {lowStockItems.Count} items with low stock levels",
                    Data = lowStockItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock items");
                return StatusCode(500, new ApiResponseDto<List<StockItemDto>>
                {
                    Success = false,
                    Message = "Error retrieving low stock items"
                });
            }
        }

        [HttpPost("test-email")]
        [AllowAnonymous] // remove in production
        public async Task<ActionResult<ApiResponseDto<object>>> TestEmail()
        {
            try
            {
                // Test low stock notification
                await _stockService.CheckAndNotifyLowStockAsync();
                
                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Test email notifications sent. Check logs for details."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing email notifications");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Error testing email notifications"
                });
            }
        }
    }
}


