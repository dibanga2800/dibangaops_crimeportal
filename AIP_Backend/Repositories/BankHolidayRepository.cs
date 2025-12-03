using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Repositories
{
	public class BankHolidayRepository : IBankHolidayRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<BankHolidayRepository> _logger;

		public BankHolidayRepository(ApplicationDbContext context, ILogger<BankHolidayRepository> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<(IEnumerable<BankHoliday> Holidays, int Total)> GetPagedAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			int page = 1,
			int limit = 10)
		{
			try
			{
				var query = _context.BankHolidays
					.AsQueryable();

				// Note: Search by officer name removed (Employee navigation deleted)
				// Search can be added back using ApplicationUser lookup if needed

				if (!string.IsNullOrWhiteSpace(status))
				{
					var normalizedStatus = status.ToLower();
					query = query.Where(bh => bh.Status.ToLower() == normalizedStatus);
				}

				if (archived.HasValue)
				{
					query = query.Where(bh => bh.Archived == archived.Value);
				}

				var total = await query.CountAsync();

				var holidays = await query
					.OrderByDescending(bh => bh.HolidayDate)
					.ThenByDescending(bh => bh.DateOfRequest)
					.Skip((page - 1) * limit)
					.Take(limit)
					.ToListAsync();

				return (holidays, total);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving bank holidays");
				throw;
			}
		}

		public async Task<BankHoliday?> GetByIdAsync(int id)
		{
			try
			{
				return await _context.BankHolidays
					.FirstOrDefaultAsync(bh => bh.Id == id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving bank holiday with ID {Id}", id);
				throw;
			}
		}

		public async Task<BankHoliday> CreateAsync(BankHoliday bankHoliday)
		{
			try
			{
				_context.BankHolidays.Add(bankHoliday);
				await _context.SaveChangesAsync();
				return bankHoliday;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating bank holiday");
				throw;
			}
		}

		public async Task<BankHoliday> UpdateAsync(BankHoliday bankHoliday)
		{
			try
			{
				_context.BankHolidays.Update(bankHoliday);
				await _context.SaveChangesAsync();
				return bankHoliday;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating bank holiday with ID {Id}", bankHoliday.Id);
				throw;
			}
		}

		public async Task<bool> DeleteAsync(int id)
		{
			try
			{
				var entity = await _context.BankHolidays.FindAsync(id);
				if (entity == null)
				{
					return false;
				}

				_context.BankHolidays.Remove(entity);
				await _context.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting bank holiday with ID {Id}", id);
				throw;
			}
		}
	}
}

