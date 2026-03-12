#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories.Models;
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
			string? siteId = null,
			string? regionId = null,
			string? incidentType = null,
			string? status = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			string? createdByUserId = null)
		{
			var query = _context.Incidents
				.Include(i => i.StolenItems)
				.Include(i => i.Customer)
				.Where(i => !i.RecordIsDeletedYN)
				.AsQueryable();

			// Apply filters
			if (customerId.HasValue)
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

			// Apply search
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

			// Get total count before pagination
			var totalCount = await query.CountAsync();

			// Apply pagination and ordering
			var incidents = await query
				.OrderByDescending(i => i.DateOfIncident)
				.ThenByDescending(i => i.DateInputted)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (incidents, totalCount);
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
			string? siteId = null,
			string? regionId = null,
			DateTime? startDate = null,
			DateTime? endDate = null)
		{
			var query = _context.Incidents
				.Include(i => i.StolenItems)
				.Include(i => i.Customer)
				.Where(i => !i.RecordIsDeletedYN)
				.AsQueryable();

			if (customerId.HasValue)
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
			var query = _context.Incidents
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

		private static DateTime GetExclusiveEndDate(DateTime endDate)
		{
			return endDate.Date.AddDays(1);
		}
	}
}

