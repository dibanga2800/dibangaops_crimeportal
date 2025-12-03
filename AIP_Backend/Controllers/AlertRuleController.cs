#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// Controller for managing alert rules
	/// </summary>
	[ApiController]
	[Route("api/alert-rules")]
	[Authorize]
	public class AlertRuleController : ControllerBase
	{
		private readonly IAlertRuleService _alertRuleService;
		private readonly ILogger<AlertRuleController> _logger;

		public AlertRuleController(
			IAlertRuleService alertRuleService,
			ILogger<AlertRuleController> logger)
		{
			_alertRuleService = alertRuleService;
			_logger = logger;
		}

		/// <summary>
		/// Get paginated list of alert rules
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<ApiResponseDto<AlertRuleListResponseDto>>> GetAlertRules(
			[FromQuery] string? search = null,
			[FromQuery] string? ruleType = null,
			[FromQuery] bool? isActive = null,
			[FromQuery] int? customerId = null,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			try
			{
				var result = await _alertRuleService.GetPagedAsync(
					search,
					ruleType,
					isActive,
					customerId,
					page,
					pageSize);

				return Ok(new ApiResponseDto<AlertRuleListResponseDto>
				{
					Success = true,
					Data = result,
					Message = "Alert rules retrieved successfully"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving alert rules");
				return StatusCode(500, new ApiResponseDto<AlertRuleListResponseDto>
				{
					Success = false,
					Message = "Failed to retrieve alert rules"
				});
			}
		}

		/// <summary>
		/// Get alert rule by ID
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<ApiResponseDto<AlertRuleDto>>> GetAlertRule(int id)
		{
			try
			{
				var rule = await _alertRuleService.GetByIdAsync(id);
				return Ok(new ApiResponseDto<AlertRuleDto>
				{
					Success = true,
					Data = rule,
					Message = "Alert rule retrieved successfully"
				});
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = $"Alert rule with ID {id} not found"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving alert rule {Id}", id);
				return StatusCode(500, new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = "Failed to retrieve alert rule"
				});
			}
		}

		/// <summary>
		/// Create a new alert rule
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<ApiResponseDto<AlertRuleDto>>> CreateAlertRule(
			[FromBody] CreateAlertRuleDto dto)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User ID not found");

				var rule = await _alertRuleService.CreateAsync(dto, userId);

				return CreatedAtAction(
					nameof(GetAlertRule),
					new { id = rule.AlertRuleId },
					new ApiResponseDto<AlertRuleDto>
					{
						Success = true,
						Data = rule,
						Message = "Alert rule created successfully"
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating alert rule");
				return StatusCode(500, new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = "Failed to create alert rule"
				});
			}
		}

		/// <summary>
		/// Update an existing alert rule
		/// </summary>
		[HttpPut("{id}")]
		public async Task<ActionResult<ApiResponseDto<AlertRuleDto>>> UpdateAlertRule(
			int id,
			[FromBody] UpdateAlertRuleDto dto)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User ID not found");

				var rule = await _alertRuleService.UpdateAsync(id, dto, userId);

				return Ok(new ApiResponseDto<AlertRuleDto>
				{
					Success = true,
					Data = rule,
					Message = "Alert rule updated successfully"
				});
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = $"Alert rule with ID {id} not found"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating alert rule {Id}", id);
				return StatusCode(500, new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = "Failed to update alert rule"
				});
			}
		}

		/// <summary>
		/// Delete an alert rule (soft delete)
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<ActionResult<ApiResponseDto<object>>> DeleteAlertRule(int id)
		{
			try
			{
				await _alertRuleService.DeleteAsync(id);

				return Ok(new ApiResponseDto<object>
				{
					Success = true,
					Message = "Alert rule deleted successfully"
				});
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new ApiResponseDto<object>
				{
					Success = false,
					Message = $"Alert rule with ID {id} not found"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting alert rule {Id}", id);
				return StatusCode(500, new ApiResponseDto<object>
				{
					Success = false,
					Message = "Failed to delete alert rule"
				});
			}
		}

		/// <summary>
		/// Toggle alert rule active status
		/// </summary>
		[HttpPatch("{id}/toggle")]
		public async Task<ActionResult<ApiResponseDto<AlertRuleDto>>> ToggleAlertRule(
			int id,
			[FromBody] bool isActive)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User ID not found");

				var rule = await _alertRuleService.ToggleActiveAsync(id, isActive, userId);

				return Ok(new ApiResponseDto<AlertRuleDto>
				{
					Success = true,
					Data = rule,
					Message = $"Alert rule {(isActive ? "activated" : "deactivated")} successfully"
				});
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = $"Alert rule with ID {id} not found"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error toggling alert rule {Id}", id);
				return StatusCode(500, new ApiResponseDto<AlertRuleDto>
				{
					Success = false,
					Message = "Failed to toggle alert rule"
				});
			}
		}

		/// <summary>
		/// Manually trigger alert check for a specific incident
		/// </summary>
		[HttpPost("check-incident/{incidentId}")]
		public async Task<ActionResult<ApiResponseDto<object>>> CheckIncidentForAlerts(int incidentId)
		{
			try
			{
				await _alertRuleService.CheckIncidentForAlertsAsync(incidentId);

				return Ok(new ApiResponseDto<object>
				{
					Success = true,
					Message = "Alert check completed successfully"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking incident {IncidentId} for alerts", incidentId);
				return StatusCode(500, new ApiResponseDto<object>
				{
					Success = false,
					Message = "Failed to check incident for alerts"
				});
			}
		}
	}
}
