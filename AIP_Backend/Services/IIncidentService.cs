#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service interface for Incident operations
	/// </summary>
	public interface IIncidentService
	{
		Task<IncidentResponseDto> GetByIdAsync(string id);
		Task<IncidentsResponseDto> GetIncidentsAsync(GetIncidentsQueryDto query);
		Task<IncidentResponseDto> CreateAsync(UpsertIncidentDto dto, string? userId = null);
		Task<IncidentResponseDto> UpdateAsync(string id, UpsertIncidentDto dto, string? userId = null);
		Task<bool> DeleteAsync(string id);
		Task<List<IncidentDto>> GetAllForStatsAsync(int? customerId = null, string? siteId = null, string? regionId = null);
		Task<RepeatOffenderSearchResponseDto> SearchRepeatOffendersAsync(RepeatOffenderSearchQueryDto query);
		Task<CrimeIntelligenceResponseDto> GetCrimeInsightsAsync(CrimeIntelligenceQueryDto query);
	}
}

