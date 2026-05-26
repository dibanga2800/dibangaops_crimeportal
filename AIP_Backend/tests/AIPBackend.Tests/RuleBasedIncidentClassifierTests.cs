using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIPBackend.Tests;

/// <summary>
/// Tests for the rule-based incident classifier risk scoring (rule-based-v3).
///
/// v3 is purely evidence-based: there is no implicit baseline from the incident
/// type. The score is driven by value-at-risk (lost value, falling back to
/// recovered when lost is null), police involvement, stolen-item-count tiers,
/// and word-boundary-matched violent language in the combined description,
/// details and incident-type text. A stage dampener halves the score for
/// "attempted"/"prevented"/"deterred" incidents that show no violence signal,
/// and a 0.05 floor ensures every reported incident registers as at least
/// nominal risk in the dashboard.
/// </summary>
public class RuleBasedIncidentClassifierTests
{
	private static RuleBasedIncidentClassifier CreateClassifier() =>
		new(NullLogger<RuleBasedIncidentClassifier>.Instance);

	private static IncidentClassificationRequestDto BuildRequest(
		string incidentType = "Shoplifting",
		string? description = null,
		string? incidentDetails = null,
		decimal? totalLostValue = null,
		decimal? totalValueRecovered = null,
		bool policeInvolvement = false,
		string? offenderName = null,
		int stolenItemCount = 0) =>
		new()
		{
			IncidentId = 1,
			IncidentType = incidentType,
			Description = description,
			IncidentDetails = incidentDetails,
			TotalLostValue = totalLostValue,
			TotalValueRecovered = totalValueRecovered,
			PoliceInvolvement = policeInvolvement,
			OffenderName = offenderName,
			StolenItemCount = stolenItemCount
		};

