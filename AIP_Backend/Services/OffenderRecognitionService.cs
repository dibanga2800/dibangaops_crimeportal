#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Default implementation of <see cref="IOffenderRecognitionService"/> that
	/// orchestrates image storage, embedding persistence and similarity search.
	///
	/// IMPORTANT: this class does not perform any computer-vision work by itself.
	/// It is designed to call an external CV provider (Python microservice, Azure AI Vision, etc.).
	/// The integration point is the protected <see cref="GenerateEmbeddingAsync"/> method which
	/// can be overridden or adapted to your chosen model.
	/// </summary>
	public class OffenderRecognitionService : IOffenderRecognitionService
	{
		private readonly ApplicationDbContext _db;
		private readonly ILogger<OffenderRecognitionService> _logger;

		public OffenderRecognitionService(
			ApplicationDbContext db,
			ILogger<OffenderRecognitionService> logger)
		{
			_db = db;
			_logger = logger;
		}

		public async Task<OffenderMatchResultDto> IndexAndMatchAsync(
			OffenderImageReferenceDto imageReference,
			CancellationToken cancellationToken = default)
		{
			if (imageReference is null) throw new ArgumentNullException(nameof(imageReference));

			// 1) Ask the CV provider to detect a face and generate an embedding for this image.
			var (embeddingVector, modelId, faceDetected) = await GenerateEmbeddingAsync(imageReference, cancellationToken);

			if (!faceDetected || string.IsNullOrWhiteSpace(embeddingVector))
			{
				_logger.LogWarning("OffenderRecognition: no face detected for {FileName}", imageReference.FileName);
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					EmbeddingId = null,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = modelId
				};
			}

			// 2) Persist the embedding so it can be re-used for future searches.
			var faceEmbedding = new FaceEmbedding
			{
				OffenderId = imageReference.OffenderId?.ToString(),
				IncidentId = imageReference.IncidentId,
				FileName = imageReference.FileName,
				ModelId = modelId,
				Embedding = embeddingVector,
				CreatedAt = DateTime.UtcNow
			};

			_db.FaceEmbeddings.Add(faceEmbedding);
			await _db.SaveChangesAsync(cancellationToken);

			// 3) Run a similarity search against existing embeddings to propose matches.
			var candidates = await FindCandidatesAsync(faceEmbedding, cancellationToken);

			return new OffenderMatchResultDto
			{
				FaceDetected = true,
				EmbeddingId = faceEmbedding.FaceEmbeddingId.ToString(),
				Candidates = candidates,
				ClassifierVersion = modelId
			};
		}

		public async Task<OffenderMatchResultDto> FindMatchesByEmbeddingAsync(
			string embeddingId,
			int maxResults = 10,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(embeddingId))
				throw new ArgumentException("Embedding id is required", nameof(embeddingId));

			if (!int.TryParse(embeddingId, out var id))
			{
				throw new ArgumentException("Embedding id must be a valid integer primary key", nameof(embeddingId));
			}

			var embedding = await _db.FaceEmbeddings
				.FirstOrDefaultAsync(f => f.FaceEmbeddingId == id, cancellationToken);

			if (embedding is null)
			{
				_logger.LogWarning("OffenderRecognition: embedding {EmbeddingId} not found", embeddingId);
				return new OffenderMatchResultDto
				{
					FaceDetected = false,
					EmbeddingId = null,
					Candidates = new List<OffenderMatchCandidateDto>(),
					ClassifierVersion = "embedding-not-found"
				};
			}

			var candidates = await FindCandidatesAsync(embedding, cancellationToken, maxResults);

			return new OffenderMatchResultDto
			{
				FaceDetected = true,
				EmbeddingId = embeddingId,
				Candidates = candidates,
				ClassifierVersion = embedding.ModelId
			};
		}

		/// <summary>
		/// Generate an embedding for the supplied image reference by calling the
		/// underlying computer-vision provider.
		///
		/// The default implementation is intentionally a stub and must be replaced
		/// with a concrete integration (e.g. HTTP call to a Python/ML service or Azure AI Vision).
		/// </summary>
		/// <returns>
		/// Tuple of (embeddingVector, modelId, faceDetected).
		/// When faceDetected is false, embeddingVector should be null or empty.
		/// </returns>
		protected virtual Task<(string? embeddingVector, string modelId, bool faceDetected)> GenerateEmbeddingAsync(
			OffenderImageReferenceDto imageReference,
			CancellationToken cancellationToken)
		{
			_logger.LogWarning(
				"OffenderRecognition: GenerateEmbeddingAsync was called but no CV provider is configured. " +
				"Configure a computer-vision integration to enable offender recognition.");

			// For now we return a stub response so that the rest of the pipeline is safe to call in development.
			return Task.FromResult<(string?, string, bool)>((null, "cv-not-configured", false));
		}

		private async Task<List<OffenderMatchCandidateDto>> FindCandidatesAsync(
			FaceEmbedding sourceEmbedding,
			CancellationToken cancellationToken,
			int maxResults = 10)
		{
			// This method is designed for future enhancement with a proper vector similarity search.
			// Initially we fall back to very simple heuristics: same offender id, same incident offender name, etc.

			var candidates = new List<OffenderMatchCandidateDto>();

			// Try to match by OffenderId first when present.
			if (!string.IsNullOrWhiteSpace(sourceEmbedding.OffenderId))
			{
				var offenderId = sourceEmbedding.OffenderId;

				var relatedIncidents = await _db.Incidents
					.Where(i => i.OffenderId == offenderId)
					.ToListAsync(cancellationToken);

				if (relatedIncidents.Count > 0)
				{
					var totalValue = relatedIncidents.Sum(i =>
						i.TotalValueRecovered ?? i.StolenItems.Sum(s => s.TotalAmount));

					candidates.Add(new OffenderMatchCandidateDto
					{
						OffenderId = 0, // logical offender id is a string in the Incident model
						OffenderName = relatedIncidents.First().OffenderName ?? offenderId!,
						IncidentCount = relatedIncidents.Count,
						TotalValue = totalValue,
						Similarity = 0.9, // placeholder score until a real similarity engine is integrated
						ThumbnailUrl = null
					});
				}
			}

			// If we have no structured offender id, we still return an empty list rather than failing.
			return candidates
				.OrderByDescending(c => c.Similarity)
				.Take(Math.Max(1, maxResults))
				.ToList();
		}
	}
}

