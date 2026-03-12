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
			var context = _userContext.GetCurrentContext();

			// Apply the same customer/site scoping rules used by IncidentService
			if (!context.IsAdministrator)
			{
				if (context.IsCustomer && context.CustomerId.HasValue)
				{
					customerId = context.CustomerId.Value;
				}

				if (context.AccessibleCustomerIds.Count > 0)
				{
					if (customerId.HasValue && !context.AccessibleCustomerIds.Contains(customerId.Value))
					{
						customerId = context.AccessibleCustomerIds.First();
					}
					else if (!customerId.HasValue)
					{
						customerId = context.AccessibleCustomerIds.First();
					}
				}

				if (context.AccessibleSiteIds.Count > 0)
				{
					if (!string.IsNullOrWhiteSpace(siteId) && !context.AccessibleSiteIds.Contains(siteId))
					{
						siteId = context.AccessibleSiteIds.First();
					}
					else if (string.IsNullOrWhiteSpace(siteId))
					{
						siteId = context.AccessibleSiteIds.First();
					}
				}
			}

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
				var context = _userContext.GetCurrentContext();

				if (!context.IsAdministrator)
				{
					if (context.IsCustomer && context.CustomerId.HasValue)
					{
						customerId = context.CustomerId.Value;
					}

					if (context.AccessibleCustomerIds.Count > 0)
					{
						if (customerId.HasValue && !context.AccessibleCustomerIds.Contains(customerId.Value))
						{
							customerId = context.AccessibleCustomerIds.First();
						}
						else if (!customerId.HasValue)
						{
							customerId = context.AccessibleCustomerIds.First();
						}
					}

					if (context.AccessibleSiteIds.Count > 0)
					{
						if (!string.IsNullOrWhiteSpace(siteId) && !context.AccessibleSiteIds.Contains(siteId))
						{
							siteId = context.AccessibleSiteIds.First();
						}
						else if (string.IsNullOrWhiteSpace(siteId))
						{
							siteId = context.AccessibleSiteIds.First();
						}
					}
				}

				var result = await _analyticsService.GetAnalyticsHubAsync(customerId, siteId, regionId, from, to);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating analytics hub for customer {CustomerId}", customerId);
				return StatusCode(500, new { message = "Failed to generate analytics data." });
			}
		}
	}
}
