#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models
{
	/// <summary>
	/// Persisted risk score for a single store on a given date.
	/// This table is designed as an output cache for ML.NET risk models
	/// so dashboards can read pre-computed values quickly.
	/// </summary>
	public class StoreRiskScore
	{
		[Key]
		public int StoreRiskScoreId { get; set; }

		public int CustomerId { get; set; }

		/// <summary>
		/// Numeric store identifier when available (e.g. SiteID).
		/// </summary>
		public int? StoreId { get; set; }

		/// <summary>
		/// String SiteId as used elsewhere in the schema.
		/// </summary>
		[MaxLength(100)]
		public string? SiteId { get; set; }

		[MaxLength(200)]
		public string StoreName { get; set; } = string.Empty;

		public DateTime ForDate { get; set; }

		/// <summary>
		/// Normalised numeric risk score between 0 and 1.
		/// </summary>
		public double Score { get; set; }

		/// <summary>
		/// Discrete risk level (e.g. low / medium / high).
		/// </summary>
		[MaxLength(20)]
		public string Level { get; set; } = "low";

		public int? ExpectedIncidentsMin { get; set; }
		public int? ExpectedIncidentsMax { get; set; }

		/// <summary>
		/// Serialised list of peak risk windows (e.g. JSON string).
		/// </summary>
		public string? PeakRiskWindows { get; set; }

		[MaxLength(50)]
		public string ModelVersion { get; set; } = "risk-not-configured";

		public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
	}
}

