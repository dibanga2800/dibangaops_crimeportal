#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Thin adapter over <see cref="IIncidentAnalyticsService"/> to expose a simplified
	/// pattern summary contract for other services and dashboards.
	///
	/// The heavy analytics (and any future ML.NET models) live inside
	/// <see cref="IncidentAnalyticsService"/>; this service keeps the AI
	/// surface area small and focused.
	/// </summary>
	public class IncidentPatternService : IIncidentPatternService
	{
		private readonly IIncidentAnalyticsService _analyticsService;

		public IncidentPatternService(IIncidentAnalyticsService analyticsService)
		{
			_analyticsService = analyticsService;
		}

		public async Task<IncidentPatternSummaryDto> GetPatternSummaryAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? from = null,
			DateTime? to = null,
			CancellationToken cancellationToken = default)
		{
			// Re-use the existing analytics summary which already computes
			// hot locations, trends and category breakdowns.
			var summary = await _analyticsService.GetAnalyticsSummaryAsync(
				customerId,
				siteId,
				regionId,
				from,
				to);

			return new IncidentPatternSummaryDto
			{
				HotLocations = summary.HotLocations,
				IncidentTrend = summary.IncidentTrend,
				CategoryBreakdown = summary.CategoryBreakdown,
				GeneratedAt = summary.GeneratedAt
			};
		}
	}
}

