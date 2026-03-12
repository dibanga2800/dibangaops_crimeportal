#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// AI-focused analytics endpoints (pattern detection and risk scoring).
	/// These wrap the underlying analytics and risk services and are intended
	/// for consumption by the Crime Portal dashboards.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Policy = "ManagerAndAbove")]
	public class AiAnalyticsController : ControllerBase
	{
		private readonly IIncidentPatternService _patternService;
		private readonly IRiskScoringService _riskScoringService;
		private readonly ILogger<AiAnalyticsController> _logger;

		public AiAnalyticsController(
			IIncidentPatternService patternService,
			IRiskScoringService riskScoringService,
			ILogger<AiAnalyticsController> logger)
		{
			_patternService = patternService;
			_riskScoringService = riskScoringService;
			_logger = logger;
		}

		/// <summary>
		/// Get an incident pattern summary (hot locations, trends, category breakdown)
		/// for the specified filters. This is a lightweight wrapper over the existing
		/// incident analytics engine.
		/// </summary>
		[HttpGet("patterns")]
		public async Task<ActionResult<IncidentPatternSummaryDto>> GetPatterns(
			[FromQuery] int? customerId = null,
			[FromQuery] string? siteId = null,
			[FromQuery] string? regionId = null,
			[FromQuery] DateTime? from = null,
			[FromQuery] DateTime? to = null,
			CancellationToken cancellationToken = default)
		{
			var result = await _patternService.GetPatternSummaryAsync(
				customerId,
				siteId,
				regionId,
				from,
				to,
				cancellationToken);

			return Ok(result);
		}

		/// <summary>
		/// Get risk scores for all stores belonging to a single customer on a specific date.
		/// Defaults to today's date when not provided.
		/// </summary>
		[HttpGet("risk-scores")]
		public async Task<ActionResult<IReadOnlyList<StoreRiskScoreDto>>> GetRiskScores(
			[FromQuery] int customerId,
			[FromQuery] DateTime? date = null,
			CancellationToken cancellationToken = default)
		{
			if (customerId <= 0)
			{
				return BadRequest(new { message = "customerId is required and must be greater than zero." });
			}

			var targetDate = (date ?? DateTime.UtcNow).Date;
			var scores = await _riskScoringService.GetCustomerRiskScoresAsync(customerId, targetDate, cancellationToken);

			_logger.LogInformation("AI risk scores generated for customer {CustomerId} on {Date}: {Count} stores",
				customerId, targetDate.ToString("yyyy-MM-dd"), scores.Count);

			return Ok(scores);
		}
	}
}

