#nullable enable

namespace AIPBackend.Models.DTOs
{
	public class ProductDto
	{
		public int ProductId { get; set; }
		/// <summary>Barcode (stored as EAN in the database).</summary>
		public string EAN { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public string? Department { get; set; }
		public string? Description { get; set; }
		public decimal? Price { get; set; }
		public bool IsActive { get; set; }
	}

	public class ProductLookupResponseDto
	{
		public int ProductId { get; set; }
		public string EAN { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		/// <summary>Catalog department (CSV Department column).</summary>
		public string? Department { get; set; }
		/// <summary>VME code (stored as Description in Products).</summary>
		public string? Description { get; set; }
		/// <summary>Retail price.</summary>
		public decimal? Price { get; set; }
	}

	public class ProductListResponseDto
	{
		public List<ProductDto> Items { get; set; } = new();
		public int TotalCount { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
	}

	public class UpdateProductPriceRequestDto
	{
		public decimal? Price { get; set; }
	}

	public class ProductCreateRequestDto
	{
		public string EAN { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public string? Department { get; set; }
		public string? Description { get; set; }
		public decimal? Price { get; set; }
	}
}
