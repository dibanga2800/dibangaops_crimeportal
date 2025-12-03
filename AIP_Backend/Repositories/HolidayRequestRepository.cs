using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Repositories
{
	public class HolidayRequestRepository : IHolidayRequestRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<HolidayRequestRepository> _logger;

		public HolidayRequestRepository(
			ApplicationDbContext context,
			ILogger<HolidayRequestRepository> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<HolidayRequest?> GetByIdAsync(int id)
		{
			try
			{
				return await _context.HolidayRequests
					.Include(hr => hr.AuthorisedByUser)
					.Include(hr => hr.CreatedByUser)
					.Include(hr => hr.UpdatedByUser)
					.FirstOrDefaultAsync(hr => hr.Id == id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving holiday request with ID {Id}", id);
				throw;
			}
		}

		public async Task<IEnumerable<HolidayRequest>> GetAllAsync()
		{
			try
			{
				return await _context.HolidayRequests
					.Include(hr => hr.AuthorisedByUser)
					.OrderByDescending(hr => hr.DateOfRequest)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving all holiday requests");
				throw;
			}
		}

		public async Task<IEnumerable<HolidayRequest>> GetByEmployeeIdAsync(int employeeId)
		{
			try
			{
				return await _context.HolidayRequests
					.Include(hr => hr.AuthorisedByUser)
					.Where(hr => hr.EmployeeId == employeeId)
					.OrderByDescending(hr => hr.DateOfRequest)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving holiday requests for employee {EmployeeId}", employeeId);
				throw;
			}
		}

		public async Task<IEnumerable<HolidayRequest>> GetByStatusAsync(string status)
		{
			try
			{
				return await _context.HolidayRequests
					.Include(hr => hr.AuthorisedByUser)
					.Where(hr => hr.Status.ToLower() == status.ToLower())
					.OrderByDescending(hr => hr.DateOfRequest)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving holiday requests with status {Status}", status);
				throw;
			}
		}

		/// <summary>
		/// Applies filters to the holiday request query
		/// </summary>
		private IQueryable<HolidayRequest> ApplyFilters(
			IQueryable<HolidayRequest> query,
			string? search = null,
			string? status = null,
			bool? archived = null,
			DateTime? startDateFrom = null,
			DateTime? startDateTo = null,
			int? employeeId = null)
		{
			// Note: Search by employee name removed (Employee navigation deleted)
			// Search can be added back using ApplicationUser lookup if needed

			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(hr => hr.Status.ToLower() == status.ToLower());
			}

			if (archived.HasValue)
			{
				query = query.Where(hr => hr.Archived == archived.Value);
			}

			if (startDateFrom.HasValue)
			{
				query = query.Where(hr => hr.StartDate >= startDateFrom.Value);
			}

			if (startDateTo.HasValue)
			{
				query = query.Where(hr => hr.StartDate <= startDateTo.Value);
			}

			if (employeeId.HasValue)
			{
				query = query.Where(hr => hr.EmployeeId == employeeId.Value);
			}

			return query;
		}

		public async Task<IEnumerable<HolidayRequest>> GetFilteredAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			DateTime? startDateFrom = null,
			DateTime? startDateTo = null,
			int? employeeId = null,
			int page = 1,
			int limit = 10)
		{
			try
			{
				var query = _context.HolidayRequests
					.Include(hr => hr.AuthorisedByUser)
					.AsQueryable();

				query = ApplyFilters(query, search, status, archived, startDateFrom, startDateTo, employeeId);

				var skip = (page - 1) * limit;
				return await query
					.OrderByDescending(hr => hr.DateOfRequest)
					.Skip(skip)
					.Take(limit)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving filtered holiday requests");
				throw;
			}
		}

		public async Task<int> GetTotalCountAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			DateTime? startDateFrom = null,
			DateTime? startDateTo = null,
			int? employeeId = null)
		{
			try
			{
				var query = _context.HolidayRequests
					.AsQueryable();

				query = ApplyFilters(query, search, status, archived, startDateFrom, startDateTo, employeeId);

				return await query.CountAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting total count of holiday requests");
				throw;
			}
		}

		public async Task<HolidayRequest> CreateAsync(HolidayRequest holidayRequest)
		{
			try
			{
				_context.HolidayRequests.Add(holidayRequest);
				await _context.SaveChangesAsync();
				return holidayRequest;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating holiday request");
				throw;
			}
		}

	public async Task<HolidayRequest> UpdateAsync(HolidayRequest holidayRequest)
	{
		try
		{
			var entry = _context.Entry(holidayRequest);
			
			// If entity is already tracked, just save changes
			if (entry.State != EntityState.Detached)
			{
				await _context.SaveChangesAsync();
				return await GetByIdAsync(holidayRequest.Id) ?? holidayRequest;
			}

			// Entity not tracked - get from database and update
			var existingEntity = await _context.HolidayRequests
				.FirstOrDefaultAsync(hr => hr.Id == holidayRequest.Id)
				?? throw new KeyNotFoundException($"Holiday request with ID {holidayRequest.Id} not found.");

			// Update scalar properties only (avoid navigation properties)
			existingEntity.EmployeeId = holidayRequest.EmployeeId;
			existingEntity.StartDate = holidayRequest.StartDate;
			existingEntity.EndDate = holidayRequest.EndDate;
			existingEntity.ReturnToWorkDate = holidayRequest.ReturnToWorkDate;
			existingEntity.DateOfRequest = holidayRequest.DateOfRequest;
			existingEntity.AuthorisedBy = holidayRequest.AuthorisedBy;
			existingEntity.DateAuthorised = holidayRequest.DateAuthorised;
			existingEntity.Status = holidayRequest.Status;
			existingEntity.Comment = holidayRequest.Comment;
			existingEntity.Reason = holidayRequest.Reason;
			existingEntity.TotalDays = holidayRequest.TotalDays;
			existingEntity.DaysLeftYTD = holidayRequest.DaysLeftYTD;
			existingEntity.Archived = holidayRequest.Archived;
			existingEntity.UpdatedAt = holidayRequest.UpdatedAt;
			existingEntity.UpdatedBy = holidayRequest.UpdatedBy;

			await _context.SaveChangesAsync();
			return await GetByIdAsync(holidayRequest.Id) ?? existingEntity;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating holiday request with ID {Id}", holidayRequest.Id);
			throw;
		}
	}

		public async Task<bool> DeleteAsync(int id)
		{
			try
			{
				var holidayRequest = await _context.HolidayRequests.FindAsync(id);
				if (holidayRequest == null)
				{
					return false;
				}

				_context.HolidayRequests.Remove(holidayRequest);
				await _context.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting holiday request with ID {Id}", id);
				throw;
			}
		}

		public async Task<bool> ExistsAsync(int id)
		{
			try
			{
				return await _context.HolidayRequests.AnyAsync(hr => hr.Id == id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking if holiday request exists with ID {Id}", id);
				throw;
			}
		}
	}
}

