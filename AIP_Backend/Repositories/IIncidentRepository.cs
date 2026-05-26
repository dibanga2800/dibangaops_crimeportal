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
			IReadOnlyCollection<int>? customerIds = null,
			string? siteId = null,
			string? regionId = null,
			string? incidentType = null,
			string? status = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			string? createdByUserId = null);

		Task<IncidentListSummaryDto> GetSummaryAsync(
			string? search = null,
			int? customerId = null,
			IReadOnlyCollection<int>? customerIds = null,
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
			IReadOnlyCollection<int>? customerIds = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? startDate = null,
			DateTime? endDate = null);
		Task<(List<RepeatOffenderRepositoryResult> Results, int TotalCount)> SearchRepeatOffendersAsync(RepeatOffenderSearchFilter filter);
		Task<List<Incident>> GetIncidentsWithDetailsAsync(CrimeIntelligenceQueryDto query);

		/// <summary>
		/// Returns incidents that need a (re-)classification pass. Picks up rows where
		/// any AI field is null AND rows persisted by superseded classifier versions
		/// (e.g. <c>rule-based-v1</c>, <c>azure-openai-v1</c>) which are known to have
		/// produced incoherent (level, score) pairs. Results are ordered newest-first
		/// so the visible dashboard heals before the deep history. Rows tagged with
		/// <c>rule-based-fallback (...)</c> are intentionally left alone - they had
		/// one fallback pass and we do not want a busy-retry loop while Azure is down.
		/// Used by the backfill background service and the admin reclassify endpoint.
		/// </summary>
		Task<List<Incident>> GetIncidentsNeedingClassificationAsync(int limit);
	}
}

