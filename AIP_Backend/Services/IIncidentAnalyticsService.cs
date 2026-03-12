#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Abstraction for AI-enriched incident analytics.
	/// Produces hot-location analysis, trend data, risk indicators, and category breakdowns.
	/// </summary>
	public interface IIncidentAnalyticsService
	{
		Task<IncidentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? from = null,
			DateTime? to = null);

		Task<AnalyticsHubDto> GetAnalyticsHubAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? from = null,
			DateTime? to = null);
	}
}
