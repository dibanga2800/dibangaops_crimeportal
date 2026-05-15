#nullable enable

using AIPBackend.Models;

namespace AIPBackend.Services
{
	/// <summary>
	/// Mirrors frontend getIncidentFinancials in IncidentReportPage.tsx.
	/// </summary>
	public static class IncidentFinancials
	{
		public static decimal GetRecoveredValue(Incident incident)
		{
			if (incident.TotalRecoveredValue.HasValue)
			{
				return incident.TotalRecoveredValue.Value;
			}

			if (incident.TotalValueRecovered.HasValue)
			{
				return incident.TotalValueRecovered.Value;
			}

			if (incident.StolenItems == null || incident.StolenItems.Count == 0)
			{
				return 0m;
			}

			return incident.StolenItems.Sum(item => item.RecoveredAmount);
		}

		public static decimal GetLostValue(Incident incident)
		{
			if (incident.TotalLostValue.HasValue)
			{
				return incident.TotalLostValue.Value;
			}

			var stolenValue = GetStolenValue(incident);
			var recoveredValue = GetRecoveredValue(incident);
			return Math.Max(stolenValue - recoveredValue, 0m);
		}

		private static decimal GetStolenValue(Incident incident)
		{
			if (incident.TotalStolenValue.HasValue)
			{
				return incident.TotalStolenValue.Value;
			}

			if (incident.StolenItems == null || incident.StolenItems.Count == 0)
			{
				return 0m;
			}

			return incident.StolenItems.Sum(item => item.TotalAmount);
		}
	}
}
