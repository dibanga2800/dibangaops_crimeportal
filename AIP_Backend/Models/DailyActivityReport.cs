#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a daily activity report
	/// </summary>
	public class DailyActivityReport
	{
		[Key]
		public int DailyActivityReportId { get; set; }

		// Core identification
		[Required]
		public int CustomerId { get; set; }

		[MaxLength(200)]
		public string? CustomerName { get; set; }

		[Required]
		[MaxLength(100)]
		public string SiteId { get; set; } = string.Empty;

		[Required]
		[MaxLength(200)]
		public string SiteName { get; set; } = string.Empty;

		// Report metadata
		[Required]
		public DateTime ReportDate { get; set; }

		[Required]
		[MaxLength(200)]
		public string OfficerName { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string ShiftStart { get; set; } = string.Empty;

		[Required]
		[MaxLength(50)]
		public string ShiftEnd { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? WeatherConditions { get; set; }

		// Compliance fields (flattened from ComplianceDataDto)
		[MaxLength(10)]
		public string? TillsContainedOverCash { get; set; } // 'yes', 'no', or ''

		[MaxLength(1000)]
		public string? TillsContainedOverCashDescription { get; set; }

		[MaxLength(10)]
		public string? CashOfficeDoorOpen { get; set; }

		[MaxLength(1000)]
		public string? CashOfficeDoorOpenDescription { get; set; }

		[MaxLength(10)]
		public string? VisibleCashOnDisplay { get; set; }

		[MaxLength(1000)]
		public string? VisibleCashOnDisplayDescription { get; set; }

		[MaxLength(10)]
		public string? VisibleKeysOnDisplay { get; set; }

		[MaxLength(1000)]
		public string? VisibleKeysOnDisplayDescription { get; set; }

		[MaxLength(10)]
		public string? FireRoutesBlocked { get; set; }

		[MaxLength(1000)]
		public string? FireRoutesBlockedDescription { get; set; }

		[MaxLength(10)]
		public string? BeSafeBSecurePoster { get; set; }

		[MaxLength(1000)]
		public string? BeSafeBSecurePosterDescription { get; set; }

		[MaxLength(10)]
		public string? AtmAbuse { get; set; }

		[MaxLength(1000)]
		public string? AtmAbuseDescription { get; set; }

		// Insecure Areas fields (flattened from InsecureAreasDataDto)
		[MaxLength(10)]
		public string? KioskSecure { get; set; }

		[MaxLength(1000)]
		public string? KioskSecureDescription { get; set; }

		[MaxLength(10)]
		public string? HighValueRoom { get; set; }

		[MaxLength(1000)]
		public string? HighValueRoomDescription { get; set; }

		[MaxLength(10)]
		public string? ManagersOffice { get; set; }

		[MaxLength(1000)]
		public string? ManagersOfficeDescription { get; set; }

		[MaxLength(10)]
		public string? WarehouseToSalesFloor { get; set; }

		[MaxLength(1000)]
		public string? WarehouseToSalesFloorDescription { get; set; }

		[MaxLength(10)]
		public string? ServiceYard { get; set; }

		[MaxLength(1000)]
		public string? ServiceYardDescription { get; set; }

		[MaxLength(10)]
		public string? CarParkGrounds { get; set; }

		[MaxLength(1000)]
		public string? CarParkGroundsDescription { get; set; }

		[MaxLength(10)]
		public string? FireDoorsBackOfHouse { get; set; }

		[MaxLength(1000)]
		public string? FireDoorsBackOfHouseDescription { get; set; }

		[MaxLength(10)]
		public string? FireDoorsShopFloor { get; set; }

		[MaxLength(1000)]
		public string? FireDoorsShopFloorDescription { get; set; }

		// Systems Not Working fields (flattened from SystemsNotWorkingDataDto)
		[MaxLength(10)]
		public string? WatchMeNow { get; set; }

		[MaxLength(1000)]
		public string? WatchMeNowDescription { get; set; }

		[MaxLength(10)]
		public string? Cctv { get; set; }

		[MaxLength(1000)]
		public string? CctvDescription { get; set; }

		[MaxLength(10)]
		public string? IntruderAlarm { get; set; }

		[MaxLength(1000)]
		public string? IntruderAlarmDescription { get; set; }

		[MaxLength(10)]
		public string? Keyholding { get; set; }

		[MaxLength(1000)]
		public string? KeyholdingDescription { get; set; }

		[MaxLength(10)]
		public string? BodyWornCctv { get; set; }

		[MaxLength(1000)]
		public string? BodyWornCctvDescription { get; set; }

		[MaxLength(10)]
		public string? CigaretteTracker { get; set; }

		[MaxLength(1000)]
		public string? CigaretteTrackerDescription { get; set; }

		[MaxLength(10)]
		public string? CrimeReporting { get; set; }

		[MaxLength(1000)]
		public string? CrimeReportingDescription { get; set; }

		[MaxLength(5000)]
		public string? Notes { get; set; }

		// Navigation properties for one-to-many relationships
		public virtual ICollection<DailyActivityReportActivity> Activities { get; set; } = new List<DailyActivityReportActivity>();
		public virtual ICollection<DailyActivityReportIncident> Incidents { get; set; } = new List<DailyActivityReportIncident>();
		public virtual ICollection<DailyActivityReportSecurityCheck> SecurityChecks { get; set; } = new List<DailyActivityReportSecurityCheck>();
		public virtual ICollection<DailyActivityReportVisitorEntry> VisitorLog { get; set; } = new List<DailyActivityReportVisitorEntry>();

		// Audit fields
		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		// Soft delete
		public bool RecordIsDeleted { get; set; } = false;

		// Navigation property
		[ForeignKey("CustomerId")]
		public virtual Customer? Customer { get; set; }
	}
}
