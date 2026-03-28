#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace AIPBackend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class ContactController : ControllerBase
	{
		private readonly IEmailService _emailService;
		private const string ToEmail = "david.ibanga@advantage1.co.uk";
		private static readonly string[] CcEmails = { "dibanga2800@gmail.com" };

		public ContactController(IEmailService emailService)
		{
			_emailService = emailService;
		}

		/// <summary>
		/// Submit contact form. Sends email to david.ibanga@advantage1.co.uk with CC to dibanga2800@gmail.com.
		/// </summary>
		[HttpPost]
		[Consumes("multipart/form-data")]
		public async Task<ActionResult<ApiResponseDto<object>>> Submit([FromForm] ContactSubmitFormDto form)
		{
			if (form == null)
			{
				return BadRequest(new ApiResponseDto<object>
				{
					Success = false,
					Message = "Request body is required."
				});
			}

			var name = form.Name;
			var email = form.Email;
			var jobRole = form.JobRole;
			var description = form.Description;
			var attachment = form.Attachment;

			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(description))
			{
				return BadRequest(new ApiResponseDto<object>
				{
					Success = false,
					Message = "Name, email and description are required."
				});
			}

			var subject = $"Crime Portal – Contact form from {WebUtility.HtmlEncode(name.Trim())}";
			var body = BuildContactEmailBody(name.Trim(), email.Trim(), jobRole?.Trim(), description.Trim());

			Stream? attachmentStream = null;
			string? attachmentFileName = null;

			if (attachment != null && attachment.Length > 0)
			{
				if (attachment.Length > 5 * 1024 * 1024) // 5MB
				{
					return BadRequest(new ApiResponseDto<object>
					{
						Success = false,
						Message = "Attachment must be 5MB or less."
					});
				}
				attachmentStream = attachment.OpenReadStream();
				attachmentFileName = attachment.FileName;
			}

			try
			{
				var sent = await _emailService.SendContactFormEmailAsync(
					ToEmail,
					CcEmails,
					subject,
					body,
					attachmentStream,
					attachmentFileName);

				if (attachmentStream != null)
				{
					await attachmentStream.DisposeAsync();
				}

				if (sent)
				{
					return Ok(new ApiResponseDto<object>
					{
						Success = true,
						Message = "Your message has been sent successfully. We will get back to you soon."
					});
				}

				return StatusCode(500, new ApiResponseDto<object>
				{
					Success = false,
					Message = "Failed to send your message. Please try again later."
				});
			}
			catch (Exception)
			{
				if (attachmentStream != null)
				{
					await attachmentStream.DisposeAsync();
				}
				throw;
			}
		}

		private static string BuildContactEmailBody(string name, string email, string? jobRole, string description)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<html><body>");
			sb.AppendLine("<h2>Crime Portal – Contact Form Submission</h2>");
			sb.AppendLine("<table style='border-collapse: collapse;'>");
			sb.AppendLine($"<tr><td style='padding: 6px 12px; font-weight: bold;'>Name:</td><td style='padding: 6px 12px;'>{WebUtility.HtmlEncode(name)}</td></tr>");
			sb.AppendLine($"<tr><td style='padding: 6px 12px; font-weight: bold;'>Email:</td><td style='padding: 6px 12px;'><a href='mailto:" + WebUtility.HtmlEncode(email) + $"'>{WebUtility.HtmlEncode(email)}</a></td></tr>");
			if (!string.IsNullOrWhiteSpace(jobRole))
				sb.AppendLine($"<tr><td style='padding: 6px 12px; font-weight: bold;'>Job Role:</td><td style='padding: 6px 12px;'>{WebUtility.HtmlEncode(jobRole)}</td></tr>");
			sb.AppendLine("</table>");
			sb.AppendLine("<h3>Description / Message</h3>");
			sb.AppendLine($"<p style='white-space: pre-wrap;'>{WebUtility.HtmlEncode(description)}</p>");
			sb.AppendLine("<p><em>— Crime Portal Contact Form</em></p>");
			sb.AppendLine("</body></html>");
			return sb.ToString();
		}
	}
}
