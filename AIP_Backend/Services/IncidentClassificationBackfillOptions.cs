#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Configuration for the periodic classification backfill safety net.
	/// Bound to the <c>IncidentClassificationBackfill</c> section of appsettings.
	/// </summary>
	public sealed class IncidentClassificationBackfillOptions
	{
		/// <summary>
		/// Master switch. Defaults to true so the safety net is always on unless
		/// explicitly disabled for diagnostics.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Delay between application start and the first backfill pass. Keeps the
		/// background pressure off during boot while still draining any backlog
		/// within a couple of minutes of every release.
		/// </summary>
		public int StartupDelaySeconds { get; set; } = 30;

		/// <summary>
		/// Interval between successive backfill passes once the service is running.
		/// 60 minutes is a reasonable default: small enough to keep gaps short-lived,
		/// large enough that even an LLM outage producing many fallbacks does not
		/// generate continuous churn.
		/// </summary>
		public int IntervalMinutes { get; set; } = 60;

		/// <summary>
		/// Maximum incidents processed per pass. Caps Azure OpenAI request volume
		/// and database write load. Each pass exits early if there is nothing to
		/// process.
		/// </summary>
		public int BatchSize { get; set; } = 25;
	}
}
