#nullable enable

namespace AIPBackend.Models.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string EAN { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductLookupResponseDto
    {
        public int ProductId { get; set; }
        public string EAN { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
    }

    public class ProductCreateRequestDto
    {
        public string EAN { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
    }
}

