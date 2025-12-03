#nullable enable

using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories.Models;

namespace AIPBackend.Repositories
{
	/// <summary>
	/// Repository interface for Incident operations
	/// </summary>
	public interface IIncidentRepository
	{
		Task<Incident?> GetByIdAsync(int incidentId);
		Task<Incident?> GetByIdWithItemsAsync(int incidentId);
		Task<(List<Incident> Incidents, int TotalCount)> GetPagedAsync(
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
			string? createdByUserId = null);
		Task<Incident> CreateAsync(Incident incident);
		Task<Incident> UpdateAsync(Incident incident);
		Task<bool> DeleteAsync(int incidentId);
		Task<bool> ExistsAsync(int incidentId);
		Task<List<Incident>> GetAllForStatsAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? startDate = null,
			DateTime? endDate = null);
		Task<(List<RepeatOffenderRepositoryResult> Results, int TotalCount)> SearchRepeatOffendersAsync(RepeatOffenderSearchFilter filter);
		Task<List<Incident>> GetIncidentsWithDetailsAsync(CrimeIntelligenceQueryDto query);
	}
}

