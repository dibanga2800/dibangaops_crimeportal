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
	public class ClassificationController : ControllerBase
	{
		private readonly IIncidentClassifier _classifier;
		private readonly IIncidentService _incidentService;
		private readonly ILogger<ClassificationController> _logger;

		public ClassificationController(
			IIncidentClassifier classifier,
			IIncidentService incidentService,
			ILogger<ClassificationController> logger)
		{
			_classifier = classifier;
			_incidentService = incidentService;
			_logger = logger;
		}

		[HttpPost("classify")]
		public async Task<ActionResult<IncidentClassificationResultDto>> ClassifyIncident(
			[FromBody] IncidentClassificationRequestDto request)
		{
			var result = await _classifier.ClassifyAsync(request);
			return Ok(result);
		}

		[HttpPost("classify/{incidentId}")]
		public async Task<ActionResult<IncidentClassificationResultDto>> ClassifyExistingIncident(int incidentId)
		{
			try
			{
				var incident = await _incidentService.GetByIdAsync(incidentId.ToString());
				var request = new IncidentClassificationRequestDto
				{
					IncidentId = incidentId,
					IncidentType = incident.Data.IncidentType,
					Description = incident.Data.Description,
					IncidentDetails = incident.Data.IncidentDetails,
					TotalValueRecovered = incident.Data.TotalValueRecovered,
					PoliceInvolvement = incident.Data.PoliceInvolvement ?? false,
					OffenderName = incident.Data.OffenderName,
					IncidentInvolved = incident.Data.IncidentInvolved,
					StolenItemCount = incident.Data.StolenItems?.Count ?? 0
				};

				var result = await _classifier.ClassifyAsync(request);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new { message = $"Incident {incidentId} not found" });
			}
		}
	}
}
