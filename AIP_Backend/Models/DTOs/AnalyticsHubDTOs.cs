#nullable enable

using System.Text.Json.Serialization;

namespace AIPBackend.Models.DTOs
{
	// ============================================================================
	// Shared
	// ============================================================================

	public class DateRangeDto
	{
		public string Start { get; set; } = string.Empty;
		public string End { get; set; } = string.Empty;
	}

	public class AnalyticsMetadataDto
	{
		public string GeneratedAt { get; set; } = string.Empty;
		public DateRangeDto DateRange { get; set; } = new();
		public int? CustomerId { get; set; }
	}

	// ============================================================================
	// Top-level Hub DTO
	// ============================================================================

	public class AnalyticsHubDto
	{
		public CrimeTrendDataDto CrimeTrends { get; set; } = new();
		public HotProductsDataDto HotProducts { get; set; } = new();
		public AnalyticsFinancialSummaryDto FinancialSummary { get; set; } = new();
		public List<StoreRecoveryComparisonDto> StoreRecoveryComparisons { get; set; } = new();
		public RepeatOffenderDataDto RepeatOffenders { get; set; } = new();
		public DeploymentRecommendationDto DeploymentRecommendations { get; set; } = new();
		public CrimeLinkingDataDto CrimeLinking { get; set; } = new();
		public AnalyticsMetadataDto Metadata { get; set; } = new();
	}

	public class AnalyticsFinancialSummaryDto
	{
		public decimal TotalStolenValue { get; set; }
		public decimal TotalRecoveredValue { get; set; }
		public decimal TotalLostValue { get; set; }
		public double RecoveryRate { get; set; }
		public int TotalRecoveredQuantity { get; set; }
		public int TotalLostQuantity { get; set; }
	}

	public class StoreRecoveryComparisonDto
	{
		public int StoreId { get; set; }
		public string StoreName { get; set; } = string.Empty;
		public int IncidentCount { get; set; }
		public decimal TotalStolenValue { get; set; }
		public decimal TotalRecoveredValue { get; set; }
		public decimal TotalLostValue { get; set; }
		public double RecoveryRate { get; set; }
		public int TotalRecoveredQuantity { get; set; }
		public int TotalLostQuantity { get; set; }
	}

	public class RecoveryTrendPointDto
	{
		public string Period { get; set; } = string.Empty;
		public int IncidentCount { get; set; }
		public decimal StolenValue { get; set; }
		public decimal RecoveredValue { get; set; }
		public decimal LostValue { get; set; }
	}

	// ============================================================================
	// Crime Trends
	// ============================================================================

	public class CrimeTrendDataDto
	{
		public List<DayOfWeekDataDto> DayOfWeek { get; set; } = new();
		public List<TimeOfDayDataDto> TimeOfDay { get; set; } = new();
		public List<IncidentTypeDataDto> IncidentTypes { get; set; } = new();
		public Dictionary<string, StoreDrilldownDataDto> StoreDrilldown { get; set; } = new();
		public List<RecoveryTrendPointDto> RecoveryTrend { get; set; } = new();
		public int TotalIncidents { get; set; }
		public DateRangeDto DateRange { get; set; } = new();
	}

	public class DayOfWeekDataDto
	{
		public string Day { get; set; } = string.Empty;
		public int Incidents { get; set; }
		public int Stores { get; set; }
		public double Percentage { get; set; }
	}

	public class TimeOfDayDataDto
	{
		public int Hour { get; set; }
		public string Label { get; set; } = string.Empty;
		public int Incidents { get; set; }
		public double Percentage { get; set; }
	}

	public class IncidentTypeDataDto
	{
		public string Type { get; set; } = string.Empty;
		public int Count { get; set; }
		public double Percentage { get; set; }
		public decimal TotalValue { get; set; }
	}

	public class StoreDrilldownDataDto
	{
		public int StoreId { get; set; }
		public string StoreName { get; set; } = string.Empty;
		public int Incidents { get; set; }
		public List<IncidentTypeDataDto> IncidentTypes { get; set; } = new();
		public decimal TotalStolenValue { get; set; }
		public decimal TotalRecoveredValue { get; set; }
		public decimal TotalLostValue { get; set; }
		public double RecoveryRate { get; set; }
		public string PeakDay { get; set; } = string.Empty;
		public int PeakHour { get; set; }
	}

	// ============================================================================
	// Hot Products
	// ============================================================================

	public class HotProductsDataDto
	{
		public List<ProductFrequencyDataDto> TopProducts { get; set; } = new();
		public List<ProductFrequencyDataDto> TopRecoveredProducts { get; set; } = new();
		public List<ProductFrequencyDataDto> WorstRecoveryProducts { get; set; } = new();
		public List<StoreProductHeatmapDataDto> StoreHeatmap { get; set; } = new();
		public decimal TotalValueStolen { get; set; }
		public decimal TotalValueRecovered { get; set; }
		public decimal TotalValueLost { get; set; }
		public double RecoveryRate { get; set; }
		public DateRangeDto Period { get; set; } = new();
	}

	public class ProductFrequencyDataDto
	{
		public string Barcode { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public int Frequency { get; set; }
		public decimal TotalValue { get; set; }
		public decimal StolenValue { get; set; }
		public decimal RecoveredValue { get; set; }
		public decimal LostValue { get; set; }
		public double RecoveryRate { get; set; }
		public int StoresAffected { get; set; }
	}

	public class StoreProductHeatmapDataDto
	{
		public int StoreId { get; set; }
		public string StoreName { get; set; } = string.Empty;
		public List<StoreProductItemDto> Products { get; set; } = new();
		public int TotalIncidents { get; set; }
		public decimal TotalValueStolen { get; set; }
		public decimal TotalValueRecovered { get; set; }
		public decimal TotalValueLost { get; set; }
		public double RecoveryRate { get; set; }
		public string RiskLevel { get; set; } = "low";
	}

