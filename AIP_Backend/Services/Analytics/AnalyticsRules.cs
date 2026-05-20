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

		private static readonly HashSet<string> NonIdentifiedOffenderNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"n/a",
			"na",
			"n.a.",
			"n.a",
			"not applicable",
			"unknown",
			"none",
			"nil",
			"-",
			"—",
			"tbc",
			"tba",
		};

		/// <summary>
		/// True when the offender name is a real identity — not a placeholder such as N/A (not applicable).
		/// </summary>
		public static bool HasIdentifiedOffenderName(string? offenderName)
		{
			if (string.IsNullOrWhiteSpace(offenderName))
			{
				return false;
			}

			return !NonIdentifiedOffenderNames.Contains(offenderName.Trim());
		}

		public static bool IncidentHasIdentifiedOffender(Incident incident) =>
			!string.IsNullOrWhiteSpace(incident.OffenderId) || HasIdentifiedOffenderName(incident.OffenderName);

		public static string GetOffenderDisplayName(Incident incident)
		{
			if (HasIdentifiedOffenderName(incident.OffenderName))
			{
				return incident.OffenderName!.Trim();
			}

			if (!string.IsNullOrWhiteSpace(incident.OffenderId))
			{
				return $"Offender {incident.OffenderId.Trim()}";
			}

			return string.Empty;
		}

		/// <summary>
		/// Groups incidents into repeat-offender profiles. Same identified name always shares one key,
		/// even when system offender IDs differ between incidents.
		/// </summary>
		public static string BuildOffenderGroupingKey(Incident incident)
		{
			if (HasIdentifiedOffenderName(incident.OffenderName))
			{
				return $"name:{NormalizeOffenderNameKey(incident.OffenderName!)}";
			}

			if (!string.IsNullOrWhiteSpace(incident.OffenderId))
			{
				return $"id:{incident.OffenderId.Trim().ToLowerInvariant()}";
			}

			return string.Empty;
		}

		public static string NormalizeOffenderNameKey(string name) =>
			System.Text.RegularExpressions.Regex.Replace(name.Trim().ToLowerInvariant(), @"\s+", " ");

		public static string ToRiskLevel(double riskScore) =>
			riskScore >= 0.7 ? "critical"
				: riskScore >= 0.4 ? "high"
				: riskScore >= 0.2 ? "medium"
				: "low";

		/// <summary>Short label for UI: score bands are volume + value + police + recency (max 100%).</summary>
		public static string BuildStoreRiskSummary(LocationRiskBreakdown breakdown)
		{
			if (breakdown.IncidentCount == 0)
			{
				return "No incidents in the selected period.";
			}

			var factorText = breakdown.Factors.Any()
				? string.Join(" · ", breakdown.Factors.Take(3).Select(f => f.Description))
				: "Insufficient factor data";

			var scorePercent = (int)Math.Round(breakdown.Score * 100, MidpointRounding.AwayFromZero);
			return $"{breakdown.Level.ToUpperInvariant()} ({scorePercent}%): {factorText}";
		}

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

			var text =
				$"{slotCount} on {day} {hourLabel} ({pct}% of period). £{lostValue:N0} lost.";

			if (lpmCount > 0)
			{
				text += $" LPM: {lpmCount} shoplifting/threat case{(lpmCount == 1 ? "" : "s")}.";
			}

			return text;
		}

		public static List<string> BuildTimeSlotReasonDetails(
			IReadOnlyList<Incident> slotIncidents,
			int totalIncidents)
		{
			var details = new List<string>();

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

			details.Add($"Deploy {StoreDetectivesOfficerType}");

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
				"increasing" => $"↑ {recentCount} vs {previousCount} (30d)",
				"decreasing" => $"↓ {recentCount} vs {previousCount} (30d)",
				_ => $"{recentCount} in last 30d",
			};

			var peakText = peakHours.Any()
				? $"Peaks: {string.Join(", ", peakHours)}"
				: "No peak hour";

			var lpmText = recommendedLpm ? "LPM: yes" : "LPM: no";

			return
				$"#{rank} · {breakdown.Level} risk ({breakdown.Score:P0}). " +
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
