#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents an alert rule for monitoring incidents and sending notifications
	/// </summary>
	public class AlertRule
	{
		[Key]
		public int AlertRuleId { get; set; }

		/// <summary>
		/// Name of the alert rule
		/// </summary>
		[Required]
		[MaxLength(200)]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Type of alert rule: 'Store' or 'LPM'
		/// </summary>
		[Required]
		[MaxLength(50)]
		public string RuleType { get; set; } = string.Empty;

		/// <summary>
		/// Keywords to match in incident descriptions (stored as JSON array)
		/// </summary>
		[MaxLength(2000)]
		public string? Keywords { get; set; }

	/// <summary>
	/// Incident types to monitor (stored as JSON array)
	/// </summary>
	[MaxLength(1000)]
	public string? IncidentTypes { get; set; }

	/// <summary>
	/// Region name for both Store and LPM rules
	/// </summary>
	[MaxLength(200)]
	public string? LpmRegion { get; set; }

		/// <summary>
		/// Region ID for LPM rules
		/// </summary>
		public int? RegionId { get; set; }

		/// <summary>
		/// Trigger condition: 'any', 'all', or 'exact-match'
		/// </summary>
		[Required]
		[MaxLength(50)]
		public string TriggerCondition { get; set; } = "any";

		/// <summary>
		/// Alert channels: 'in-app', 'email', etc. (stored as JSON array)
		/// </summary>
		[MaxLength(500)]
		public string? Channels { get; set; }

		/// <summary>
		/// Email recipients for notifications (stored as JSON array)
		/// </summary>
		[MaxLength(2000)]
		public string? EmailRecipients { get; set; }

		/// <summary>
		/// Whether the alert rule is active
		/// </summary>
		public bool IsActive { get; set; } = true;

		/// <summary>
		/// Customer ID this rule belongs to
		/// </summary>
		public int? CustomerId { get; set; }

		/// <summary>
		/// Store/Site ID for Store rules
		/// </summary>
		public int? SiteId { get; set; }

		// Audit fields
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string? CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		/// <summary>
		/// Soft delete flag
		/// </summary>
		public bool IsDeleted { get; set; } = false;

		/// <summary>
		/// Last time this rule was triggered
		/// </summary>
		public DateTime? LastTriggered { get; set; }

		/// <summary>
		/// Number of times this rule has been triggered
		/// </summary>
		public int TriggerCount { get; set; } = 0;

		// Navigation properties
		[ForeignKey("CustomerId")]
		public Customer? Customer { get; set; }

		[ForeignKey("SiteId")]
		public Site? Site { get; set; }

		[ForeignKey("RegionId")]
		public Region? Region { get; set; }
	}
}
