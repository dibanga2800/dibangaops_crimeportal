#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories.Models;
using AIPBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Repositories
{
	/// <summary>
	/// Repository implementation for Incident operations
	/// </summary>
	public class IncidentRepository : IIncidentRepository
	{
		private readonly ApplicationDbContext _context;

		public IncidentRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<Incident?> GetByIdAsync(int incidentId)
		{
			return await _context.Incidents
				.Include(i => i.StolenItems)
				.Include(i => i.Customer)
				.FirstOrDefaultAsync(i => i.IncidentId == incidentId && !i.RecordIsDeletedYN);
		}

		public async Task<Incident?> GetByIdWithItemsAsync(int incidentId)
		{
			return await _context.Incidents
				.Include(i => i.StolenItems)
				.Include(i => i.Customer)
				.FirstOrDefaultAsync(i => i.IncidentId == incidentId && !i.RecordIsDeletedYN);
		}

		public async Task<(List<Incident> Incidents, int TotalCount)> GetPagedAsync(
			int page,
			int pageSize,
			string? search = null,
			int? customerId = null,
			IReadOnlyCollection<int>? customerIds = null,
			string? siteId = null,
			string? regionId = null,
			string? incidentType = null,
			string? status = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			string? createdByUserId = null)
		{
			// Read-only list: no change tracking, and stolen items load in a
			// second query so each wide incident row is not repeated per item.
			var query = ApplyListFilters(
				ReadOnlyIncidents()
					.Include(i => i.StolenItems)
					.Include(i => i.Customer)
					.Where(i => !i.RecordIsDeletedYN),
				search,
				customerId,
				customerIds,
				siteId,
				regionId,
				incidentType,
				status,
				fromDate,
				toDate,
				createdByUserId);

			var totalCount = await query.CountAsync();

			var incidents = await query
				.OrderByDescending(i => i.DateOfIncident)
				.ThenByDescending(i => i.DateInputted)
				.ThenByDescending(i => i.IncidentId)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (incidents, totalCount);
		}

		public async Task<IncidentListSummaryDto> GetSummaryAsync(
			string? search = null,
			int? customerId = null,
			IReadOnlyCollection<int>? customerIds = null,
			string? siteId = null,
			string? regionId = null,
			string? incidentType = null,
			string? status = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			string? createdByUserId = null)
		{
			var query = ApplyListFilters(
				_context.Incidents.Where(i => !i.RecordIsDeletedYN),
				search,
				customerId,
				customerIds,
				siteId,
				regionId,
				incidentType,
				status,
				fromDate,
				toDate,
				createdByUserId);

			// Only the columns the totals need. Loading full incidents (every text
			// column plus stolen items) here made every list request read the whole
			// filtered table.
			var incidents = await query
				.AsNoTracking()
				.Select(i => new SummaryRow
				{
					SiteId = i.SiteId,
					StoreName = i.StoreName,
					IncidentType = i.IncidentType,
					DateOfIncident = i.DateOfIncident,
					Priority = i.Priority,
					Status = i.Status,
					TotalRecoveredValue = i.TotalRecoveredValue,
					TotalValueRecovered = i.TotalValueRecovered,
					TotalLostValue = i.TotalLostValue,
					TotalStolenValue = i.TotalStolenValue,
					HasItems = i.StolenItems.Any(),
					ItemsRecovered = i.StolenItems.Sum(s => (decimal?)s.RecoveredAmount) ?? 0m,
					ItemsTotal = i.StolenItems.Sum(s => (decimal?)s.TotalAmount) ?? 0m,
				})
				.ToListAsync();

			var uniqueSiteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			decimal totalRecovered = 0m;
			decimal totalLost = 0m;
			var shopliftingCount = 0;
			var todayCount = 0;
			var highPriorityCount = 0;
			var pendingCount = 0;
			var resolvedCount = 0;
			var today = DateTime.UtcNow.Date;

			foreach (var incident in incidents)
			{
				// Same rules as IncidentFinancials.GetRecoveredValue / GetLostValue.
				var recovered = incident.TotalRecoveredValue
					?? incident.TotalValueRecovered
					?? (incident.HasItems ? incident.ItemsRecovered : 0m);
				var stolen = incident.TotalStolenValue ?? (incident.HasItems ? incident.ItemsTotal : 0m);
				totalRecovered += recovered;
				totalLost += incident.TotalLostValue ?? Math.Max(stolen - recovered, 0m);

				var siteKey = !string.IsNullOrWhiteSpace(incident.SiteId)
					? incident.SiteId
					: incident.StoreName;
				if (!string.IsNullOrWhiteSpace(siteKey))
				{
					uniqueSiteKeys.Add(siteKey.Trim());
				}

				if (IsShopliftingIncidentType(incident.IncidentType))
				{
					shopliftingCount++;
				}

				if (incident.DateOfIncident.Date == today)
				{
					todayCount++;
				}

				if (string.Equals(incident.Priority, "high", StringComparison.OrdinalIgnoreCase))
				{
					highPriorityCount++;
				}

				if (string.Equals(incident.Status, "pending", StringComparison.OrdinalIgnoreCase))
				{
					pendingCount++;
				}
				else if (string.Equals(incident.Status, "resolved", StringComparison.OrdinalIgnoreCase))
				{
					resolvedCount++;
				}
			}

			return new IncidentListSummaryDto
			{
				TotalIncidents = incidents.Count,
				TotalAmountRecovered = totalRecovered,
				TotalAmountLost = totalLost,
				UniqueSites = uniqueSiteKeys.Count,
				ShopliftingIncidents = shopliftingCount,
				TodayIncidents = todayCount,
				HighPriorityIncidents = highPriorityCount,
				PendingIncidents = pendingCount,
				ResolvedIncidents = resolvedCount,
			};
		}

		public async Task<(List<IncidentLiteDto> Rows, int TotalCount)> GetLiteAsync(
			int maxRows,
			string? search = null,
			int? customerId = null,
			IReadOnlyCollection<int>? customerIds = null,
			string? siteId = null,
			string? regionId = null,
			string? incidentType = null,
			string? status = null,
			DateTime? fromDate = null,
			DateTime? toDate = null)
		{
			var query = ApplyListFilters(
				_context.Incidents.AsNoTracking().Where(i => !i.RecordIsDeletedYN),
				search,
				customerId,
				customerIds,
				siteId,
				regionId,
				incidentType,
				status,
				fromDate,
				toDate,
				createdByUserId: null);

			var totalCount = await query.CountAsync();

			// One query, only the columns dashboards use. Stolen-item totals are
			// summed in SQL so no item rows are loaded.
			var rows = await query
				.OrderByDescending(i => i.DateOfIncident)
				.ThenByDescending(i => i.DateInputted)
				.ThenByDescending(i => i.IncidentId)
				.Take(maxRows)
				.Select(i => new
				{
					i.IncidentId,
					i.CustomerId,
					CustomerName = i.Customer != null ? i.Customer.CompanyName : null,
					i.StoreName,
					i.SiteId,
					i.RegionId,
					i.RegionName,
					i.StaffMemberName,
					i.DateOfIncident,
					i.TimeOfIncident,
					i.DateInputted,
					i.IncidentType,
					i.TotalStolenValue,
					i.TotalRecoveredValue,
					i.TotalValueRecovered,
					i.TotalLostValue,
					HasItems = i.StolenItems.Any(),
					ItemsRecovered = i.StolenItems.Sum(s => (decimal?)s.RecoveredAmount) ?? 0m,
					ItemsTotal = i.StolenItems.Sum(s => (decimal?)s.TotalAmount) ?? 0m,
					i.PoliceInvolvement,
					i.OffenderName,
					i.Status,
					i.Priority,
					i.IncidentCategory,
					i.IncidentCategoryConfidence,
					i.RiskLevel,
					i.RiskScore,
				})
				.ToListAsync();

			var result = rows.Select(r =>
			{
				// Same rules as IncidentFinancials.
				var recovered = r.TotalRecoveredValue ?? r.TotalValueRecovered ?? (r.HasItems ? r.ItemsRecovered : 0m);
				var stolen = r.TotalStolenValue ?? (r.HasItems ? r.ItemsTotal : 0m);
				var lost = r.TotalLostValue ?? Math.Max(stolen - recovered, 0m);
				return new IncidentLiteDto
				{
					Id = r.IncidentId.ToString(),
					CustomerId = r.CustomerId,
					CustomerName = r.CustomerName ?? string.Empty,
					SiteName = r.StoreName,
					SiteId = r.SiteId,
					RegionId = r.RegionId,
					RegionName = r.RegionName,
					OfficerName = r.StaffMemberName,
					DateOfIncident = r.DateOfIncident.ToString("yyyy-MM-dd"),
					TimeOfIncident = r.TimeOfIncident,
					DateInputted = r.DateInputted.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					IncidentType = r.IncidentType,
					TotalStolenValue = stolen,
					TotalRecoveredValue = recovered,
					TotalLostValue = lost,
					PoliceInvolvement = r.PoliceInvolvement,
					OffenderName = r.OffenderName,
					Status = r.Status,
					Priority = r.Priority,
					IncidentCategory = r.IncidentCategory,
					IncidentCategoryConfidence = r.IncidentCategoryConfidence,
					RiskLevel = r.RiskLevel,
					RiskScore = r.RiskScore,
				};
			}).ToList();

			return (result, totalCount);
		}

		/// <summary>
		/// Incidents for read-only queries: no change tracking, and on SQL Server
		/// included collections load in separate queries so each wide incident
		/// row is not repeated once per stolen item.
		/// </summary>
		private IQueryable<Incident> ReadOnlyIncidents()
		{
			var query = _context.Incidents.AsNoTracking();
			return _context.Database.IsRelational() ? query.AsSplitQuery() : query;
		}

		private sealed class SummaryRow
		{
			public string? SiteId { get; set; }
			public string? StoreName { get; set; }
			public string? IncidentType { get; set; }
			public DateTime DateOfIncident { get; set; }
			public string? Priority { get; set; }
			public string? Status { get; set; }
			public decimal? TotalRecoveredValue { get; set; }
			public decimal? TotalValueRecovered { get; set; }
			public decimal? TotalLostValue { get; set; }
			public decimal? TotalStolenValue { get; set; }
			public bool HasItems { get; set; }
			public decimal ItemsRecovered { get; set; }
			public decimal ItemsTotal { get; set; }
		}

		/// <summary>
		/// True when the incident type names shoplifting as the primary
		/// offense. We match on the first word so "Shoplifting",
		/// "shoplifting", and qualified variants like "Shoplifting / Theft"
		/// all count, but distinct offenses that merely contain the word
		/// as a modifier (notably "Attempted Shoplifting", where the theft
		/// never completed) are excluded. The next character after the
		/// "shoplifting" prefix must be a non-letter so we don't accidentally
		/// bucket implausible derivatives ("Shopliftings" etc.).
		/// </summary>
		private static bool IsShopliftingIncidentType(string? incidentType)
		{
			if (string.IsNullOrWhiteSpace(incidentType))
			{
				return false;
			}

			var trimmed = incidentType.Trim();
			const string prefix = "shoplifting";

			if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (trimmed.Length == prefix.Length)
			{
				return true;
			}

			return !char.IsLetter(trimmed[prefix.Length]);
		}

		private IQueryable<Incident> ApplyListFilters(
			IQueryable<Incident> query,
			string? search,
			int? customerId,
			IReadOnlyCollection<int>? customerIds,
			string? siteId,
			string? regionId,
			string? incidentType,
			string? status,
			DateTime? fromDate,
			DateTime? toDate,
			string? createdByUserId)
		{
			if (customerIds is { Count: > 0 })
			{
				query = query.Where(i => customerIds.Contains(i.CustomerId));
			}
			else if (customerId.HasValue)
			{
				query = query.Where(i => i.CustomerId == customerId.Value);
			}

			if (!string.IsNullOrWhiteSpace(siteId))
			{
				query = query.Where(i => i.SiteId == siteId);
			}

			if (!string.IsNullOrWhiteSpace(regionId))
			{
				query = query.Where(i => i.RegionId == regionId);
			}

			if (!string.IsNullOrWhiteSpace(incidentType))
			{
				query = query.Where(i => i.IncidentType == incidentType);
			}

			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(i => i.Status == status);
			}

			if (!string.IsNullOrWhiteSpace(createdByUserId))
			{
				query = query.Where(i => i.CreatedBy == createdByUserId);
			}

			if (fromDate.HasValue)
			{
				query = query.Where(i => i.DateOfIncident >= fromDate.Value);
			}

			if (toDate.HasValue)
			{
				var exclusiveEndDate = GetExclusiveEndDate(toDate.Value);
				query = query.Where(i => i.DateOfIncident < exclusiveEndDate);
			}

			if (!string.IsNullOrWhiteSpace(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(i =>
					i.StoreName.ToLower().Contains(searchLower) ||
					i.StaffMemberName.ToLower().Contains(searchLower) ||
					(i.Description != null && i.Description.ToLower().Contains(searchLower)) ||
					(i.IncidentType != null && i.IncidentType.ToLower().Contains(searchLower)) ||
					(i.Customer != null && i.Customer.CompanyName.ToLower().Contains(searchLower)));
			}

			return query;
		}

		public async Task<Incident> CreateAsync(Incident incident)
		{
			// Validate foreign keys before saving
			var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == incident.CustomerId);
			if (!customerExists)
			{
				throw new InvalidOperationException($"Customer with ID {incident.CustomerId} does not exist.");
			}

			// Validate CreatedBy if provided
			if (!string.IsNullOrEmpty(incident.CreatedBy))
			{
				var userExists = await _context.ApplicationUsers.AnyAsync(u => u.Id == incident.CreatedBy);
				if (!userExists)
				{
					throw new InvalidOperationException($"User with ID {incident.CreatedBy} does not exist.");
				}
			}

			incident.CreatedAt = DateTime.UtcNow;
			incident.DateInputted = DateTime.UtcNow;
			_context.Incidents.Add(incident);
			await _context.SaveChangesAsync();
			return incident;
		}

		public async Task<Incident> UpdateAsync(Incident incident)
		{
			// Validate foreign keys before saving
			var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == incident.CustomerId);
			if (!customerExists)
			{
				throw new InvalidOperationException($"Customer with ID {incident.CustomerId} does not exist.");
			}

			// Validate UpdatedBy if provided
			if (!string.IsNullOrEmpty(incident.UpdatedBy))
			{
				var userExists = await _context.ApplicationUsers.AnyAsync(u => u.Id == incident.UpdatedBy);
				if (!userExists)
				{
					throw new InvalidOperationException($"User with ID {incident.UpdatedBy} does not exist.");
				}
			}

			// Validate CreatedBy if provided (shouldn't change on update, but verify)
			if (!string.IsNullOrEmpty(incident.CreatedBy))
			{
				var userExists = await _context.ApplicationUsers.AnyAsync(u => u.Id == incident.CreatedBy);
				if (!userExists)
				{
					throw new InvalidOperationException($"User with ID {incident.CreatedBy} does not exist.");
				}
			}

			incident.UpdatedAt = DateTime.UtcNow;
			_context.Incidents.Update(incident);
			await _context.SaveChangesAsync();
			return incident;
		}

		public async Task<bool> DeleteAsync(int incidentId)
		{
			var incident = await _context.Incidents
				.Include(i => i.StolenItems)
				.FirstOrDefaultAsync(i => i.IncidentId == incidentId && !i.RecordIsDeletedYN);

			if (incident == null)
			{
				return false;
			}

			// Soft delete: mark as deleted
			incident.RecordIsDeletedYN = true;
			incident.UpdatedAt = DateTime.UtcNow;

			// Also soft delete related stolen items
			foreach (var item in incident.StolenItems)
			{
				// Note: StolenItem doesn't have RecordIsDeletedYN, so we'll just remove them
				// If you want soft delete for items too, add the field to StolenItem model
				_context.StolenItems.Remove(item);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(int incidentId)
		{
			return await _context.Incidents
				.AnyAsync(i => i.IncidentId == incidentId && !i.RecordIsDeletedYN);
		}

		public async Task<List<Incident>> GetAllForStatsAsync(
			int? customerId = null,
			IReadOnlyCollection<int>? customerIds = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? startDate = null,
			DateTime? endDate = null)
		{
			var query = ReadOnlyIncidents()
				.Include(i => i.StolenItems)
				.Include(i => i.Customer)
				.Where(i => !i.RecordIsDeletedYN)
				.AsQueryable();

			if (customerIds is { Count: > 0 })
			{
				query = query.Where(i => customerIds.Contains(i.CustomerId));
			}
			else if (customerId.HasValue)
			{
				query = query.Where(i => i.CustomerId == customerId.Value);
			}

			if (!string.IsNullOrWhiteSpace(siteId))
			{
				query = query.Where(i => i.SiteId == siteId);
			}

			if (!string.IsNullOrWhiteSpace(regionId))
			{
				query = query.Where(i => i.RegionId == regionId);
			}

			if (startDate.HasValue)
			{
				query = query.Where(i => i.DateOfIncident >= startDate.Value);
			}

			if (endDate.HasValue)
			{
				query = query.Where(i => i.DateOfIncident <= endDate.Value);
			}

			return await query.ToListAsync();
		}

		public async Task<(List<RepeatOffenderRepositoryResult> Results, int TotalCount)> SearchRepeatOffendersAsync(RepeatOffenderSearchFilter filter)
		{
			var normalizedPage = filter.Page < 1 ? 1 : filter.Page;
			var normalizedPageSize = filter.PageSize < 1 ? 10 : filter.PageSize;
			normalizedPageSize = normalizedPageSize > 50 ? 50 : normalizedPageSize;

			var query = _context.Incidents
				.Where(i => !i.RecordIsDeletedYN && !string.IsNullOrEmpty(i.OffenderName))
				.AsQueryable();

			if (filter.AccessibleCustomerIds.Count > 0)
			{
				query = query.Where(i => filter.AccessibleCustomerIds.Contains(i.CustomerId));
			}

			if (filter.AccessibleSiteIds.Count > 0)
			{
				query = query.Where(i => i.SiteId != null && filter.AccessibleSiteIds.Contains(i.SiteId));
			}

			if (!string.IsNullOrWhiteSpace(filter.Name))
			{
				var nameLower = filter.Name.ToLower();
				query = query.Where(i => i.OffenderName != null && i.OffenderName.ToLower().Contains(nameLower));
			}

			if (filter.DateOfBirth.HasValue)
			{
				var dob = filter.DateOfBirth.Value.Date;
				query = query.Where(i => i.OffenderDOB.HasValue && i.OffenderDOB.Value.Date == dob);
			}

			if (!string.IsNullOrWhiteSpace(filter.Marks))
			{
				var marksLower = filter.Marks.ToLower();
				query = query.Where(i => i.OffenderMarks != null && i.OffenderMarks.ToLower().Contains(marksLower));
			}

			var groupedQuery = query.GroupBy(i => new { i.OffenderName, i.OffenderDOB, i.OffenderMarks });

			var totalGroups = await groupedQuery.CountAsync();

			var results = await groupedQuery
				.OrderByDescending(g => g.Count())
				.ThenByDescending(g => g.Max(i => i.DateOfIncident))
				.Skip((normalizedPage - 1) * normalizedPageSize)
				.Take(normalizedPageSize)
				.Select(g => new RepeatOffenderRepositoryResult
				{
					OffenderName = g.Key.OffenderName ?? string.Empty,
					OffenderDOB = g.Key.OffenderDOB,
					Gender = g.Select(i => i.Gender).FirstOrDefault(),
					OffenderMarks = g.Key.OffenderMarks,
					OffenderPlaceOfBirth = g.Select(i => i.OffenderPlaceOfBirth).FirstOrDefault(),
					HouseName = g.Select(i => i.OffenderHouseName).FirstOrDefault(),
					NumberAndStreet = g.Select(i => i.OffenderNumberAndStreet).FirstOrDefault(),
					VillageOrSuburb = g.Select(i => i.OffenderVillageOrSuburb).FirstOrDefault(),
					Town = g.Select(i => i.OffenderTown).FirstOrDefault(),
					County = g.Select(i => i.OffenderCounty).FirstOrDefault(),
					PostCode = g.Select(i => i.OffenderPostCode).FirstOrDefault(),
					IncidentCount = g.Count(),
					RecentIncidents = g
						.OrderByDescending(i => i.DateOfIncident)
						.ThenByDescending(i => i.CreatedAt)
						.Take(5)
						.Select(i => new RepeatOffenderRepositoryIncident
						{
							IncidentId = i.IncidentId,
							DateOfIncident = i.DateOfIncident,
							StoreName = i.StoreName,
							IncidentType = i.IncidentType,
							Description = i.Description,
							OffenderMarks = i.OffenderMarks,
							OffenderDetailsVerified = i.OffenderDetailsVerified,
							VerificationMethod = i.VerificationMethod,
							VerificationEvidenceImage = i.VerificationEvidenceImage
						})
						.ToList()
				})
				.ToListAsync();

			return (results, totalGroups);
		}

		public async Task<List<Incident>> GetIncidentsWithDetailsAsync(CrimeIntelligenceQueryDto queryDto)
		{
			var query = ReadOnlyIncidents()
				.Include(i => i.StolenItems)
				.Where(i => !i.RecordIsDeletedYN && i.CustomerId == queryDto.CustomerId)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(queryDto.SiteId))
			{
				query = query.Where(i => i.SiteId == queryDto.SiteId);
			}

			if (!string.IsNullOrWhiteSpace(queryDto.RegionId))
			{
				query = query.Where(i => i.RegionId == queryDto.RegionId);
			}

			if (queryDto.StartDate.HasValue)
			{
				query = query.Where(i => i.DateOfIncident >= queryDto.StartDate.Value);
			}

			if (queryDto.EndDate.HasValue)
			{
				var exclusiveEndDate = GetExclusiveEndDate(queryDto.EndDate.Value);
				query = query.Where(i => i.DateOfIncident < exclusiveEndDate);
			}

			return await query.ToListAsync();
		}

		public async Task<List<Incident>> GetIncidentsNeedingClassificationAsync(int limit)
		{
			if (limit <= 0)
			{
				return new List<Incident>();
			}

			// Pick up:
			//   1. Rows missing any AI field (never classified or partially saved).
			//   2. Rows tagged with a superseded classifier version. v1 LLM output
			//      was sometimes incoherent (e.g. "Risk: Low 70/100"); the v1 and
			//      v2 rule-based scorers used a type-baseline that produced
			//      implausible scores (e.g. "Attempted Shoplifting" with no loss
			//      bucketed as Medium 45/100); and the v3 scorer applied the
			//      stage dampener whenever the type matched "Deter/Attempted/
			//      Prevent" regardless of whether a real loss was recorded, so
			//      "Deter + £500 lost" was suppressed to Low 38/100 instead of
			//      registering as High. None of these look untrustworthy on the
			//      surface but they are under the v4 formula.
			//
			// Deliberately NOT picked up:
			//   - "rule-based-fallback (azure-error)" / "rule-based-fallback (disabled)"
			//     rows. They had one fallback pass and we do not want a busy-retry
			//     loop while Azure OpenAI is down. An admin can manually re-trigger
			//     once Azure is back up if needed.
			//
			// Deliberately picked up:
			//   - "rule-based-fallback (inline-timeout)" rows. These were set by the
			//     inline Create/Update path when the LLM exceeded the 750ms response
			//     budget. The LLM may have been healthy but momentarily slow, so the
			//     periodic loop is the right place to refine these once load eases.
			//   - "rule-based-fallback (inline-error)" rows. The Azure wrapper already
			//     catches LLM errors and returns its own (azure-error) result without
			//     throwing, so an exception reaching the inline handler is an
			//     unexpected/transient condition (cancellation, transient infra, bug
			//     in a downstream dependency). Re-trying via the periodic loop is
			//     cheap and lets the row recover once the underlying issue clears;
			//     leaving it out strands incidents on a first fallback pass forever.
			//
			// Ordered newest-first so the recent-incidents dashboard fills in
			// before the deep history.
			return await _context.Incidents
				.Include(i => i.StolenItems)
				.Where(i => !i.RecordIsDeletedYN &&
					(i.IncidentCategory == null
						|| i.RiskLevel == null
						|| i.RiskScore == null
						|| i.ClassificationVersion == null
						|| i.ClassificationVersion == "rule-based-v1"
						|| i.ClassificationVersion == "rule-based-v2"
						|| i.ClassificationVersion == "rule-based-v3"
						|| i.ClassificationVersion == "azure-openai-v1"
						|| i.ClassificationVersion == "rule-based-fallback (inline-timeout)"
						|| i.ClassificationVersion == "rule-based-fallback (inline-error)"))
				.OrderByDescending(i => i.IncidentId)
				.Take(limit)
				.ToListAsync();
		}

		private static DateTime GetExclusiveEndDate(DateTime endDate)
		{
			return endDate.Date.AddDays(1);
		}
	}
}
