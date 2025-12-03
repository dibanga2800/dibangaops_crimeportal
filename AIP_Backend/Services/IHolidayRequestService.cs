using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	public interface IHolidayRequestService
	{
		Task<AIPBackend.Models.DTOs.HolidayRequestDto> GetByIdAsync(int id);
		Task<HolidayRequestListResponseDto> GetAllAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			int page = 1,
			int limit = 10);
		Task<AIPBackend.Models.DTOs.HolidayRequestDto> CreateAsync(CreateHolidayRequestDto request, string currentUserId);
		Task<AIPBackend.Models.DTOs.HolidayRequestDto> UpdateAsync(int id, UpdateHolidayRequestDto request, string currentUserId);
		Task<bool> DeleteAsync(int id);
		Task<AIPBackend.Models.DTOs.HolidayRequestDto> ArchiveAsync(int id, string currentUserId);
		Task<AIPBackend.Models.DTOs.HolidayRequestDto> UnarchiveAsync(int id, string currentUserId);
	}
}

