#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Single source of truth for the score-to-level mapping used by every classifier.
	/// Whether the score comes from the rule-based classifier, Azure OpenAI, or any
	/// future provider, the persisted <c>RiskLevel</c> must be derived from the score
	/// using this method so the two fields are guaranteed coherent.
	/// </summary>
	public static class IncidentRiskLevel
	{
		public const string Low = "low";
		public const string Medium = "medium";
		public const string High = "high";

		/// <summary>
		/// Thresholds intentionally match the original rule-based classifier:
		///   score &gt;= 0.7 -&gt; high
		///   score &gt;= 0.4 -&gt; medium
		///   else            -&gt; low
		/// The score is clamped to [0, 1] before bucketing so out-of-range inputs
		/// from external providers never produce surprising labels.
		/// </summary>
		public static string FromScore(double riskScore)
		{
			var clamped = ClampScore(riskScore);
			return clamped switch
			{
				>= 0.7 => High,
				>= 0.4 => Medium,
				_ => Low
			};
		}

		/// <summary>
		/// Clamps any score (including NaN/Infinity, which LLMs occasionally emit)
		/// into the canonical [0, 1] range. NaN collapses to 0 (lowest).
		/// </summary>
		public static double ClampScore(double riskScore)
		{
			if (double.IsNaN(riskScore))
			{
				return 0.0;
			}

			if (riskScore < 0.0)
			{
				return 0.0;
			}

			if (riskScore > 1.0)
			{
				return 1.0;
			}

			return riskScore;
		}
	}
}