	public class StoreProductItemDto
	{
		public string Barcode { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public int Frequency { get; set; }
		public decimal Value { get; set; }
		public decimal StolenValue { get; set; }
		public decimal RecoveredValue { get; set; }
		public decimal LostValue { get; set; }
		public double RecoveryRate { get; set; }
	}

	// ============================================================================
	// Repeat Offenders
	// ============================================================================

	public class RepeatOffenderDataDto
	{
		public List<OffenderProfileDto> MostActive { get; set; } = new();
		public List<CrossStoreMovementDto> CrossStoreMovements { get; set; } = new();
		public OffenderNetworkDataDto NetworkMap { get; set; } = new();
		public int TotalOffenders { get; set; }
	}

	public class OffenderProfileDto
	{
		public string OffenderId { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public int IncidentCount { get; set; }
		public string FirstIncident { get; set; } = string.Empty;
		public string LastIncident { get; set; } = string.Empty;
		public List<string> StoresTargeted { get; set; } = new();
		public decimal TotalValue { get; set; }
		public string RiskLevel { get; set; } = "low";
		public List<string> ModusOperandi { get; set; } = new();
	}

	public class CrossStoreMovementDto
	{
		public string OffenderId { get; set; } = string.Empty;
		public string OffenderName { get; set; } = string.Empty;
		public List<MovementEventDto> Movements { get; set; } = new();
		public int TotalStores { get; set; }
	}

	public class MovementEventDto
	{
		public string FromStore { get; set; } = string.Empty;
		public string ToStore { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty;
		public string IncidentType { get; set; } = string.Empty;
	}

	public class OffenderNetworkDataDto
	{
		public List<OffenderNetworkNodeDto> Nodes { get; set; } = new();
		public List<OffenderNetworkLinkDto> Links { get; set; } = new();
	}

	public class OffenderNetworkNodeDto
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public double X { get; set; }
		public double Y { get; set; }
	}

	public class OffenderNetworkLinkDto
	{
		public string Source { get; set; } = string.Empty;
		public string Target { get; set; } = string.Empty;
		public double Strength { get; set; }
		public int IncidentCount { get; set; }
	}

	// ============================================================================
	// Deployment Recommendations
	// ============================================================================

	public class DeploymentRecommendationDto
	{
		public List<TimeDeploymentRecommendationDto> BestTimes { get; set; } = new();
		public List<StoreRiskRankingDto> StoreRankings { get; set; } = new();
		public string OverallStrategy { get; set; } = string.Empty;
		public string LastUpdated { get; set; } = string.Empty;
	}

	public class TimeDeploymentRecommendationDto
	{
		public string Day { get; set; } = string.Empty;
		public int Hour { get; set; }
		public string HourLabel { get; set; } = string.Empty;
		public int RecommendedOfficers { get; set; }
		public string OfficerType { get; set; } = "uniform";

		[JsonPropertyName("recommendedLPM")]
		public bool RecommendedLpm { get; set; }

		public string Priority { get; set; } = "low";
		public string Reason { get; set; } = string.Empty;
		public int ExpectedIncidents { get; set; }
	}

	public class StoreRiskRankingDto
	{
		public int StoreId { get; set; }
		public string StoreName { get; set; } = string.Empty;
		public double RiskScore { get; set; }
		public string RiskLevel { get; set; } = "low";
		public int IncidentCount { get; set; }
		public string Trend { get; set; } = "stable";
		public string RecommendedOfficerType { get; set; } = "uniform";

		[JsonPropertyName("recommendedLPM")]
		public bool RecommendedLpm { get; set; }

		public List<string> RecommendedHours { get; set; } = new();
		public int Priority { get; set; }
	}

	// ============================================================================
	// Crime Linking
	// ============================================================================

	public class CrimeLinkingDataDto
	{
		public List<IncidentClusterDto> Clusters { get; set; } = new();
		public List<OffenderChainDto> OffenderChains { get; set; } = new();
		public int TotalLinkedIncidents { get; set; }
		public DateRangeDto Period { get; set; } = new();
	}

	public class LinkedIncidentDto
	{
		public string IncidentId { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty;
		public string StoreName { get; set; } = string.Empty;
		public string IncidentType { get; set; } = string.Empty;
		public string? OffenderId { get; set; }
		public string? OffenderName { get; set; }
		public decimal Value { get; set; }
		public double SimilarityScore { get; set; }
		public List<string> MatchingFeatures { get; set; } = new();
	}

	public class IncidentClusterDto
	{
		public string ClusterId { get; set; } = string.Empty;
		public List<LinkedIncidentDto> Incidents { get; set; } = new();
		public List<string> CommonFeatures { get; set; } = new();
		public SuspectedOffenderDto? SuspectedOffender { get; set; }
		public decimal TotalValue { get; set; }
		public DateRangeDto DateRange { get; set; } = new();
	}

	public class SuspectedOffenderDto
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public double Confidence { get; set; }
	}

	public class OffenderChainDto
	{
		public string ChainId { get; set; } = string.Empty;
		public string OffenderId { get; set; } = string.Empty;
		public string OffenderName { get; set; } = string.Empty;
		public List<LinkedIncidentDto> Incidents { get; set; } = new();
		public List<ChainTimelineEventDto> Timeline { get; set; } = new();
		public decimal TotalValue { get; set; }
		public string Pattern { get; set; } = string.Empty;
	}

	public class ChainTimelineEventDto
	{
		public string Date { get; set; } = string.Empty;
		public string Store { get; set; } = string.Empty;
		public string IncidentType { get; set; } = string.Empty;
	}
}
