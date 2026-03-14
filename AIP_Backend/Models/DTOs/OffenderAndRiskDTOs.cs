#nullable enable

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// Reference to an offender image that should be indexed or searched.
	/// The image can be addressed by a persisted URL or by file name in the evidence store.
	/// </summary>
	public sealed class OffenderImageReferenceDto
	{
		public int? OffenderId { get; set; }
		public int? IncidentId { get; set; }
		public int? CustomerId { get; set; }
		public string? SiteId { get; set; }
		public string? RegionId { get; set; }

		/// <summary>
		/// Logical file name or blob name for the stored image.
		/// </summary>
		public string FileName { get; set; } = string.Empty;

		/// <summary>
		/// Public or signed URL that the vision service can use to retrieve the image.
		/// </summary>
		public string? Url { get; set; }
	}

	/// <summary>
	/// Summary of a single incident for offender match display.
	/// </summary>
	public sealed class OffenderMatchIncidentSummaryDto
	{
		public string IncidentId { get; set; } = string.Empty;
		public string DateOfIncident { get; set; } = string.Empty;
		public string SiteName { get; set; } = string.Empty;
		public string IncidentType { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string? OffenderMarks { get; set; }
		public bool? OffenderDetailsVerified { get; set; }
		public string? VerificationMethod { get; set; }
		public string? VerificationEvidenceImage { get; set; }
	}

	/// <summary>
	/// A single candidate returned from the offender recognition engine.
	/// </summary>
	public sealed class OffenderMatchCandidateDto
	{
		public int OffenderId { get; set; }
		public string OffenderName { get; set; } = string.Empty;
		public int IncidentCount { get; set; }
		public decimal? TotalValue { get; set; }

		/// <summary>
		/// Similarity score between 0 and 1 (cosine similarity or equivalent).
		/// </summary>
		public double Similarity { get; set; }

		/// <summary>
		/// Optional thumbnail URL to show in the UI.
		/// </summary>
		public string? ThumbnailUrl { get; set; }

		/// <summary>
		/// Recent incidents linked to this offender for UI display.
		/// </summary>
		public List<OffenderMatchIncidentSummaryDto> RecentIncidents { get; set; } = new();
	}

	/// <summary>
	/// Result of re-indexing verification evidence for incidents created before face indexing existed.
	/// </summary>
	public sealed class ReindexResultDto
	{
		public int IndexedCount { get; set; }
		public int SkippedCount { get; set; }
		public int FailedCount { get; set; }
		public List<string> IndexedIncidentIds { get; set; } = new();
		public List<string> Errors { get; set; } = new();
	}

	/// <summary>
	/// Result of running offender recognition against a single image.
	/// </summary>
	public sealed class OffenderMatchResultDto
	{
		public bool FaceDetected { get; set; }

		/// <summary>
		/// Identifier of the stored face embedding (database or vector index key).
		/// </summary>
		public string? EmbeddingId { get; set; }

		public List<OffenderMatchCandidateDto> Candidates { get; set; } = new();
		public string ClassifierVersion { get; set; } = "cv-not-configured";
	}

	/// <summary>
	/// Aggregated incident pattern summary suitable for dashboards and analytics.
	/// Re-uses existing hot location, trend, and category DTOs.
	/// </summary>
	public sealed class IncidentPatternSummaryDto
	{
		public List<HotLocationDto> HotLocations { get; set; } = new();
		public List<TrendDataPointDto> IncidentTrend { get; set; } = new();
		public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
		public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
	}

	/// <summary>
	/// Predicted risk score for a single store on a given day or shift.
	/// </summary>
	public sealed class StoreRiskScoreDto
	{
		public int StoreId { get; set; }
		public string StoreName { get; set; } = string.Empty;
		public DateTime ForDate { get; set; }

		/// <summary>
		/// Normalised numeric risk score between 0 and 1.
		/// </summary>
		public double Score { get; set; }

		/// <summary>
		/// Discrete risk level derived from the score (e.g. low, medium, high).
		/// </summary>
		public string Level { get; set; } = "low";

		/// <summary>
		/// Optional predicted incident count range for the period.
		/// </summary>
		public int? ExpectedIncidentsMin { get; set; }
		public int? ExpectedIncidentsMax { get; set; }

		/// <summary>
		/// Human-readable peak risk windows (e.g. 18:00-21:00).
		/// </summary>
		public List<string> PeakRiskWindows { get; set; } = new();

		/// <summary>
		/// Version of the underlying model used to produce this score.
		/// </summary>
		public string ModelVersion { get; set; } = "risk-not-configured";
	}
}

