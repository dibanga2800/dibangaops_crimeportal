#nullable enable

using AIPBackend.Models;
using AIPBackend.Models.DTOs;

namespace AIPBackend.Services.Analytics
{
	/// <summary>
	/// Business rules for analytics hub recommendations derived from real incident data.
	/// </summary>
	public static class AnalyticsRules
	{
		public const string StoreDetectivesOfficerType = AnalyticsOfficerTypes.StoreDetectives;

		/// <summary>
		/// Incident types that trigger LPM involvement recommendations in analytics.
		/// </summary>
		private static readonly HashSet<string> LpmIncidentTypes = new(StringComparer.OrdinalIgnoreCase)
		{
			"Shoplifting",
			"Shoplifting / Theft",
			"Threats and Intimidation",
		};

		public static bool RequiresLpm(Incident incident)
		{
			if (string.IsNullOrWhiteSpace(incident.IncidentType))
			{
				return false;
			}

			return LpmIncidentTypes.Contains(incident.IncidentType.Trim());
		}

		public static bool RequiresLpm(IEnumerable<Incident> incidents) =>
			incidents.Any(RequiresLpm);

		public static string ToRiskLevel(double riskScore) =>
			riskScore >= 0.7 ? "critical"
				: riskScore >= 0.4 ? "high"
				: riskScore >= 0.2 ? "medium"
				: "low";

		public static string ToDeploymentPriority(double ratio) =>
			ratio >= 0.7 ? "critical"
				: ratio >= 0.4 ? "high"
				: ratio >= 0.2 ? "medium"
				: "low";

		public static string ComputeTrend(int recentCount, int previousCount)
		{
			if (previousCount == 0)
			{
				return recentCount > 0 ? "increasing" : "stable";
			}

			if (recentCount > previousCount * 1.1)
			{
				return "increasing";
			}

			if (recentCount < previousCount * 0.9)
			{
				return "decreasing";
			}

			return "stable";
		}

		public static LocationRiskBreakdown BuildLocationRiskBreakdown(
			List<Incident> incidents,
			DateTime periodEnd,
			Func<Incident, decimal> getLostValue)
		{
			var factors = new List<RiskFactorDto>();
			var incidentCount = incidents.Count;
			var lostValue = incidents.Sum(getLostValue);
			var policeCount = incidents.Count(i => i.PoliceInvolvement);
			var recentCount = incidents.Count(i => i.DateOfIncident >= periodEnd.AddDays(-7));

			var countWeight = Math.Min(incidentCount / 20.0, 0.4);
			if (countWeight > 0)
			{
				factors.Add(new RiskFactorDto
				{
					Factor = "incident_volume",
					Score = Math.Round(countWeight, 2),
					Description = $"{incidentCount} incident{(incidentCount == 1 ? "" : "s")} in period (volume weight capped at 20)"
				});
			}

			var valueWeight = Math.Min((double)(lostValue / 5000m), 0.3);
			if (valueWeight > 0)
			{
				factors.Add(new RiskFactorDto
				{
					Factor = "value_impact",
					Score = Math.Round(valueWeight, 2),
					Description = $"£{lostValue:N0} lost value in period"
				});
			}

			if (policeCount > 0)
			{
				factors.Add(new RiskFactorDto
				{
					Factor = "police_involvement",
					Score = 0.2,
					Description = $"Police involved in {policeCount} of {incidentCount} incident{(incidentCount == 1 ? "" : "s")}"
				});
			}

			if (recentCount > 0)
			{
				factors.Add(new RiskFactorDto
				{
					Factor = "recency",
					Score = 0.1,
					Description = $"{recentCount} incident{(recentCount == 1 ? "" : "s")} in the last 7 days (to {periodEnd:yyyy-MM-dd})"
				});
			}

			var totalScore = Math.Round(Math.Min(factors.Sum(f => f.Score), 1.0), 2);

			return new LocationRiskBreakdown
			{
				Score = totalScore,
				Level = ToRiskLevel(totalScore),
				Factors = factors,
				IncidentCount = incidentCount,
				LostValue = lostValue,
				PoliceInvolvedCount = policeCount,
				RecentIncidentCount = recentCount,
			};
		}

