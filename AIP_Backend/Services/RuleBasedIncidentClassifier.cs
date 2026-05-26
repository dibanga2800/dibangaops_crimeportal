#nullable enable

using System.Text.RegularExpressions;
using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Rule-based incident classifier. Evaluates risk purely from evidence on the
	/// incident (loss value, police involvement, stolen item count, violent
	/// language) - no implicit baseline from the incident type. The category is
	/// derived from the incident type first, with text-driven overrides used only
	/// when the description provides stronger evidence than the type itself.
	/// Replace with ML/AI provider by implementing <see cref="IIncidentClassifier"/>.
	/// </summary>
	public class RuleBasedIncidentClassifier : IIncidentClassifier
	{
		private readonly ILogger<RuleBasedIncidentClassifier> _logger;

		/// <summary>
		/// Word-boundary matched keywords that signal a violent component.
		/// Boundaries prevent false positives like "attackable" or "knifeless".
		/// The same regex is also scanned against the IncidentType so that types
		/// like "Colleague Assault" surface as violent even with no description.
		/// </summary>
		private static readonly Regex ViolentRegex = new(
			@"\b(weapon|knife|knives|assault|attack(ed|ing)?|blood(ied)?|injured|stabbed|punch(ed|ing)?)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Matches "attempted", "prevented", "deterred" and similar stage-modifier
		/// terms on the IncidentType. When present (and no violence signal exists)
		/// the accrued score is halved and capped below the Medium threshold so a
		/// prevented incident cannot bucket as Medium or High.
		/// </summary>
		private static readonly Regex StageDampenerRegex = new(
			@"\b(attempt|prevent|deter)\w*\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Word-boundary phrases that signal a weapon or physical injury component.
		/// Used by the category routing to upgrade non-violent types to "Violent
		/// Incident" when the description makes it clear violence occurred.
		/// </summary>
		private static readonly Regex WeaponEvidenceRegex = new(
			@"\b(weapon|knife|knives|stabbed|blood(ied)?)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Word-boundary phrases that signal organised retail crime. Used by the
		/// category routing to upgrade theft-style types to "Organised Retail Crime"
		/// when the description names a group, gang or syndicate.
		/// </summary>
		private static readonly Regex OrganisedCrimeRegex = new(
			@"\b(gang|organised|organized|syndicate)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Multi-token phrases that signal the offender was a staff member or insider.
		/// Bare keywords like "staff" alone are deliberately excluded because phrases
		/// such as "staff intervened" or "staff member witnessed" are extremely
		/// common in non-internal incidents and previously produced false positives.
		/// </summary>
		private static readonly string[] InternalTheftPhrases = new[]
		{
			"staff stole",
			"staff theft",
			"employee stole",
			"employee theft",
			"internal theft",
			"by staff member",
			"by an employee",
			"collusion"
		};

		public RuleBasedIncidentClassifier(ILogger<RuleBasedIncidentClassifier> logger)
		{
			_logger = logger;
		}

		public Task<IncidentClassificationResultDto> ClassifyAsync(IncidentClassificationRequestDto request)
		{
			var combinedText = $"{request.Description} {request.IncidentDetails}".ToLowerInvariant();
			var decision = DetermineCategoryDecision(combinedText, request.IncidentType);
			var rawScore = CalculateRiskScore(request, combinedText);
			var (riskLevel, riskScore) = IncidentRiskLevel.Bucket(rawScore);

			var actions = GenerateSuggestedActions(riskLevel, request, combinedText);
			var tags = GenerateTags(request, combinedText);

			var result = new IncidentClassificationResultDto
			{
				SuggestedCategory = decision.Category,
				RiskLevel = riskLevel,
				RiskScore = riskScore,
				Confidence = decision.Confidence,
				SuggestedActions = actions,
				Tags = tags,
				ClassifierVersion = "rule-based-v4"
			};

			_logger.LogInformation(
				"Classified incident {IncidentId}: category={Category} ({Confidence:P0}), risk={RiskLevel} ({RiskScore})",
				request.IncidentId, decision.Category, decision.Confidence, riskLevel, riskScore);

			return Task.FromResult(result);
		}

		/// <summary>
		/// Provenance of a category decision. Drives the confidence value reported
		/// back to the UI so reviewers can tell at a glance how the classifier
		/// arrived at the suggested category.
		/// </summary>
		private enum CategoryProvenance
		{
			/// <summary>Category derived from incident type alone; text was neutral.</summary>
			TypeOnly,

			/// <summary>Category derived from incident type and confirmed by description text.</summary>
			TypeWithTextAgreement,

			/// <summary>Category came from a description-driven override (text contradicted or strengthened the type).</summary>
			TextOverride,

			/// <summary>Neither type nor text yielded a confident category; flagged as "General Incident".</summary>
			Fallback
		}

		private readonly record struct CategoryDecision(string Category, CategoryProvenance Provenance)
		{
			public double Confidence => Provenance switch
			{
				CategoryProvenance.TypeWithTextAgreement => 0.90,
				CategoryProvenance.TypeOnly => 0.80,
				CategoryProvenance.TextOverride => 0.70,
				CategoryProvenance.Fallback => 0.50,
				_ => 0.50
			};
		}

		/// <summary>
		/// Resolves the suggested category from the incident type first, then applies
		/// description-driven upgrades (violence, organised crime, internal theft)
		/// when the text provides unambiguous evidence stronger than the type.
		/// Exposed as internal-static for unit testing.
		/// </summary>
		internal static (string Category, double Confidence) DetermineCategoryForTest(string text, string? incidentType)
		{
			var decision = DetermineCategoryDecision(text, incidentType);
			return (decision.Category, decision.Confidence);
		}

		private static CategoryDecision DetermineCategoryDecision(string text, string? incidentType)
		{
			var typeCategory = DetermineCategoryFromType(incidentType);

			// Severity upgrades from description. Priority: violence > organised
			// crime > internal theft. Only weapon/injury evidence is strong enough
			// to upgrade to "Violent Incident" - the bare ViolentRegex used by the
			// scorer would over-trigger on incident-type words like "Assault" that
			// the type-first layer already covers.
			if (WeaponEvidenceRegex.IsMatch(text))
			{
				var agrees = typeCategory == "Violent Incident";
				return new CategoryDecision(
					"Violent Incident",
					agrees ? CategoryProvenance.TypeWithTextAgreement : CategoryProvenance.TextOverride);
			}

			if (OrganisedCrimeRegex.IsMatch(text))
			{
				return new CategoryDecision("Organised Retail Crime", CategoryProvenance.TextOverride);
			}

			if (InternalTheftPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase)))
			{
				return new CategoryDecision("Internal Theft", CategoryProvenance.TextOverride);
			}

			if (typeCategory != null)
			{
				return new CategoryDecision(typeCategory, CategoryProvenance.TypeOnly);
			}

			return new CategoryDecision("General Incident", CategoryProvenance.Fallback);
		}

		/// <summary>
		/// Maps the incident type string to a canonical category. Operates on a
		/// normalised (trimmed, lower-cased) substring of the type so that the
		/// real human-readable values from the frontend lookup table ("Attempted
		/// Shoplifting", "Self-Scan Misuse", "Anti-social Behaviour", etc.) are
		/// matched correctly. Returns <c>null</c> when no fragment matches; the
		/// caller decides whether to fall back to "General Incident".
		/// </summary>
		private static string? DetermineCategoryFromType(string? incidentType)
		{
			if (string.IsNullOrWhiteSpace(incidentType))
			{
				return null;
			}

			var normalised = incidentType.Trim().ToLowerInvariant();

			if (normalised.Contains("self-scan", StringComparison.Ordinal) ||
				normalised.Contains("self scan", StringComparison.Ordinal))
			{
				return "Self-Scan Misuse";
			}

			var isStageModified = StageDampenerRegex.IsMatch(normalised);
			var isTheft = normalised.Contains("shoplift", StringComparison.Ordinal) ||
				normalised.Contains("theft", StringComparison.Ordinal) ||
				normalised.Contains("steal", StringComparison.Ordinal);

			if (isStageModified && isTheft)
			{
				return "Attempted Shoplifting";
			}

			if (normalised.Contains("fraud", StringComparison.Ordinal) ||
				normalised.Contains("counterfeit", StringComparison.Ordinal))
			{
				return "Fraud";
			}

			if (normalised.Contains("assault", StringComparison.Ordinal) ||
				normalised.Contains("attack", StringComparison.Ordinal) ||
				normalised.Contains("violent", StringComparison.Ordinal))
			{
				return "Violent Incident";
			}

			if (normalised.Contains("anti-social", StringComparison.Ordinal) ||
				normalised.Contains("antisocial", StringComparison.Ordinal) ||
				normalised.Contains("disorder", StringComparison.Ordinal))
			{
				return "Anti-Social Behaviour";
			}

			if (normalised.Contains("vandal", StringComparison.Ordinal) ||
				normalised.Contains("criminal damage", StringComparison.Ordinal))
			{
				return "Criminal Damage";
			}

			if (normalised.Contains("arrest", StringComparison.Ordinal))
			{
				return "Arrest";
			}

			if (normalised.Contains("trespass", StringComparison.Ordinal))
			{
				return "Trespass";
			}

			if (isTheft)
			{
				return "Shoplifting";
			}

			return null;
		}

		/// <summary>
		/// Computes a 0..1 risk score purely from evidence on the incident.
		/// Inputs (no implicit baseline from the incident type):
		///   - Value at risk (lost value, falling back to recovered when lost is null)
		///   - Police involvement
		///   - Stolen item count tiers
		///   - Violent-language signal (scans description, details and incident type)
		///   - Stage dampener for "attempted/prevented/deterred" types without violence
		///   - Floor of 0.05 so every recorded incident carries at least a sliver of risk
		/// </summary>
		internal static double CalculateRiskScore(IncidentClassificationRequestDto request, string text)
		{
			var score = 0.0;

			// Value at risk. Prefer TotalLostValue (the loss that actually represents risk);
			// fall back to TotalValueRecovered only as a coarse proxy when lost is null
			// (e.g. legacy incidents predating the field). Recovered value is NOT a risk
			// driver on its own; the fallback is intentionally conservative.
			//
			// Tiers are calibrated to the actual COOP retail loss distribution:
			// most observed incidents sit below £100, the current single-incident
			// maximum is ~£574, and anything in that £500+ band already represents
			// a material loss for the store. £500+ therefore reaches the High
			// bucket on loss alone (>= 0.70). £1,000+ pushes deeper into High to
			// leave room for further escalation from other signals as inflation
			// and product prices raise the realistic ceiling over time.
			var valueAtRisk = request.TotalLostValue ?? request.TotalValueRecovered ?? 0m;
			score += valueAtRisk switch
			{
				>= 1000m => 0.85,
				>= 500m => 0.75,
				>= 250m => 0.50,
				>= 100m => 0.30,
				> 0m => 0.10,
				_ => 0.0
			};

			if (request.PoliceInvolvement)
			{
				score += 0.25;
			}

			score += request.StolenItemCount switch
			{
				> 20 => 0.20,
				> 10 => 0.10,
				> 5 => 0.05,
				_ => 0.0
			};

			// Violence signal scans the combined description text AND the incident
			// type, so a "Colleague Assault" with no description still registers as
			// violent rather than falling to the 0.05 floor.
			var typeText = request.IncidentType ?? string.Empty;
			var violentSignal = ViolentRegex.IsMatch(text) || ViolentRegex.IsMatch(typeText);
			if (violentSignal)
			{
				score += 0.40;
			}

			// Stage dampener: a prevented or attempted incident is treated as a
			// successful intervention only when there was no actual realised loss.
			// We halve the accrued score and cap below 0.40 (the Medium threshold)
			// so "Attempted Shoplifting" with police involvement but no loss still
			// surfaces as Low.
			//
			// Evidence wins over labels: if a positive TotalLostValue was recorded,
			// the loss is the source of truth (the deterrence/attempt failed) and
			// the dampener steps aside. Violence in the description also disables
			// the dampener regardless of loss.
			var hasActualLoss = (request.TotalLostValue ?? 0m) > 0m;
			var isStageDampened = StageDampenerRegex.IsMatch(typeText)
				&& !violentSignal
				&& !hasActualLoss;
			if (isStageDampened)
			{
				score = Math.Min(score * 0.5, 0.39);
			}

			// Floor: every reported incident has at least a small acknowledgement
			// of risk. Prevents pure-zero scores looking like "no risk at all".
			score = Math.Max(0.05, score);

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
