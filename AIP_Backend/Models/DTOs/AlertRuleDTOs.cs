#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// DTO for alert rule display
	/// </summary>
	public class AlertRuleDto
	{
		public int AlertRuleId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string RuleType { get; set; } = string.Empty;
		public List<string> Keywords { get; set; } = new();
		public List<string> IncidentTypes { get; set; } = new();
		public string? LpmRegion { get; set; }
		public int? RegionId { get; set; }
		public string TriggerCondition { get; set; } = "any";
		public List<string> Channels { get; set; } = new();
		public List<string> EmailRecipients { get; set; } = new();
		public bool IsActive { get; set; }
		public int? CustomerId { get; set; }
		public int? SiteId { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }
		public DateTime? LastTriggered { get; set; }
		public int TriggerCount { get; set; }
	}

	/// <summary>
	/// DTO for creating a new alert rule
	/// </summary>
	public class CreateAlertRuleDto
	{
		[Required]
		[MaxLength(200)]
		public string Name { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string RuleType { get; set; } = string.Empty; // 'Store' or 'LPM'

		public List<string> Keywords { get; set; } = new();
		public List<string> IncidentTypes { get; set; } = new();
		public string? LpmRegion { get; set; }
		public int? RegionId { get; set; }
		
		[Required]
		[MaxLength(50)]
		public string TriggerCondition { get; set; } = "any";
		
		public List<string> Channels { get; set; } = new();
		public List<string> EmailRecipients { get; set; } = new();
		public bool IsActive { get; set; } = true;
		public int? CustomerId { get; set; }
		public int? SiteId { get; set; }
	}

	/// <summary>
	/// DTO for updating an existing alert rule
	/// </summary>
	public class UpdateAlertRuleDto
	{
		[MaxLength(200)]
		public string? Name { get; set; }
		
		public List<string>? Keywords { get; set; }
		public List<string>? IncidentTypes { get; set; }
		public string? LpmRegion { get; set; }
		public int? RegionId { get; set; }
		
		[MaxLength(50)]
		public string? TriggerCondition { get; set; }
		
		public List<string>? Channels { get; set; }
		public List<string>? EmailRecipients { get; set; }
		public bool? IsActive { get; set; }
		public int? CustomerId { get; set; }
		public int? SiteId { get; set; }
	}

	/// <summary>
	/// DTO for paginated alert rules response
	/// </summary>
	public class AlertRuleListResponseDto
	{
		public List<AlertRuleDto> Data { get; set; } = new();
		public int Total { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
	}

	/// <summary>
	/// DTO for alert rule trigger event
	/// </summary>
	public class AlertTriggerDto
	{
		public int AlertRuleId { get; set; }
		public string RuleName { get; set; } = string.Empty;
		public int IncidentId { get; set; }
		public string IncidentType { get; set; } = string.Empty;
		public string SiteName { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public DateTime TriggeredAt { get; set; }
		public List<string> MatchedKeywords { get; set; } = new();
		public List<string> NotificationChannels { get; set; } = new();
		public List<string> EmailRecipients { get; set; } = new();
	}
}
