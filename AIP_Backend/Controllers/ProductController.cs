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
        [AllowAnonymous] // Temporary for development - remove in production
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
                    Category = product.Category,
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
                    Message = "An error occurred while retrieving the product",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all products with optional filtering and pagination
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<List<ProductDto>>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get products request by user {CurrentUserId}", currentUserId);

                var products = await _productService.GetProductsAsync(page, pageSize, search, category);

                return Ok(new ApiResponseDto<List<ProductDto>>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiResponseDto<List<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving products",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
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
                    Message = "An error occurred while retrieving the product",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

