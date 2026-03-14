#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
	/// <summary>
	/// AI offender recognition endpoints using Azure Face API.
	/// Search by captured image against indexed verification evidence.
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
		/// Lightweight face detection only. Returns faceDetected for guided capture UX (red/green overlay).
		/// </summary>
		[HttpPost("detect-only")]
		public async Task<ActionResult<OffenderMatchResultDto>> DetectOnly(CancellationToken cancellationToken)
		{
			byte[]? imageBytes = null;
			int? imageWidth = null;
			int? imageHeight = null;
			if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
			{
				using var reader = new StreamReader(Request.Body);
				var body = await reader.ReadToEndAsync(cancellationToken);
				var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
				if (json.TryGetProperty("imageBase64", out var prop))
				{
					var base64 = prop.GetString();
					if (!string.IsNullOrWhiteSpace(base64))
					{
						try { imageBytes = Convert.FromBase64String(base64); } catch { return BadRequest(new { message = "Invalid base64 image data." }); }
					}
				}
				if (json.TryGetProperty("imageWidth", out var wProp) && wProp.TryGetInt32(out var w))
					imageWidth = w;
				if (json.TryGetProperty("imageHeight", out var hProp) && hProp.TryGetInt32(out var h))
					imageHeight = h;
			}
			if (imageBytes == null || imageBytes.Length == 0)
				return BadRequest(new { message = "Provide JSON body with imageBase64." });
			var result = await _offenderRecognitionService.DetectFaceOnlyAsync(imageBytes, cancellationToken, imageWidth, imageHeight);
			return Ok(result);
		}

		/// <summary>
		/// Search for repeat offenders by captured image. Upload image bytes (multipart) or base64 JSON.
		/// Searches against stored verification evidence indexed in the database.
		/// </summary>
		[HttpPost("search-by-image")]
		public async Task<ActionResult<OffenderMatchResultDto>> SearchByImage(
			CancellationToken cancellationToken)
		{
			byte[]? imageBytes = null;

			if (Request.HasFormContentType && Request.Form.Files.Count > 0)
			{
				var file = Request.Form.Files[0];
				await using var ms = new MemoryStream();
				await file.CopyToAsync(ms, cancellationToken);
				imageBytes = ms.ToArray();
			}
			else if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
			{
				using var reader = new StreamReader(Request.Body);
				var body = await reader.ReadToEndAsync(cancellationToken);
				var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
				if (json.TryGetProperty("imageBase64", out var prop))
				{
					var base64 = prop.GetString();
					if (!string.IsNullOrWhiteSpace(base64))
					{
						try
						{
							imageBytes = Convert.FromBase64String(base64);
						}
						catch
						{
							return BadRequest(new { message = "Invalid base64 image data." });
						}
					}
				}
			}

			if (imageBytes == null || imageBytes.Length == 0)
			{
				return BadRequest(new { message = "Provide image via multipart/form-data (file) or JSON body with imageBase64." });
			}

			var result = await _offenderRecognitionService.SearchByImageAsync(imageBytes, cancellationToken);
			return Ok(result);
		}

		/// <summary>
		/// Index an offender image and return potential matches. Accepts URL or base64 data URL.
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
		/// Re-index all incidents with verification evidence that were not previously indexed (e.g. Pius Joan).
		/// Admin only. Call this to backfill faces so scan-to-search can find them.
		/// </summary>
		[HttpPost("reindex")]
		[Authorize(Roles = "administrator")]
		public async Task<ActionResult<ReindexResultDto>> Reindex(CancellationToken cancellationToken)
		{
			var result = await _offenderRecognitionService.ReindexVerificationEvidenceAsync(cancellationToken);
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

