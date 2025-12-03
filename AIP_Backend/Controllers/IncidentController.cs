#nullable enable

using AIPBackend.Exceptions;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// Controller for managing incident reports
	/// </summary>
	[ApiController]
	[Route("api/incidents")]
	[Authorize]
	public class IncidentController : ControllerBase
	{
		private readonly IIncidentService _incidentService;
		private readonly ILogger<IncidentController> _logger;

		public IncidentController(
			IIncidentService incidentService,
			ILogger<IncidentController> logger)
		{
			_incidentService = incidentService;
			_logger = logger;
		}

		/// <summary>
		/// Get paginated list of incidents
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<IncidentsResponseDto>> GetIncidents(
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10,
			[FromQuery] string? search = null,
			[FromQuery] string? fromDate = null,
			[FromQuery] string? toDate = null,
			[FromQuery] string? incidentType = null,
			[FromQuery] string? siteName = null,
			[FromQuery] string? siteId = null,
			[FromQuery] string? status = null,
			[FromQuery] string? customerId = null)
		{
			try
			{
				var query = new GetIncidentsQueryDto
				{
					Page = page,
					PageSize = pageSize,
					Search = search,
					FromDate = fromDate,
					ToDate = toDate,
					IncidentType = incidentType,
					SiteName = siteName,
					SiteId = siteId,
					Status = status,
					CustomerId = customerId
				};

				var result = await _incidentService.GetIncidentsAsync(query);
				return Ok(result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden incidents list request");
				return Forbid();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving incidents");
				return StatusCode(500, new IncidentsResponseDto
				{
					Data = new List<IncidentDto>(),
					Pagination = new PaginationInfoDto()
				});
			}
		}

		/// <summary>
		/// Get a single incident by ID
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<IncidentResponseDto>> GetIncident(string id)
		{
			try
			{
				var result = await _incidentService.GetByIdAsync(id);
				return Ok(result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden incident access: {Id}", id);
				return Forbid();
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Incident not found: {Id}", id);
				return NotFound(new IncidentResponseDto
				{
					Success = false,
					Message = $"Incident with ID {id} not found"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving incident {Id}", id);
				return StatusCode(500, new IncidentResponseDto
				{
					Success = false,
					Message = "Error retrieving incident"
				});
			}
		}

		/// <summary>
		/// Create a new incident
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<IncidentResponseDto>> CreateIncident([FromBody] UpsertIncidentRequestDto request)
		{
			try
			{
				if (request?.Incident == null)
				{
					return BadRequest(new IncidentResponseDto
					{
						Success = false,
						Message = "Incident data is required"
					});
				}

				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				var result = await _incidentService.CreateAsync(request.Incident, userId);

				return CreatedAtAction(
					nameof(GetIncident),
					new { id = result.Data.Id },
					result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden incident create request");
				return Forbid();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating incident");
				return StatusCode(500, new IncidentResponseDto
				{
					Success = false,
					Message = $"Error creating incident: {ex.Message}"
				});
			}
		}

		/// <summary>
		/// Update an existing incident
		/// </summary>
		[HttpPut("{id}")]
		public async Task<ActionResult<IncidentResponseDto>> UpdateIncident(
			string id,
			[FromBody] UpsertIncidentRequestDto request)
		{
			try
			{
				if (request?.Incident == null)
				{
					return BadRequest(new IncidentResponseDto
					{
						Success = false,
						Message = "Incident data is required"
					});
				}

				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				var result = await _incidentService.UpdateAsync(id, request.Incident, userId);

				return Ok(result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden incident update request for {Id}", id);
				return Forbid();
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Incident not found for update: {Id}", id);
				return NotFound(new IncidentResponseDto
				{
					Success = false,
					Message = $"Incident with ID {id} not found"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating incident {Id}", id);
				return StatusCode(500, new IncidentResponseDto
				{
					Success = false,
					Message = $"Error updating incident: {ex.Message}"
				});
			}
		}

		/// <summary>
		/// Delete an incident
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteIncident(string id)
		{
			try
			{
				var result = await _incidentService.DeleteAsync(id);
				if (!result)
				{
					return NotFound(new { success = false, message = $"Incident with ID {id} not found" });
				}

				return Ok(new { success = true, message = "Incident deleted successfully" });
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden incident delete request for {Id}", id);
				return Forbid();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting incident {Id}", id);
				return StatusCode(500, new { success = false, message = $"Error deleting incident: {ex.Message}" });
			}
		}

		/// <summary>
		/// Search for repeat offenders by name, DOB, or distinguishing marks
		/// </summary>
		[HttpGet("repeat-offenders")]
		[AllowAnonymous] // Remove in production - add proper authorization
		public async Task<ActionResult<RepeatOffenderSearchResponseDto>> SearchRepeatOffenders(
			[FromQuery] RepeatOffenderSearchQueryDto query)
		{
			try
			{
				var result = await _incidentService.SearchRepeatOffendersAsync(query);
				return Ok(result);
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid repeat offender search criteria");
				return BadRequest(new RepeatOffenderSearchResponseDto
				{
					Success = false,
					Message = ex.Message
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error searching repeat offenders");
				return StatusCode(500, new RepeatOffenderSearchResponseDto
				{
					Success = false,
					Message = "Error searching repeat offenders"
				});
			}
		}

		/// <summary>
		/// Get crime intelligence insights for a customer
		/// </summary>
		[HttpGet("insights")]
		[AllowAnonymous] // Remove in production - add proper authorization
		public async Task<ActionResult<CrimeIntelligenceResponseDto>> GetCrimeInsights(
			[FromQuery] CrimeIntelligenceQueryDto query)
		{
			try
			{
				var result = await _incidentService.GetCrimeInsightsAsync(query);
				return Ok(result);
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid crime insight query");
				return BadRequest(new CrimeIntelligenceResponseDto
				{
					Success = false,
					Message = ex.Message
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating crime insights");
				return StatusCode(500, new CrimeIntelligenceResponseDto
				{
					Success = false,
					Message = "Error generating crime intelligence insights"
				});
			}
		}
	}
}

