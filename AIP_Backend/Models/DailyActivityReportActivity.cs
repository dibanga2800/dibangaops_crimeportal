#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a daily activity entry in a daily activity report
	/// </summary>
	public class DailyActivityReportActivity
	{
		[Key]
		public int DailyActivityReportActivityId { get; set; }

		[Required]
		public int DailyActivityReportId { get; set; }

		[Required]
		[MaxLength(50)]
		public string ActivityId { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Time { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string Activity { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string Location { get; set; } = string.Empty;

		[MaxLength(1000)]
		public string? Description { get; set; }

		[Required]
		[MaxLength(50)]
		public string Status { get; set; } = string.Empty; // 'completed', 'in-progress', 'delayed', 'cancelled'

		// Navigation property
		[ForeignKey("DailyActivityReportId")]
		public virtual DailyActivityReport? DailyActivityReport { get; set; }
	}
}

