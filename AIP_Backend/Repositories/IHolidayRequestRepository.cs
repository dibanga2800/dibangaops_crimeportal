using AIPBackend.Models;

namespace AIPBackend.Repositories
{
	public interface IHolidayRequestRepository
	{
		Task<HolidayRequest?> GetByIdAsync(int id);
		Task<IEnumerable<HolidayRequest>> GetAllAsync();
		Task<IEnumerable<HolidayRequest>> GetByEmployeeIdAsync(int employeeId);
		Task<IEnumerable<HolidayRequest>> GetByStatusAsync(string status);
		Task<IEnumerable<HolidayRequest>> GetFilteredAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			DateTime? startDateFrom = null,
			DateTime? startDateTo = null,
			int? employeeId = null,
			int page = 1,
			int limit = 10);
		Task<int> GetTotalCountAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			DateTime? startDateFrom = null,
			DateTime? startDateTo = null,
			int? employeeId = null);
		Task<HolidayRequest> CreateAsync(HolidayRequest holidayRequest);
		Task<HolidayRequest> UpdateAsync(HolidayRequest holidayRequest);
		Task<bool> DeleteAsync(int id);
		Task<bool> ExistsAsync(int id);
	}
}

