#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	public class AlertInstance
	{
		[Key]
		public int AlertInstanceId { get; set; }

		[Required]
		public int AlertRuleId { get; set; }

		public int? IncidentId { get; set; }

		[Required]
		[MaxLength(50)]
		public string Severity { get; set; } = "medium";

		[Required]
		[MaxLength(50)]
		public string Status { get; set; } = "new";

		[MaxLength(2000)]
		public string? Message { get; set; }

		[MaxLength(500)]
		public string? MatchDetails { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime? AcknowledgedAt { get; set; }

		[MaxLength(450)]
		public string? AcknowledgedBy { get; set; }

		public DateTime? EscalatedAt { get; set; }

		[MaxLength(450)]
		public string? EscalatedTo { get; set; }

		public DateTime? ResolvedAt { get; set; }

		[MaxLength(450)]
		public string? ResolvedBy { get; set; }

		[MaxLength(1000)]
		public string? ResolutionNotes { get; set; }

		public int EscalationLevel { get; set; } = 0;

		[ForeignKey("AlertRuleId")]
		public virtual AlertRule AlertRule { get; set; } = null!;

		[ForeignKey("IncidentId")]
		public virtual Incident? Incident { get; set; }
	}
}
