#nullable enable

using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	/// <summary>
	/// IIncidentClassifier implementation that prefers Azure OpenAI but
	/// gracefully falls back to the existing RuleBasedIncidentClassifier
	/// when Azure OpenAI is disabled or fails.
	/// </summary>
	public sealed class AzureOpenAiIncidentClassifier : IIncidentClassifier
	{
		private readonly IAzureOpenAiClient _client;
		private readonly RuleBasedIncidentClassifier _fallback;
		private readonly AzureOpenAiOptions _options;
		private readonly ILogger<AzureOpenAiIncidentClassifier> _logger;

		public AzureOpenAiIncidentClassifier(
			IAzureOpenAiClient client,
			RuleBasedIncidentClassifier fallback,
			IOptions<AzureOpenAiOptions> options,
			ILogger<AzureOpenAiIncidentClassifier> logger)
		{
			_client = client;
			_fallback = fallback;
			_options = options.Value;
			_logger = logger;
		}

		public async Task<IncidentClassificationResultDto> ClassifyAsync(IncidentClassificationRequestDto request)
		{
			if (!_options.Enabled)
			{
				_logger.LogInformation("Azure OpenAI classification disabled; using rule-based classifier for incident {IncidentId}", request.IncidentId);
				return await _fallback.ClassifyAsync(request);
			}

			try
			{
				_logger.LogInformation("Classifying incident {IncidentId} using Azure OpenAI", request.IncidentId);
				return await _client.ClassifyIncidentAsync(request);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Azure OpenAI classification failed for incident {IncidentId}; falling back to rule-based classifier", request.IncidentId);
				return await _fallback.ClassifyAsync(request);
			}
		}
	}
}

