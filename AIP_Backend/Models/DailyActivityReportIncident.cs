#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents an activity incident in a daily activity report
	/// </summary>
	public class DailyActivityReportIncident
	{
		[Key]
		public int DailyActivityReportIncidentId { get; set; }

		[Required]
		public int DailyActivityReportId { get; set; }

		[Required]
		[MaxLength(50)]
		public string IncidentId { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string Time { get; set; } = string.Empty;

		[Required]
		[MaxLength(100)]
		public string Type { get; set; } = string.Empty; // 'security', 'safety', 'maintenance', 'other'

		[Required]
		[MaxLength(50)]
		public string Severity { get; set; } = string.Empty; // 'low', 'medium', 'high', 'critical'

		[MaxLength(2000)]
		public string? Description { get; set; }

		[MaxLength(2000)]
		public string? ActionTaken { get; set; }

		[Required]
		public bool Resolved { get; set; }

		// Navigation property
		[ForeignKey("DailyActivityReportId")]
		public virtual DailyActivityReport? DailyActivityReport { get; set; }
	}
}