	[Theory]
	[InlineData("Shoplifting", 0.05)]
	[InlineData("Theft", 0.05)]
	[InlineData("Self-Scan Misuse", 0.05)]
	[InlineData("Anti-social Behaviour", 0.05)]
	[InlineData("Trespass", 0.05)]
	[InlineData("Fraud", 0.05)]
	[InlineData("Criminal Damage", 0.05)]
	[InlineData("Unknown Type", 0.05)]
	[InlineData("", 0.05)]
	public async Task Non_violent_type_alone_yields_the_floor(string incidentType, double expectedScore)
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(incidentType: incidentType);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(expectedScore, result.RiskScore, precision: 2);
	}

	[Theory]
	[InlineData(0.0, 0.05)]    // £0 -> floor only
	[InlineData(3.15, 0.10)]   // Typical petty theft from the dashboard data
	[InlineData(50.0, 0.10)]   // > 0 and < £100 -> +0.10
	[InlineData(99.99, 0.10)]
	[InlineData(100.0, 0.30)]  // >= £100 and < £250 -> +0.30 (Low high)
	[InlineData(249.99, 0.30)]
	[InlineData(250.0, 0.50)]  // >= £250 and < £500 -> +0.50 (Medium entry)
	[InlineData(499.99, 0.50)]
	[InlineData(500.0, 0.75)]  // >= £500 and < £1000 -> +0.75 (High - already material for the store)
	[InlineData(574.15, 0.75)] // Current single-incident maximum in the system
	[InlineData(999.99, 0.75)]
	[InlineData(1000.0, 0.85)] // >= £1000 -> +0.85 (elevated High, room for future inflation)
	[InlineData(5000.0, 0.85)]
	public async Task Lost_value_tiers_drive_risk_score(double lostValue, double expectedScore)
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: (decimal)lostValue);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(expectedScore, result.RiskScore, precision: 2);
	}

	[Fact]
	public async Task Recovered_value_does_not_inflate_risk_when_nothing_was_lost()
	{
		var classifier = CreateClassifier();

		// £2000 recovered (good outcome) and £0 lost - the loss tier contributes
		// nothing and the score should bottom out at the 0.05 floor.
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: 0m,
			totalValueRecovered: 2000m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.05, result.RiskScore, precision: 2);
	}

	[Fact]
	public async Task Recovered_value_is_used_as_proxy_when_lost_value_is_null()
	{
		var classifier = CreateClassifier();

		// Legacy incidents where TotalLostValue was never persisted fall back to
		// recovered as a coarse proxy. £600 recovered (lost null) -> 0.75 tier.
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: null,
			totalValueRecovered: 600m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.75, result.RiskScore, precision: 2);
	}

	[Theory]
	[InlineData(0, 0.05)]   // 0 items -> floor only
	[InlineData(5, 0.05)]   // <= 5 -> no bonus, floor wins
	[InlineData(6, 0.05)]   // 6-10 -> +0.05 (equals floor)
	[InlineData(11, 0.10)]  // 11-20 -> +0.10
	[InlineData(25, 0.20)]  // > 20 -> +0.20
	public async Task Stolen_item_count_is_tiered_not_binary(int itemCount, double expectedScore)
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(
			incidentType: "Shoplifting",
			stolenItemCount: itemCount);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(expectedScore, result.RiskScore, precision: 2);
	}

	[Fact]
	public async Task Police_involvement_adds_fixed_bonus()
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(
			incidentType: "Shoplifting",
			policeInvolvement: true);

		var result = await classifier.ClassifyAsync(request);

		// 0 base + 0.25 police = 0.25
		Assert.Equal(0.25, result.RiskScore, precision: 2);
	}

	[Fact]
	public async Task Offender_name_alone_does_not_change_risk_score()
	{
		var classifier = CreateClassifier();

		var withoutOffender = await classifier.ClassifyAsync(
			BuildRequest(incidentType: "Shoplifting"));

		var withOffender = await classifier.ClassifyAsync(
			BuildRequest(incidentType: "Shoplifting", offenderName: "John Doe"));

		Assert.Equal(withoutOffender.RiskScore, withOffender.RiskScore);
	}

	[Theory]
	[InlineData("Offender stabbed staff with knife", true)]
	[InlineData("Offender attacked the manager", true)]
	[InlineData("Weapon was brandished", true)]
	[InlineData("Was bloodied during the struggle", true)]
	// Word-boundary checks: these should NOT match the violent regex.
	[InlineData("This was an attackable display", false)]
	[InlineData("Customer panicked but no violence", false)]
	[InlineData("Witness lives off Knifecrest Avenue", false)]
	public async Task Violent_keyword_bonus_uses_word_boundaries(string description, bool shouldTriggerBonus)
	{
		var classifier = CreateClassifier();

		// Baseline uses £150 lost (loss tier = 0.20) so the score sits above the
		// 0.05 floor; otherwise the floor would mask the visible +0.40 violence
		// bonus and the delta would collapse to 0.35.
		var withText = await classifier.ClassifyAsync(BuildRequest(
			incidentType: "Shoplifting",
			description: description,
			totalLostValue: 150m));

		var withoutText = await classifier.ClassifyAsync(BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: 150m));

		// Bonus is +0.40 when triggered.
		var delta = withText.RiskScore - withoutText.RiskScore;
		if (shouldTriggerBonus)
		{
			Assert.Equal(0.40, delta, precision: 2);
		}
		else
		{
			Assert.Equal(0.0, delta, precision: 2);
		}
	}

	[Fact]
	public async Task Violent_incident_type_alone_triggers_violence_bonus()
	{
		var classifier = CreateClassifier();

		// "Colleague Assault" with no description should still surface as violent
		// because the ViolentRegex now scans the incident type as well as the text.
		var request = BuildRequest(incidentType: "Colleague Assault");

		var result = await classifier.ClassifyAsync(request);

		// 0 base + 0.40 violence = 0.40 -> bucketed Medium.
		Assert.Equal(0.40, result.RiskScore, precision: 2);
		Assert.Equal("medium", result.RiskLevel);
	}

	[Fact]
	public async Task Attempted_incidents_with_no_loss_are_capped_below_medium()
	{
		var classifier = CreateClassifier();

		// When no realised loss is recorded, an attempted/prevented incident is
		// treated as a successful intervention and cannot escape Low even with
		// other supporting signals (police + items). Without loss the dampener
		// halves the score; the 0.39 cap is defensive against future signals.
		var request = BuildRequest(
			incidentType: "Attempted Shoplifting",
			totalLostValue: 0m,
			policeInvolvement: true,
			stolenItemCount: 25);

		var result = await classifier.ClassifyAsync(request);

		// Raw before dampener: 0 + 0.25 + 0.20 = 0.45
		// After dampener: min(0.45 * 0.5, 0.39) = 0.225
		// IncidentRiskLevel.Bucket uses Math.Round with banker's rounding
		// (round-half-to-even), so 0.225 stores as 0.22.
		Assert.Equal(0.22, result.RiskScore, precision: 2);
		Assert.Equal("low", result.RiskLevel);
	}

	[Fact]
	public async Task Positive_loss_disables_stage_dampener()
	{
		var classifier = CreateClassifier();

		// A "Deter" or "Attempted" label on an incident where loss was ACTUALLY
		// recorded means the intervention failed - the loss is the source of
		// truth and the dampener must step aside, even with no other signals.
		// This is the case that produced "Deter + £500 lost = Low 38/100" before.
		var request = BuildRequest(
			incidentType: "Deter",
			totalLostValue: 500m);

		var result = await classifier.ClassifyAsync(request);

		// Loss tier alone hits 0.75 -> High; dampener does NOT halve it.
		Assert.Equal(0.75, result.RiskScore, precision: 2);
		Assert.Equal("high", result.RiskLevel);
	}

	[Fact]
	public async Task Attempted_with_loss_still_buckets_to_high()
	{
		var classifier = CreateClassifier();

		// "Attempted Shoplifting" with a £600 loss means the attempt succeeded
		// in causing loss; loss overrides the "attempt" label.
		var request = BuildRequest(
			incidentType: "Attempted Shoplifting",
			totalLostValue: 600m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.75, result.RiskScore, precision: 2);
		Assert.Equal("high", result.RiskLevel);
	}

	[Fact]
	public async Task Attempted_with_violence_signal_ignores_dampener()
	{
		var classifier = CreateClassifier();

		// Violence trumps the "attempted" stage modifier - if a weapon or assault
		// is described, the dampener does NOT halve the score.
		var request = BuildRequest(
			incidentType: "Attempted Shoplifting",
			description: "offender stabbed staff during the attempt");

		var result = await classifier.ClassifyAsync(request);

		// 0 (no loss) + 0 (no police) + 0 (no items) + 0.40 (violence) = 0.40
		Assert.Equal(0.40, result.RiskScore, precision: 2);
		Assert.Equal("medium", result.RiskLevel);
	}

	[Fact]
	public async Task Floor_of_0_05_applies_when_no_signals_present()
	{
		var classifier = CreateClassifier();

		// No loss, no police, no items, no violent text - a recorded incident
		// should still register as a sliver of risk rather than a flat zero.
		var request = BuildRequest(incidentType: "Trespass");

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.05, result.RiskScore, precision: 2);
		Assert.Equal("low", result.RiskLevel);
	}

	[Fact]
	public async Task Internal_theft_requires_phrase_not_bare_staff_keyword()
	{
		var classifier = CreateClassifier();

		// "staff intervened" describes staff as a witness, not as an offender,
		// and must not be routed to Internal Theft.
		var witness = await classifier.ClassifyAsync(BuildRequest(
			incidentType: "Shoplifting",
			description: "Staff intervened and apprehended the offender at the door"));

		Assert.Equal("Shoplifting", witness.SuggestedCategory);

		// A multi-token phrase that clearly names staff as the offender should
		// upgrade the category to Internal Theft.
		var offender = await classifier.ClassifyAsync(BuildRequest(
			incidentType: "Shoplifting",
			description: "Staff stole stock from the back room over several weeks"));

		Assert.Equal("Internal Theft", offender.SuggestedCategory);
	}

	[Fact]
	public async Task Self_scan_misuse_type_routes_to_self_scan_category()
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(incidentType: "Self-Scan Misuse");

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("Self-Scan Misuse", result.SuggestedCategory);
		Assert.Equal(0.80, result.Confidence, precision: 2);
	}

	[Fact]
	public async Task Attempted_shoplifting_type_routes_to_attempted_shoplifting_category()
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(incidentType: "Attempted Shoplifting");

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("Attempted Shoplifting", result.SuggestedCategory);
		Assert.Equal(0.80, result.Confidence, precision: 2);
	}

	[Fact]
	public async Task General_incident_fallback_lowers_confidence_to_0_5()
	{
		var classifier = CreateClassifier();

		// A type that matches none of the known fragments and no text overrides
		// should report General Incident with the "we don't know" confidence.
		var request = BuildRequest(
			incidentType: "Lost Property",
			description: "Customer left a bag at the till");

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("General Incident", result.SuggestedCategory);
		Assert.Equal(0.50, result.Confidence, precision: 2);
	}

	[Fact]
	public async Task Text_override_agreeing_with_type_yields_highest_confidence()
	{
		var classifier = CreateClassifier();

		// Type already says violent AND description provides weapon evidence -
		// the override and the type agree, so confidence should reach 0.90.
		var request = BuildRequest(
			incidentType: "Colleague Assault",
			description: "Offender brandished a knife at the staff member");

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("Violent Incident", result.SuggestedCategory);
		Assert.Equal(0.90, result.Confidence, precision: 2);
	}

	[Fact]
	public async Task Text_override_upgrading_non_violent_type_uses_override_confidence()
	{
		var classifier = CreateClassifier();

		// Shoplifting type but description names an organised group - the text
		// strictly overrides the type, so confidence drops to 0.70 to signal
		// the upgrade is description-driven, not type-driven.
		var request = BuildRequest(
			incidentType: "Shoplifting",
			description: "An organised group of repeat offenders cleared the aisle");

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("Organised Retail Crime", result.SuggestedCategory);
		Assert.Equal(0.70, result.Confidence, precision: 2);
	}

	[Fact]
	public async Task Risk_level_bucket_high_at_or_above_0_7()
	{
		var classifier = CreateClassifier();

		// £500 (the threshold where a single retail incident becomes material)
		// lost alone hits 0.75 and buckets as High.
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: 500m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.75, result.RiskScore, precision: 2);
		Assert.Equal("high", result.RiskLevel);
	}

	[Fact]
	public async Task Risk_level_bucket_medium_between_0_4_and_0_7()
	{
		var classifier = CreateClassifier();

		// Shoplifting + £250 lost = 0.50 -> medium
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: 250m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.50, result.RiskScore, precision: 2);
		Assert.Equal("medium", result.RiskLevel);
	}

	[Fact]
	public async Task Risk_level_bucket_low_below_0_4()
	{
		var classifier = CreateClassifier();

		// Trespass with no other signals = 0.05 floor -> low
		var request = BuildRequest(incidentType: "Trespass");
		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("low", result.RiskLevel);
	}

	[Fact]
	public async Task Compound_high_risk_caps_at_1_0()
	{
		var classifier = CreateClassifier();

		// Assault + £1500 lost + police + 25 items + violent keyword
		// = 0.40 + 0.50 + 0.25 + 0.20 = 1.35 -> capped 1.0
		var request = BuildRequest(
			incidentType: "Colleague Assault",
			description: "offender attacked staff with a knife",
			totalLostValue: 1500m,
			policeInvolvement: true,
			stolenItemCount: 25);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(1.0, result.RiskScore, precision: 2);
		Assert.Equal("high", result.RiskLevel);
	}

	[Fact]
	public async Task Classifier_version_is_v4()
	{
		var classifier = CreateClassifier();
		var request = BuildRequest();

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("rule-based-v4", result.ClassifierVersion);
	}
}
