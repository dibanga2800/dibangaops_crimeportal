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
	}
}
