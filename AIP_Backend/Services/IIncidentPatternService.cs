#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Abstraction for higher-level incident pattern detection and hotspot analysis.
	/// This service is optimised for dashboards and reports that need
	/// store, time and category level summaries computed from historical data.
	/// </summary>
	public interface IIncidentPatternService
	{
		/// <summary>
		/// Build an aggregated incident pattern view for the specified slice
		/// of data (optionally constrained by customer / site / region and date range).
		/// Implementations are free to call ML.NET models, cached aggregates or
		/// external analytics engines.
		/// </summary>
		Task<IncidentPatternSummaryDto> GetPatternSummaryAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? from = null,
			DateTime? to = null,
			CancellationToken cancellationToken = default);
	}
}

