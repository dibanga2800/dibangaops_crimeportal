#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Client for Azure Face API: detect faces, manage Person Group, and identify.
	/// </summary>
	public interface IAzureFaceClient
	{
		/// <summary>
		/// Detect faces in an image. Returns faceIds for use with Identify, or null if disabled/failed.
		/// </summary>
		/// <param name="preferPermissiveModel">When true, try detection_01 first (more permissive) for guided capture UX.</param>
		Task<AzureFaceDetectResult?> DetectFacesAsync(byte[] imageBytes, CancellationToken cancellationToken = default, bool preferPermissiveModel = false);

		/// <summary>
		/// Ensure the Person Group exists; create it if missing.
		/// </summary>
		Task<bool> EnsurePersonGroupExistsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a Person, add a face to it, and optionally trigger training. Returns PersonId or null.
		/// </summary>
		Task<string?> CreatePersonAndAddFaceAsync(
			string name,
			byte[] faceImageBytes,
			string userData,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Train the Person Group. Call after adding new faces.
		/// </summary>
		Task<bool> TrainPersonGroupAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Wait for Person Group training to complete. Required before Identify will find newly added faces.
		/// </summary>
		Task<bool> WaitForTrainingCompletionAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Identify faceIds against the Person Group. Returns matched (PersonId, Confidence) pairs.
		/// </summary>
		Task<IReadOnlyList<AzureFaceIdentifyCandidate>> IdentifyAsync(
			IReadOnlyList<string> faceIds,
			int maxNumOfCandidatesReturned = 10,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Get Person details including userData (e.g. incidentId JSON).
		/// </summary>
		Task<AzurePerson?> GetPersonAsync(string personId, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Face rectangle from Azure Detect API (top, left, width, height in pixels).
	/// </summary>
	public class AzureFaceRectangle
	{
		public int Top { get; set; }
		public int Left { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
	}

	/// <summary>
	/// Single face from Detect API response.
	/// </summary>
	public class AzureFaceDetectItem
	{
		public string? FaceId { get; set; }
		public AzureFaceRectangle? FaceRectangle { get; set; }
	}

	/// <summary>
	/// Result of Detect API.
	/// </summary>
	public class AzureFaceDetectResult
	{
		public List<AzureFaceDetectItem> Faces { get; set; } = new();
	}

	/// <summary>
	/// Matched person from Identify API.
	/// </summary>
	public class AzureFaceIdentifyCandidate
	{
		public string? PersonId { get; set; }
		public double Confidence { get; set; }
	}

	/// <summary>
	/// Person from GetPerson API.
	/// </summary>
	public class AzurePerson
	{
		public string? PersonId { get; set; }
		public string? Name { get; set; }
		public string? UserData { get; set; }
	}
}
