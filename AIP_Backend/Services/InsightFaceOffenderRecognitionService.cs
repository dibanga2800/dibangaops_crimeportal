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
	/// Offender recognition using InsightFace REST service. Stores embeddings in FaceEmbedding table
	/// and performs similarity search in .NET.
	/// </summary>
	public sealed class InsightFaceOffenderRecognitionService : IOffenderRecognitionService
	{
		private const string ModelId = "insightface-v1";
		private const int DefaultMaxSearchResults = 3;
		private const double DefaultMinSimilarity = 0.9;
		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

		private readonly ApplicationDbContext _db;
		private readonly IInsightFaceClient _client;
		private readonly InsightFaceOptions _options;
		private readonly ILogger<InsightFaceOffenderRecognitionService> _logger;

		public InsightFaceOffenderRecognitionService(
			ApplicationDbContext db,
			IInsightFaceClient client,
			IOptions<InsightFaceOptions> options,
			ILogger<InsightFaceOffenderRecognitionService> logger)
		{
			_db = db;
			_client = client;
			_options = options.Value;
			_logger = logger;
		}

		public async Task<OffenderMatchResultDto> DetectFaceOnlyAsync(
			byte[] imageBytes,
			CancellationToken cancellationToken = default,
			int? imageWidth = null,
			int? imageHeight = null)
		{
			if (!_options.Enabled)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "face-api-disabled" };

			var detectResult = await _client.DetectAsync(imageBytes, cancellationToken);
			if (detectResult == null)
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = ModelId,
					ServiceUnavailable = true,
					ServiceErrorMessage = "Face recognition service is unavailable. Ensure the InsightFace service is running."
				};
			if (!detectResult.FaceDetected)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = ModelId };

			if (imageWidth is > 0 && imageHeight is > 0 && detectResult.FaceRectangle != null)
			{
				var r = detectResult.FaceRectangle;
				var cx = imageWidth.Value / 2.0;
				var cy = imageHeight.Value / 2.0;
				var rx = Math.Min(imageWidth.Value * 0.3, 120.0);
				var ry = Math.Min(imageHeight.Value * 0.4, 180.0);
				if (!IsFaceRectFullyInsideEllipse(r.Left, r.Top, r.Width, r.Height, cx, cy, rx, ry))
					return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = ModelId };
			}

			return new OffenderMatchResultDto { FaceDetected = true, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = ModelId };
		}

		public async Task<OffenderMatchResultDto> SearchByImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "face-api-disabled" };

			var detectResult = await _client.DetectAsync(imageBytes, cancellationToken);
			if (detectResult == null)
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = ModelId,
					ServiceUnavailable = true,
					ServiceErrorMessage = "Face recognition service is unavailable. Ensure the InsightFace service is running."
				};
			if (!detectResult.FaceDetected)
			{
				_logger.LogInformation("InsightFace: no face detected in search image (size: {Size} bytes).", imageBytes.Length);
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = ModelId };
			}

			var embedResult = await _client.GetEmbeddingAsync(imageBytes, cancellationToken);
			if (embedResult == null || !embedResult.FaceDetected || embedResult.Embedding == null || embedResult.Embedding.Length == 0)
				return new OffenderMatchResultDto { FaceDetected = true, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = ModelId };

			var searchEmb = embedResult.Embedding;
			var minSimilarity = _options.MinSimilarity is > 0 and <= 1 ? _options.MinSimilarity.Value : DefaultMinSimilarity;
			var maxResults = _options.MaxSearchResults is > 0 and <= 20 ? _options.MaxSearchResults.Value : DefaultMaxSearchResults;

			var stored = await _db.FaceEmbeddings
				.AsNoTracking()
				.Where(f => f.ModelId == ModelId && !string.IsNullOrEmpty(f.Embedding))
				.ToListAsync(cancellationToken);

			var scored = new List<(FaceEmbedding Embedding, double Similarity)>();
			foreach (var row in stored)
			{
				var storedEmb = TryParseEmbedding(row.Embedding);
				if (storedEmb == null) continue;
				var sim = CosineSimilarity(searchEmb, storedEmb);
				scored.Add((row, sim));
			}

			var top = scored
				.OrderByDescending(x => x.Similarity)
				.Take(maxResults * 2) // widen initial pool slightly before thresholding and deduplication
				.ToList();
			var matches = new List<OffenderMatchCandidateDto>();
			var seenIncidentIds = new HashSet<int>();

			foreach (var (embedding, similarity) in top)
			{
				if (similarity < minSimilarity)
					continue;

				if (embedding.IncidentId == null || seenIncidentIds.Contains(embedding.IncidentId.Value)) continue;
				seenIncidentIds.Add(embedding.IncidentId.Value);

				var incident = await _db.Incidents
					.AsNoTracking()
					.Include(i => i.StolenItems)
					.FirstOrDefaultAsync(i => i.IncidentId == embedding.IncidentId.Value, cancellationToken);
				if (incident == null) continue;

				var relatedIncidents = await _db.Incidents
					.AsNoTracking()
					.Where(i => i.OffenderId == incident.OffenderId && !string.IsNullOrEmpty(incident.OffenderId))
					.OrderByDescending(i => i.DateOfIncident)
					.Take(5)
					.ToListAsync(cancellationToken);
				if (relatedIncidents.Count == 0)
					relatedIncidents.Add(incident);

				var totalValue = relatedIncidents.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
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
					OffenderName = incident.OffenderName ?? "Unknown",
					IncidentCount = relatedIncidents.Count,
					TotalValue = totalValue,
					Similarity = (double)Math.Clamp((decimal)similarity, 0, 1),
					ThumbnailUrl = incident.VerificationEvidenceImage,
					RecentIncidents = recentIncidents
				});

				if (matches.Count >= maxResults)
					break;
			}

			return new OffenderMatchResultDto
			{
				FaceDetected = true,
				Candidates = matches,
				ClassifierVersion = ModelId
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

			var embedResult = await _client.GetEmbeddingAsync(imageBytes, cancellationToken);
			if (embedResult == null || !embedResult.FaceDetected || embedResult.Embedding == null || embedResult.Embedding.Length == 0)
			{
				_logger.LogDebug("InsightFace: no face in verification evidence for incident {IncidentId}.", incidentId);
				return;
			}

			var embeddingJson = JsonSerializer.Serialize(embedResult.Embedding);
			var faceEmbedding = new FaceEmbedding
			{
				IncidentId = incidentId,
				OffenderId = offenderId,
				FileName = $"incident-{incidentId}-verification.jpg",
				ModelId = ModelId,
				Embedding = embeddingJson,
				AzurePersonId = null,
				CreatedAt = DateTime.UtcNow
			};
			_db.FaceEmbeddings.Add(faceEmbedding);
			await _db.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("InsightFace: indexed face for incident {IncidentId}.", incidentId);
		}

		public async Task<ReindexResultDto> ReindexVerificationEvidenceAsync(CancellationToken cancellationToken = default)
		{
			var result = new ReindexResultDto();
			if (!_options.Enabled) return result;

			var incidentIdsWithEvidence = await _db.Incidents
				.AsNoTracking()
				.Where(i => !string.IsNullOrWhiteSpace(i.VerificationEvidenceImage))
				.Select(i => i.IncidentId)
				.ToListAsync(cancellationToken);

			var alreadyIndexedInsightFace = await _db.FaceEmbeddings
				.AsNoTracking()
				.Where(f => f.ModelId == ModelId && f.IncidentId != null)
				.Select(f => f.IncidentId!.Value)
				.Distinct()
				.ToListAsync(cancellationToken);
			var alreadySet = new HashSet<int>(alreadyIndexedInsightFace);
			var toIndex = incidentIdsWithEvidence.Where(id => !alreadySet.Contains(id)).ToList();

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
					_logger.LogWarning(ex, "InsightFace: reindex failed for incident {IncidentId}.", incidentId);
					result.Errors.Add($"Incident {incidentId}: {ex.Message}");
					result.FailedCount++;
				}
			}

			result.SkippedCount = incidentIdsWithEvidence.Count - result.IndexedCount - result.FailedCount;
			return result;
		}

		public async Task<OffenderMatchResultDto> FindMatchesByEmbeddingAsync(string embeddingId, int maxResults = 10, CancellationToken cancellationToken = default)
		{
			var embedding = await _db.FaceEmbeddings
				.FirstOrDefaultAsync(f => f.FaceEmbeddingId.ToString() == embeddingId, cancellationToken);
			if (embedding == null)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "embedding-not-found" };

			var candidates = await FindCandidatesByHeuristicsAsync(embedding, maxResults, cancellationToken);
			return new OffenderMatchResultDto
			{
				FaceDetected = true,
				EmbeddingId = embeddingId,
				Candidates = candidates,
				ClassifierVersion = embedding.ModelId
			};
		}

		public async Task<OffenderMatchResultDto> IndexAndMatchAsync(OffenderImageReferenceDto imageReference, CancellationToken cancellationToken = default)
		{
			if (imageReference == null) throw new ArgumentNullException(nameof(imageReference));

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
						_logger.LogWarning(ex, "InsightFace: could not fetch image from URL.");
					}
				}
			}

			if (imageBytes == null || imageBytes.Length == 0)
				return new OffenderMatchResultDto { FaceDetected = false, Candidates = new List<OffenderMatchCandidateDto>(), ClassifierVersion = "cv-not-configured" };

			return await SearchByImageAsync(imageBytes, cancellationToken);
		}

		private static bool IsFaceRectFullyInsideEllipse(double left, double top, double width, double height, double cx, double cy, double rx, double ry)
		{
			if (rx <= 0 || ry <= 0) return false;
			var corners = new[] { (left, top), (left + width, top), (left, top + height), (left + width, top + height) };
			foreach (var (x, y) in corners)
			{
				var term = (y - cy) * (y - cy) / (rx * rx) + (x - cx) * (x - cx) / (ry * ry);
				if (term > 1.0) return false;
			}
			return true;
		}

		private static float[]? TryParseEmbedding(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return null;
			try
			{
				return JsonSerializer.Deserialize<float[]>(json);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>Cosine similarity for L2-normalized vectors (dot product).</summary>
		private static double CosineSimilarity(float[] a, float[] b)
		{
			if (a == null || b == null || a.Length != b.Length) return 0;
			double dot = 0;
			for (var i = 0; i < a.Length; i++)
				dot += a[i] * b[i];
			return Math.Clamp(dot, -1, 1);
		}

		private static byte[]? TryDecodeBase64DataUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
			var comma = url.IndexOf(',');
			if (comma < 0) return null;
			try { return Convert.FromBase64String(url[(comma + 1)..]); }
			catch { return null; }
		}

		private async Task<List<OffenderMatchCandidateDto>> FindCandidatesByHeuristicsAsync(FaceEmbedding sourceEmbedding, int maxResults, CancellationToken cancellationToken)
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

			var totalValue = relatedIncidents.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
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
				OffenderName = relatedIncidents[0].OffenderName ?? sourceEmbedding.OffenderId!,
				IncidentCount = relatedIncidents.Count,
				TotalValue = totalValue,
				Similarity = 0.8,
				ThumbnailUrl = relatedIncidents[0].VerificationEvidenceImage,
				RecentIncidents = recentIncidents
			});
			return candidates.Take(maxResults).ToList();
		}
	}
}
