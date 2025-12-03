#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a security check in a daily activity report
	/// </summary>
	public class DailyActivityReportSecurityCheck
	{
		[Key]
		public int DailyActivityReportSecurityCheckId { get; set; }

		[Required]
		public int DailyActivityReportId { get; set; }

		[Required]
		[MaxLength(50)]
		public string SecurityCheckId { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Time { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string Area { get; set; } = string.Empty;

		[Required]
		[MaxLength(100)]
		public string CheckType { get; set; } = string.Empty; // 'perimeter', 'building', 'equipment', 'access-control', 'fire-safety'

		[Required]
		[MaxLength(50)]
		public string Status { get; set; } = string.Empty; // 'all-clear', 'issue-found', 'requires-attention'

		[MaxLength(1000)]
		public string? Notes { get; set; }

		// Navigation property
		[ForeignKey("DailyActivityReportId")]
		public virtual DailyActivityReport? DailyActivityReport { get; set; }
	}
}

