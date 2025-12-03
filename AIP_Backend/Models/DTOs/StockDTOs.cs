#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class StockItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int MinimumStock { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int NumberAdded { get; set; }
        public DateTime Date { get; set; }
        public int NumberIssued { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
    }

    public class StockCreateRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int MinimumStock { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int NumberAdded { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int NumberIssued { get; set; }

        [Required]
        [MaxLength(450)]
        public string IssuedBy { get; set; } = string.Empty;
    }

    public class StockUpdateRequestDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public int? MinimumStock { get; set; }
        [MaxLength(100)]
        public string? Category { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public int? NumberAdded { get; set; }
        public DateTime? Date { get; set; }
        public int? NumberIssued { get; set; }
        [MaxLength(450)]
        public string? IssuedBy { get; set; }
    }

    public class StockIssueRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity to issue must be greater than 0")]
        public int QuantityToIssue { get; set; }

        [Required]
        [MaxLength(450)]
        public string IssuedBy { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Reason { get; set; }
    }

    public class StockAddRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity to add must be greater than 0")]
        public int QuantityToAdd { get; set; }

        [Required]
        [MaxLength(450)]
        public string AddedBy { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Reason { get; set; }
    }
}


