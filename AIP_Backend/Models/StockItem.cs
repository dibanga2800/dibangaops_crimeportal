#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models
{
    public class StockItem
    {
        [Key]
        public int Id { get; set; }

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

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "In Stock";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int NumberAdded { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        public int NumberIssued { get; set; }

        [Required]
        [MaxLength(450)]
        public string IssuedBy { get; set; } = string.Empty;

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? DateModified { get; set; }
    }
}


