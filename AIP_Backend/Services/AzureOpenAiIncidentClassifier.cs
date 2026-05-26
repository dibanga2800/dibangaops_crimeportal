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
				_logger.LogInformation(
					"Azure OpenAI classification disabled; using rule-based classifier for incident {IncidentId}",
					request.IncidentId);
				return await ClassifyWithRuleBasedFallbackAsync(request, fallbackReason: "disabled");
			}

			try
			{
				_logger.LogInformation("Classifying incident {IncidentId} using Azure OpenAI", request.IncidentId);
				return await _client.ClassifyIncidentAsync(request);
			}
			catch (Exception ex)
			{
				// Covers: network unreachable, HTTP 401/403/429/5xx, response timeout,
				// malformed/empty JSON, deserialisation errors. We never want a single
				// incident to end up unclassified just because the LLM is having a bad
				// day - rule-based is deterministic and always available.
				_logger.LogError(
					ex,
					"Azure OpenAI classification failed for incident {IncidentId}; falling back to rule-based classifier",
					request.IncidentId);
				return await ClassifyWithRuleBasedFallbackAsync(request, fallbackReason: "azure-error");
			}
		}

		/// <summary>
		/// Runs the rule-based classifier and tags the result so downstream consumers
		/// (analytics, audit, manual review) can tell which incidents were classified
		/// purely by the deterministic fallback path versus by Azure OpenAI.
		/// </summary>
		private async Task<IncidentClassificationResultDto> ClassifyWithRuleBasedFallbackAsync(
			IncidentClassificationRequestDto request,
			string fallbackReason)
		{
			var result = await _fallback.ClassifyAsync(request);
			result.ClassifierVersion = $"rule-based-fallback ({fallbackReason})";
			return result;
		}
	}
}

