#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service interface for Daily Occurrence Book operations
	/// </summary>
	public interface IDailyOccurrenceBookService
	{
		Task<DailyOccurrenceBookListResponseDto> GetOccurrencesAsync(int customerId, DailyOccurrenceBookFilterDto? filters = null);
		Task<DailyOccurrenceBookResponseDto> GetByIdAsync(int customerId, string occurrenceId);
		Task<DailyOccurrenceBookResponseDto> CreateAsync(CreateDailyOccurrenceBookRequestDto request, string currentUserId);
		Task<DailyOccurrenceBookResponseDto> UpdateAsync(int customerId, string occurrenceId, UpdateDailyOccurrenceBookRequestDto request, string currentUserId);
		Task<bool> DeleteAsync(int customerId, string occurrenceId, string currentUserId);
		Task<DailyOccurrenceBookStatsDto> GetStatsAsync(int customerId, DailyOccurrenceBookFilterDto? filters = null);
	}
}

