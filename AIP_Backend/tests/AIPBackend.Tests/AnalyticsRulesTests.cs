using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services.Analytics;

namespace AIPBackend.Tests;

public class AnalyticsRulesTests
{
	[Theory]
	[InlineData("Shoplifting", true)]
	[InlineData("Shoplifting / Theft", true)]
	[InlineData("Threats and Intimidation", true)]
	[InlineData("Theft", false)]
	[InlineData("Colleague Assault", false)]
	[InlineData("Suspicious Activity", false)]
	public void RequiresLpm_matches_expected_incident_types(string incidentType, bool expected)
	{
		var incident = new Incident
		{
			IncidentType = incidentType,
			StoreName = "Test Store",
			StaffMemberName = "Officer",
			DateOfIncident = DateTime.UtcNow,
		};

		Assert.Equal(expected, AnalyticsRules.RequiresLpm(incident));
	}

	[Fact]
	public void Recommended_officer_type_is_always_store_detectives()
	{
		Assert.Equal("store detectives", AnalyticsRules.StoreDetectivesOfficerType);
	}

	[Fact]
	public void BuildLocationRiskBreakdown_includes_volume_and_value_factors()
	{
		var periodEnd = new DateTime(2026, 5, 18);
		var incidents = new List<Incident>
		{
			CreateIncident("Store A", periodEnd.AddDays(-1), "Shoplifting", 500m, 0m),
			CreateIncident("Store A", periodEnd.AddDays(-2), "Threats and Intimidation", 600m, 100m),
		};

		var breakdown = AnalyticsRules.BuildLocationRiskBreakdown(
			incidents,
			periodEnd,
			i => (i.TotalLostValue ?? 0) > 0 ? i.TotalLostValue!.Value : 500m);

		Assert.True(breakdown.Score > 0);
		Assert.Contains(breakdown.Factors, f => f.Factor == "incident_volume");
		Assert.Contains(breakdown.Factors, f => f.Factor == "value_impact");
	}

	[Fact]
	public void ComputeTrend_increasing_when_recent_exceeds_prior()
	{
		Assert.Equal("increasing", AnalyticsRules.ComputeTrend(12, 5));
		Assert.Equal("decreasing", AnalyticsRules.ComputeTrend(4, 10));
		Assert.Equal("stable", AnalyticsRules.ComputeTrend(10, 10));
	}

	[Theory]
	[InlineData("N/A", false)]
	[InlineData("n/a", false)]
	[InlineData("not applicable", false)]
	[InlineData("unknown", false)]
	[InlineData("John Smith", true)]
	public void HasIdentifiedOffenderName_excludes_placeholders(string name, bool expected) =>
		Assert.Equal(expected, AnalyticsRules.HasIdentifiedOffenderName(name));

	[Fact]
	public void BuildStoreRiskSummary_returns_no_incidents_message_when_empty()
	{
		var summary = AnalyticsRules.BuildStoreRiskSummary(new LocationRiskBreakdown
		{
			IncidentCount = 0,
			Level = "low",
			Score = 0,
		});

		Assert.Equal("No incidents in the selected period.", summary);
	}

	[Fact]
	public void BuildOffenderGroupingKey_groups_same_identified_name_across_different_ids()
	{
		var withId = new Incident
		{
			OffenderName = "Blonde Bob",
			OffenderId = "OFF-NX-20260511095356",
		};
		var withoutId = new Incident
		{
			OffenderName = "Blonde Bob",
		};

		Assert.Equal(
			AnalyticsRules.BuildOffenderGroupingKey(withId),
			AnalyticsRules.BuildOffenderGroupingKey(withoutId));
	}

	[Fact]
	public void BuildOffenderGroupingKey_uses_id_when_no_identified_name()
	{
		var incident = new Incident { OffenderId = "OFF-123", OffenderName = "N/A" };

		Assert.Equal("id:off-123", AnalyticsRules.BuildOffenderGroupingKey(incident));
	}

	[Fact]
	public void BuildStoreRiskSummary_includes_level_score_and_top_factors()
	{
		var breakdown = new LocationRiskBreakdown
		{
			IncidentCount = 5,
			Level = "high",
			Score = 0.55,
			Factors =
			[
				new() { Factor = "incident_volume", Description = "12 incidents", Score = 0.3 },
				new() { Factor = "value_impact", Description = "£2,400 lost", Score = 0.25 },
			],
		};

		var summary = AnalyticsRules.BuildStoreRiskSummary(breakdown);

		Assert.StartsWith("HIGH (55%):", summary);
		Assert.Contains("12 incidents", summary);
		Assert.Contains("£2,400 lost", summary);
	}

	private static Incident CreateIncident(
		string store,
		DateTime date,
		string type,
		decimal stolen,
		decimal recovered)
	{
		return new Incident
		{
			StoreName = store,
			StaffMemberName = "Officer",
			DateOfIncident = date,
			IncidentType = type,
			TotalStolenValue = stolen,
			TotalRecoveredValue = recovered,
			TotalLostValue = stolen - recovered,
		};
	}
}
