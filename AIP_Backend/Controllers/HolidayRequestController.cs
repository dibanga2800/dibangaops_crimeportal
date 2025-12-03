#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// Controller for managing Holiday Requests
	/// </summary>
	[ApiController]
	[Route("api/holiday-requests")]
	[Authorize]
	public class HolidayRequestController : ControllerBase
	{
		private readonly IHolidayRequestService _service;
		private readonly ILogger<HolidayRequestController> _logger;

		public HolidayRequestController(
			IHolidayRequestService service,
			ILogger<HolidayRequestController> logger)
		{
			_service = service;
			_logger = logger;
		}

		/// <summary>
		/// Get all holiday requests with optional filters
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<HolidayRequestListResponseDto>> GetHolidayRequests(
			[FromQuery] string? search = null,
			[FromQuery] string? status = null,
			[FromQuery] bool? archived = null,
			[FromQuery] int page = 1,
			[FromQuery] int limit = 10)
		{
			try
			{
				var result = await _service.GetAllAsync(search, status, archived, page, limit);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving holiday requests");
				return StatusCode(500, new { message = "An error occurred while retrieving holiday requests" });
			}
		}

		/// <summary>
		/// Get a single holiday request by ID
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<AIPBackend.Models.DTOs.HolidayRequestDto>> GetHolidayRequest(int id)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				_logger.LogInformation("Get holiday request {Id} by user {CurrentUserId}", id, currentUserId);

				var result = await _service.GetByIdAsync(id);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Holiday request {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving holiday request {Id}", id);
				return StatusCode(500, new { message = "An error occurred while retrieving the holiday request" });
			}
		}

		/// <summary>
		/// Create a new holiday request
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<AIPBackend.Models.DTOs.HolidayRequestDto>> CreateHolidayRequest([FromBody] CreateHolidayRequestDto request)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated");

				var result = await _service.CreateAsync(request, currentUserId);
				return CreatedAtAction(nameof(GetHolidayRequest), new { id = int.Parse(result.Id) }, result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated" });
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid holiday request data");
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating holiday request");
				return StatusCode(500, new { message = "An error occurred while creating the holiday request" });
			}
		}

		/// <summary>
		/// Update an existing holiday request
		/// </summary>
		[HttpPut("{id}")]
		public async Task<ActionResult<AIPBackend.Models.DTOs.HolidayRequestDto>> UpdateHolidayRequest(
			int id,
			[FromBody] UpdateHolidayRequestDto request)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated");

				var result = await _service.UpdateAsync(id, request, currentUserId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated" });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Holiday request {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid holiday request data for ID {Id}", id);
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating holiday request {Id}", id);
				return StatusCode(500, new { message = "An error occurred while updating the holiday request" });
			}
		}

		/// <summary>
		/// Delete a holiday request
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteHolidayRequest(int id)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				_logger.LogInformation("Delete holiday request {Id} by user {CurrentUserId}", id, currentUserId);

				var deleted = await _service.DeleteAsync(id);
				if (!deleted)
				{
					return NotFound(new { message = $"Holiday request with ID {id} not found" });
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting holiday request {Id}", id);
				return StatusCode(500, new { message = "An error occurred while deleting the holiday request" });
			}
		}

		/// <summary>
		/// Archive a holiday request
		/// </summary>
		[HttpPut("{id}/archive")]
		public async Task<ActionResult<AIPBackend.Models.DTOs.HolidayRequestDto>> ArchiveHolidayRequest(int id)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated");

				var result = await _service.ArchiveAsync(id, currentUserId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated" });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Holiday request {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error archiving holiday request {Id}", id);
				return StatusCode(500, new { message = "An error occurred while archiving the holiday request" });
			}
		}

		[HttpPut("{id}/unarchive")]
		public async Task<ActionResult<AIPBackend.Models.DTOs.HolidayRequestDto>> UnarchiveHolidayRequest(int id)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated");

				var result = await _service.UnarchiveAsync(id, currentUserId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated" });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Holiday request {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error unarchiving holiday request {Id}", id);
				return StatusCode(500, new { message = "An error occurred while unarchiving the holiday request" });
			}
		}
	}
}

