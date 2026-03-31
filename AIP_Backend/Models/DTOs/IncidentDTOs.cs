#nullable enable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using System.Text.Json.Serialization;

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// DTO for stolen item
	/// </summary>
	public class StolenItemDto
	{
		public string Id { get; set; } = string.Empty;
		public string? Category { get; set; }
		public string? Description { get; set; }
		public string? ProductName { get; set; }
		public decimal Cost { get; set; }
		public int Quantity { get; set; }
		public decimal TotalAmount { get; set; }
		public bool WasRecovered { get; set; }
		public int RecoveredQuantity { get; set; }
		public decimal RecoveredAmount { get; set; }
		public string? Barcode { get; set; }
	}

	/// <summary>
	/// DTO for offender address
	/// </summary>
	public class OffenderAddressDto
	{
		public string? HouseName { get; set; }
		public string? NumberAndStreet { get; set; }
		public string? VillageOrSuburb { get; set; }
		public string? Town { get; set; }
		public string? County { get; set; }
		public string? PostCode { get; set; }
	}

	/// <summary>
	/// DTO for incident response (matches frontend structure)
	/// </summary>
	public class IncidentDto
	{
		// Core identification
		public string Id { get; set; } = string.Empty;
		public int CustomerId { get; set; }
		public string CustomerName { get; set; } = string.Empty;

		// Location information
		[JsonPropertyName("siteName")]
		public string StoreName { get; set; } = string.Empty;
		public string? SiteId { get; set; }
		public string? RegionId { get; set; }
		public string? RegionName { get; set; }
		public string? Location { get; set; }
		public string? Store { get; set; }

		// Personnel information
		[JsonPropertyName("officerName")]
		public string StaffMemberName { get; set; } = string.Empty;
		[JsonPropertyName("officerRole")]
		public string? StaffMemberRole { get; set; }
		public string? OfficerType { get; set; }
		public string? DutyManagerName { get; set; }
		public string? AssignedTo { get; set; }

		// Time information
		public string DateOfIncident { get; set; } = string.Empty;
		public string? Date { get; set; }
		public string? TimeOfIncident { get; set; }
		public string? DateInputted { get; set; }

		// Incident classification
		public string IncidentType { get; set; } = string.Empty;
		public string? Type { get; set; }
		public string? ActionCode { get; set; }
		public List<string>? IncidentInvolved { get; set; }

		// AI-assisted classification
		public string? IncidentCategory { get; set; }
		public double? IncidentCategoryConfidence { get; set; }
		public string? RiskLevel { get; set; }
		public double? RiskScore { get; set; }
		public string? ClassificationVersion { get; set; }

		// Description and details
		public string? Description { get; set; }
		public string? IncidentDetails { get; set; }
		public string? StoreComments { get; set; }

		// Financial information
		public decimal? TotalStolenValue { get; set; }
		public decimal? TotalRecoveredValue { get; set; }
		public decimal? TotalLostValue { get; set; }
		public int? TotalRecoveredQuantity { get; set; }
		public decimal? TotalValueRecovered { get; set; }
		public decimal? Value { get; set; }
		public decimal? ValueRecovered { get; set; }
		public int? QuantityRecovered { get; set; }
		public decimal? Amount { get; set; }
		public decimal? Total { get; set; }

		// Stolen items
		public List<StolenItemDto>? StolenItems { get; set; }

		// Police involvement
		public bool? PoliceInvolvement { get; set; }
		public string? UrnNumber { get; set; }
		public string? CrimeRefNumber { get; set; }
		public string? PoliceID { get; set; }

		// Status tracking
		public string? Status { get; set; }
		public string? Priority { get; set; }
		public string? ActionTaken { get; set; }
		public bool? EvidenceAttached { get; set; }
		public List<string>? WitnessStatements { get; set; }
		public List<string>? InvolvedParties { get; set; }
		public string? ReportNumber { get; set; }

		// Offender information
		public string? OffenderId { get; set; }
		public string? OffenderName { get; set; }
		public string? OffenderSex { get; set; }
		public string? Gender { get; set; }
		public string? OffenderDOB { get; set; }
		public string? OffenderPlaceOfBirth { get; set; }
		public string? OffenderMarks { get; set; }
		public bool? OffenderDetailsVerified { get; set; }
		public string? VerificationMethod { get; set; }
		public string? VerificationEvidenceImage { get; set; }
		public OffenderAddressDto? OffenderAddress { get; set; }

		public List<string>? ModusOperandi { get; set; }

		// Special fields
		public string? ArrestSaveComment { get; set; }
	}

	/// <summary>
	/// Pagination information
	/// </summary>
	public class PaginationInfoDto
	{
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
		public int PageSize { get; set; }
		public int TotalCount { get; set; }
		public bool HasPrevious { get; set; }
		public bool HasNext { get; set; }
	}

	/// <summary>
	/// Paginated incidents response (matches frontend structure)
	/// </summary>
	public class IncidentsResponseDto
	{
		public List<IncidentDto> Data { get; set; } = new();
		public PaginationInfoDto Pagination { get; set; } = new();
	}

	/// <summary>
	/// Single incident response (matches frontend structure)
	/// </summary>
	public class IncidentResponseDto
	{
		public IncidentDto Data { get; set; } = new();
		public bool Success { get; set; } = true;
		public string Message { get; set; } = string.Empty;
	}

	/// <summary>
	/// DTO for creating/updating an incident
	/// </summary>
	public class UpsertIncidentDto
	{
		// Core identification
		[Required]
		public int CustomerId { get; set; }

		public string? SiteId { get; set; }
		public string? RegionId { get; set; }

		// Location information
		[Required]
		[MaxLength(200)]
		[JsonPropertyName("siteName")]
		public string StoreName { get; set; } = string.Empty;

		public string? RegionName { get; set; }
		public string? Location { get; set; }

		// Personnel information
		[Required]
		[MaxLength(200)]
		[JsonPropertyName("officerName")]
		public string StaffMemberName { get; set; } = string.Empty;

		[MaxLength(100)]
		[JsonPropertyName("officerRole")]
		public string? StaffMemberRole { get; set; }

		[MaxLength(50)]
		public string? OfficerType { get; set; }

		[MaxLength(200)]
		public string? DutyManagerName { get; set; }

		[MaxLength(200)]
		public string? AssignedTo { get; set; }

		// Time information
		[Required]
		public DateTime DateOfIncident { get; set; }

		[MaxLength(20)]
		public string? TimeOfIncident { get; set; }

		// Incident classification
		[Required]
		[MaxLength(100)]
		public string IncidentType { get; set; } = string.Empty;

		[MaxLength(50)]
		public string? ActionCode { get; set; }

		public List<string>? IncidentInvolved { get; set; }

		// Description and details
		[MaxLength(5000)]
		public string? Description { get; set; }

		[MaxLength(5000)]
		public string? IncidentDetails { get; set; }

		[MaxLength(5000)]
		public string? StoreComments { get; set; }

		// Financial information
		public decimal? TotalStolenValue { get; set; }
		public decimal? TotalRecoveredValue { get; set; }
		public decimal? TotalLostValue { get; set; }
		public int? TotalRecoveredQuantity { get; set; }
		public decimal? TotalValueRecovered { get; set; }
		public decimal? ValueRecovered { get; set; }
		public int? QuantityRecovered { get; set; }

		// Stolen items
		public List<StolenItemDto>? StolenItems { get; set; }

		// Police involvement
		public bool PoliceInvolvement { get; set; } = false;

		[MaxLength(100)]
		public string? UrnNumber { get; set; }

		[MaxLength(100)]
		public string? CrimeRefNumber { get; set; }

		[MaxLength(100)]
		public string? PoliceId { get; set; }

		// Status tracking
		[MaxLength(50)]
		public string? Status { get; set; }

		[MaxLength(50)]
		public string? Priority { get; set; }

		[MaxLength(2000)]
		public string? ActionTaken { get; set; }

		public bool EvidenceAttached { get; set; } = false;

		public List<string>? WitnessStatements { get; set; }
		public List<string>? InvolvedParties { get; set; }

		[MaxLength(100)]
		public string? ReportNumber { get; set; }

		// Offender information
		[MaxLength(100)]
		public string? OffenderId { get; set; }

		[MaxLength(200)]
		public string? OffenderName { get; set; }

		[MaxLength(50)]
		public string? OffenderSex { get; set; }

		[MaxLength(50)]
		public string? Gender { get; set; }

		public DateTime? OffenderDOB { get; set; }

		[MaxLength(200)]
		public string? OffenderPlaceOfBirth { get; set; }

		[MaxLength(500)]
		public string? OffenderMarks { get; set; }

		public bool OffenderDetailsVerified { get; set; } = false;

		[MaxLength(100)]
		public string? VerificationMethod { get; set; }

		public string? VerificationEvidenceImage { get; set; }

		public OffenderAddressDto? OffenderAddress { get; set; }

		public List<string>? ModusOperandi { get; set; }

		// Special fields
		[MaxLength(2000)]
		public string? ArrestSaveComment { get; set; }
	}

	/// <summary>
	/// Request DTO for creating/updating incident (matches frontend structure)
	/// </summary>
	public class UpsertIncidentRequestDto
	{
		public UpsertIncidentDto Incident { get; set; } = new();
	}

	/// <summary>
	/// Query parameters for getting incidents
	/// </summary>
	public class GetIncidentsQueryDto
	{
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 10;
		public string? Search { get; set; }
		public string? FromDate { get; set; }
		public string? ToDate { get; set; }
		public string? IncidentType { get; set; }
		public string? SiteName { get; set; }
		public string? SiteId { get; set; }
		public string? Status { get; set; }
		public string? CustomerId { get; set; }
	}

	public class RepeatOffenderIncidentSummaryDto
	{
		public string IncidentId { get; set; } = string.Empty;
		public string DateOfIncident { get; set; } = string.Empty;
		public string SiteName { get; set; } = string.Empty;
		public string IncidentType { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string? OffenderMarks { get; set; }
		public bool? OffenderDetailsVerified { get; set; }
		public string? VerificationMethod { get; set; }
		public string? VerificationEvidenceImage { get; set; }
	}

	public class RepeatOffenderMatchDto
	{
		public string OffenderName { get; set; } = string.Empty;
		public string? OffenderDOB { get; set; }
		public string? Gender { get; set; }
		public string? OffenderMarks { get; set; }
		public string? OffenderPlaceOfBirth { get; set; }
		public OffenderAddressDto? OffenderAddress { get; set; }
		public int IncidentCount { get; set; }
		public List<RepeatOffenderIncidentSummaryDto> RecentIncidents { get; set; } = new();
	}

	public class RepeatOffenderSearchResponseDto
	{
		public bool Success { get; set; } = true;
		public string Message { get; set; } = string.Empty;
		public List<RepeatOffenderMatchDto> Data { get; set; } = new();
		public PaginationInfoDto Pagination { get; set; } = new();
	}

	public class RepeatOffenderSearchQueryDto
	{
		public string? Name { get; set; }
		public string? DateOfBirth { get; set; }
		public string? Marks { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 10;
	}

	public class CrimeIntelligenceQueryDto
	{
		[Required]
		public int CustomerId { get; set; }
		public string? SiteId { get; set; }
		public string? RegionId { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
	}

	public class CrimeInsightMetricDto
	{
		public string Title { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
		public string? Subtext { get; set; }
		public string? Trend { get; set; }
		public bool TrendIsPositive { get; set; }
	}

	public class CrimeInsightListItemDto
	{
		public string Name { get; set; } = string.Empty;
		public int Count { get; set; }
		public decimal? Value { get; set; }
		public double Percentage { get; set; }
		public string? SecondaryText { get; set; }
	}

	public class CrimeInsightTimeBucketDto
	{
		public string Bucket { get; set; } = string.Empty;
		public int Count { get; set; }
		public double Percentage { get; set; }
	}

	public class CrimeInsightHotProductDto
	{
		public string ProductName { get; set; } = string.Empty;
		public string? Category { get; set; }
		public int Quantity { get; set; }
		public decimal TotalValue { get; set; }
		public string? MostTargetedStore { get; set; }
		public string? TypicalTime { get; set; }
	}

	public class CrimeIntelligenceResponseDto
	{
		public bool Success { get; set; } = true;
		public string Message { get; set; } = string.Empty;
		public List<CrimeInsightMetricDto> HeroMetrics { get; set; } = new();
		public List<CrimeInsightListItemDto> TopIncidentTypes { get; set; } = new();
		public List<CrimeInsightListItemDto> TopStores { get; set; } = new();
		public List<CrimeInsightListItemDto> TopProducts { get; set; } = new();
		public List<CrimeInsightListItemDto> TopRegions { get; set; } = new();
		public List<CrimeInsightTimeBucketDto> TimeBuckets { get; set; } = new();
		public CrimeInsightHotProductDto? HotProduct { get; set; }
		public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
	}
}

