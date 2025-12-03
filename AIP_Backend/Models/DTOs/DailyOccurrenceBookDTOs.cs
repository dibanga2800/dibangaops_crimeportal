#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// DTO for Daily Occurrence Book entry (response)
	/// </summary>
	public class DailyOccurrenceBookDto
	{
		public string Id { get; set; } = string.Empty;
		public int CustomerId { get; set; }
		public string SiteId { get; set; } = string.Empty;
		public string? SiteName { get; set; }
		public string? StoreName { get; set; }
		public string? StoreNumber { get; set; }
		public string? DateCommenced { get; set; }
		public string Date { get; set; } = string.Empty; // ISO date string
		public string Time { get; set; } = string.Empty; // HH:MM format
		public string OfficerName { get; set; } = string.Empty;
		public string Code { get; set; } = string.Empty;
		public string CodeDescription { get; set; } = string.Empty;
		public string? CrimeReportCompletedDate { get; set; }
		public string? CrimeReportCompletedTime { get; set; }
		public string Details { get; set; } = string.Empty;
		public string Signature { get; set; } = string.Empty;
		public ReportedByDto ReportedBy { get; set; } = new();
		public string CreatedAt { get; set; } = string.Empty;
		public string? UpdatedAt { get; set; }
		public string? CreatedBy { get; set; }
		public string? UpdatedBy { get; set; }
	}

	/// <summary>
	/// DTO for reported by information
	/// </summary>
	public class ReportedByDto
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string? BadgeNumber { get; set; }
	}

	/// <summary>
	/// DTO for creating a new occurrence
	/// </summary>
	public class CreateDailyOccurrenceBookRequestDto
	{
		[Required]
		public int CustomerId { get; set; }

		[Required]
		[MaxLength(100)]
		public string SiteId { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string StoreName { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string StoreNumber { get; set; } = string.Empty;

		public string? DateCommenced { get; set; }

		[Required]
		public string Date { get; set; } = string.Empty; // ISO date string

		[Required]
		[MaxLength(10)]
		public string Time { get; set; } = string.Empty; // HH:MM format

		[Required]
		[MaxLength(200)]
		public string OfficerName { get; set; } = string.Empty;

		[Required]
		[MaxLength(2)]
		public string Code { get; set; } = string.Empty;

		public string? CrimeReportCompletedDate { get; set; }

		public string? CrimeReportCompletedTime { get; set; }

		[Required]
		public string Details { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string Signature { get; set; } = string.Empty;
	}

	/// <summary>
	/// DTO for updating an occurrence
	/// </summary>
	public class UpdateDailyOccurrenceBookRequestDto
	{
		[Required]
		public string Id { get; set; } = string.Empty;

		public string? StoreName { get; set; }
		public string? StoreNumber { get; set; }
		public string? DateCommenced { get; set; }
		public string? Date { get; set; }
		public string? Time { get; set; }
		public string? OfficerName { get; set; }
		public string? Code { get; set; }
		public string? CrimeReportCompletedDate { get; set; }
		public string? CrimeReportCompletedTime { get; set; }
		public string? Details { get; set; }
		public string? Signature { get; set; }
	}

	/// <summary>
	/// DTO for filtering occurrences
	/// </summary>
	public class DailyOccurrenceBookFilterDto
	{
		public string? SiteId { get; set; }
		public string? DateFrom { get; set; }
		public string? DateTo { get; set; }
		public string? StoreName { get; set; }
		public string? StoreNumber { get; set; }
		public string? OfficerName { get; set; }
		public string? Code { get; set; }
		public string? ReportedBy { get; set; }
		public string? Search { get; set; }
	}

	/// <summary>
	/// DTO for occurrence statistics
	/// </summary>
	public class DailyOccurrenceBookStatsDto
	{
		public int TotalEntries { get; set; }
		public int EntriesThisWeek { get; set; }
		public int EntriesThisMonth { get; set; }
		public Dictionary<string, int> ByCode { get; set; } = new();
		public Dictionary<string, int> ByStore { get; set; } = new();
	}

	/// <summary>
	/// Response DTO for list of occurrences
	/// </summary>
	public class DailyOccurrenceBookListResponseDto
	{
		public bool Success { get; set; }
		public List<DailyOccurrenceBookDto> Data { get; set; } = new();
		public DailyOccurrenceBookStatsDto? Stats { get; set; }
		public string? Message { get; set; }
	}

	/// <summary>
	/// Response DTO for single occurrence
	/// </summary>
	public class DailyOccurrenceBookResponseDto
	{
		public bool Success { get; set; }
		public DailyOccurrenceBookDto? Data { get; set; }
		public string? Message { get; set; }
	}
}

