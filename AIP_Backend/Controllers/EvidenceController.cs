#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class EvidenceController : ControllerBase
	{
		private readonly IEvidenceService _evidenceService;
		private readonly IIncidentService _incidentService;
		private readonly ILogger<EvidenceController> _logger;

		public EvidenceController(
			IEvidenceService evidenceService,
			IIncidentService incidentService,
			ILogger<EvidenceController> logger)
		{
			_evidenceService = evidenceService;
			_incidentService = incidentService;
			_logger = logger;
		}

		[HttpPost("incidents/{incidentId}/evidence")]
		public async Task<ActionResult<EvidenceItemDto>> RegisterEvidence(int incidentId, [FromBody] RegisterEvidenceDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			try
			{
				// Validates user has access to this incident's customer via UserContextService
				await _incidentService.GetByIdAsync(incidentId.ToString());

				var result = await _evidenceService.RegisterEvidenceAsync(incidentId, dto, userId);
				return CreatedAtAction(nameof(GetEvidence), new { evidenceItemId = result.EvidenceItemId }, result);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = ex.Message });
			}
		}

		[HttpGet("{evidenceItemId}")]
		public async Task<ActionResult<EvidenceItemDto>> GetEvidence(int evidenceItemId)
		{
			try
			{
				var result = await _evidenceService.GetByIdAsync(evidenceItemId);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
		}

		[HttpGet("incidents/{incidentId}")]
		public async Task<ActionResult<EvidenceListResponseDto>> GetByIncident(int incidentId)
		{
			try
			{
				await _incidentService.GetByIdAsync(incidentId.ToString());
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}

			var result = await _evidenceService.GetByIncidentAsync(incidentId);
			return Ok(result);
		}

		[HttpPost("scan")]
		public async Task<ActionResult<BarcodeScanResultDto>> ScanBarcode([FromBody] BarcodeScanDto dto)
		{
			var result = await _evidenceService.ScanBarcodeAsync(dto);
			return Ok(result);
		}

		[HttpPost("{evidenceItemId}/custody")]
		public async Task<ActionResult<EvidenceCustodyEventDto>> RecordCustodyEvent(
			int evidenceItemId, [FromBody] RecordCustodyEventDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			try
			{
				var result = await _evidenceService.RecordCustodyEventAsync(evidenceItemId, dto, userId);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = ex.Message });
			}
		}
	}
}
