#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service implementation for DailyActivityReport operations
	/// </summary>
	public class DailyActivityReportService : IDailyActivityReportService
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<DailyActivityReportService> _logger;
		private readonly IUserContextService _userContext;

		public DailyActivityReportService(
			ApplicationDbContext context,
			ILogger<DailyActivityReportService> logger,
			IUserContextService userContext)
		{
			_context = context;
			_logger = logger;
			_userContext = userContext;
		}

		public async Task<DailyActivityReportResponseDto> GetByIdAsync(string id)
		{
			if (!int.TryParse(id, out var reportId))
			{
				throw new ArgumentException("Invalid report ID format", nameof(id));
			}

			var report = await _context.DailyActivityReports
				.Include(r => r.Customer)
				.Include(r => r.Activities)
				.Include(r => r.Incidents)
				.Include(r => r.SecurityChecks)
				.Include(r => r.VisitorLog)
				.FirstOrDefaultAsync(r => r.DailyActivityReportId == reportId && !r.RecordIsDeleted);

			if (report == null)
			{
				throw new KeyNotFoundException($"Daily activity report with ID {id} not found");
			}

			_userContext.EnsureCanAccessRecord(report.CustomerId, report.CreatedBy);

			return new DailyActivityReportResponseDto
			{
				Data = MapToDto(report)
			};
		}

		public async Task<DailyActivityReportsResponseDto> GetReportsAsync(DailyActivityReportQueryDto query)
		{
			var userContext = _userContext.GetCurrentContext();

			var queryBuilder = _context.DailyActivityReports
				.Include(r => r.Customer)
				.Where(r => !r.RecordIsDeleted)
				.AsQueryable();

			if (!userContext.IsAdministrator)
			{
				if (userContext.IsCustomer && userContext.CustomerId.HasValue)
				{
					query.CustomerId = userContext.CustomerId.Value.ToString();
					queryBuilder = queryBuilder.Where(r => r.CustomerId == userContext.CustomerId.Value);
				}
				else if (userContext.IsStore)
				{
					queryBuilder = queryBuilder.Where(r => r.CreatedBy == userContext.UserId);
				}
			}

			// Filter by customer ID
			if (!string.IsNullOrWhiteSpace(query.CustomerId) && int.TryParse(query.CustomerId, out var customerId))
			{
				queryBuilder = queryBuilder.Where(r => r.CustomerId == customerId);
			}

			// Filter by site ID
			if (!string.IsNullOrWhiteSpace(query.SiteId))
			{
				queryBuilder = queryBuilder.Where(r => r.SiteId == query.SiteId);
			}

			// Filter by report date
			if (!string.IsNullOrWhiteSpace(query.ReportDate) && DateTime.TryParse(query.ReportDate, out var reportDate))
			{
				queryBuilder = queryBuilder.Where(r => r.ReportDate.Date == reportDate.Date);
			}

			// Filter by date range
			if (!string.IsNullOrWhiteSpace(query.From) && DateTime.TryParse(query.From, out var fromDate))
			{
				queryBuilder = queryBuilder.Where(r => r.ReportDate >= fromDate);
			}

			if (!string.IsNullOrWhiteSpace(query.To) && DateTime.TryParse(query.To, out var toDate))
			{
				queryBuilder = queryBuilder.Where(r => r.ReportDate <= toDate);
			}

			// Filter by officer name
			if (!string.IsNullOrWhiteSpace(query.OfficerName))
			{
				queryBuilder = queryBuilder.Where(r => r.OfficerName.Contains(query.OfficerName));
			}

			// Search across multiple fields
			if (!string.IsNullOrWhiteSpace(query.Search))
			{
				var searchTerm = query.Search.ToLower();
				queryBuilder = queryBuilder.Where(r =>
					r.OfficerName.ToLower().Contains(searchTerm) ||
					r.SiteName.ToLower().Contains(searchTerm) ||
					(r.CustomerName != null && r.CustomerName.ToLower().Contains(searchTerm)) ||
					(r.Notes != null && r.Notes.ToLower().Contains(searchTerm)));
			}

			// Get total count before pagination
			var totalCount = await queryBuilder.CountAsync();

			// Apply pagination and ordering
			var reports = await queryBuilder
				.Include(r => r.Activities)
				.Include(r => r.Incidents)
				.Include(r => r.SecurityChecks)
				.Include(r => r.VisitorLog)
				.OrderByDescending(r => r.ReportDate)
				.ThenByDescending(r => r.CreatedAt)
				.Skip((query.Page - 1) * query.PageSize)
				.Take(query.PageSize)
				.ToListAsync();

			var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

			return new DailyActivityReportsResponseDto
			{
				Data = reports.Select(MapToDto).ToList(),
				Pagination = new PaginationInfoDto
				{
					CurrentPage = query.Page,
					TotalPages = totalPages,
					PageSize = query.PageSize,
					TotalCount = totalCount,
					HasPrevious = query.Page > 1,
					HasNext = query.Page < totalPages
				}
			};
		}

		public async Task<DailyActivityReportResponseDto> CreateAsync(DailyActivityReportRequestDto dto, string? userId = null)
		{
			_userContext.EnsureCanAccessCustomer(dto.CustomerId);
			var userContext = _userContext.GetCurrentContext();

			if (!DateTime.TryParse(dto.ReportDate, out var reportDate))
			{
				throw new ArgumentException("Invalid report date format", nameof(dto));
			}

			var report = new DailyActivityReport
			{
				CustomerId = dto.CustomerId,
				CustomerName = dto.CustomerName,
				SiteId = dto.SiteId,
				SiteName = dto.SiteName,
				ReportDate = reportDate,
				OfficerName = dto.OfficerName,
				ShiftStart = dto.ShiftStart,
				ShiftEnd = dto.ShiftEnd,
				WeatherConditions = dto.WeatherConditions,
				Notes = dto.Notes,
				CreatedBy = userContext.UserId,
				CreatedAt = DateTime.UtcNow,
				RecordIsDeleted = false
			};

			// Map Compliance fields
			report.TillsContainedOverCash = dto.Compliance?.TillsContainedOverCash?.Value ?? string.Empty;
			report.TillsContainedOverCashDescription = dto.Compliance?.TillsContainedOverCash?.Description;
			report.CashOfficeDoorOpen = dto.Compliance?.CashOfficeDoorOpen?.Value ?? string.Empty;
			report.CashOfficeDoorOpenDescription = dto.Compliance?.CashOfficeDoorOpen?.Description;
			report.VisibleCashOnDisplay = dto.Compliance?.VisibleCashOnDisplay?.Value ?? string.Empty;
			report.VisibleCashOnDisplayDescription = dto.Compliance?.VisibleCashOnDisplay?.Description;
			report.VisibleKeysOnDisplay = dto.Compliance?.VisibleKeysOnDisplay?.Value ?? string.Empty;
			report.VisibleKeysOnDisplayDescription = dto.Compliance?.VisibleKeysOnDisplay?.Description;
			report.FireRoutesBlocked = dto.Compliance?.FireRoutesBlocked?.Value ?? string.Empty;
			report.FireRoutesBlockedDescription = dto.Compliance?.FireRoutesBlocked?.Description;
			report.BeSafeBSecurePoster = dto.Compliance?.BeSafeBSecurePoster?.Value ?? string.Empty;
			report.BeSafeBSecurePosterDescription = dto.Compliance?.BeSafeBSecurePoster?.Description;
			report.AtmAbuse = dto.Compliance?.AtmAbuse?.Value ?? string.Empty;
			report.AtmAbuseDescription = dto.Compliance?.AtmAbuse?.Description;

			// Map InsecureAreas fields
			report.KioskSecure = dto.InsecureAreas?.KioskSecure?.Value ?? string.Empty;
			report.KioskSecureDescription = dto.InsecureAreas?.KioskSecure?.Description;
			report.HighValueRoom = dto.InsecureAreas?.HighValueRoom?.Value ?? string.Empty;
			report.HighValueRoomDescription = dto.InsecureAreas?.HighValueRoom?.Description;
			report.ManagersOffice = dto.InsecureAreas?.ManagersOffice?.Value ?? string.Empty;
			report.ManagersOfficeDescription = dto.InsecureAreas?.ManagersOffice?.Description;
			report.WarehouseToSalesFloor = dto.InsecureAreas?.WarehouseToSalesFloor?.Value ?? string.Empty;
			report.WarehouseToSalesFloorDescription = dto.InsecureAreas?.WarehouseToSalesFloor?.Description;
			report.ServiceYard = dto.InsecureAreas?.ServiceYard?.Value ?? string.Empty;
			report.ServiceYardDescription = dto.InsecureAreas?.ServiceYard?.Description;
			report.CarParkGrounds = dto.InsecureAreas?.CarParkGrounds?.Value ?? string.Empty;
			report.CarParkGroundsDescription = dto.InsecureAreas?.CarParkGrounds?.Description;
			report.FireDoorsBackOfHouse = dto.InsecureAreas?.FireDoorsBackOfHouse?.Value ?? string.Empty;
			report.FireDoorsBackOfHouseDescription = dto.InsecureAreas?.FireDoorsBackOfHouse?.Description;
			report.FireDoorsShopFloor = dto.InsecureAreas?.FireDoorsShopFloor?.Value ?? string.Empty;
			report.FireDoorsShopFloorDescription = dto.InsecureAreas?.FireDoorsShopFloor?.Description;

			// Map SystemsNotWorking fields
			report.WatchMeNow = dto.SystemsNotWorking?.WatchMeNow?.Value ?? string.Empty;
			report.WatchMeNowDescription = dto.SystemsNotWorking?.WatchMeNow?.Description;
			report.Cctv = dto.SystemsNotWorking?.Cctv?.Value ?? string.Empty;
			report.CctvDescription = dto.SystemsNotWorking?.Cctv?.Description;
			report.IntruderAlarm = dto.SystemsNotWorking?.IntruderAlarm?.Value ?? string.Empty;
			report.IntruderAlarmDescription = dto.SystemsNotWorking?.IntruderAlarm?.Description;
			report.Keyholding = dto.SystemsNotWorking?.Keyholding?.Value ?? string.Empty;
			report.KeyholdingDescription = dto.SystemsNotWorking?.Keyholding?.Description;
			report.BodyWornCctv = dto.SystemsNotWorking?.BodyWornCctv?.Value ?? string.Empty;
			report.BodyWornCctvDescription = dto.SystemsNotWorking?.BodyWornCctv?.Description;
			report.CigaretteTracker = dto.SystemsNotWorking?.CigaretteTracker?.Value ?? string.Empty;
			report.CigaretteTrackerDescription = dto.SystemsNotWorking?.CigaretteTracker?.Description;
			report.CrimeReporting = dto.SystemsNotWorking?.CrimeReporting?.Value ?? string.Empty;
			report.CrimeReportingDescription = dto.SystemsNotWorking?.CrimeReporting?.Description;

			// Map Activities
			report.Activities = dto.Activities.Select(a => new DailyActivityReportActivity
			{
				ActivityId = a.Id,
				Time = a.Time,
				Activity = a.Activity,
				Location = a.Location,
				Description = a.Description,
				Status = a.Status
			}).ToList();

			// Map Incidents
			report.Incidents = dto.Incidents.Select(i => new DailyActivityReportIncident
			{
				IncidentId = i.Id,
				Time = i.Time,
				Type = i.Type,
				Severity = i.Severity,
				Description = i.Description,
				ActionTaken = i.ActionTaken,
				Resolved = i.Resolved
			}).ToList();

			// Map SecurityChecks
			report.SecurityChecks = dto.SecurityChecks.Select(sc => new DailyActivityReportSecurityCheck
			{
				SecurityCheckId = sc.Id,
				Time = sc.Time,
				Area = sc.Area,
				CheckType = sc.CheckType,
				Status = sc.Status,
				Notes = sc.Notes
			}).ToList();

			// Map VisitorLog
			report.VisitorLog = dto.VisitorLog.Select(ve => new DailyActivityReportVisitorEntry
			{
				VisitorEntryId = ve.Id,
				Time = ve.Time,
				VisitorName = ve.VisitorName,
				Company = ve.Company,
				Purpose = ve.Purpose,
				EscortedBy = ve.EscortedBy,
				ExitTime = ve.ExitTime,
				BadgeNumber = ve.BadgeNumber
			}).ToList();

			_context.DailyActivityReports.Add(report);
			await _context.SaveChangesAsync();

			_logger.LogInformation("Daily activity report created with ID {ReportId} by user {UserId}", report.DailyActivityReportId, userContext.UserId);

			// Reload with customer relationship
			await _context.Entry(report).Reference(r => r.Customer).LoadAsync();

			return new DailyActivityReportResponseDto
			{
				Data = MapToDto(report)
			};
		}

		public async Task<DailyActivityReportResponseDto> UpdateAsync(string id, DailyActivityReportRequestDto dto, string? userId = null)
		{
			if (!int.TryParse(id, out var reportId))
			{
				throw new ArgumentException("Invalid report ID format", nameof(id));
			}

			var existing = await _context.DailyActivityReports
				.FirstOrDefaultAsync(r => r.DailyActivityReportId == reportId && !r.RecordIsDeleted);

			if (existing == null)
			{
				throw new KeyNotFoundException($"Daily activity report with ID {id} not found");
			}

			_userContext.EnsureCanAccessRecord(existing.CustomerId, existing.CreatedBy);
			_userContext.EnsureCanAccessCustomer(dto.CustomerId);

			if (!DateTime.TryParse(dto.ReportDate, out var reportDate))
			{
				throw new ArgumentException("Invalid report date format", nameof(dto));
			}

			var userContext = _userContext.GetCurrentContext();

			// Update fields
			existing.CustomerId = dto.CustomerId;
			existing.CustomerName = dto.CustomerName;
			existing.SiteId = dto.SiteId;
			existing.SiteName = dto.SiteName;
			existing.ReportDate = reportDate;
			existing.OfficerName = dto.OfficerName;
			existing.ShiftStart = dto.ShiftStart;
			existing.ShiftEnd = dto.ShiftEnd;
			existing.WeatherConditions = dto.WeatherConditions;
			existing.Notes = dto.Notes;
			existing.UpdatedBy = userContext.UserId;
			existing.UpdatedAt = DateTime.UtcNow;

			// Map Compliance fields
			existing.TillsContainedOverCash = dto.Compliance?.TillsContainedOverCash?.Value ?? string.Empty;
			existing.TillsContainedOverCashDescription = dto.Compliance?.TillsContainedOverCash?.Description;
			existing.CashOfficeDoorOpen = dto.Compliance?.CashOfficeDoorOpen?.Value ?? string.Empty;
			existing.CashOfficeDoorOpenDescription = dto.Compliance?.CashOfficeDoorOpen?.Description;
			existing.VisibleCashOnDisplay = dto.Compliance?.VisibleCashOnDisplay?.Value ?? string.Empty;
			existing.VisibleCashOnDisplayDescription = dto.Compliance?.VisibleCashOnDisplay?.Description;
			existing.VisibleKeysOnDisplay = dto.Compliance?.VisibleKeysOnDisplay?.Value ?? string.Empty;
			existing.VisibleKeysOnDisplayDescription = dto.Compliance?.VisibleKeysOnDisplay?.Description;
			existing.FireRoutesBlocked = dto.Compliance?.FireRoutesBlocked?.Value ?? string.Empty;
			existing.FireRoutesBlockedDescription = dto.Compliance?.FireRoutesBlocked?.Description;
			existing.BeSafeBSecurePoster = dto.Compliance?.BeSafeBSecurePoster?.Value ?? string.Empty;
			existing.BeSafeBSecurePosterDescription = dto.Compliance?.BeSafeBSecurePoster?.Description;
			existing.AtmAbuse = dto.Compliance?.AtmAbuse?.Value ?? string.Empty;
			existing.AtmAbuseDescription = dto.Compliance?.AtmAbuse?.Description;

			// Map InsecureAreas fields
			existing.KioskSecure = dto.InsecureAreas?.KioskSecure?.Value ?? string.Empty;
			existing.KioskSecureDescription = dto.InsecureAreas?.KioskSecure?.Description;
			existing.HighValueRoom = dto.InsecureAreas?.HighValueRoom?.Value ?? string.Empty;
			existing.HighValueRoomDescription = dto.InsecureAreas?.HighValueRoom?.Description;
			existing.ManagersOffice = dto.InsecureAreas?.ManagersOffice?.Value ?? string.Empty;
			existing.ManagersOfficeDescription = dto.InsecureAreas?.ManagersOffice?.Description;
			existing.WarehouseToSalesFloor = dto.InsecureAreas?.WarehouseToSalesFloor?.Value ?? string.Empty;
			existing.WarehouseToSalesFloorDescription = dto.InsecureAreas?.WarehouseToSalesFloor?.Description;
			existing.ServiceYard = dto.InsecureAreas?.ServiceYard?.Value ?? string.Empty;
			existing.ServiceYardDescription = dto.InsecureAreas?.ServiceYard?.Description;
			existing.CarParkGrounds = dto.InsecureAreas?.CarParkGrounds?.Value ?? string.Empty;
			existing.CarParkGroundsDescription = dto.InsecureAreas?.CarParkGrounds?.Description;
			existing.FireDoorsBackOfHouse = dto.InsecureAreas?.FireDoorsBackOfHouse?.Value ?? string.Empty;
			existing.FireDoorsBackOfHouseDescription = dto.InsecureAreas?.FireDoorsBackOfHouse?.Description;
			existing.FireDoorsShopFloor = dto.InsecureAreas?.FireDoorsShopFloor?.Value ?? string.Empty;
			existing.FireDoorsShopFloorDescription = dto.InsecureAreas?.FireDoorsShopFloor?.Description;

			// Map SystemsNotWorking fields
			existing.WatchMeNow = dto.SystemsNotWorking?.WatchMeNow?.Value ?? string.Empty;
			existing.WatchMeNowDescription = dto.SystemsNotWorking?.WatchMeNow?.Description;
			existing.Cctv = dto.SystemsNotWorking?.Cctv?.Value ?? string.Empty;
			existing.CctvDescription = dto.SystemsNotWorking?.Cctv?.Description;
			existing.IntruderAlarm = dto.SystemsNotWorking?.IntruderAlarm?.Value ?? string.Empty;
			existing.IntruderAlarmDescription = dto.SystemsNotWorking?.IntruderAlarm?.Description;
			existing.Keyholding = dto.SystemsNotWorking?.Keyholding?.Value ?? string.Empty;
			existing.KeyholdingDescription = dto.SystemsNotWorking?.Keyholding?.Description;
			existing.BodyWornCctv = dto.SystemsNotWorking?.BodyWornCctv?.Value ?? string.Empty;
			existing.BodyWornCctvDescription = dto.SystemsNotWorking?.BodyWornCctv?.Description;
			existing.CigaretteTracker = dto.SystemsNotWorking?.CigaretteTracker?.Value ?? string.Empty;
			existing.CigaretteTrackerDescription = dto.SystemsNotWorking?.CigaretteTracker?.Description;
			existing.CrimeReporting = dto.SystemsNotWorking?.CrimeReporting?.Value ?? string.Empty;
			existing.CrimeReportingDescription = dto.SystemsNotWorking?.CrimeReporting?.Description;

			// Update Activities - remove existing and add new
			_context.DailyActivityReportActivities.RemoveRange(existing.Activities);
			existing.Activities = dto.Activities.Select(a => new DailyActivityReportActivity
			{
				ActivityId = a.Id,
				Time = a.Time,
				Activity = a.Activity,
				Location = a.Location,
				Description = a.Description,
				Status = a.Status
			}).ToList();

			// Update Incidents - remove existing and add new
			_context.DailyActivityReportIncidents.RemoveRange(existing.Incidents);
			existing.Incidents = dto.Incidents.Select(i => new DailyActivityReportIncident
			{
				IncidentId = i.Id,
				Time = i.Time,
				Type = i.Type,
				Severity = i.Severity,
				Description = i.Description,
				ActionTaken = i.ActionTaken,
				Resolved = i.Resolved
			}).ToList();

			// Update SecurityChecks - remove existing and add new
			_context.DailyActivityReportSecurityChecks.RemoveRange(existing.SecurityChecks);
			existing.SecurityChecks = dto.SecurityChecks.Select(sc => new DailyActivityReportSecurityCheck
			{
				SecurityCheckId = sc.Id,
				Time = sc.Time,
				Area = sc.Area,
				CheckType = sc.CheckType,
				Status = sc.Status,
				Notes = sc.Notes
			}).ToList();

			// Update VisitorLog - remove existing and add new
			_context.DailyActivityReportVisitorEntries.RemoveRange(existing.VisitorLog);
			existing.VisitorLog = dto.VisitorLog.Select(ve => new DailyActivityReportVisitorEntry
			{
				VisitorEntryId = ve.Id,
				Time = ve.Time,
				VisitorName = ve.VisitorName,
				Company = ve.Company,
				Purpose = ve.Purpose,
				EscortedBy = ve.EscortedBy,
				ExitTime = ve.ExitTime,
				BadgeNumber = ve.BadgeNumber
			}).ToList();

			await _context.SaveChangesAsync();

			_logger.LogInformation("Daily activity report updated with ID {ReportId} by user {UserId}", reportId, userContext.UserId);

			// Reload with customer relationship
			await _context.Entry(existing).Reference(r => r.Customer).LoadAsync();

			return new DailyActivityReportResponseDto
			{
				Data = MapToDto(existing)
			};
		}

		public async Task<bool> DeleteAsync(string id)
		{
			if (!int.TryParse(id, out var reportId))
			{
				throw new ArgumentException("Invalid report ID format", nameof(id));
			}

			var report = await _context.DailyActivityReports
				.FirstOrDefaultAsync(r => r.DailyActivityReportId == reportId && !r.RecordIsDeleted);

			if (report == null)
			{
				return false;
			}

			_userContext.EnsureCanAccessRecord(report.CustomerId, report.CreatedBy);

			// Soft delete
			report.RecordIsDeleted = true;
			report.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			_logger.LogInformation("Daily activity report deleted with ID {ReportId}", reportId);
			return true;
		}

		#region Mapping Methods

		private DailyActivityReportDto MapToDto(DailyActivityReport report)
		{
			// Map from navigation properties and direct columns
			var activities = report.Activities.Select(a => new DailyActivityDto
			{
				Id = a.ActivityId,
				Time = a.Time,
				Activity = a.Activity,
				Location = a.Location,
				Description = a.Description ?? string.Empty,
				Status = a.Status
			}).ToList();

			var incidents = report.Incidents.Select(i => new ActivityIncidentDto
			{
				Id = i.IncidentId,
				Time = i.Time,
				Type = i.Type,
				Severity = i.Severity,
				Description = i.Description ?? string.Empty,
				ActionTaken = i.ActionTaken ?? string.Empty,
				Resolved = i.Resolved
			}).ToList();

			var securityChecks = report.SecurityChecks.Select(sc => new SecurityCheckDto
			{
				Id = sc.SecurityCheckId,
				Time = sc.Time,
				Area = sc.Area,
				CheckType = sc.CheckType,
				Status = sc.Status,
				Notes = sc.Notes
			}).ToList();

			var visitorLog = report.VisitorLog.Select(ve => new VisitorEntryDto
			{
				Id = ve.VisitorEntryId,
				Time = ve.Time,
				VisitorName = ve.VisitorName,
				Company = ve.Company,
				Purpose = ve.Purpose,
				EscortedBy = ve.EscortedBy,
				ExitTime = ve.ExitTime,
				BadgeNumber = ve.BadgeNumber
			}).ToList();

			// Map Compliance from direct columns
			var compliance = new ComplianceDataDto
			{
				TillsContainedOverCash = new YesNoFieldDto
				{
					Value = report.TillsContainedOverCash ?? string.Empty,
					Description = report.TillsContainedOverCashDescription ?? string.Empty
				},
				CashOfficeDoorOpen = new YesNoFieldDto
				{
					Value = report.CashOfficeDoorOpen ?? string.Empty,
					Description = report.CashOfficeDoorOpenDescription ?? string.Empty
				},
				VisibleCashOnDisplay = new YesNoFieldDto
				{
					Value = report.VisibleCashOnDisplay ?? string.Empty,
					Description = report.VisibleCashOnDisplayDescription ?? string.Empty
				},
				VisibleKeysOnDisplay = new YesNoFieldDto
				{
					Value = report.VisibleKeysOnDisplay ?? string.Empty,
					Description = report.VisibleKeysOnDisplayDescription ?? string.Empty
				},
				FireRoutesBlocked = new YesNoFieldDto
				{
					Value = report.FireRoutesBlocked ?? string.Empty,
					Description = report.FireRoutesBlockedDescription ?? string.Empty
				},
				BeSafeBSecurePoster = new YesNoFieldDto
				{
					Value = report.BeSafeBSecurePoster ?? string.Empty,
					Description = report.BeSafeBSecurePosterDescription ?? string.Empty
				},
				AtmAbuse = new YesNoFieldDto
				{
					Value = report.AtmAbuse ?? string.Empty,
					Description = report.AtmAbuseDescription ?? string.Empty
				}
			};

			// Map InsecureAreas from direct columns
			var insecureAreas = new InsecureAreasDataDto
			{
				KioskSecure = new YesNoFieldDto
				{
					Value = report.KioskSecure ?? string.Empty,
					Description = report.KioskSecureDescription ?? string.Empty
				},
				HighValueRoom = new YesNoFieldDto
				{
					Value = report.HighValueRoom ?? string.Empty,
					Description = report.HighValueRoomDescription ?? string.Empty
				},
				ManagersOffice = new YesNoFieldDto
				{
					Value = report.ManagersOffice ?? string.Empty,
					Description = report.ManagersOfficeDescription ?? string.Empty
				},
				WarehouseToSalesFloor = new YesNoFieldDto
				{
					Value = report.WarehouseToSalesFloor ?? string.Empty,
					Description = report.WarehouseToSalesFloorDescription ?? string.Empty
				},
				ServiceYard = new YesNoFieldDto
				{
					Value = report.ServiceYard ?? string.Empty,
					Description = report.ServiceYardDescription ?? string.Empty
				},
				CarParkGrounds = new YesNoFieldDto
				{
					Value = report.CarParkGrounds ?? string.Empty,
					Description = report.CarParkGroundsDescription ?? string.Empty
				},
				FireDoorsBackOfHouse = new YesNoFieldDto
				{
					Value = report.FireDoorsBackOfHouse ?? string.Empty,
					Description = report.FireDoorsBackOfHouseDescription ?? string.Empty
				},
				FireDoorsShopFloor = new YesNoFieldDto
				{
					Value = report.FireDoorsShopFloor ?? string.Empty,
					Description = report.FireDoorsShopFloorDescription ?? string.Empty
				}
			};

			// Map SystemsNotWorking from direct columns
			var systemsNotWorking = new SystemsNotWorkingDataDto
			{
				WatchMeNow = new YesNoFieldDto
				{
					Value = report.WatchMeNow ?? string.Empty,
					Description = report.WatchMeNowDescription ?? string.Empty
				},
				Cctv = new YesNoFieldDto
				{
					Value = report.Cctv ?? string.Empty,
					Description = report.CctvDescription ?? string.Empty
				},
				IntruderAlarm = new YesNoFieldDto
				{
					Value = report.IntruderAlarm ?? string.Empty,
					Description = report.IntruderAlarmDescription ?? string.Empty
				},
				Keyholding = new YesNoFieldDto
				{
					Value = report.Keyholding ?? string.Empty,
					Description = report.KeyholdingDescription ?? string.Empty
				},
				BodyWornCctv = new YesNoFieldDto
				{
					Value = report.BodyWornCctv ?? string.Empty,
					Description = report.BodyWornCctvDescription ?? string.Empty
				},
				CigaretteTracker = new YesNoFieldDto
				{
					Value = report.CigaretteTracker ?? string.Empty,
					Description = report.CigaretteTrackerDescription ?? string.Empty
				},
				CrimeReporting = new YesNoFieldDto
				{
					Value = report.CrimeReporting ?? string.Empty,
					Description = report.CrimeReportingDescription ?? string.Empty
				}
			};

			return new DailyActivityReportDto
			{
				Id = report.DailyActivityReportId.ToString(),
				CustomerId = report.CustomerId,
				CustomerName = report.CustomerName ?? report.Customer?.CompanyName ?? string.Empty,
				SiteId = report.SiteId,
				SiteName = report.SiteName,
				ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
				OfficerName = report.OfficerName,
				ShiftStart = report.ShiftStart,
				ShiftEnd = report.ShiftEnd,
				WeatherConditions = report.WeatherConditions,
				Activities = activities,
				Incidents = incidents,
				SecurityChecks = securityChecks,
				VisitorLog = visitorLog,
				Compliance = compliance,
				InsecureAreas = insecureAreas,
				SystemsNotWorking = systemsNotWorking,
				Notes = report.Notes,
				CreatedAt = report.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				UpdatedAt = report.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? report.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
			};
		}

		#endregion
	}
}
