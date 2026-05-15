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

		private static ProductDto MapToDto(Product p) => new()
		{
			ProductId = p.ProductId,
			EAN = p.EAN,
			ProductName = p.ProductName,
			Department = p.Department,
			Description = p.Description,
			Price = p.Price,
			IsActive = p.IsActive,
		};

		public async Task<ProductDto?> GetProductByEANAsync(string ean)
		{
			var product = await _context.Products
				.Where(p => p.EAN == ean && p.IsActive)
				.FirstOrDefaultAsync();

			return product == null ? null : MapToDto(product);
		}

		public async Task<ProductDto?> GetProductByIdAsync(int productId)
		{
			var product = await _context.Products
				.Where(p => p.ProductId == productId && p.IsActive)
				.FirstOrDefaultAsync();

			return product == null ? null : MapToDto(product);
		}

		public async Task<ProductListResponseDto> GetProductsAsync(int page = 1, int pageSize = 10, string? search = null)
		{
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 200);

			var query = _context.Products.Where(p => p.IsActive).AsQueryable();

			if (!string.IsNullOrEmpty(search))
			{
				query = query.Where(p =>
					p.ProductName.Contains(search) ||
					p.EAN.Contains(search) ||
					(p.Description != null && p.Description.Contains(search)) ||
					(p.Department != null && p.Department.Contains(search)));
			}

			var totalCount = await query.CountAsync();

			var products = await query
				.OrderBy(p => p.ProductName)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(p => new ProductDto
				{
					ProductId = p.ProductId,
					EAN = p.EAN,
					ProductName = p.ProductName,
					Department = p.Department,
					Description = p.Description,
					Price = p.Price,
					IsActive = p.IsActive,
				})
				.ToListAsync();

			return new ProductListResponseDto
			{
				Items = products,
				TotalCount = totalCount,
				Page = page,
				PageSize = pageSize,
			};
		}

		public async Task<IReadOnlyList<string>> GetDistinctDepartmentsAsync()
		{
			var raw = await _context.Products
				.AsNoTracking()
				.Where(p => p.IsActive && p.Department != null && p.Department != "")
				.Select(p => p.Department!)
				.ToListAsync();

			return raw
				.Select(d => d.Trim())
				.Where(d => d.Length > 0)
				.GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.First())
				.OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public async Task<ProductDto> CreateProductAsync(ProductCreateRequestDto productDto, string createdBy)
		{
			var product = new Product
			{
				EAN = productDto.EAN,
				ProductName = productDto.ProductName,
				Department = productDto.Department,
				Description = productDto.Description,
				Price = productDto.Price,
				CreatedBy = createdBy,
				CreatedAt = DateTime.UtcNow,
				IsActive = true,
			};

			_context.Products.Add(product);
			await _context.SaveChangesAsync();

			return MapToDto(product);
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
			product.Department = productDto.Department;
			product.Description = productDto.Description;
			product.Price = productDto.Price;
			product.UpdatedBy = updatedBy;
			product.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			return MapToDto(product);
		}

		public async Task<ProductDto?> UpdateProductPriceAsync(int productId, decimal? price, string updatedBy)
		{
			if (price.HasValue && price.Value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
			}

			var product = await _context.Products.FindAsync(productId);
			if (product == null || !product.IsActive)
			{
				return null;
			}

			product.Price = price;
			product.UpdatedBy = updatedBy;
			product.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			return MapToDto(product);
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
