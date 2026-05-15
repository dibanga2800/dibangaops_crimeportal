#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private const int MaxProductPageSize = 200;

        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get product by EAN/barcode
        /// </summary>
        [HttpGet("ean/{ean}")]
        public async Task<ActionResult<ApiResponseDto<ProductLookupResponseDto>>> GetProductByEAN(string ean)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get product by EAN request for EAN: {EAN} by user {CurrentUserId}", ean, currentUserId);

                var product = await _productService.GetProductByEANAsync(ean);

                if (product == null)
                {
                    return NotFound(new ApiResponseDto<ProductLookupResponseDto>
                    {
                        Success = false,
                        Message = $"Product with EAN '{ean}' not found"
                    });
                }

                var response = new ProductLookupResponseDto
                {
                    ProductId = product.ProductId,
                    EAN = product.EAN,
                    ProductName = product.ProductName,
                    Department = product.Department,
                    Description = product.Description,
                    Price = product.Price
                };

                return Ok(new ApiResponseDto<ProductLookupResponseDto>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by EAN: {EAN}", ean);
                return StatusCode(500, new ApiResponseDto<ProductLookupResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the product"
                });
            }
        }

        /// <summary>
        /// Distinct department values from the product catalog (for incident stolen-item dropdown).
        /// </summary>
        [HttpGet("departments")]
        public async Task<ActionResult<ApiResponseDto<List<string>>>> GetProductDepartments()
        {
            try
            {
                var departments = await _productService.GetDistinctDepartmentsAsync();
                return Ok(new ApiResponseDto<List<string>>
                {
                    Success = true,
                    Message = "Product departments retrieved successfully",
                    Data = departments.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product departments");
                return StatusCode(500, new ApiResponseDto<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving product departments"
                });
            }
        }

        /// <summary>
        /// Get all products with optional filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<ProductListResponseDto>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get products request by user {CurrentUserId}", currentUserId);

                pageSize = Math.Clamp(pageSize, 1, MaxProductPageSize);
                var result = await _productService.GetProductsAsync(page, pageSize, search);

                return Ok(new ApiResponseDto<ProductListResponseDto>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiResponseDto<ProductListResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving products"
                });
            }
        }

        /// <summary>
        /// Update retail price for a product (administrator only).
        /// </summary>
        [HttpPatch("{id}/price")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<ProductDto>>> UpdateProductPrice(
            int id,
            [FromBody] UpdateProductPriceRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _logger.LogInformation("Update product price for ID {ProductId} by user {CurrentUserId}", id, currentUserId);

                ProductDto? product;
                try
                {
                    product = await _productService.UpdateProductPriceAsync(id, request.Price, currentUserId);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return BadRequest(new ApiResponseDto<ProductDto>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }

                if (product == null)
                {
                    return NotFound(new ApiResponseDto<ProductDto>
                    {
                        Success = false,
                        Message = $"Product with ID '{id}' not found"
                    });
                }

                return Ok(new ApiResponseDto<ProductDto>
                {
                    Success = true,
                    Message = "Product price updated successfully",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product price for ID: {ProductId}", id);
                return StatusCode(500, new ApiResponseDto<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the product price"
                });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDto<ProductDto>>> GetProductById(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get product by ID request for ID: {ProductId} by user {CurrentUserId}", id, currentUserId);

                var product = await _productService.GetProductByIdAsync(id);

                if (product == null)
                {
                    return NotFound(new ApiResponseDto<ProductDto>
                    {
                        Success = false,
                        Message = $"Product with ID '{id}' not found"
                    });
                }

                return Ok(new ApiResponseDto<ProductDto>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID: {ProductId}", id);
                return StatusCode(500, new ApiResponseDto<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the product"
                });
            }
        }
    }
}

