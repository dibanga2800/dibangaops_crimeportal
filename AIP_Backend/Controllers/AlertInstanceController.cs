#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
	[ApiController]
	[Route("api/alerts")]
	[Authorize]
	public class AlertInstanceController : ControllerBase
	{
		private readonly IAlertEscalationService _alertService;
		private readonly ILogger<AlertInstanceController> _logger;

		public AlertInstanceController(
			IAlertEscalationService alertService,
			ILogger<AlertInstanceController> logger)
		{
			_alertService = alertService;
			_logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult<AlertInstanceListResponseDto>> GetAlerts(
			[FromQuery] string? status,
			[FromQuery] string? severity,
			[FromQuery] int? customerId,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 20)
		{
			var result = await _alertService.GetAlertsAsync(status, severity, customerId, page, pageSize);
			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<AlertInstanceDto>> GetAlert(int id)
		{
			try
			{
				var result = await _alertService.GetByIdAsync(id);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
		}

		[HttpGet("summary")]
		public async Task<ActionResult<AlertSummaryDto>> GetSummary([FromQuery] int? customerId)
		{
			var result = await _alertService.GetSummaryAsync(customerId);
			return Ok(result);
		}

		[HttpPatch("{id}/acknowledge")]
		public async Task<ActionResult<AlertInstanceDto>> Acknowledge(int id, [FromBody] AcknowledgeAlertDto? dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			try
			{
				var result = await _alertService.AcknowledgeAsync(id, userId, dto);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPatch("{id}/escalate")]
		public async Task<ActionResult<AlertInstanceDto>> Escalate(int id, [FromBody] EscalateAlertDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			try
			{
				var result = await _alertService.EscalateAsync(id, userId, dto);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPatch("{id}/resolve")]
		public async Task<ActionResult<AlertInstanceDto>> Resolve(int id, [FromBody] ResolveAlertDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			try
			{
				var result = await _alertService.ResolveAsync(id, userId, dto);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
		}
	}
}
