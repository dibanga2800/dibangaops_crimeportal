using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIPBackend.Tests;

/// <summary>
/// Tests for the rule-based incident classifier risk scoring (rule-based-v2).
///
/// These tests cover the dashboard "AI Insight" column: incident-type baseline matching,
/// value-at-risk tiers (lost value, not recovered), stolen-item-count tiers,
/// police involvement, and word-boundary-matched violent-keyword bonus.
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
	[InlineData("Shoplifting", 0.3)]
	[InlineData("shoplifting", 0.3)]
	[InlineData("Theft", 0.3)]
	[InlineData("Theft Prevention", 0.1)]
	[InlineData("Arrest - Saved?", 0.5)]
	[InlineData("Colleague Assault", 0.7)]
	[InlineData("Anti-social Behaviour", 0.3)]
	[InlineData("Anti-Social Behaviour", 0.3)]
	[InlineData("Antisocial Behaviour", 0.3)]
	[InlineData("Criminal Damage", 0.4)]
	[InlineData("Vandalism", 0.4)]
	[InlineData("Fraud / Counterfeit", 0.5)]
	[InlineData("Trespass", 0.2)]
	[InlineData("Deterred", 0.1)]
	[InlineData("Unknown Type", 0.2)]
	[InlineData("", 0.2)]
	public async Task Base_risk_matches_human_readable_incident_types(string incidentType, double expectedBase)
	{
		var classifier = CreateClassifier();
		var request = BuildRequest(incidentType: incidentType);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(expectedBase, result.RiskScore, precision: 2);
	}

	[Theory]
	[InlineData(50.0, 0.3)]    // < £100 -> no bonus, just base
	[InlineData(150.0, 0.4)]   // >= £100 -> +0.1
	[InlineData(600.0, 0.5)]   // >= £500 -> +0.2
	[InlineData(1500.0, 0.6)]  // >= £1000 -> +0.3
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

		// £2000 recovered (good outcome) and £0 lost - risk should stay at the baseline.
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: 0m,
			totalValueRecovered: 2000m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.3, result.RiskScore, precision: 2);
	}

	[Fact]
	public async Task Recovered_value_is_used_as_proxy_when_lost_value_is_null()
	{
		var classifier = CreateClassifier();

		// Legacy incidents where TotalLostValue was never persisted fall back to recovered
		// as a coarse proxy. £600 recovered (lost null) -> +0.2 tier.
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: null,
			totalValueRecovered: 600m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal(0.5, result.RiskScore, precision: 2);
	}

	[Theory]
	[InlineData(5, 0.3)]   // <= 5 -> no bonus
	[InlineData(6, 0.35)]  // 6-10 -> +0.05
	[InlineData(11, 0.4)]  // 11-20 -> +0.10
	[InlineData(25, 0.45)] // > 20 -> +0.15
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

		// 0.3 base + 0.15 police = 0.45
		Assert.Equal(0.45, result.RiskScore, precision: 2);
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
	[InlineData("Staff member is unrelated to Knifecrest Avenue", false)]
	[InlineData("Customer panicked but no violence", false)]
	public async Task Violent_keyword_bonus_uses_word_boundaries(string description, bool shouldTriggerBonus)
	{
		var classifier = CreateClassifier();

		var withText = await classifier.ClassifyAsync(BuildRequest(
			incidentType: "Shoplifting",
			description: description));

		var withoutText = await classifier.ClassifyAsync(BuildRequest(
			incidentType: "Shoplifting"));

		// Bonus is +0.2 when triggered.
		var delta = withText.RiskScore - withoutText.RiskScore;
		if (shouldTriggerBonus)
		{
			Assert.Equal(0.2, delta, precision: 2);
		}
		else
		{
			Assert.Equal(0.0, delta, precision: 2);
		}
	}

	[Fact]
	public async Task Risk_level_bucket_high_at_or_above_0_7()
	{
		var classifier = CreateClassifier();

		// Assault baseline alone hits 0.7.
		var request = BuildRequest(incidentType: "Colleague Assault");
		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("high", result.RiskLevel);
	}

	[Fact]
	public async Task Risk_level_bucket_medium_between_0_4_and_0_7()
	{
		var classifier = CreateClassifier();

		// Shoplifting + £600 lost = 0.3 + 0.2 = 0.5 -> medium
		var request = BuildRequest(
			incidentType: "Shoplifting",
			totalLostValue: 600m);

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("medium", result.RiskLevel);
	}

	[Fact]
	public async Task Risk_level_bucket_low_below_0_4()
	{
		var classifier = CreateClassifier();

		// Trespass with no other signals = 0.2 -> low
		var request = BuildRequest(incidentType: "Trespass");
		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("low", result.RiskLevel);
	}

	[Fact]
	public async Task Compound_high_risk_caps_at_1_0()
	{
		var classifier = CreateClassifier();

		// Assault + £1500 lost + police + 25 items + violent keyword = 0.7 + 0.3 + 0.15 + 0.15 + 0.2 = 1.5 -> capped 1.0
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
	public async Task Classifier_version_is_v2()
	{
		var classifier = CreateClassifier();
		var request = BuildRequest();

		var result = await classifier.ClassifyAsync(request);

		Assert.Equal("rule-based-v2", result.ClassifierVersion);
	}
}
