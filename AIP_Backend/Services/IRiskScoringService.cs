#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Abstraction for predictive risk scoring at store / region level.
	/// Implementations typically wrap ML.NET models that are trained on
	/// historical incident data to estimate near-term risk.
	/// </summary>
	public interface IRiskScoringService
	{
		/// <summary>
		/// Get a risk score for a single store on a specific date (usually today).
		/// Implementations can read from a pre-computed cache or run the model on demand.
		/// </summary>
		Task<StoreRiskScoreDto?> GetStoreRiskScoreAsync(
			int storeId,
			DateTime forDate,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Get risk scores for all stores belonging to a given customer on the specified date.
		/// This is the typical entry point for Data Analytics Hub / management dashboards.
		/// </summary>
		Task<IReadOnlyList<StoreRiskScoreDto>> GetCustomerRiskScoresAsync(
			int customerId,
			DateTime forDate,
			CancellationToken cancellationToken = default);
	}
}

