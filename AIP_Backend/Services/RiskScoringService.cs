#nullable enable

using System.Globalization;
using System.Text.Json;
using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Heuristic implementation of <see cref="IRiskScoringService"/> that computes
	/// store risk scores from recent incident history.
	///
	/// This class is intentionally ML.NET-friendly: the feature engineering and
	/// persistence model are separated so you can later plug in a trained model
	/// without changing the public contract.
	/// </summary>
	public class RiskScoringService : IRiskScoringService
	{
		private readonly ApplicationDbContext _db;
		private readonly ILogger<RiskScoringService> _logger;

		private static readonly string ModelVersion = "heuristic-v1";

		public RiskScoringService(
			ApplicationDbContext db,
			ILogger<RiskScoringService> logger)
		{
			_db = db;
			_logger = logger;
		}

		public async Task<StoreRiskScoreDto?> GetStoreRiskScoreAsync(
			int storeId,
			DateTime forDate,
			CancellationToken cancellationToken = default)
		{
			var siteId = storeId.ToString(CultureInfo.InvariantCulture);
			var existing = await _db.StoreRiskScores
				.AsNoTracking()
				.FirstOrDefaultAsync(s =>
					s.SiteId == siteId &&
					s.ForDate == forDate.Date,
					cancellationToken);

			if (existing != null)
			{
				return MapToDto(existing);
			}

			var computed = await ComputeRiskForStoreAsync(siteId, forDate, cancellationToken);
			return computed;
		}

		public async Task<IReadOnlyList<StoreRiskScoreDto>> GetCustomerRiskScoresAsync(
			int customerId,
			DateTime forDate,
			CancellationToken cancellationToken = default)
		{
			var forDay = forDate.Date;

			// Load any existing scores for this customer/day.
			var cached = await _db.StoreRiskScores
				.AsNoTracking()
				.Where(s => s.CustomerId == customerId && s.ForDate == forDay)
				.ToListAsync(cancellationToken);

			// Discover all active sites with incidents for this customer in the lookback window.
			var lookbackStart = forDay.AddDays(-30);
			var recentIncidents = await _db.Incidents
				.AsNoTracking()
				.Where(i =>
					i.CustomerId == customerId &&
					i.DateOfIncident >= lookbackStart &&
					i.DateOfIncident <= forDay &&
					i.SiteId != null)
				.ToListAsync(cancellationToken);

			var siteIds = recentIncidents
				.Select(i => i.SiteId!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			var results = new List<StoreRiskScoreDto>();

			foreach (var siteId in siteIds)
			{
				var fromCache = cached.FirstOrDefault(s => s.SiteId == siteId);
				if (fromCache != null)
				{
					results.Add(MapToDto(fromCache));
					continue;
				}

				var computed = await ComputeRiskForStoreAsync(siteId, forDay, cancellationToken);
				if (computed != null)
				{
					results.Add(computed);
				}
			}

			return results
				.OrderByDescending(r => r.Score)
				.ToList();
		}

		private async Task<StoreRiskScoreDto?> ComputeRiskForStoreAsync(
			string siteId,
			DateTime forDate,
			CancellationToken cancellationToken)
		{
			var forDay = forDate.Date;
			var lookbackStart = forDay.AddDays(-30);

			var incidents = await _db.Incidents
				.AsNoTracking()
				.Where(i =>
					i.SiteId == siteId &&
					i.DateOfIncident >= lookbackStart &&
					i.DateOfIncident <= forDay)
				.Include(i => i.StolenItems)
				.ToListAsync(cancellationToken);

			if (!incidents.Any())
			{
				return null;
			}

			var customerId = incidents.First().CustomerId;
			var storeName = incidents.First().StoreName;

			var totalIncidents = incidents.Count;
			var valueImpact = incidents.Sum(i =>
				i.TotalValueRecovered ?? i.StolenItems.Sum(s => s.TotalAmount));
			var repeatOffenderCount = incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.OffenderName))
				.GroupBy(i => i.OffenderName!.Trim().ToLowerInvariant())
				.Count(g => g.Count() >= 2);

			var last7Days = forDay.AddDays(-7);
			var recentIncidents = incidents.Count(i => i.DateOfIncident >= last7Days);

			// Simple heuristic score in [0,1] combining volume, value, repeat activity and recency.
			var volumeScore = Math.Min(totalIncidents / 20.0, 0.4);
			var valueScore = Math.Min((double)valueImpact / 5000.0, 0.3);
			var repeatScore = Math.Min(repeatOffenderCount / 5.0, 0.2);
			var recencyScore = Math.Min(recentIncidents / 10.0, 0.1);
			var rawScore = volumeScore + valueScore + repeatScore + recencyScore;
			var score = Math.Round(Math.Min(rawScore, 1.0), 2);

			var level = score switch
			{
				>= 0.7 => "high",
				>= 0.4 => "medium",
				_ => "low"
			};

			// Expected incidents is a rough extrapolation from last 30 days.
			var avgPerDay = totalIncidents / 30.0;
			var expectedMin = Math.Max(0, (int)Math.Floor(avgPerDay * 0.6));
			var expectedMax = Math.Max(expectedMin + 1, (int)Math.Ceiling(avgPerDay * 1.4));

			// Peak windows are based on hours with the highest counts.
			var peakLabels = incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.TimeOfIncident))
				.Select(i => i.TimeOfIncident!)
				.Select(ParseHourSafe)
				.Where(h => h.HasValue)
				.GroupBy(h => h!.Value)
				.OrderByDescending(g => g.Count())
				.Take(3)
				.Select(g => FormatHourWindow(g.Key))
				.ToList();

			var entity = new StoreRiskScore
			{
				CustomerId = customerId,
				StoreId = int.TryParse(siteId, out var id) ? id : null,
				SiteId = siteId,
				StoreName = storeName,
				ForDate = forDay,
				Score = score,
				Level = level,
				ExpectedIncidentsMin = expectedMin,
				ExpectedIncidentsMax = expectedMax,
				PeakRiskWindows = peakLabels.Any() ? JsonSerializer.Serialize(peakLabels) : null,
				ModelVersion = ModelVersion,
				GeneratedAt = DateTime.UtcNow
			};

			_db.StoreRiskScores.Add(entity);
			await _db.SaveChangesAsync(cancellationToken);

			_logger.LogInformation(
				"Computed risk score for site {SiteId} on {Date}: score={Score}, level={Level}, incidents={TotalIncidents}",
				siteId, forDay.ToString("yyyy-MM-dd"), score, level, totalIncidents);

			return MapToDto(entity, peakLabels);
		}

		private static StoreRiskScoreDto MapToDto(StoreRiskScore entity, List<string>? peakWindowsOverride = null)
		{
			List<string> windows;
			if (peakWindowsOverride != null)
			{
				windows = peakWindowsOverride;
			}
			else if (!string.IsNullOrWhiteSpace(entity.PeakRiskWindows))
			{
				try
				{
					windows = JsonSerializer.Deserialize<List<string>>(entity.PeakRiskWindows) ?? new List<string>();
				}
				catch
				{
					windows = new List<string>();
				}
			}
			else
			{
				windows = new List<string>();
			}

			return new StoreRiskScoreDto
			{
				StoreId = entity.StoreId ?? 0,
				StoreName = entity.StoreName,
				ForDate = entity.ForDate,
				Score = entity.Score,
				Level = entity.Level,
				ExpectedIncidentsMin = entity.ExpectedIncidentsMin,
				ExpectedIncidentsMax = entity.ExpectedIncidentsMax,
				PeakRiskWindows = windows,
				ModelVersion = entity.ModelVersion
			};
		}

		private static int? ParseHourSafe(string timeString)
		{
			if (string.IsNullOrWhiteSpace(timeString)) return null;
			if (!TimeSpan.TryParse(timeString, out var ts)) return null;
			return ts.Hours;
		}

		private static string FormatHourWindow(int hour)
		{
			var start = TimeSpan.FromHours(hour);
			var end = TimeSpan.FromHours((hour + 1) % 24);
			return $"{start:hh\\:mm}-{end:hh\\:mm}";
		}
	}
}

