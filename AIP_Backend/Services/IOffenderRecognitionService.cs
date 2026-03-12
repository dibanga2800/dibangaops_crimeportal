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
	}
}

