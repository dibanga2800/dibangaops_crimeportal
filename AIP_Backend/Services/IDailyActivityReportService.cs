#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service interface for DailyActivityReport operations
	/// </summary>
	public interface IDailyActivityReportService
	{
		Task<DailyActivityReportResponseDto> GetByIdAsync(string id);
		Task<DailyActivityReportsResponseDto> GetReportsAsync(DailyActivityReportQueryDto query);
		Task<DailyActivityReportResponseDto> CreateAsync(DailyActivityReportRequestDto dto, string? userId = null);
		Task<DailyActivityReportResponseDto> UpdateAsync(string id, DailyActivityReportRequestDto dto, string? userId = null);
		Task<bool> DeleteAsync(string id);
	}
}
