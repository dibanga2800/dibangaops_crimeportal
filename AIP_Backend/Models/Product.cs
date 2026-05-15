#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Product catalog item. <see cref="EAN"/> is the barcode (same value; UI/CSV use "barcode").
	/// </summary>
	public class Product
	{
		[Key]
		public int ProductId { get; set; }

		/// <summary>Barcode (EAN/GTIN). Unique business key.</summary>
		[Required]
		[MaxLength(50)]
		public string EAN { get; set; } = string.Empty;

		[Required]
		[MaxLength(500)]
		public string ProductName { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Department { get; set; }

		/// <summary>Typically VME code from catalog CSV import.</summary>
		[MaxLength(500)]
		public string? Description { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal? Price { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		public bool IsActive { get; set; } = true;
	}
}
