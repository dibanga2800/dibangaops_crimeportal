#nullable enable

using Microsoft.AspNetCore.Http;

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// Multipart form payload for <see cref="Controllers.ContactController.Submit"/>.
	/// Single [FromForm] model avoids Swashbuckle errors with mixed IFormFile + primitive [FromForm] parameters.
	/// </summary>
	public class ContactSubmitFormDto
	{
		public string Name { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public string? JobRole { get; set; }

		public string Description { get; set; } = string.Empty;

		public IFormFile? Attachment { get; set; }
	}
}
