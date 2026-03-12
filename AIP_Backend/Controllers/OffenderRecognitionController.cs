#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// AI offender recognition endpoints.
	/// 
	/// NOTE: The underlying OffenderRecognitionService currently exposes a stub
	/// implementation for the computer-vision provider. Once a real CV model
	/// is integrated, these endpoints will begin returning live matches.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class OffenderRecognitionController : ControllerBase
	{
		private readonly IOffenderRecognitionService _offenderRecognitionService;

		public OffenderRecognitionController(IOffenderRecognitionService offenderRecognitionService)
		{
			_offenderRecognitionService = offenderRecognitionService;
		}

		/// <summary>
		/// Index an offender image and return potential matches.
		/// The caller is responsible for ensuring that FileName/Url refer to a
		/// valid image location that the computer-vision provider can access.
		/// </summary>
		[HttpPost("index-and-match")]
		public async Task<ActionResult<OffenderMatchResultDto>> IndexAndMatch(
			[FromBody] OffenderImageReferenceDto imageReference,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(imageReference.FileName) && string.IsNullOrWhiteSpace(imageReference.Url))
			{
				return BadRequest(new { message = "Either FileName or Url must be provided for offender image reference." });
			}

			var result = await _offenderRecognitionService.IndexAndMatchAsync(imageReference, cancellationToken);
			return Ok(result);
		}

		/// <summary>
		/// Run offender match search for an existing embedding id.
		/// </summary>
		[HttpGet("matches/{embeddingId}")]
		public async Task<ActionResult<OffenderMatchResultDto>> GetMatches(
			string embeddingId,
			[FromQuery] int maxResults = 10,
			CancellationToken cancellationToken = default)
		{
			var result = await _offenderRecognitionService.FindMatchesByEmbeddingAsync(
				embeddingId,
				maxResults,
				cancellationToken);

			return Ok(result);
		}
	}
}

