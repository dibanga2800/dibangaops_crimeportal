#nullable enable

using System.Text.RegularExpressions;
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

		/// <summary>
		/// Word-boundary matched keywords that signal a violent component.
		/// Boundaries prevent false positives like "attackable" or "knifeless".
		/// </summary>
		private static readonly Regex ViolentRegex = new(
			@"\b(weapon|knife|knives|assault|attack(ed|ing)?|blood(ied)?|injured|stabbed)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Substring fragments used to derive a base risk for the incident type.
		/// Frontend types come from a runtime LookupTable (e.g. "Shoplifting", "Arrest - Saved?",
		/// "Anti-social behaviour"), so we match on normalised substrings rather than an exact enum.
		/// First match wins; ordered most-severe first.
		/// </summary>
		private static readonly (string Fragment, double Baseline)[] TypeBaselines = new[]
		{
			("assault", 0.7),
			("attack", 0.7),
			("violent", 0.7),
			("arrest", 0.5),
			("fraud", 0.5),
			("counterfeit", 0.5),
			("criminal damage", 0.4),
			("vandal", 0.4),
			// "prevention" and "deter" must precede "theft"/"shoplift" so that compound
			// types like "Theft Prevention" or "Deterred theft" - which describe a
			// successful intervention - score low rather than inheriting a generic theft
			// baseline.
			("prevention", 0.1),
			("deter", 0.1),
			("theft", 0.3),
			("shoplift", 0.3),
			("steal", 0.3),
			("anti-social", 0.3),
			("antisocial", 0.3),
			("disorder", 0.3),
			("trespass", 0.2)
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
				ClassifierVersion = "rule-based-v2"
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

		/// <summary>
		/// Returns the base risk for a given (free-form) incident type by case-insensitive
		/// substring match against <see cref="TypeBaselines"/>. Defaults to 0.2 if no match.
		/// Exposed as internal-static for unit testing.
		/// </summary>
		internal static double DetermineBaseRisk(string? incidentType)
		{
			if (string.IsNullOrWhiteSpace(incidentType))
			{
				return 0.2;
			}

			var normalised = incidentType.Trim().ToLowerInvariant();
			foreach (var (fragment, baseline) in TypeBaselines)
			{
				if (normalised.Contains(fragment, StringComparison.Ordinal))
				{
					return baseline;
				}
			}

			return 0.2;
		}

		/// <summary>
		/// Computes a 0..1 risk score from the classification request.
		/// Inputs: incident type baseline, value at risk (lost), police involvement,
		/// stolen item count tiers, and word-boundary-matched violent language in
		/// the combined description/details text.
		/// </summary>
		internal static double CalculateRiskScore(IncidentClassificationRequestDto request, string text)
		{
			var score = DetermineBaseRisk(request.IncidentType);

			// Value at risk. Prefer TotalLostValue (the loss that actually represents risk);
			// fall back to TotalValueRecovered only as a coarse proxy when lost is null
			// (e.g. older incidents predating the field). Recovered value is NOT a risk
			// driver on its own; the fallback is intentionally conservative.
			var valueAtRisk = request.TotalLostValue ?? request.TotalValueRecovered ?? 0m;
			score += valueAtRisk switch
			{
				>= 1000m => 0.3,
				>= 500m => 0.2,
				>= 100m => 0.1,
				_ => 0
			};

			if (request.PoliceInvolvement)
			{
				score += 0.15;
			}

			score += request.StolenItemCount switch
			{
				> 20 => 0.15,
				> 10 => 0.1,
				> 5 => 0.05,
				_ => 0
			};

			if (ViolentRegex.IsMatch(text))
			{
				score += 0.2;
			}

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
