using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
	[ApiController]
	[Route("api/bank-holidays")]
	[Authorize]
	public class BankHolidayController : ControllerBase
	{
		private readonly IBankHolidayService _bankHolidayService;
		private readonly ILogger<BankHolidayController> _logger;

		public BankHolidayController(
			IBankHolidayService bankHolidayService,
			ILogger<BankHolidayController> logger)
		{
			_bankHolidayService = bankHolidayService;
			_logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult<BankHolidayListResponseDto>> GetBankHolidays(
			[FromQuery] string? search = null,
			[FromQuery] string? status = null,
			[FromQuery] bool? archived = null,
			[FromQuery] int page = 1,
			[FromQuery] int limit = 10)
		{
			var result = await _bankHolidayService.GetPagedAsync(search, status, archived, page, limit);
			return Ok(result);
		}

		[HttpGet("{id:int}")]
		public async Task<ActionResult<BankHolidayDto>> GetBankHoliday(int id)
		{
			try
			{
				var result = await _bankHolidayService.GetByIdAsync(id);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Bank holiday {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
		}

		[HttpPost]
		public async Task<ActionResult<BankHolidayDto>> CreateBankHoliday([FromBody] CreateBankHolidayDto dto)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated.");

				var result = await _bankHolidayService.CreateAsync(dto, userId);
				return CreatedAtAction(nameof(GetBankHoliday), new { id = int.Parse(result.Id) }, result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated." });
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid bank holiday create request");
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPut("{id:int}")]
		public async Task<ActionResult<BankHolidayDto>> UpdateBankHoliday(
			int id,
			[FromBody] UpdateBankHolidayDto dto)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated.");

				var result = await _bankHolidayService.UpdateAsync(id, dto, userId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated." });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Bank holiday {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid bank holiday update for {Id}", id);
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpDelete("{id:int}")]
		public async Task<IActionResult> DeleteBankHoliday(int id)
		{
			var deleted = await _bankHolidayService.DeleteAsync(id);
			if (!deleted)
			{
				return NotFound(new { message = $"Bank holiday with ID {id} not found." });
			}

			return NoContent();
		}

		[HttpPut("{id:int}/archive")]
		public async Task<ActionResult<BankHolidayDto>> ArchiveBankHoliday(int id)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated.");

				var result = await _bankHolidayService.ArchiveAsync(id, userId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated." });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Bank holiday {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
		}

		[HttpPut("{id:int}/unarchive")]
		public async Task<ActionResult<BankHolidayDto>> UnarchiveBankHoliday(int id)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? throw new UnauthorizedAccessException("User not authenticated.");

				var result = await _bankHolidayService.UnarchiveAsync(id, userId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { message = "User not authenticated." });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, "Bank holiday {Id} not found", id);
				return NotFound(new { message = ex.Message });
			}
		}
	}
}

