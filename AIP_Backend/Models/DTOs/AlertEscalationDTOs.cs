#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	public class AlertInstanceDto
	{
		public int AlertInstanceId { get; set; }
		public int AlertRuleId { get; set; }
		public string? AlertRuleName { get; set; }
		public int? IncidentId { get; set; }
		public string Severity { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public string? Message { get; set; }
		public string? MatchDetails { get; set; }
		public string CreatedAt { get; set; } = string.Empty;
		public string? AcknowledgedAt { get; set; }
		public string? AcknowledgedBy { get; set; }
		public string? EscalatedAt { get; set; }
		public string? EscalatedTo { get; set; }
		public string? ResolvedAt { get; set; }
		public string? ResolvedBy { get; set; }
		public string? ResolutionNotes { get; set; }
		public int EscalationLevel { get; set; }
	}

	public class AcknowledgeAlertDto
	{
		[MaxLength(500)]
		public string? Notes { get; set; }
	}

	public class EscalateAlertDto
	{
		[Required]
		[MaxLength(450)]
		public string EscalateTo { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Notes { get; set; }
	}

	public class ResolveAlertDto
	{
		[MaxLength(1000)]
		public string? ResolutionNotes { get; set; }
	}

	public class AlertInstanceListResponseDto
	{
		public bool Success { get; set; } = true;
		public List<AlertInstanceDto> Data { get; set; } = new();
		public PaginationInfoDto Pagination { get; set; } = new();
	}

	public class AlertSummaryDto
	{
		public int TotalActive { get; set; }
		public int NewCount { get; set; }
		public int AcknowledgedCount { get; set; }
		public int EscalatedCount { get; set; }
		public int ResolvedTodayCount { get; set; }
		public List<AlertInstanceDto> RecentAlerts { get; set; } = new();
	}
}
