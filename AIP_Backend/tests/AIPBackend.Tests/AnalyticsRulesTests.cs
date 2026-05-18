using AIPBackend.Models;
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
