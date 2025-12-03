#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a visitor entry in a daily activity report
	/// </summary>
	public class DailyActivityReportVisitorEntry
	{
		[Key]
		public int DailyActivityReportVisitorEntryId { get; set; }

		[Required]
		public int DailyActivityReportId { get; set; }

		[Required]
		[MaxLength(50)]
		public string VisitorEntryId { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Time { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string VisitorName { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? Company { get; set; }

		[Required]
		[MaxLength(500)]
		public string Purpose { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? EscortedBy { get; set; }

		[MaxLength(50)]
		public string? ExitTime { get; set; }

		[MaxLength(50)]
		public string? BadgeNumber { get; set; }

		// Navigation property
		[ForeignKey("DailyActivityReportId")]
		public virtual DailyActivityReport? DailyActivityReport { get; set; }
	}
}

