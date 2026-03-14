#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Abstraction over the computer-vision layer responsible for
	/// indexing offender images and suggesting potential matches.
	///
	/// Implementations may call an external Python service, Azure AI Vision,
	/// or another CV provider. This interface is deliberately high-level so
	/// the rest of the application is decoupled from the underlying model.
	/// </summary>
	public interface IOffenderRecognitionService
	{
		/// <summary>
		/// Index a new offender image and, optionally, link it to an existing offender record.
		/// Returns basic match information so the caller can immediately see potential repeats.
		/// </summary>
		/// <param name="imageReference">
		/// Logical reference to the stored image (file name / URL plus contextual IDs).
		/// </param>
		/// <param name="cancellationToken">Cancellation token for long-running CV calls.</param>
		Task<OffenderMatchResultDto> IndexAndMatchAsync(
			OffenderImageReferenceDto imageReference,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Run a search for possible repeat offenders using an already-indexed embedding.
		/// This is useful when embeddings are generated out-of-band and only an embedding
		/// identifier is available.
		/// </summary>
		/// <param name="embeddingId">Identifier of an existing face embedding.</param>
		/// <param name="maxResults">Maximum number of candidates to return.</param>
		Task<OffenderMatchResultDto> FindMatchesByEmbeddingAsync(
			string embeddingId,
			int maxResults = 10,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Search for repeat offenders by capturing a new image and comparing against indexed faces in the DB.
		/// Uses Azure Face API to detect face, identify against Person Group, and return matching incidents.
		/// </summary>
		/// <param name="imageBytes">Raw image bytes (e.g. from camera capture or file upload).</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task<OffenderMatchResultDto> SearchByImageAsync(
			byte[] imageBytes,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Index verification evidence image when an incident is saved. Adds face to Person Group for future search.
		/// </summary>
		Task IndexVerificationEvidenceAsync(
			int incidentId,
			byte[] imageBytes,
			string offenderName,
			string? offenderId,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Re-index all incidents that have verification evidence but no FaceEmbedding (e.g. created before indexing existed).
		/// Call this to backfill faces for offenders like Pius Joan.
		/// </summary>
		Task<ReindexResultDto> ReindexVerificationEvidenceAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Detect if a face is present in the image. Lightweight check for guided capture UX.
		/// </summary>
		Task<OffenderMatchResultDto> DetectFaceOnlyAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
	}
}

