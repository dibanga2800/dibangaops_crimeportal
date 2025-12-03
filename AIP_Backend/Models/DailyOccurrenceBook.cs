#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents an entry in the Daily Occurrence Book (DOB)
	/// </summary>
	public class DailyOccurrenceBook
	{
		[Key]
		public int Id { get; set; }

		// Core identification
		[Required]
		public int CustomerId { get; set; }

		[Required]
		[MaxLength(100)]
		public string SiteId { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? SiteName { get; set; }

		[MaxLength(200)]
		public string? StoreName { get; set; }

		[MaxLength(50)]
		public string? StoreNumber { get; set; }

		public DateTime? DateCommenced { get; set; }

		// Date and time
		[Required]
		public DateTime OccurrenceDate { get; set; }

		[Required]
		[MaxLength(10)]
		public string OccurrenceTime { get; set; } = string.Empty; // HH:MM format

		// Officer + incident coding
		[Required]
		[MaxLength(200)]
		public string OfficerName { get; set; } = string.Empty;

		[Required]
		[MaxLength(2)]
		public string OccurrenceCode { get; set; } = string.Empty; // A - M

		[MaxLength(200)]
		public string? OccurrenceCodeDescription { get; set; }

		public DateTime? CrimeReportCompletedAt { get; set; }

		[Required]
		[Column(TypeName = "nvarchar(max)")]
		public string Details { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string Signature { get; set; } = string.Empty;

		// Reported by information
		[Required]
		[MaxLength(450)]
		public string ReportedById { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? ReportedByName { get; set; }

		[MaxLength(100)]
		public string? ReportedByRole { get; set; }

		[MaxLength(50)]
		public string? ReportedByBadgeNumber { get; set; }

		// Audit fields
		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		// Navigation properties
		[ForeignKey("CustomerId")]
		public virtual Customer? Customer { get; set; }

		[ForeignKey("ReportedById")]
		public virtual ApplicationUser? ReportedByUser { get; set; }
	}
}

