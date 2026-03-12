#nullable enable

using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Rule-based incident classifier. Evaluates risk using keyword and threshold heuristics.
	/// Replace with ML/AI provider by implementing IIncidentClassifier.
	/// </summary>
	public class RuleBasedIncidentClassifier : IIncidentClassifier
	{
		private readonly ILogger<RuleBasedIncidentClassifier> _logger;

		private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
		{
			["Organised Retail Crime"] = new[] { "gang", "organised", "organized", "syndicate", "repeat", "network", "group" },
			["Violent Incident"] = new[] { "assault", "attack", "weapon", "knife", "threat", "violence", "aggressive", "punch", "injured" },
			["Internal Theft"] = new[] { "employee", "staff", "internal", "collusion", "insider" },
			["Shoplifting"] = new[] { "shoplifting", "concealed", "walked out", "unpaid", "theft" },
			["Fraud"] = new[] { "fraud", "counterfeit", "scam", "refund fraud", "return fraud", "fake" },
			["Anti-Social Behaviour"] = new[] { "drunk", "disorderly", "abuse", "verbal", "harassment", "nuisance" }
		};

		private static readonly Dictionary<string, double> TypeBaseRisk = new(StringComparer.OrdinalIgnoreCase)
		{
			["THEFT"] = 0.3,
			["THEFT_PREVENTION"] = 0.2,
			["ARREST"] = 0.5,
			["DETER"] = 0.1,
			["ASSAULT"] = 0.7,
			["FRAUD"] = 0.5,
			["ANTI_SOCIAL"] = 0.3,
			["CRIMINAL_DAMAGE"] = 0.4,
			["TRESPASS"] = 0.2,
			["OTHER"] = 0.2
		};

		public RuleBasedIncidentClassifier(ILogger<RuleBasedIncidentClassifier> logger)
		{
			_logger = logger;
		}

		public Task<IncidentClassificationResultDto> ClassifyAsync(IncidentClassificationRequestDto request)
		{
			var combinedText = $"{request.Description} {request.IncidentDetails}".ToLowerInvariant();
			var suggestedCategory = DetermineCategory(combinedText, request.IncidentType);
			var riskScore = CalculateRiskScore(request, combinedText);
			var riskLevel = riskScore switch
			{
				>= 0.7 => "high",
				>= 0.4 => "medium",
				_ => "low"
			};

			var actions = GenerateSuggestedActions(riskLevel, request, combinedText);
			var tags = GenerateTags(request, combinedText);

			var result = new IncidentClassificationResultDto
			{
				SuggestedCategory = suggestedCategory,
				RiskLevel = riskLevel,
				RiskScore = Math.Round(riskScore, 2),
				Confidence = 0.75,
				SuggestedActions = actions,
				Tags = tags,
				ClassifierVersion = "rule-based-v1"
			};

			_logger.LogInformation(
				"Classified incident {IncidentId}: category={Category}, risk={RiskLevel} ({RiskScore})",
				request.IncidentId, suggestedCategory, riskLevel, riskScore);

			return Task.FromResult(result);
		}

		private static string DetermineCategory(string text, string incidentType)
		{
			foreach (var (category, keywords) in CategoryKeywords)
			{
				if (keywords.Any(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase)))
				{
					return category;
				}
			}

			return incidentType switch
			{
				"THEFT" or "THEFT_PREVENTION" => "Shoplifting",
				"ARREST" => "Arrest",
				"ASSAULT" => "Violent Incident",
				"FRAUD" => "Fraud",
				"ANTI_SOCIAL" => "Anti-Social Behaviour",
				_ => "General Incident"
			};
		}

		private static double CalculateRiskScore(IncidentClassificationRequestDto request, string text)
		{
			var score = TypeBaseRisk.GetValueOrDefault(request.IncidentType, 0.2);

			if (request.TotalValueRecovered.HasValue)
			{
				score += request.TotalValueRecovered.Value switch
				{
					>= 1000m => 0.3,
					>= 500m => 0.2,
					>= 100m => 0.1,
					_ => 0
				};
			}

			if (request.PoliceInvolvement)
				score += 0.15;

			if (!string.IsNullOrWhiteSpace(request.OffenderName))
				score += 0.05;

			if (request.StolenItemCount > 5)
				score += 0.1;

			var violentKeywords = new[] { "weapon", "knife", "assault", "attack", "blood", "injured" };
			if (violentKeywords.Any(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase)))
				score += 0.2;

			return Math.Min(score, 1.0);
		}

		private static List<string> GenerateSuggestedActions(string riskLevel, IncidentClassificationRequestDto request, string text)
		{
			var actions = new List<string>();

			if (riskLevel == "high")
			{
				actions.Add("Escalate to Loss Prevention Manager immediately");
				actions.Add("Review CCTV footage for the incident period");
			}

			if (request.PoliceInvolvement)
				actions.Add("Follow up with police for case reference updates");

			if (request.TotalValueRecovered > 500)
				actions.Add("Flag for value recovery review");

			if (text.Contains("repeat", StringComparison.OrdinalIgnoreCase) ||
				text.Contains("known", StringComparison.OrdinalIgnoreCase))
				actions.Add("Cross-reference with repeat offender database");

			if (!string.IsNullOrWhiteSpace(request.OffenderName))
				actions.Add("Verify offender identity and check prior incidents");

			if (actions.Count == 0)
				actions.Add("Standard processing - no immediate escalation required");

			return actions;
		}

		private static List<string> GenerateTags(IncidentClassificationRequestDto request, string text)
		{
			var tags = new List<string> { request.IncidentType };

			if (request.PoliceInvolvement)
				tags.Add("police-involved");

			if (request.TotalValueRecovered > 500)
				tags.Add("high-value");

			if (!string.IsNullOrWhiteSpace(request.OffenderName))
				tags.Add("offender-identified");

			if (text.Contains("weapon", StringComparison.OrdinalIgnoreCase) ||
				text.Contains("knife", StringComparison.OrdinalIgnoreCase))
				tags.Add("weapon-involved");

			if (text.Contains("gang", StringComparison.OrdinalIgnoreCase) ||
				text.Contains("organised", StringComparison.OrdinalIgnoreCase))
				tags.Add("organised-crime");

			return tags.Distinct().ToList();
		}
	}
}
