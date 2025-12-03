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
	/// Controller for managing daily activity reports
	/// </summary>
	[ApiController]
	[Route("api/daily-activity-reports")]
	[Authorize]
	public class DailyActivityReportController : ControllerBase
	{
		private readonly IDailyActivityReportService _service;
		private readonly ILogger<DailyActivityReportController> _logger;

		public DailyActivityReportController(
			IDailyActivityReportService service,
			ILogger<DailyActivityReportController> logger)
		{
			_service = service;
			_logger = logger;
		}

		/// <summary>
		/// Get paginated list of daily activity reports
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<DailyActivityReportsResponseDto>> GetReports(
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10,
			[FromQuery] string? search = null,
			[FromQuery] string? customerId = null,
			[FromQuery] string? siteId = null,
			[FromQuery] string? reportDate = null,
			[FromQuery] string? officerName = null,
			[FromQuery] string? from = null,
			[FromQuery] string? to = null)
		{
			try
			{
				// Get customer ID from header (for non-admin users)
				var headerCustomerId = Request.Headers["X-Customer-Id"].FirstOrDefault();
				var effectiveCustomerId = customerId ?? headerCustomerId;

				var query = new DailyActivityReportQueryDto
				{
					Page = page,
					PageSize = pageSize,
					Search = search,
					CustomerId = effectiveCustomerId,
					SiteId = siteId,
					ReportDate = reportDate,
					OfficerName = officerName,
					From = from,
					To = to
				};

				var result = await _service.GetReportsAsync(query);
				return Ok(result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden daily activity reports request");
				return Forbid();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving daily activity reports");
				return StatusCode(500, new DailyActivityReportsResponseDto
				{
					Data = new List<DailyActivityReportDto>(),
					Pagination = new PaginationInfoDto()
				});
			}
		}

		/// <summary>
		/// Get a single daily activity report by ID
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<DailyActivityReportResponseDto>> GetReport(string id)
		{
			try
			{
				var result = await _service.GetByIdAsync(id);
				return Ok(result);
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new { message = "Daily activity report not found" });
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden daily activity report access for {ReportId}", id);
				return Forbid();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving daily activity report {ReportId}", id);
				return StatusCode(500, new { message = "An error occurred while retrieving the report" });
			}
		}

		/// <summary>
		/// Create a new daily activity report
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<DailyActivityReportResponseDto>> CreateReport([FromBody] DailyActivityReportRequestDto dto)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				
				// Get customer ID from header if not provided in DTO
				var headerCustomerId = Request.Headers["X-Customer-Id"].FirstOrDefault();
				if (dto.CustomerId == 0 && !string.IsNullOrWhiteSpace(headerCustomerId) && int.TryParse(headerCustomerId, out var parsedCustomerId))
				{
					dto.CustomerId = parsedCustomerId;
				}

				var result = await _service.CreateAsync(dto, userId);
				return CreatedAtAction(nameof(GetReport), new { id = result.Data.Id }, result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden create request for daily activity report");
				return Forbid();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating daily activity report");
				return StatusCode(500, new { message = "An error occurred while creating the report" });
			}
		}

		/// <summary>
		/// Update an existing daily activity report
		/// </summary>
		[HttpPut("{id}")]
		public async Task<ActionResult<DailyActivityReportResponseDto>> UpdateReport(string id, [FromBody] DailyActivityReportRequestDto dto)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				var result = await _service.UpdateAsync(id, dto, userId);
				return Ok(result);
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden update request for daily activity report {ReportId}", id);
				return Forbid();
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new { message = "Daily activity report not found" });
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating daily activity report {ReportId}", id);
				return StatusCode(500, new { message = "An error occurred while updating the report" });
			}
		}

		/// <summary>
		/// Delete a daily activity report
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteReport(string id)
		{
			try
			{
				var result = await _service.DeleteAsync(id);
				if (result)
				{
					return NoContent();
				}
				return NotFound(new { message = "Daily activity report not found" });
			}
			catch (ForbiddenAccessException ex)
			{
				_logger.LogWarning(ex, "Forbidden delete request for daily activity report {ReportId}", id);
				return Forbid();
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting daily activity report {ReportId}", id);
				return StatusCode(500, new { message = "An error occurred while deleting the report" });
			}
		}
	}
}
