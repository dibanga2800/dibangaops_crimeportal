#nullable enable

using System.Text.Json;
using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	/// <summary>
	/// Offender recognition using Azure Face API. Indexes verification evidence on incident save
	/// and searches by captured image against stored faces.
	/// </summary>
	public class OffenderRecognitionService : IOffenderRecognitionService
	{
		private readonly ApplicationDbContext _db;
		private readonly IAzureFaceClient _faceClient;
		private readonly AzureFaceOptions _options;
		private readonly ILogger<OffenderRecognitionService> _logger;

		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
		{
			PropertyNameCaseInsensitive = true
		};

		public OffenderRecognitionService(
			ApplicationDbContext db,
			IAzureFaceClient faceClient,
			IOptions<AzureFaceOptions> options,
			ILogger<OffenderRecognitionService> logger)
		{
			_db = db;
			_faceClient = faceClient;
			_options = options.Value;
			_logger = logger;
		}

		public async Task<OffenderMatchResultDto> SearchByImageAsync(
			byte[] imageBytes,
			CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled)
			{
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = "face-api-disabled"
				};
			}

			// Use detection_01 first (more permissive) for search-by-image so manual capture finds faces
			var detectResult = await _faceClient.DetectFacesAsync(imageBytes, cancellationToken, preferPermissiveModel: true);
			if (detectResult == null || detectResult.Faces.Count == 0)
			{
				_logger.LogInformation(
					"OffenderRecognition: no face detected in search image (size: {Size} bytes). Tips: ensure face is clearly visible, centered, well-lit; image 1KB–6MB; face at least 36x36px.",
					imageBytes.Length);
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = "azure-face-v1"
				};
			}

			var faceIds = detectResult.Faces
				.Where(f => !string.IsNullOrEmpty(f.FaceId))
				.Select(f => f.FaceId!)
				.ToList();
			if (faceIds.Count == 0)
			{
				return new OffenderMatchResultDto
				{
					FaceDetected = true,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = "azure-face-v1"
				};
			}

			var candidates = await _faceClient.IdentifyAsync(faceIds, 10, cancellationToken);
			var matches = new List<OffenderMatchCandidateDto>();
			var seenIncidentIds = new HashSet<int>();

			foreach (var c in candidates.OrderByDescending(x => x.Confidence))
			{
				if (string.IsNullOrEmpty(c.PersonId)) continue;

				var person = await _faceClient.GetPersonAsync(c.PersonId, cancellationToken);
				if (person?.UserData == null) continue;

				int? incidentId = null;
				try
				{
					using var doc = JsonDocument.Parse(person.UserData);
					if (doc.RootElement.TryGetProperty("incidentId", out var pid))
						incidentId = pid.TryGetInt32(out var id) ? id : null;
				}
				catch
				{
					continue;
				}

				if (!incidentId.HasValue || seenIncidentIds.Contains(incidentId.Value))
					continue;
				seenIncidentIds.Add(incidentId.Value);

				var incident = await _db.Incidents
					.AsNoTracking()
					.Include(i => i.StolenItems)
					.FirstOrDefaultAsync(i => i.IncidentId == incidentId.Value, cancellationToken);
				if (incident == null) continue;

				var relatedIncidents = await _db.Incidents
					.AsNoTracking()
					.Where(i => i.OffenderId == incident.OffenderId && !string.IsNullOrEmpty(incident.OffenderId))
					.OrderByDescending(i => i.DateOfIncident)
					.Take(5)
					.ToListAsync(cancellationToken);
				if (relatedIncidents.Count == 0)
					relatedIncidents.Add(incident);

				var totalValue = relatedIncidents.Sum(i =>
					i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
				var recentIncidents = relatedIncidents.Select(i => new OffenderMatchIncidentSummaryDto
				{
					IncidentId = i.IncidentId.ToString(),
					DateOfIncident = i.DateOfIncident.ToString("yyyy-MM-dd"),
					SiteName = i.StoreName ?? i.SiteId ?? "",
					IncidentType = i.IncidentType ?? "",
					Description = i.Description,
					OffenderMarks = i.OffenderMarks,
					OffenderDetailsVerified = i.OffenderDetailsVerified,
					VerificationMethod = i.VerificationMethod,
					VerificationEvidenceImage = i.VerificationEvidenceImage
				}).ToList();

				matches.Add(new OffenderMatchCandidateDto
				{
					OffenderId = 0,
					OffenderName = person.Name ?? incident.OffenderName ?? "Unknown",
					IncidentCount = relatedIncidents.Count,
					TotalValue = totalValue,
					Similarity = c.Confidence,
					ThumbnailUrl = incident.VerificationEvidenceImage,
					RecentIncidents = recentIncidents
				});
			}

			return new OffenderMatchResultDto
			{
				FaceDetected = true,
				Candidates = matches,
				ClassifierVersion = "azure-face-v1"
			};
		}

		public async Task IndexVerificationEvidenceAsync(
			int incidentId,
			byte[] imageBytes,
			string offenderName,
			string? offenderId,
			CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled) return;

			var detected = await _faceClient.DetectFacesAsync(imageBytes, cancellationToken);
			if (detected == null || detected.Faces.Count == 0)
			{
				_logger.LogDebug("OffenderRecognition: no face in verification evidence for incident {IncidentId}", incidentId);
				return;
			}

			var ok = await _faceClient.EnsurePersonGroupExistsAsync(cancellationToken);
			if (!ok)
			{
				_logger.LogWarning("OffenderRecognition: could not ensure Person Group exists");
				return;
			}

			var userData = JsonSerializer.Serialize(new
			{
				incidentId,
				offenderName = offenderName ?? "Unknown",
				offenderId
			}, JsonOptions);

			var personId = await _faceClient.CreatePersonAndAddFaceAsync(
				offenderName ?? $"Incident-{incidentId}",
				imageBytes,
				userData,
				cancellationToken);
			if (personId == null)
			{
				_logger.LogWarning("OffenderRecognition: failed to create person for incident {IncidentId}", incidentId);
				return;
			}

			var trained = await _faceClient.TrainPersonGroupAsync(cancellationToken);
			if (trained)
			{
				await _faceClient.WaitForTrainingCompletionAsync(TimeSpan.FromSeconds(60), cancellationToken);
			}

			var faceEmbedding = new FaceEmbedding
			{
				IncidentId = incidentId,
				OffenderId = offenderId,
				FileName = $"incident-{incidentId}-verification.jpg",
				ModelId = "azure-face-v1",
				Embedding = personId,
				AzurePersonId = personId,
				CreatedAt = DateTime.UtcNow
			};
			_db.FaceEmbeddings.Add(faceEmbedding);
			await _db.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("OffenderRecognition: indexed face for incident {IncidentId}, PersonId {PersonId}", incidentId, personId);
		}

		public async Task<OffenderMatchResultDto> DetectFaceOnlyAsync(
			byte[] imageBytes,
			CancellationToken cancellationToken = default,
			int? imageWidth = null,
			int? imageHeight = null)
		{
			if (!_options.Enabled)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "face-api-disabled" };
			// Use detection_01 first for guided capture (more permissive for live camera)
			var detectResult = await _faceClient.DetectFacesAsync(imageBytes, cancellationToken, preferPermissiveModel: true);
			if (detectResult == null || detectResult.Faces.Count == 0)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "azure-face-v1" };

			// When dimensions are provided, only report "face in oval" when a face is fully inside the guide oval
			if (imageWidth is > 0 && imageHeight is > 0)
			{
				var cx = imageWidth.Value / 2.0;
				var cy = imageHeight.Value / 2.0;
				var rx = Math.Min(imageWidth.Value * 0.3, 120.0);
				var ry = Math.Min(imageHeight.Value * 0.4, 180.0);
				foreach (var face in detectResult.Faces)
				{
					var rect = face.FaceRectangle;
					if (rect == null) continue;
					if (IsFaceRectFullyInsideEllipse(rect.Left, rect.Top, rect.Width, rect.Height, cx, cy, rx, ry))
					{
						return new OffenderMatchResultDto { FaceDetected = true, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "azure-face-v1" };
					}
				}
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "azure-face-v1" };
			}

			var faceDetected = detectResult.Faces.Count > 0;
			return new OffenderMatchResultDto { FaceDetected = faceDetected, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "azure-face-v1" };
		}

		/// <summary>
		/// Ellipse is centered at (cx, cy) with radii rx, ry (rotation -90° so ellipse is horizontal).
		/// Point (x,y) is inside when (y-cy)²/rx² + (x-cx)²/ry² ≤ 1.
		/// </summary>
		private static bool IsFaceRectFullyInsideEllipse(double left, double top, double width, double height, double cx, double cy, double rx, double ry)
		{
			if (rx <= 0 || ry <= 0) return false;
			var corners = new[]
			{
				(left, top),
				(left + width, top),
				(left, top + height),
				(left + width, top + height)
			};
			foreach (var (x, y) in corners)
			{
				var term = (y - cy) * (y - cy) / (rx * rx) + (x - cx) * (x - cx) / (ry * ry);
				if (term > 1.0) return false;
			}
			return true;
		}

		public async Task<ReindexResultDto> ReindexVerificationEvidenceAsync(CancellationToken cancellationToken = default)
		{
			var result = new ReindexResultDto();
			if (!_options.Enabled)
				return result;

			var ok = await _faceClient.EnsurePersonGroupExistsAsync(cancellationToken);
			if (!ok)
			{
				result.Errors.Add("Could not ensure Person Group exists");
				return result;
			}

			var incidentIdsWithEvidence = await _db.Incidents
				.AsNoTracking()
				.Where(i => !string.IsNullOrWhiteSpace(i.VerificationEvidenceImage))
				.Select(i => i.IncidentId)
				.ToListAsync(cancellationToken);

			var alreadyIndexed = await _db.FaceEmbeddings
				.Where(f => f.IncidentId != null)
				.Select(f => f.IncidentId!.Value)
				.Distinct()
				.ToListAsync(cancellationToken);
			var alreadyIndexedSet = new HashSet<int>(alreadyIndexed);
			var toIndex = incidentIdsWithEvidence.Where(id => !alreadyIndexedSet.Contains(id)).ToList();

			foreach (var incidentId in toIndex)
			{
				var incident = await _db.Incidents
					.AsNoTracking()
					.FirstOrDefaultAsync(i => i.IncidentId == incidentId, cancellationToken);
				if (incident == null || string.IsNullOrWhiteSpace(incident.VerificationEvidenceImage))
				{
					result.SkippedCount++;
					continue;
				}

				var imageBytes = TryDecodeBase64DataUrl(incident.VerificationEvidenceImage);
				if (imageBytes == null || imageBytes.Length == 0)
				{
					result.Errors.Add($"Incident {incidentId}: could not decode verification image");
					result.FailedCount++;
					continue;
				}

				try
				{
					await IndexVerificationEvidenceAsync(
						incident.IncidentId,
						imageBytes,
						incident.OffenderName ?? $"Incident-{incidentId}",
						incident.OffenderId,
						cancellationToken);
					result.IndexedCount++;
					result.IndexedIncidentIds.Add(incidentId.ToString());
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "OffenderRecognition: reindex failed for incident {IncidentId}", incidentId);
					result.Errors.Add($"Incident {incidentId}: {ex.Message}");
					result.FailedCount++;
				}
			}

			result.SkippedCount = incidentIdsWithEvidence.Count - result.IndexedCount - result.FailedCount;
			return result;
		}

		public async Task<OffenderMatchResultDto> IndexAndMatchAsync(
			OffenderImageReferenceDto imageReference,
			CancellationToken cancellationToken = default)
		{
			if (imageReference is null) throw new ArgumentNullException(nameof(imageReference));

			byte[]? imageBytes = null;
			if (!string.IsNullOrWhiteSpace(imageReference.Url))
			{
				imageBytes = TryDecodeBase64DataUrl(imageReference.Url);
				if (imageBytes == null && imageReference.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using var http = new HttpClient();
						var resp = await http.GetAsync(imageReference.Url, cancellationToken);
						if (resp.IsSuccessStatusCode)
							imageBytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "OffenderRecognition: could not fetch image from URL");
					}
				}
			}

			if (imageBytes == null || imageBytes.Length == 0)
			{
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = "cv-not-configured"
				};
			}

			return await SearchByImageAsync(imageBytes, cancellationToken);
		}

		public async Task<OffenderMatchResultDto> FindMatchesByEmbeddingAsync(
			string embeddingId,
			int maxResults = 10,
			CancellationToken cancellationToken = default)
		{
			var embedding = await _db.FaceEmbeddings
				.FirstOrDefaultAsync(f => f.FaceEmbeddingId.ToString() == embeddingId, cancellationToken);
			if (embedding == null)
			{
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = "embedding-not-found"
				};
			}

			if (string.IsNullOrEmpty(embedding.AzurePersonId))
			{
				var heuristics = await FindCandidatesByHeuristicsAsync(embedding, maxResults, cancellationToken);
				return new OffenderMatchResultDto
				{
					FaceDetected = true,
					EmbeddingId = embeddingId,
					Candidates = heuristics,
					ClassifierVersion = embedding.ModelId
				};
			}

			var person = await _faceClient.GetPersonAsync(embedding.AzurePersonId, cancellationToken);
			if (person == null)
			{
				var heuristics = await FindCandidatesByHeuristicsAsync(embedding, maxResults, cancellationToken);
				return new OffenderMatchResultDto
				{
					FaceDetected = true,
					EmbeddingId = embeddingId,
					Candidates = heuristics,
					ClassifierVersion = embedding.ModelId
				};
			}

			int? incidentId = null;
			try
			{
				if (!string.IsNullOrEmpty(person.UserData))
				{
					using var doc = JsonDocument.Parse(person.UserData);
					if (doc.RootElement.TryGetProperty("incidentId", out var pid))
						incidentId = pid.TryGetInt32(out var id) ? id : null;
				}
			}
			catch { }

			if (incidentId.HasValue)
			{
				var incident = await _db.Incidents
					.AsNoTracking()
					.Include(i => i.StolenItems)
					.FirstOrDefaultAsync(i => i.IncidentId == incidentId.Value, cancellationToken);
				if (incident != null)
				{
					var relatedIncidents = await _db.Incidents
						.AsNoTracking()
						.Where(i => i.OffenderId == incident.OffenderId && !string.IsNullOrEmpty(incident.OffenderId))
						.OrderByDescending(i => i.DateOfIncident)
						.Take(5)
						.ToListAsync(cancellationToken);
					if (relatedIncidents.Count == 0) relatedIncidents.Add(incident);

					var totalValue = relatedIncidents.Sum(i =>
						i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
					var recentIncidents = relatedIncidents.Select(i => new OffenderMatchIncidentSummaryDto
					{
						IncidentId = i.IncidentId.ToString(),
						DateOfIncident = i.DateOfIncident.ToString("yyyy-MM-dd"),
						SiteName = i.StoreName ?? i.SiteId ?? "",
						IncidentType = i.IncidentType ?? "",
						Description = i.Description,
						OffenderMarks = i.OffenderMarks,
						OffenderDetailsVerified = i.OffenderDetailsVerified,
						VerificationMethod = i.VerificationMethod,
						VerificationEvidenceImage = i.VerificationEvidenceImage
					}).ToList();

					return new OffenderMatchResultDto
					{
						FaceDetected = true,
						EmbeddingId = embeddingId,
						Candidates = new List<OffenderMatchCandidateDto>
						{
							new()
							{
								OffenderName = person.Name ?? incident.OffenderName ?? "Unknown",
								IncidentCount = relatedIncidents.Count,
								TotalValue = totalValue,
								Similarity = 0.9,
								ThumbnailUrl = incident.VerificationEvidenceImage,
								RecentIncidents = recentIncidents
							}
						},
						ClassifierVersion = embedding.ModelId
					};
				}
			}

			var fallback = await FindCandidatesByHeuristicsAsync(embedding, maxResults, cancellationToken);
			return new OffenderMatchResultDto
			{
				FaceDetected = true,
				EmbeddingId = embeddingId,
				Candidates = fallback,
				ClassifierVersion = embedding.ModelId
			};
		}

		private static byte[]? TryDecodeBase64DataUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
				return null;
			var comma = url.IndexOf(',');
			if (comma < 0) return null;
			try
			{
				return Convert.FromBase64String(url[(comma + 1)..]);
			}
			catch
			{
				return null;
			}
		}

		private async Task<List<OffenderMatchCandidateDto>> FindCandidatesByHeuristicsAsync(
			FaceEmbedding sourceEmbedding,
			int maxResults,
			CancellationToken cancellationToken)
		{
			var candidates = new List<OffenderMatchCandidateDto>();
			if (string.IsNullOrWhiteSpace(sourceEmbedding.OffenderId)) return candidates;

			var relatedIncidents = await _db.Incidents
				.AsNoTracking()
				.Include(i => i.StolenItems)
				.Where(i => i.OffenderId == sourceEmbedding.OffenderId)
				.OrderByDescending(i => i.DateOfIncident)
				.Take(5)
				.ToListAsync(cancellationToken);
			if (relatedIncidents.Count == 0) return candidates;

			var totalValue = relatedIncidents.Sum(i =>
				i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
			var recentIncidents = relatedIncidents.Select(i => new OffenderMatchIncidentSummaryDto
			{
				IncidentId = i.IncidentId.ToString(),
				DateOfIncident = i.DateOfIncident.ToString("yyyy-MM-dd"),
				SiteName = i.StoreName ?? i.SiteId ?? "",
				IncidentType = i.IncidentType ?? "",
				Description = i.Description,
				OffenderMarks = i.OffenderMarks,
				OffenderDetailsVerified = i.OffenderDetailsVerified,
				VerificationMethod = i.VerificationMethod,
				VerificationEvidenceImage = i.VerificationEvidenceImage
			}).ToList();

			candidates.Add(new OffenderMatchCandidateDto
			{
				OffenderName = relatedIncidents.First().OffenderName ?? sourceEmbedding.OffenderId!,
				IncidentCount = relatedIncidents.Count,
				TotalValue = totalValue,
				Similarity = 0.9,
				ThumbnailUrl = relatedIncidents.First().VerificationEvidenceImage,
				RecentIncidents = recentIncidents
			});

			return candidates.Take(maxResults).ToList();
		}
	}
}
