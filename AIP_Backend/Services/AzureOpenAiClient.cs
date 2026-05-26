#nullable enable

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	/// <summary>
	/// Default Azure OpenAI HTTP client for incident-related prompts.
	/// Uses Chat Completions with a JSON response format contract.
	/// </summary>
	public sealed class AzureOpenAiClient : IAzureOpenAiClient
	{
		private readonly HttpClient _httpClient;
		private readonly AzureOpenAiOptions _options;
		private readonly ILogger<AzureOpenAiClient> _logger;

		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
		{
			PropertyNameCaseInsensitive = true
		};

		public AzureOpenAiClient(
			HttpClient httpClient,
			IOptions<AzureOpenAiOptions> options,
			ILogger<AzureOpenAiClient> logger)
		{
			_httpClient = httpClient;
			_options = options.Value;
			_logger = logger;

			if (!string.IsNullOrWhiteSpace(_options.Endpoint))
			{
				_httpClient.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/");
			}
		}

		public async Task<IncidentClassificationResultDto> ClassifyIncidentAsync(
			IncidentClassificationRequestDto request,
			CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled)
			{
				throw new InvalidOperationException("Azure OpenAI is disabled in configuration.");
			}

			if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
			    string.IsNullOrWhiteSpace(_options.ApiKey) ||
			    string.IsNullOrWhiteSpace(_options.Deployment))
			{
				throw new InvalidOperationException("Azure OpenAI configuration is incomplete. Please set Endpoint, ApiKey, and Deployment.");
			}

			var url = $"openai/deployments/{_options.Deployment}/chat/completions?api-version=2024-02-15-preview";

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
			requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Api-Key", _options.ApiKey);

			var systemPrompt =
				"You are an AI crime and loss-prevention analyst for a UK retail co-operative. " +
				"Given a structured incident record, you must respond ONLY with a strict JSON object " +
				"matching this C# shape: " +
				"{ \"suggestedCategory\": string, \"riskLevel\": \"low\"|\"medium\"|\"high\", " +
				"\"riskScore\": number, \"confidence\": number, " +
				"\"suggestedActions\": string[], \"tags\": string[], \"classifierVersion\": string }. " +
				"Do not include any extra fields or text. " +
				"RiskScore must be a number between 0 and 1 (inclusive). " +
				"RiskLevel MUST be derived from riskScore using these exact thresholds: " +
				"\"high\" if riskScore >= 0.7, \"medium\" if 0.4 <= riskScore < 0.7, \"low\" otherwise. " +
				"Risk should reflect VALUE AT RISK (totalLostValue), violence, weapon use, organised crime indicators, " +
				"and stolen-item volume. TotalValueRecovered is the value SAVED and should NOT increase risk. " +
				"Confidence is your own self-assessment in [0, 1]; do not echo riskScore.";

			var userContent = new
			{
				request.IncidentId,
				request.IncidentType,
				request.Description,
				request.IncidentDetails,
				request.TotalValueRecovered,
				request.TotalLostValue,
				request.PoliceInvolvement,
				request.OffenderName,
				request.IncidentInvolved,
				request.StolenItemCount
			};

			var payload = new
			{
				messages = new[]
				{
					new { role = "system", content = systemPrompt },
					new
					{
						role = "user",
						content = "Classify this incident and return JSON only:\n" +
						          JsonSerializer.Serialize(userContent, JsonOptions)
					}
				},
				temperature = 0.1,
				max_tokens = 512,
				response_format = new { type = "json_object" }
			};

			var json = JsonSerializer.Serialize(payload, JsonOptions);
			requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				var statusCode = (int)response.StatusCode;
				// 401/403 typically indicate invalid API key or expired subscription
				if (statusCode is 401 or 403)
				{
					_logger.LogWarning(
						"Azure OpenAI authentication failed (HTTP {StatusCode}). Check ApiKey and subscription. " +
						"This may indicate an expired subscription or invalid key. Falling back to rule-based classifier. Response: {Body}",
						response.StatusCode,
						body);
				}
				else
				{
					_logger.LogWarning("Azure OpenAI returned {StatusCode}: {Body}", response.StatusCode, body);
				}

				throw new InvalidOperationException($"Azure OpenAI call failed with status code {response.StatusCode}");
			}

			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

			try
			{
				using var document = JsonDocument.Parse(responseBody);
				var root = document.RootElement;
				var content = root
					.GetProperty("choices")[0]
					.GetProperty("message")
					.GetProperty("content")
					.GetString();

				if (string.IsNullOrWhiteSpace(content))
				{
					throw new InvalidOperationException("Azure OpenAI response content was empty.");
				}

				var result = JsonSerializer.Deserialize<IncidentClassificationResultDto>(content, JsonOptions);
				if (result == null)
				{
					throw new InvalidOperationException("Failed to deserialize Azure OpenAI JSON into IncidentClassificationResultDto.");
				}

				// Canonicalise the score and re-derive the level server-side. The LLM has
				// been observed returning incoherent pairs like (riskLevel="low", riskScore=0.7).
				// IncidentRiskLevel.Bucket is the single source of truth - it derives the
				// level from the precise (unrounded) score and only rounds for storage
				// afterwards, so an LLM-emitted 0.6999 stays "medium" rather than being
				// rounded up to 0.70 and mis-bucketed into "high".
				var (level, storedScore) = IncidentRiskLevel.Bucket(result.RiskScore);
				result.RiskScore = storedScore;
				result.RiskLevel = level;
				result.Confidence = IncidentRiskLevel.ClampScore(result.Confidence);

				// Bump version so v1 (incoherent level) and v2 (canonicalised) rows are
				// distinguishable in the database for audit and backfill purposes.
				result.ClassifierVersion = "azure-openai-v2";
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to parse Azure OpenAI response: {Body}", responseBody);
				throw;
			}
		}
	}
}

