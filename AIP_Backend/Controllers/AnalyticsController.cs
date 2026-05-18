#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class AnalyticsController : ControllerBase
	{
		private readonly IIncidentAnalyticsService _analyticsService;
		private readonly ILogger<AnalyticsController> _logger;
		private readonly IUserContextService _userContext;

		public AnalyticsController(
			IIncidentAnalyticsService analyticsService,
			ILogger<AnalyticsController> logger,
			IUserContextService userContext)
		{
			_analyticsService = analyticsService;
			_logger = logger;
			_userContext = userContext;
		}

		[HttpGet("summary")]
		public async Task<ActionResult<IncidentAnalyticsSummaryDto>> GetSummary(
			[FromQuery] int? customerId,
			[FromQuery] string? siteId,
			[FromQuery] string? regionId,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to)
		{
			(customerId, siteId) = ResolveScopedFilters(customerId, siteId);

			var result = await _analyticsService.GetAnalyticsSummaryAsync(customerId, siteId, regionId, from, to);
			return Ok(result);
		}

		[HttpGet("hub")]
		public async Task<ActionResult<AnalyticsHubDto>> GetHub(
			[FromQuery] int? customerId,
			[FromQuery] string? siteId,
			[FromQuery] string? regionId,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to)
		{
			try
			{
				(customerId, siteId) = ResolveScopedFilters(customerId, siteId);

				var result = await _analyticsService.GetAnalyticsHubAsync(customerId, siteId, regionId, from, to);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating analytics hub for customer {CustomerId}", customerId);
				return StatusCode(500, new { message = "Failed to generate analytics data." });
			}
		}

		private (int? CustomerId, string? SiteId) ResolveScopedFilters(int? customerId, string? siteId)
		{
			var filter = _userContext.ResolveCustomerFilter(customerId);
			var resolvedCustomerId = filter.SingleCustomerId
				?? (filter.CustomerIds.Count > 0 ? filter.CustomerIds.First() : customerId);
			var resolvedSiteId = _userContext.ResolveSiteFilter(siteId);
			return (resolvedCustomerId, resolvedSiteId);
		}
	}
}
