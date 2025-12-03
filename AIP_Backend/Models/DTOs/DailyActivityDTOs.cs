#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// DTO for Yes/No fields with description
	/// </summary>
	public class YesNoFieldDto
	{
		public string Value { get; set; } = string.Empty; // 'yes', 'no', or ''
		public string Description { get; set; } = string.Empty;
	}

	/// <summary>
	/// DTO for daily activity entry
	/// </summary>
	public class DailyActivityDto
	{
		public string Id { get; set; } = string.Empty;
		public string Time { get; set; } = string.Empty;
		public string Activity { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty; // 'completed', 'in-progress', 'delayed', 'cancelled'
	}

	/// <summary>
	/// DTO for activity incident
	/// </summary>
	public class ActivityIncidentDto
	{
		public string Id { get; set; } = string.Empty;
		public string Time { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty; // 'security', 'safety', 'maintenance', 'other'
		public string Severity { get; set; } = string.Empty; // 'low', 'medium', 'high', 'critical'
		public string Description { get; set; } = string.Empty;
		public string ActionTaken { get; set; } = string.Empty;
		public bool Resolved { get; set; }
	}

	/// <summary>
	/// DTO for security check
	/// </summary>
	public class SecurityCheckDto
	{
		public string Id { get; set; } = string.Empty;
		public string Time { get; set; } = string.Empty;
		public string Area { get; set; } = string.Empty;
		public string CheckType { get; set; } = string.Empty; // 'perimeter', 'building', 'equipment', 'access-control', 'fire-safety'
		public string Status { get; set; } = string.Empty; // 'all-clear', 'issue-found', 'requires-attention'
		public string? Notes { get; set; }
	}

	/// <summary>
	/// DTO for visitor entry
	/// </summary>
	public class VisitorEntryDto
	{
		public string Id { get; set; } = string.Empty;
		public string Time { get; set; } = string.Empty;
		public string VisitorName { get; set; } = string.Empty;
		public string? Company { get; set; }
		public string Purpose { get; set; } = string.Empty;
		public string? EscortedBy { get; set; }
		public string? ExitTime { get; set; }
		public string? BadgeNumber { get; set; }
	}

	/// <summary>
	/// DTO for compliance data
	/// </summary>
	public class ComplianceDataDto
	{
		public YesNoFieldDto TillsContainedOverCash { get; set; } = new();
		public YesNoFieldDto CashOfficeDoorOpen { get; set; } = new();
		public YesNoFieldDto VisibleCashOnDisplay { get; set; } = new();
		public YesNoFieldDto VisibleKeysOnDisplay { get; set; } = new();
		public YesNoFieldDto FireRoutesBlocked { get; set; } = new();
		public YesNoFieldDto BeSafeBSecurePoster { get; set; } = new();
		public YesNoFieldDto AtmAbuse { get; set; } = new();
	}

	/// <summary>
	/// DTO for insecure areas data
	/// </summary>
	public class InsecureAreasDataDto
	{
		public YesNoFieldDto KioskSecure { get; set; } = new();
		public YesNoFieldDto HighValueRoom { get; set; } = new();
		public YesNoFieldDto ManagersOffice { get; set; } = new();
		public YesNoFieldDto WarehouseToSalesFloor { get; set; } = new();
		public YesNoFieldDto ServiceYard { get; set; } = new();
		public YesNoFieldDto CarParkGrounds { get; set; } = new();
		public YesNoFieldDto FireDoorsBackOfHouse { get; set; } = new();
		public YesNoFieldDto FireDoorsShopFloor { get; set; } = new();
	}

	/// <summary>
	/// DTO for systems not working data
	/// </summary>
	public class SystemsNotWorkingDataDto
	{
		public YesNoFieldDto WatchMeNow { get; set; } = new();
		public YesNoFieldDto Cctv { get; set; } = new();
		public YesNoFieldDto IntruderAlarm { get; set; } = new();
		public YesNoFieldDto Keyholding { get; set; } = new();
		public YesNoFieldDto BodyWornCctv { get; set; } = new();
		public YesNoFieldDto CigaretteTracker { get; set; } = new();
		public YesNoFieldDto CrimeReporting { get; set; } = new();
	}

	/// <summary>
	/// DTO for daily activity report response
	/// </summary>
	public class DailyActivityReportDto
	{
		public string Id { get; set; } = string.Empty;
		public int CustomerId { get; set; }
		public string CustomerName { get; set; } = string.Empty;
		public string SiteId { get; set; } = string.Empty;
		public string SiteName { get; set; } = string.Empty;
		public string ReportDate { get; set; } = string.Empty;
		public string OfficerName { get; set; } = string.Empty;
		public string ShiftStart { get; set; } = string.Empty;
		public string ShiftEnd { get; set; } = string.Empty;
		public string? WeatherConditions { get; set; }
		public List<DailyActivityDto> Activities { get; set; } = new();
		public List<ActivityIncidentDto> Incidents { get; set; } = new();
		public List<SecurityCheckDto> SecurityChecks { get; set; } = new();
		public List<VisitorEntryDto> VisitorLog { get; set; } = new();
		public ComplianceDataDto Compliance { get; set; } = new();
		public InsecureAreasDataDto InsecureAreas { get; set; } = new();
		public SystemsNotWorkingDataDto SystemsNotWorking { get; set; } = new();
		public string? Notes { get; set; }
		public string CreatedAt { get; set; } = string.Empty;
		public string UpdatedAt { get; set; } = string.Empty;
	}

	/// <summary>
	/// DTO for daily activity report request
	/// </summary>
	public class DailyActivityReportRequestDto
	{
		[Required]
		public int CustomerId { get; set; }

		public string? CustomerName { get; set; }

		[Required]
		public string SiteId { get; set; } = string.Empty;

		[Required]
		public string SiteName { get; set; } = string.Empty;

		[Required]
		public string ReportDate { get; set; } = string.Empty;

		[Required]
		public string OfficerName { get; set; } = string.Empty;

		[Required]
		public string ShiftStart { get; set; } = string.Empty;

		[Required]
		public string ShiftEnd { get; set; } = string.Empty;

		public string? WeatherConditions { get; set; }
		public List<DailyActivityDto> Activities { get; set; } = new();
		public List<ActivityIncidentDto> Incidents { get; set; } = new();
		public List<SecurityCheckDto> SecurityChecks { get; set; } = new();
		public List<VisitorEntryDto> VisitorLog { get; set; } = new();
		public ComplianceDataDto Compliance { get; set; } = new();
		public InsecureAreasDataDto InsecureAreas { get; set; } = new();
		public SystemsNotWorkingDataDto SystemsNotWorking { get; set; } = new();
		public string? Notes { get; set; }
	}

	/// <summary>
	/// DTO for daily activity report query filters
	/// </summary>
	public class DailyActivityReportQueryDto
	{
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 10;
		public string? Search { get; set; }
		public string? CustomerId { get; set; }
		public string? SiteId { get; set; }
		public string? ReportDate { get; set; }
		public string? OfficerName { get; set; }
		public string? From { get; set; }
		public string? To { get; set; }
	}

	/// <summary>
	/// DTO for paginated daily activity reports response
	/// </summary>
	public class DailyActivityReportsResponseDto
	{
		public List<DailyActivityReportDto> Data { get; set; } = new();
		public PaginationInfoDto Pagination { get; set; } = new();
	}

	/// <summary>
	/// DTO for single daily activity report response
	/// </summary>
	public class DailyActivityReportResponseDto
	{
		public DailyActivityReportDto Data { get; set; } = new();
	}
}
