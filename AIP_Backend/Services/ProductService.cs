#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductDto?> GetProductByEANAsync(string ean)
        {
            var product = await _context.Products
                .Where(p => p.EAN == ean && p.IsActive)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    EAN = p.EAN,
                    ProductName = p.ProductName,
                    Section = p.Section,
                    Category = p.Category,
                    Department = p.Department,
                    Description = p.Description,
                    Price = p.Price,
                    Brand = p.Brand,
                    Manufacturer = p.Manufacturer,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

            return product;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == productId && p.IsActive)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    EAN = p.EAN,
                    ProductName = p.ProductName,
                    Section = p.Section,
                    Category = p.Category,
                    Department = p.Department,
                    Description = p.Description,
                    Price = p.Price,
                    Brand = p.Brand,
                    Manufacturer = p.Manufacturer,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

            return product;
        }

        public async Task<List<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 10, string? search = null, string? category = null)
        {
            var query = _context.Products.Where(p => p.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    p.ProductName.Contains(search) || 
                    p.EAN.Contains(search) ||
                    (p.Section != null && p.Section.Contains(search)) ||
                    (p.Description != null && p.Description.Contains(search)));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var products = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    EAN = p.EAN,
                    ProductName = p.ProductName,
                    Section = p.Section,
                    Category = p.Category,
                    Department = p.Department,
                    Description = p.Description,
                    Price = p.Price,
                    Brand = p.Brand,
                    Manufacturer = p.Manufacturer,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return products;
        }

        public async Task<ProductDto> CreateProductAsync(ProductCreateRequestDto productDto, string createdBy)
        {
            var product = new Product
            {
                EAN = productDto.EAN,
                ProductName = productDto.ProductName,
                Section = productDto.Section,
                Category = productDto.Category,
                Department = productDto.Department,
                Description = productDto.Description,
                Price = productDto.Price,
                Brand = productDto.Brand,
                Manufacturer = productDto.Manufacturer,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return new ProductDto
            {
                ProductId = product.ProductId,
                EAN = product.EAN,
                ProductName = product.ProductName,
                Section = product.Section,
                Category = product.Category,
                Department = product.Department,
                Description = product.Description,
                Price = product.Price,
                Brand = product.Brand,
                Manufacturer = product.Manufacturer,
                IsActive = product.IsActive
            };
        }

        public async Task<ProductDto?> UpdateProductAsync(int productId, ProductCreateRequestDto productDto, string updatedBy)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                return null;
            }

            product.EAN = productDto.EAN;
            product.ProductName = productDto.ProductName;
            product.Section = productDto.Section;
            product.Category = productDto.Category;
            product.Department = productDto.Department;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.Brand = productDto.Brand;
            product.Manufacturer = productDto.Manufacturer;
            product.UpdatedBy = updatedBy;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ProductDto
            {
                ProductId = product.ProductId,
                EAN = product.EAN,
                ProductName = product.ProductName,
                Section = product.Section,
                Category = product.Category,
                Department = product.Department,
                Description = product.Description,
                Price = product.Price,
                Brand = product.Brand,
                Manufacturer = product.Manufacturer,
                IsActive = product.IsActive
            };
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return false;
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

