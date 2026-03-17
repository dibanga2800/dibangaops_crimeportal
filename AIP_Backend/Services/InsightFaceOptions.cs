#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Configuration for InsightFace REST service used for offender recognition.
	/// When Enabled is true, the backend uses InsightFace instead of Azure Face API.
	/// </summary>
	public sealed class InsightFaceOptions
	{
		public bool Enabled { get; set; }
		public string BaseUrl { get; set; } = "http://localhost:8000";
		public int TimeoutSeconds { get; set; } = 30;

		/// <summary>
		/// Minimum cosine similarity (0–1) required for a candidate to be returned.
		/// Higher values mean stricter matching. When not set or invalid, a safe
		/// default is applied in the offender recognition service.
		/// </summary>
		public double? MinSimilarity { get; set; }

		/// <summary>
		/// Maximum number of top candidates to return for a single search.
		/// </summary>
		public int? MaxSearchResults { get; set; }
	}
}
