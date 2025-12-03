using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	public interface IBankHolidayService
	{
		Task<BankHolidayListResponseDto> GetPagedAsync(string? search, string? status, bool? archived, int page, int limit);
		Task<BankHolidayDto> GetByIdAsync(int id);
		Task<BankHolidayDto> CreateAsync(CreateBankHolidayDto dto, string currentUserId);
		Task<BankHolidayDto> UpdateAsync(int id, UpdateBankHolidayDto dto, string currentUserId);
		Task<bool> DeleteAsync(int id);
		Task<BankHolidayDto> ArchiveAsync(int id, string currentUserId);
		Task<BankHolidayDto> UnarchiveAsync(int id, string currentUserId);
	}
}

