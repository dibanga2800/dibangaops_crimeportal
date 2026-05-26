#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Idempotent backfill pass that classifies any incident whose AI-derived fields
	/// (<c>IncidentCategory</c> / <c>RiskLevel</c>) are missing. Acts as a safety net
	/// for the fire-and-forget classification path used by Create/Update, which can
	/// silently fail under load, transient Azure errors, or pod restarts.
	/// </summary>
	public interface IIncidentClassificationBackfillService
	{
		/// <summary>
		/// Classifies up to <paramref name="batchSize"/> incidents that are missing
		/// classification fields. Returns the number of incidents successfully
		/// classified and persisted. Safe to call concurrently - each row only flips
		/// from null -&gt; populated once.
		/// </summary>
		Task<int> RunAsync(int batchSize, CancellationToken cancellationToken);
	}
}
