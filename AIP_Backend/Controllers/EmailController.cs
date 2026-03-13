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
	public class EmailController : ControllerBase
	{
		private readonly IEmailService _emailService;

		public EmailController(IEmailService emailService)
		{
			_emailService = emailService;
		}

		/// <summary>
		/// Send a test email to the given address (admin only). Use to verify SMTP delivery.
		/// </summary>
		[HttpPost("test")]
		[Authorize(Roles = "administrator")]
		public async Task<ActionResult<ApiResponseDto<object>>> SendTestEmail([FromBody] TestEmailRequest request)
		{
			if (string.IsNullOrWhiteSpace(request?.To))
			{
				return BadRequest(new ApiResponseDto<object>
				{
					Success = false,
					Message = "Email address (To) is required."
				});
			}

			var to = request.To.Trim();
			var subject = "Crime Portal – Test Email";
			var body = @"<html><body>
<h2>Test email</h2>
<p>If you received this, the Crime Portal email service is working.</p>
<p>Sent at: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + @" UTC</p>
<p>— Crime Portal</p>
</body></html>";

			var sent = await _emailService.SendEmailAsync(to, subject, body, isHtml: true, fromName: "Crime Portal");

			if (sent)
			{
				return Ok(new ApiResponseDto<object>
				{
					Success = true,
					Message = $"Test email sent to {to}. Check inbox and spam."
				});
			}

			return StatusCode(500, new ApiResponseDto<object>
			{
				Success = false,
				Message = "Failed to send test email. Check backend logs."
			});
		}
	}

	public class TestEmailRequest
	{
		public string To { get; set; } = string.Empty;
	}
}
