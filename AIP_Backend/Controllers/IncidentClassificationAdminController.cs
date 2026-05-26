#nullable enable

using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// Admin-only endpoints for managing AI classification state on incidents.
	/// Lives under the <c>/api/admin/incidents</c> path to keep operational tooling
	/// separate from the regular <see cref="IncidentController"/> surface.
	/// </summary>
	[ApiController]
	[Route("api/admin/incidents")]
	[Authorize(Policy = "AdminOnly")]
	public sealed class IncidentClassificationAdminController : ControllerBase
	{
		private readonly IIncidentClassificationBackfillService _backfill;
		private readonly IOptionsMonitor<IncidentClassificationBackfillOptions> _optionsMonitor;
		private readonly ILogger<IncidentClassificationAdminController> _logger;

		public IncidentClassificationAdminController(
			IIncidentClassificationBackfillService backfill,
			IOptionsMonitor<IncidentClassificationBackfillOptions> optionsMonitor,
			ILogger<IncidentClassificationAdminController> logger)
		{
			_backfill = backfill;
			_optionsMonitor = optionsMonitor;
			_logger = logger;
		}

		/// <summary>
		/// Triggers a one-off backfill pass that classifies up to <paramref name="batchSize"/>
		/// incidents whose AI fields are missing. Hard-capped at 200 per call to keep the
		/// endpoint cheap and avoid abusive payloads.
		/// </summary>
		[HttpPost("classify-missing")]
		[ProducesResponseType(typeof(ClassifyMissingResponse), StatusCodes.Status200OK)]
		public async Task<ActionResult<ClassifyMissingResponse>> ClassifyMissing(
			[FromQuery] int? batchSize,
			CancellationToken cancellationToken)
		{
			var configuredBatch = _optionsMonitor.CurrentValue.BatchSize;
			var effectiveBatch = batchSize ?? configuredBatch;
			if (effectiveBatch <= 0)
			{
				effectiveBatch = configuredBatch > 0 ? configuredBatch : 25;
			}
			effectiveBatch = Math.Min(effectiveBatch, 200);

			_logger.LogInformation(
				"Admin-triggered classification backfill: batchSize={BatchSize}",
				effectiveBatch);

			var processed = await _backfill.RunAsync(effectiveBatch, cancellationToken);

			return Ok(new ClassifyMissingResponse
			{
				ProcessedCount = processed,
				RequestedBatchSize = effectiveBatch
			});
		}

		public sealed class ClassifyMissingResponse
		{
			public int ProcessedCount { get; set; }
			public int RequestedBatchSize { get; set; }
		}
	}
}
