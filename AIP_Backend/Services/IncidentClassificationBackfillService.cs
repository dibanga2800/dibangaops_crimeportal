#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Default backfill implementation. Resolves the configured
	/// <see cref="IIncidentClassifier"/> (Azure-with-rule-based-fallback in production)
	/// and the incident repository inside a scoped DI lifetime so it is safe to call
	/// from both the hosted timer and the admin endpoint.
	/// </summary>
	public sealed class IncidentClassificationBackfillService : IIncidentClassificationBackfillService
	{
		private readonly IIncidentRepository _repository;
		private readonly IIncidentClassifier _classifier;
		private readonly ILogger<IncidentClassificationBackfillService> _logger;

		public IncidentClassificationBackfillService(
			IIncidentRepository repository,
			IIncidentClassifier classifier,
			ILogger<IncidentClassificationBackfillService> logger)
		{
			_repository = repository;
			_classifier = classifier;
			_logger = logger;
		}

		public async Task<int> RunAsync(int batchSize, CancellationToken cancellationToken)
		{
			if (batchSize <= 0)
			{
				return 0;
			}

			var candidates = await _repository.GetIncidentsNeedingClassificationAsync(batchSize);
			if (candidates.Count == 0)
			{
				return 0;
			}

			_logger.LogInformation(
				"Classification backfill: processing {Count} incident(s) with missing AI fields",
				candidates.Count);

			var successCount = 0;

			foreach (var incident in candidates)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					var request = new IncidentClassificationRequestDto
					{
						IncidentId = incident.IncidentId,
						IncidentType = incident.IncidentType,
						Description = incident.Description,
						IncidentDetails = incident.IncidentDetails,
						TotalValueRecovered = incident.TotalValueRecovered,
						TotalLostValue = incident.TotalLostValue,
						PoliceInvolvement = incident.PoliceInvolvement,
						OffenderName = incident.OffenderName,
						StolenItemCount = incident.StolenItems?.Count ?? 0
					};

					var classification = await _classifier.ClassifyAsync(request);

					incident.IncidentCategory = classification.SuggestedCategory;
					incident.IncidentCategoryConfidence = classification.Confidence;
					incident.RiskLevel = classification.RiskLevel;
					incident.RiskScore = classification.RiskScore;
					incident.ClassificationVersion = classification.ClassifierVersion;

					if (string.IsNullOrWhiteSpace(incident.Priority))
					{
						incident.Priority = classification.RiskLevel;
					}

					await _repository.UpdateAsync(incident);
					successCount++;
				}
				catch (Exception ex)
				{
					// Skip and keep going. The row stays in the backlog and will be
					// retried on the next pass; we do not want one poison-pill row to
					// block the entire backfill.
					_logger.LogError(
						ex,
						"Classification backfill failed for incident {IncidentId}; will retry next cycle",
						incident.IncidentId);
				}
			}

			_logger.LogInformation(
				"Classification backfill: completed {Success}/{Total} incident(s)",
				successCount,
				candidates.Count);

			return successCount;
		}
	}
}
