using AIPBackend.Models;

namespace AIPBackend.Repositories
{
	public interface IBankHolidayRepository
	{
		Task<(IEnumerable<BankHoliday> Holidays, int Total)> GetPagedAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			int page = 1,
			int limit = 10);

		Task<BankHoliday?> GetByIdAsync(int id);
		Task<BankHoliday> CreateAsync(BankHoliday bankHoliday);
		Task<BankHoliday> UpdateAsync(BankHoliday bankHoliday);
		Task<bool> DeleteAsync(int id);
	}
}

