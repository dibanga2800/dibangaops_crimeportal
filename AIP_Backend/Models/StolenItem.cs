#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a stolen item associated with an incident
	/// </summary>
	public class StolenItem
	{
		[Key]
		public int StolenItemId { get; set; }

		[Required]
		public int IncidentId { get; set; }

		[MaxLength(100)]
		public string? Category { get; set; }

		[MaxLength(500)]
		public string? Description { get; set; }

		[MaxLength(200)]
		public string? ProductName { get; set; }

		[Required]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Cost { get; set; }

		[Required]
		public int Quantity { get; set; }

		[Required]
		[Column(TypeName = "decimal(18,2)")]
		public decimal TotalAmount { get; set; }

		[MaxLength(100)]
		public string? Barcode { get; set; }

		// Audit Fields
		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		// Navigation Property
		[ForeignKey("IncidentId")]
		public virtual Incident Incident { get; set; } = null!;
	}
}

