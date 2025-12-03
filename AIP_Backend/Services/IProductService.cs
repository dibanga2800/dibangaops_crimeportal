#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface IProductService
    {
        Task<ProductDto?> GetProductByEANAsync(string ean);
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<List<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 10, string? search = null, string? category = null);
        Task<ProductDto> CreateProductAsync(ProductCreateRequestDto productDto, string createdBy);
        Task<ProductDto?> UpdateProductAsync(int productId, ProductCreateRequestDto productDto, string updatedBy);
        Task<bool> DeleteProductAsync(int productId);
    }
}

