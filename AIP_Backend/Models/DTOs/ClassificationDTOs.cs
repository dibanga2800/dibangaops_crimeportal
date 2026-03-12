#nullable enable

namespace AIPBackend.Models.DTOs
{
	public class IncidentClassificationRequestDto
	{
		public int IncidentId { get; set; }
		public string IncidentType { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string? IncidentDetails { get; set; }
		public decimal? TotalValueRecovered { get; set; }
		public bool PoliceInvolvement { get; set; }
		public string? OffenderName { get; set; }
		public List<string>? IncidentInvolved { get; set; }
		public int StolenItemCount { get; set; }
	}

	public class IncidentClassificationResultDto
	{
		public string SuggestedCategory { get; set; } = string.Empty;
		public string RiskLevel { get; set; } = "low";
		public double RiskScore { get; set; }
		public double Confidence { get; set; }
		public List<string> SuggestedActions { get; set; } = new();
		public List<string> Tags { get; set; } = new();
		public string ClassifierVersion { get; set; } = "rule-based-v1";
	}

	public class IncidentAnalyticsSummaryDto
	{
		public int TotalIncidents { get; set; }
		public decimal TotalValueAtRisk { get; set; }
		public int RepeatOffenderCount { get; set; }
		public List<HotLocationDto> HotLocations { get; set; } = new();
		public List<TrendDataPointDto> IncidentTrend { get; set; } = new();
		public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
		public List<RiskIndicatorDto> RiskIndicators { get; set; } = new();
		public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
	}

	public class HotLocationDto
	{
		public string SiteName { get; set; } = string.Empty;
		public string? RegionName { get; set; }
		public int IncidentCount { get; set; }
		public decimal TotalValue { get; set; }
		public double RiskScore { get; set; }
	}

	public class TrendDataPointDto
	{
		public string Period { get; set; } = string.Empty;
		public int Count { get; set; }
		public decimal Value { get; set; }
	}

	public class CategoryBreakdownDto
	{
		public string Category { get; set; } = string.Empty;
		public int Count { get; set; }
		public double Percentage { get; set; }
		public decimal TotalValue { get; set; }
	}

	public class RiskIndicatorDto
	{
		public string Indicator { get; set; } = string.Empty;
		public string Level { get; set; } = "low";
		public double Score { get; set; }
		public string? Description { get; set; }
	}
}