		public static string BuildTimeSlotReason(
			int slotCount,
			int totalIncidents,
			string day,
			string hourLabel,
			IReadOnlyList<Incident> slotIncidents)
		{
			var pct = totalIncidents > 0
				? Math.Round((double)slotCount / totalIncidents * 100, 1)
				: 0;

			var lpmCount = slotIncidents.Count(RequiresLpm);
			var lostValue = slotIncidents.Sum(i => GetIncidentLostValueForRules(i));

			var parts = new List<string>
			{
				$"{slotCount} incident{(slotCount == 1 ? "" : "s")} on {day} at {hourLabel} ({pct}% of {totalIncidents} in period)",
				$"£{lostValue:N0} lost in this window",
			};

			if (lpmCount > 0)
			{
				parts.Add($"LPM recommended: {lpmCount} incident{(lpmCount == 1 ? "" : "s")} match LPM categories (shoplifting or threats and intimidation)");
			}

			return string.Join(". ", parts) + ".";
		}

		public static List<string> BuildTimeSlotReasonDetails(
			IReadOnlyList<Incident> slotIncidents,
			int totalIncidents)
		{
			var details = new List<string>();
			if (totalIncidents > 0)
			{
				details.Add($"Represents {Math.Round((double)slotIncidents.Count / totalIncidents * 100, 1)}% of all incidents in the filtered period");
			}

			var typeGroups = slotIncidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType.Trim())
				.OrderByDescending(g => g.Count())
				.Take(3)
				.Select(g => $"{g.Key}: {g.Count()}")
				.ToList();

			if (typeGroups.Any())
			{
				details.Add($"Top types: {string.Join(", ", typeGroups)}");
			}

			details.Add($"Recommended {StoreDetectivesOfficerType} based on organisational deployment policy");

			return details;
		}

		public static string BuildStoreRiskReason(
			LocationRiskBreakdown breakdown,
			int rank,
			string trend,
			int recentCount,
			int previousCount,
			IReadOnlyList<string> peakHours,
			bool recommendedLpm)
		{
			var trendText = trend switch
			{
				"increasing" => $"up from {previousCount} to {recentCount} in the last 30 days vs prior 30",
				"decreasing" => $"down from {previousCount} to {recentCount} in the last 30 days vs prior 30",
				_ => $"{recentCount} incidents in the last 30 days (prior 30: {previousCount})",
			};

			var peakText = peakHours.Any()
				? $"Peak hours: {string.Join(", ", peakHours)}"
				: "No consistent peak hour recorded";

			var lpmText = recommendedLpm
				? "LPM involvement recommended: at least one shoplifting or threats and intimidation incident in this period"
				: "LPM not indicated: no shoplifting or threats and intimidation incidents in this period";

			return
				$"Rank #{rank}: risk score {breakdown.Score:F2} ({breakdown.Level}). " +
				$"{breakdown.IncidentCount} incidents, £{breakdown.LostValue:N0} lost. {trendText}. {peakText}. {lpmText}.";
		}

		public static List<string> BuildStoreRiskReasonDetails(LocationRiskBreakdown breakdown) =>
			breakdown.Factors.Select(f => $"{f.Description} (+{f.Score:F2})").ToList();

		private static decimal GetIncidentLostValueForRules(Incident incident)
		{
			if ((incident.TotalLostValue ?? 0) > 0)
			{
				return incident.TotalLostValue ?? 0;
			}

			var stolen = (incident.TotalStolenValue ?? 0) > 0
				? incident.TotalStolenValue ?? 0
				: incident.StolenItems?.Sum(item =>
					item.TotalAmount > 0 ? item.TotalAmount : item.Cost * item.Quantity) ?? 0;

			var recovered = (incident.TotalRecoveredValue ?? 0) > 0
				? incident.TotalRecoveredValue ?? 0
				: incident.StolenItems?.Sum(item =>
					item.RecoveredAmount > 0 ? item.RecoveredAmount : item.Cost * item.RecoveredQuantity) ?? 0;

			var lost = stolen - recovered;
			return lost > 0 ? lost : 0;
		}
	}

	public class LocationRiskBreakdown
	{
		public double Score { get; set; }
		public string Level { get; set; } = "low";
		public List<RiskFactorDto> Factors { get; set; } = new();
		public int IncidentCount { get; set; }
		public decimal LostValue { get; set; }
		public int PoliceInvolvedCount { get; set; }
		public int RecentIncidentCount { get; set; }
	}
}
