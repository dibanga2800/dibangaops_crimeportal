#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Configuration for Azure Face API used for offender recognition.
	/// Create a Face resource in Azure Portal and add Endpoint + ApiKey.
	/// </summary>
	public sealed class AzureFaceOptions
	{
		public string Endpoint { get; set; } = string.Empty;
		public string ApiKey { get; set; } = string.Empty;
		public string PersonGroupId { get; set; } = "crime-portal-offenders";
		/// <summary>Identify confidence threshold (0.0–1.0). Lower = more permissive for marginal matches. Default 0.5.</summary>
		public double ConfidenceThreshold { get; set; } = 0.5;
		public bool Enabled { get; set; } = true;
	}
}
