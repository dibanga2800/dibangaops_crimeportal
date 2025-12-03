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
	/// Controller for managing Daily Occurrence Book entries
	/// </summary>
	[ApiController]
	[Route("api/customers/{customerId:int}/daily-occurrence-book")]
	[Authorize]
	public class DailyOccurrenceBookController : ControllerBase
	{
		private readonly IDailyOccurrenceBookService _service;
		private readonly ILogger<DailyOccurrenceBookController> _logger;

		public DailyOccurrenceBookController(
			IDailyOccurrenceBookService service,
			ILogger<DailyOccurrenceBookController> logger)
		{
			_service = service;
			_logger = logger;
		}

		/// <summary>
		/// Get all occurrences for a customer with optional filters
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<DailyOccurrenceBookListResponseDto>> GetOccurrences(
			int customerId,
			[FromQuery] string? siteId = null,
			[FromQuery] string? dateFrom = null,
			[FromQuery] string? dateTo = null,
			[FromQuery] string? storeName = null,
			[FromQuery] string? storeNumber = null,
			[FromQuery] string? officerName = null,
			[FromQuery] string? code = null,
			[FromQuery] string? reportedBy = null,
			[FromQuery] string? search = null)
		{
			try
			{
				var filters = new DailyOccurrenceBookFilterDto
				{
					SiteId = siteId,
					DateFrom = dateFrom,
					DateTo = dateTo,
					StoreName = storeName,
					StoreNumber = storeNumber,
					OfficerName = officerName,
					Code = code,
					ReportedBy = reportedBy,
					Search = search
				};

				var result = await _service.GetOccurrencesAsync(customerId, filters);

				// Return in format expected by frontend: { success, data, stats, message }
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving occurrences for customer {CustomerId}", customerId);
				return StatusCode(500, new DailyOccurrenceBookListResponseDto
				{
					Success = false,
					Message = "An error occurred while retrieving occurrences",
					Data = new List<DailyOccurrenceBookDto>()
				});
			}
		}

		/// <summary>
		/// Get a single occurrence by ID
		/// </summary>
		[HttpGet("{occurrenceId}")]
		public async Task<ActionResult<DailyOccurrenceBookResponseDto>> GetOccurrence(
			int customerId,
			string occurrenceId)
		{
			try
			{
				var result = await _service.GetByIdAsync(customerId, occurrenceId);

				// Return in format expected by frontend: { success, data, message }
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Occurrence {OccurrenceId} not found for customer {CustomerId}", occurrenceId, customerId);
				return NotFound(new DailyOccurrenceBookResponseDto
				{
					Success = false,
					Message = ex.Message
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);
				return StatusCode(500, new DailyOccurrenceBookResponseDto
				{
					Success = false,
					Message = "An error occurred while retrieving the occurrence"
				});
			}
		}

		/// <summary>
		/// Create a new occurrence
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<DailyOccurrenceBookResponseDto>> CreateOccurrence(
			int customerId,
			[FromBody] CreateDailyOccurrenceBookRequestDto request)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrWhiteSpace(currentUserId))
				{
					return Unauthorized(new DailyOccurrenceBookResponseDto
					{
						Success = false,
						Message = "User not authenticated"
					});
				}

				// Ensure customerId matches
				if (request.CustomerId != customerId)
				{
					return BadRequest(new DailyOccurrenceBookResponseDto
					{
						Success = false,
						Message = "Customer ID in request body does not match URL parameter"
					});
				}

				var result = await _service.CreateAsync(request, currentUserId);

				// Return in format expected by frontend: { success, data, message }
				return Ok(result);
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid request for creating occurrence: {Message}", ex.Message);
				return BadRequest(new DailyOccurrenceBookResponseDto
				{
					Success = false,
					Message = ex.Message
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating occurrence for customer {CustomerId}", customerId);
				return StatusCode(500, new DailyOccurrenceBookResponseDto
				{
					Success = false,
					Message = "An error occurred while creating the occurrence"
				});
			}
		}

		/// <summary>
		/// Update an existing occurrence
		/// </summary>
		[HttpPut("{occurrenceId}")]
		public async Task<ActionResult<DailyOccurrenceBookResponseDto>> UpdateOccurrence(
			int customerId,
			string occurrenceId,
			[FromBody] UpdateDailyOccurrenceBookRequestDto request)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrWhiteSpace(currentUserId))
				{
					return Unauthorized(new DailyOccurrenceBookResponseDto
					{
						Success = false,
						Message = "User not authenticated"
					});
				}

				// Ensure ID matches
				if (request.Id != occurrenceId)
				{
					return BadRequest(new DailyOccurrenceBookResponseDto
					{
						Success = false,
						Message = "Occurrence ID in request body does not match URL parameter"
					});
				}

				var result = await _service.UpdateAsync(customerId, occurrenceId, request, currentUserId);

				// Return in format expected by frontend: { success, data, message }
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Occurrence {OccurrenceId} not found for customer {CustomerId}", occurrenceId, customerId);
				return NotFound(new DailyOccurrenceBookResponseDto
				{
					Success = false,
					Message = ex.Message
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);
				return StatusCode(500, new DailyOccurrenceBookResponseDto
				{
					Success = false,
					Message = "An error occurred while updating the occurrence"
				});
			}
		}

		/// <summary>
		/// Delete an occurrence
		/// </summary>
		[HttpDelete("{occurrenceId}")]
		public async Task<ActionResult> DeleteOccurrence(
			int customerId,
			string occurrenceId)
		{
			try
			{
				var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrWhiteSpace(currentUserId))
				{
					return Unauthorized(new { success = false, message = "User not authenticated" });
				}

				var result = await _service.DeleteAsync(customerId, occurrenceId, currentUserId);

				if (result)
				{
					return Ok(new { success = true, message = "Occurrence deleted successfully" });
				}
				else
				{
					return NotFound(new { success = false, message = "Occurrence not found" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);
				return StatusCode(500, new { success = false, message = "An error occurred while deleting the occurrence" });
			}
		}

		/// <summary>
		/// Get occurrence statistics
		/// </summary>
		[HttpGet("stats")]
		public async Task<ActionResult> GetStats(
			int customerId,
			[FromQuery] string? siteId = null,
			[FromQuery] string? dateFrom = null,
			[FromQuery] string? dateTo = null,
			[FromQuery] string? storeName = null,
			[FromQuery] string? storeNumber = null,
			[FromQuery] string? officerName = null,
			[FromQuery] string? code = null)
		{
			try
			{
				var filters = new DailyOccurrenceBookFilterDto
				{
					SiteId = siteId,
					DateFrom = dateFrom,
					DateTo = dateTo,
					StoreName = storeName,
					StoreNumber = storeNumber,
					OfficerName = officerName,
					Code = code
				};

				var result = await _service.GetStatsAsync(customerId, filters);

				return Ok(new { success = true, data = result, message = "Statistics retrieved successfully" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving statistics for customer {CustomerId}", customerId);
				return StatusCode(500, new { success = false, message = "An error occurred while retrieving statistics" });
			}
		}
	}
}

