#nullable enable

using System.Globalization;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using AIPBackend.Services.Analytics;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	public class IncidentAnalyticsService : IIncidentAnalyticsService
	{
		private readonly IIncidentRepository _repository;
		private readonly ILogger<IncidentAnalyticsService> _logger;

		public IncidentAnalyticsService(
			IIncidentRepository repository,
			ILogger<IncidentAnalyticsService> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IncidentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? from = null,
			DateTime? to = null)
		{
			// Treat 'from' and 'to' as whole-calendar-day bounds.
			// 'from' is inclusive from midnight; 'to' is inclusive through the end of that day.
			var fromDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
			var toDateInclusive = (to ?? DateTime.UtcNow).Date;
			var effectiveFrom = fromDate;
			var effectiveTo = toDateInclusive.AddDays(1).AddTicks(-1);

			var incidents = await _repository.GetAllForStatsAsync(customerId, customerIds: null, siteId, regionId);
			var filtered = incidents
				.Where(i => i.DateOfIncident >= effectiveFrom && i.DateOfIncident <= effectiveTo)
				.ToList();

			var totalValue = filtered.Sum(GetIncidentLostValue);

			var repeatOffenders = filtered
				.Where(AnalyticsRules.IncidentHasIdentifiedOffender)
				.GroupBy(AnalyticsRules.BuildOffenderGroupingKey)
				.Where(g => g.Count() > 1)
				.Count();

			var hotLocations = filtered
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.GroupBy(i => new { i.StoreName, i.RegionName })
				.Select(g =>
				{
					var siteIncidents = g.ToList();
					var siteValue = siteIncidents.Sum(GetIncidentLostValue);
					return new HotLocationDto
					{
						SiteName = g.Key.StoreName!,
						RegionName = g.Key.RegionName,
						IncidentCount = siteIncidents.Count,
						TotalValue = siteValue,
						RiskScore = CalculateLocationRiskScore(siteIncidents, effectiveTo)
					};
				})
				.OrderByDescending(h => h.RiskScore)
				.Take(10)
				.ToList();

			var trend = BuildTrend(filtered, effectiveFrom, effectiveTo);
			var categories = BuildCategoryBreakdown(filtered);
			var riskIndicators = BuildRiskIndicators(filtered, repeatOffenders, totalValue);

			_logger.LogInformation(
				"Analytics summary generated: {Count} incidents, {Value:C0} value, {Repeats} repeat offenders",
				filtered.Count, totalValue, repeatOffenders);

			return new IncidentAnalyticsSummaryDto
			{
				TotalIncidents = filtered.Count,
				TotalValueAtRisk = totalValue,
				RepeatOffenderCount = repeatOffenders,
				HotLocations = hotLocations,
				IncidentTrend = trend,
				CategoryBreakdown = categories,
				RiskIndicators = riskIndicators,
				GeneratedAt = DateTime.UtcNow
			};
		}

		private static double CalculateLocationRiskScore(List<Incident> incidents, DateTime periodEnd) =>
			AnalyticsRules.BuildLocationRiskBreakdown(incidents, periodEnd, GetIncidentLostValue).Score;

		private static string GetProductGroupKey(StolenItem item)
		{
			var barcode = item.Barcode?.Trim();
			if (!string.IsNullOrWhiteSpace(barcode) &&
			    !string.Equals(barcode, "unknown", StringComparison.OrdinalIgnoreCase) &&
			    barcode.Length >= 4)
			{
				return $"barcode:{barcode.ToLowerInvariant()}";
			}

			var name = (item.ProductName ?? item.Description)?.Trim();
			if (!string.IsNullOrWhiteSpace(name))
			{
				return $"name:{name.ToLowerInvariant()}";
			}

			return "unknown";
		}

		private static List<TrendDataPointDto> BuildTrend(List<Incident> incidents, DateTime from, DateTime to)
		{
			var daySpan = (to - from).TotalDays;
			if (daySpan <= 14)
			{
				return incidents
					.GroupBy(i => i.DateOfIncident.Date)
					.Select(g => new TrendDataPointDto
					{
						Period = g.Key.ToString("yyyy-MM-dd"),
						Count = g.Count(),
						Value = g.Sum(GetIncidentLostValue)
					})
					.OrderBy(t => t.Period)
					.ToList();
			}

			var startOfWeek = from.Date;
			var weeks = new List<TrendDataPointDto>();
			while (startOfWeek < to)
			{
				var endOfWeek = startOfWeek.AddDays(7);
				var weekIncidents = incidents.Where(i => i.DateOfIncident >= startOfWeek && i.DateOfIncident < endOfWeek).ToList();
				weeks.Add(new TrendDataPointDto
				{
					Period = $"W/C {startOfWeek:dd MMM}",
					Count = weekIncidents.Count,
					Value = weekIncidents.Sum(GetIncidentLostValue)
				});
				startOfWeek = endOfWeek;
			}

			return weeks;
		}

		private static List<CategoryBreakdownDto> BuildCategoryBreakdown(List<Incident> incidents)
		{
			var total = incidents.Count;
			if (total == 0) return new();

			return incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType)
				.Select(g => new CategoryBreakdownDto
				{
					Category = g.Key,
					Count = g.Count(),
					Percentage = Math.Round((double)g.Count() / total * 100, 1),
					TotalValue = g.Sum(GetIncidentLostValue)
				})
				.OrderByDescending(c => c.Count)
				.ToList();
		}

		private static List<RiskIndicatorDto> BuildRiskIndicators(List<Incident> incidents, int repeatCount, decimal totalValue)
		{
			var indicators = new List<RiskIndicatorDto>();

			var recentCount = incidents.Count(i => i.DateOfIncident >= DateTime.UtcNow.AddDays(-7));
			var previousWeekCount = incidents.Count(i =>
				i.DateOfIncident >= DateTime.UtcNow.AddDays(-14) &&
				i.DateOfIncident < DateTime.UtcNow.AddDays(-7));

			var velocityScore = previousWeekCount > 0
				? Math.Min((double)recentCount / previousWeekCount, 2.0) / 2.0
				: recentCount > 0 ? 0.5 : 0;

			indicators.Add(new RiskIndicatorDto
			{
				Indicator = "Incident Velocity",
				Level = velocityScore >= 0.7 ? "high" : velocityScore >= 0.4 ? "medium" : "low",
				Score = Math.Round(velocityScore, 2),
				Description = $"{recentCount} incidents in last 7 days vs {previousWeekCount} prior week"
			});

			var repeatScore = Math.Min(repeatCount / 5.0, 1.0);
			indicators.Add(new RiskIndicatorDto
			{
				Indicator = "Repeat Offender Activity",
				Level = repeatScore >= 0.6 ? "high" : repeatScore >= 0.3 ? "medium" : "low",
				Score = Math.Round(repeatScore, 2),
				Description = $"{repeatCount} repeat offenders identified"
			});

			var valueScore = totalValue switch
			{
				>= 10000m => 1.0,
				>= 5000m => 0.7,
				>= 1000m => 0.4,
				_ => 0.1
			};
			indicators.Add(new RiskIndicatorDto
			{
				Indicator = "Value at Risk",
				Level = valueScore >= 0.7 ? "high" : valueScore >= 0.4 ? "medium" : "low",
				Score = valueScore,
				Description = $"Total value impact (last 90 days): {totalValue.ToString("C0", CultureInfo.CreateSpecificCulture("en-GB"))}"
			});

			var policeRate = incidents.Count > 0
				? (double)incidents.Count(i => i.PoliceInvolvement) / incidents.Count
				: 0;
			indicators.Add(new RiskIndicatorDto
			{
				Indicator = "Police Involvement Rate",
				Level = policeRate >= 0.3 ? "high" : policeRate >= 0.1 ? "medium" : "low",
				Score = Math.Round(policeRate, 2),
				Description = $"{policeRate:P0} of incidents involved police"
			});

			return indicators;
		}

		// ============================================================================
		// Analytics Hub
		// ============================================================================

		public async Task<AnalyticsHubDto> GetAnalyticsHubAsync(
			int? customerId = null,
			string? siteId = null,
			string? regionId = null,
			DateTime? from = null,
			DateTime? to = null)
		{
			// Treat 'from' and 'to' as calendar dates; include the full 'to' day.
			var fromDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
			var toDateInclusive = (to ?? DateTime.UtcNow).Date;
			var effectiveFrom = fromDate;
			var effectiveTo = toDateInclusive.AddDays(1).AddTicks(-1);

			var filtered = await _repository.GetAllForStatsAsync(
				customerId,
				customerIds: null,
				siteId,
				regionId,
				startDate: effectiveFrom,
				endDate: effectiveTo);

			var total = filtered.Count;
			var fromStr = fromDate.ToString("yyyy-MM-dd");
			var toStr = toDateInclusive.ToString("yyyy-MM-dd");

			T BuildSection<T>(string sectionName, Func<T> builder, Func<T> fallback)
			{
				try
				{
					return builder();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to generate analytics hub section {SectionName}", sectionName);
					return fallback();
				}
			}

			var crimeTrends = BuildSection(
				"crimeTrends",
				() => BuildCrimeTrends(filtered, total, fromStr, toStr),
				() => new CrimeTrendDataDto
				{
					TotalIncidents = total,
					DateRange = new DateRangeDto { Start = fromStr, End = toStr }
				});
			var hotProducts = BuildSection(
				"hotProducts",
				() => BuildHotProducts(filtered, fromStr, toStr, effectiveTo),
				() => new HotProductsDataDto
				{
					Period = new DateRangeDto { Start = fromStr, End = toStr }
				});
			var financialSummary = BuildSection(
				"financialSummary",
				() => BuildFinancialSummary(filtered),
				() => new AnalyticsFinancialSummaryDto());
			var storeRecoveryComparisons = BuildSection(
				"storeRecoveryComparisons",
				() => BuildStoreRecoveryComparisons(filtered),
				() => new List<StoreRecoveryComparisonDto>());
			var repeatOffenders = BuildSection(
				"repeatOffenders",
				() => BuildRepeatOffenders(filtered),
				() => new RepeatOffenderDataDto());
			var hotLocations = BuildSection(
				"hotLocations",
				() => BuildHotLocationsForDeployment(filtered, effectiveTo),
				() => new List<HotLocationDto>());
			var deployment = BuildSection(
				"deploymentRecommendations",
				() => BuildDeploymentRecommendations(filtered, hotLocations, effectiveTo),
				() => new DeploymentRecommendationDto
				{
					OverallStrategy = "Analytics data could not be fully generated for the selected period."
				});
			var crimeLinking = BuildSection(
				"crimeLinking",
				() => BuildCrimeLinking(filtered, fromStr, toStr),
				() => new CrimeLinkingDataDto
				{
					Period = new DateRangeDto { Start = fromStr, End = toStr }
				});

			_logger.LogInformation(
				"Analytics hub generated: {Count} incidents, {Offenders} repeat offenders, {Clusters} clusters",
				total, repeatOffenders.TotalOffenders, crimeLinking.Clusters?.Count ?? 0);

			return new AnalyticsHubDto
			{
				CrimeTrends = crimeTrends,
				HotProducts = hotProducts,
				FinancialSummary = financialSummary,
				StoreRecoveryComparisons = storeRecoveryComparisons,
				RepeatOffenders = repeatOffenders,
				DeploymentRecommendations = deployment,
				CrimeLinking = crimeLinking,
				Metadata = new AnalyticsMetadataDto
				{
					GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					DateRange = new DateRangeDto { Start = fromStr, End = toStr },
					CustomerId = customerId
				}
			};
		}

		private static CrimeTrendDataDto BuildCrimeTrends(List<Incident> incidents, int total, string fromStr, string toStr)
		{
			var dayOrder = new[]
			{
				DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
				DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
			};

			var dayGroups = incidents
				.GroupBy(i => i.DateOfIncident.DayOfWeek)
				.ToDictionary(g => g.Key, g => g.ToList());

			var dayOfWeek = dayOrder.Select(d =>
			{
				var group = dayGroups.GetValueOrDefault(d);
				var count = group?.Count ?? 0;
				return new DayOfWeekDataDto
				{
					Day = d.ToString(),
					Incidents = count,
					Stores = group?.Select(i => i.StoreName).Distinct().Count() ?? 0,
					Percentage = total > 0 ? Math.Round((double)count / total * 100, 1) : 0
				};
			}).ToList();

			var hourGroups = incidents
				.Select(i => ParseHour(i.TimeOfIncident))
				.Where(h => h.HasValue)
				.GroupBy(h => h!.Value)
				.ToDictionary(g => g.Key, g => g.Count());

			// Emit all 24 hours so every incident is represented and the
			// per-hour percentages reconcile to 100% of the period total.
			// Hours outside store operating windows were previously dropped,
			// which hid late-night/early-morning incidents from the chart.
			var timeOfDay = Enumerable.Range(0, 24).Select(h =>
			{
				var count = hourGroups.GetValueOrDefault(h, 0);
				return new TimeOfDayDataDto
				{
					Hour = h,
					Label = FormatHourLabel(h),
					Incidents = count,
					Percentage = total > 0 ? Math.Round((double)count / total * 100, 1) : 0
				};
			}).ToList();

			var incidentTypes = incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType)
				.Select(g => new IncidentTypeDataDto
				{
					Type = g.Key,
					Count = g.Count(),
					Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0,
					TotalValue = g.Sum(GetIncidentLostValue)
				})
				.OrderByDescending(t => t.Count)
				.ToList();

			var storeDrilldown = incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.GroupBy(i => i.StoreName!)
				.ToDictionary(g => g.Key, g =>
				{
					var storeIncidents = g.ToList();
					var storeTotal = storeIncidents.Count;

					var peakDay = storeIncidents
						.GroupBy(i => i.DateOfIncident.DayOfWeek)
						.OrderByDescending(d => d.Count())
						.FirstOrDefault()?.Key.ToString() ?? "Monday";

					var peakHour = storeIncidents
						.Select(i => ParseHour(i.TimeOfIncident))
						.Where(h => h.HasValue)
						.GroupBy(h => h!.Value)
						.OrderByDescending(d => d.Count())
						.FirstOrDefault()?.Key ?? 12;

					int.TryParse(storeIncidents.First().SiteId, out var siteIdInt);

					var incidentsByDay = storeIncidents
						.GroupBy(i => i.DateOfIncident.DayOfWeek.ToString())
						.ToDictionary(d => d.Key, d => d.Count());

					var incidentsByHour = storeIncidents
						.Select(i => new { Incident = i, Hour = ParseHour(i.TimeOfIncident) })
						.Where(x => x.Hour.HasValue)
						.GroupBy(x => x.Hour!.Value)
						.ToDictionary(h => h.Key, h => h.Count());

					return new StoreDrilldownDataDto
					{
						StoreId = siteIdInt,
						StoreName = g.Key,
						Incidents = storeTotal,
						IncidentsByDay = incidentsByDay,
						IncidentsByHour = incidentsByHour,
						IncidentTypesByDay = storeIncidents
							.GroupBy(i => i.DateOfIncident.DayOfWeek.ToString())
							.ToDictionary(
								d => d.Key,
								d => BuildIncidentTypeBreakdown(d.ToList())),
						IncidentTypesByHour = storeIncidents
							.Select(i => new { Incident = i, Hour = ParseHour(i.TimeOfIncident) })
							.Where(x => x.Hour.HasValue)
							.GroupBy(x => x.Hour!.Value)
							.ToDictionary(
								h => h.Key,
								h => BuildIncidentTypeBreakdown(h.Select(x => x.Incident).ToList())),
						TotalStolenValue = storeIncidents.Sum(GetIncidentStolenValue),
						TotalRecoveredValue = storeIncidents.Sum(GetIncidentRecoveredValue),
						TotalLostValue = storeIncidents.Sum(GetIncidentLostValue),
						RecoveryRate = CalculateRecoveryRate(
							storeIncidents.Sum(GetIncidentRecoveredValue),
							storeIncidents.Sum(GetIncidentStolenValue)),
						IncidentTypes = BuildIncidentTypeBreakdown(storeIncidents),
						PeakDay = peakDay,
						PeakHour = peakHour
					};
				});

			var recoveryTrend = BuildRecoveryTrend(incidents, fromStr, toStr);

			return new CrimeTrendDataDto
			{
				DayOfWeek = dayOfWeek,
				TimeOfDay = timeOfDay,
				IncidentTypes = incidentTypes,
				StoreDrilldown = storeDrilldown,
				RecoveryTrend = recoveryTrend,
				TotalIncidents = total,
				DateRange = new DateRangeDto { Start = fromStr, End = toStr }
			};
		}

		private static HotProductsDataDto BuildHotProducts(
			List<Incident> incidents,
			string fromStr,
			string toStr,
			DateTime periodEnd)
		{
			var allItems = incidents
				.SelectMany(i => (i.StolenItems ?? Array.Empty<StolenItem>())
					.Select(si => new { Incident = i, Item = si }))
				.ToList();

			var totalValueStolen = allItems.Sum(x => GetItemStolenValue(x.Item));
			var totalValueRecovered = allItems.Sum(x => GetItemRecoveredValue(x.Item));
			var totalValueLost = allItems.Sum(x => GetItemLostValue(x.Item));

			var productGroups = allItems
				.Where(x => x.Item.Quantity > 0 || GetItemStolenValue(x.Item) > 0)
				.GroupBy(x => GetProductGroupKey(x.Item))
				.Select(g =>
				{
					var displayBarcode = g
						.Select(x => x.Item.Barcode?.Trim())
						.FirstOrDefault(b => !string.IsNullOrWhiteSpace(b) && b.Length >= 4) ?? string.Empty;
					var productName = g
						.Select(x => x.Item.ProductName ?? x.Item.Description)
						.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "Unknown product";
					var frequency = g.Count();
					var stolenValue = g.Sum(x => GetItemStolenValue(x.Item));
					var recoveredValue = g.Sum(x => GetItemRecoveredValue(x.Item));
					var lostValue = g.Sum(x => GetItemLostValue(x.Item));
					var storeBreakdown = g
						.Where(x => !string.IsNullOrWhiteSpace(x.Incident.StoreName))
						.GroupBy(x => x.Incident.StoreName!, StringComparer.OrdinalIgnoreCase)
						.Select(sg =>
						{
							int.TryParse(sg.First().Incident.SiteId, out var storeId);
							var storeStolen = sg.Sum(x => GetItemStolenValue(x.Item));
							var storeRecovered = sg.Sum(x => GetItemRecoveredValue(x.Item));
							var storeLost = sg.Sum(x => GetItemLostValue(x.Item));
							return new ProductStoreBreakdownDto
							{
								StoreId = storeId,
								StoreName = sg.Key,
								Frequency = sg.Count(),
								StolenValue = storeStolen,
								RecoveredValue = storeRecovered,
								LostValue = storeLost,
								RecoveryRate = CalculateRecoveryRate(storeRecovered, storeStolen),
							};
						})
						.OrderByDescending(s => s.LostValue)
						.ThenByDescending(s => s.Frequency)
						.ToList();

					var storesAffected = storeBreakdown.Count;
					var recoveryRate = CalculateRecoveryRate(recoveredValue, stolenValue);

					return new ProductFrequencyDataDto
					{
						Barcode = displayBarcode,
						ProductName = productName,
						Frequency = frequency,
						TotalValue = lostValue,
						StolenValue = stolenValue,
						RecoveredValue = recoveredValue,
						LostValue = lostValue,
						RecoveryRate = recoveryRate,
						StoresAffected = storesAffected,
						Stores = storeBreakdown,
						Reason =
							$"Stolen in {frequency} line item{(frequency == 1 ? "" : "s")} across {storesAffected} store{(storesAffected == 1 ? "" : "s")}: " +
							$"£{lostValue:N0} lost, {recoveryRate:F1}% recovered (£{stolenValue:N0} stolen)."
					};
				})
				.ToList();

			var topProducts = productGroups
				.OrderByDescending(p => p.LostValue)
				.ThenByDescending(p => p.Frequency)
				.Take(20)
				.ToList();

			var topRecoveredProducts = productGroups
				.Where(p => p.RecoveredValue > 0)
				.OrderByDescending(p => p.RecoveredValue)
				.ThenByDescending(p => p.Frequency)
				.Take(20)
				.ToList();

			var worstRecoveryProducts = productGroups
				.Where(p => p.StolenValue > 0)
				.OrderBy(p => p.RecoveryRate)
				.ThenByDescending(p => p.LostValue)
				.Take(20)
				.ToList();

			var storeHeatmap = allItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Incident.StoreName))
				.GroupBy(x => x.Incident.StoreName!)
				.Select(g =>
				{
					int.TryParse(g.First().Incident.SiteId, out var sId);
					var storeIncidentsForRisk = incidents
						.Where(i => string.Equals(i.StoreName, g.Key, StringComparison.OrdinalIgnoreCase))
						.ToList();
					var storeIncidentCount = storeIncidentsForRisk.Count;

					var theftIncidentIds = g
						.Select(x => x.Incident.IncidentId)
						.Where(id => id > 0)
						.ToHashSet();
					var incidentsWithStolenItems = theftIncidentIds.Count > 0
						? theftIncidentIds.Count
						: g.Select(x => x.Incident).DistinctBy(i => i.IncidentId).Count();

					var storeRisk = AnalyticsRules.BuildLocationRiskBreakdown(
						storeIncidentsForRisk,
						periodEnd,
						GetIncidentLostValue);

					var products = g
						.GroupBy(x => GetProductGroupKey(x.Item))
						.Select(pg => new StoreProductItemDto
						{
							Barcode = pg
								.Select(x => x.Item.Barcode?.Trim())
								.FirstOrDefault(b => !string.IsNullOrWhiteSpace(b)) ?? string.Empty,
							ProductName = pg
								.Select(x => x.Item.ProductName ?? x.Item.Description)
								.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "Unknown product",
							Frequency = pg.Count(),
							Value = pg.Sum(x => GetItemLostValue(x.Item)),
							StolenValue = pg.Sum(x => GetItemStolenValue(x.Item)),
							RecoveredValue = pg.Sum(x => GetItemRecoveredValue(x.Item)),
							LostValue = pg.Sum(x => GetItemLostValue(x.Item)),
							RecoveryRate = CalculateRecoveryRate(
								pg.Sum(x => GetItemRecoveredValue(x.Item)),
								pg.Sum(x => GetItemStolenValue(x.Item)))
						})
						.OrderByDescending(p => p.LostValue)
						.ThenByDescending(p => p.Frequency)
						.ToList();

					var productLineCount = g.Count();

					return new StoreProductHeatmapDataDto
					{
						StoreId = sId,
						StoreName = g.Key,
						Products = products,
						TotalIncidents = storeIncidentCount,
						IncidentsWithStolenItems = incidentsWithStolenItems,
						ProductLineCount = productLineCount,
						ProductGroupCount = products.Count,
						TotalValueStolen = g.Sum(x => GetItemStolenValue(x.Item)),
						TotalValueRecovered = g.Sum(x => GetItemRecoveredValue(x.Item)),
						TotalValueLost = g.Sum(x => GetItemLostValue(x.Item)),
						RecoveryRate = CalculateRecoveryRate(
							g.Sum(x => GetItemRecoveredValue(x.Item)),
							g.Sum(x => GetItemStolenValue(x.Item))),
						RiskLevel = storeRisk.Level,
						RiskScore = storeRisk.Score,
						RiskSummary = AnalyticsRules.BuildStoreRiskSummary(storeRisk),
						RiskFactors = storeRisk.Factors,
					};
				})
				.OrderByDescending(s => s.TotalValueLost)
				.ThenByDescending(s => s.IncidentsWithStolenItems)
				.ToList();

			return new HotProductsDataDto
			{
				TopProducts = topProducts,
				TopRecoveredProducts = topRecoveredProducts,
				WorstRecoveryProducts = worstRecoveryProducts,
				StoreHeatmap = storeHeatmap,
				TotalValueStolen = totalValueStolen,
				TotalValueRecovered = totalValueRecovered,
				TotalValueLost = totalValueLost,
				RecoveryRate = CalculateRecoveryRate(totalValueRecovered, totalValueStolen),
				Period = new DateRangeDto { Start = fromStr, End = toStr }
			};
		}

		private static OffenderProfileDto BuildOffenderProfile(IGrouping<string, Incident> g)
		{
			var offenderIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
			var totalVal = offenderIncidents.Sum(GetIncidentLostValue);
			var storesTargeted = offenderIncidents
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.Select(i => i.StoreName!)
				.Distinct()
				.ToList();

			var incCount = offenderIncidents.Count;
			var riskLevel = incCount >= 5 || totalVal >= 1000m ? "critical"
				: incCount >= 3 || totalVal >= 500m ? "high"
				: incCount >= 2 ? "medium"
				: "low";

			var firstDate = offenderIncidents.First().DateOfIncident.ToString("yyyy-MM-dd");
			var lastDate = offenderIncidents.Last().DateOfIncident.ToString("yyyy-MM-dd");

			var displayName = offenderIncidents
				.Select(AnalyticsRules.GetOffenderDisplayName)
				.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
				?? "Unidentified";

			var reason = incCount >= 2
				? $"Repeat offender: {incCount} incidents from {firstDate} to {lastDate}, " +
				  $"£{totalVal:N0} lost across {storesTargeted.Count} store{(storesTargeted.Count == 1 ? "" : "s")} (risk: {riskLevel})."
				: $"Single incident on {lastDate}, £{totalVal:N0} lost" +
				  (storesTargeted.Count > 0 ? $" at {storesTargeted[0]}." : ".");

			return new OffenderProfileDto
			{
				OffenderId = g.Key,
				Name = displayName,
				IncidentCount = incCount,
				FirstIncident = firstDate,
				LastIncident = lastDate,
				StoresTargeted = storesTargeted,
				TotalValue = totalVal,
				RiskLevel = riskLevel,
				Incidents = offenderIncidents.Select(ToOffenderIncidentSummary).ToList(),
				Reason = reason,
			};
		}

		private static RepeatOffenderDataDto BuildRepeatOffenders(List<Incident> incidents)
		{
			// Group all offenders (by OffenderId when present, otherwise by normalised OffenderName)
			// We no longer require 2+ incidents to appear in the analytics; single-incident offenders
			// are included with a low risk level, so they can still be analysed and tracked.
			var offenderGroups = incidents
				.Where(AnalyticsRules.IncidentHasIdentifiedOffender)
				.GroupBy(AnalyticsRules.BuildOffenderGroupingKey)
				.Where(g => !string.IsNullOrEmpty(g.Key))
				.ToList();

			var mostActive = offenderGroups
				.Select(BuildOffenderProfile)
				.OrderByDescending(o => o.IncidentCount)
				.ThenByDescending(o => o.TotalValue)
				.Take(50)
				.ToList();

			var crossStoreMovements = offenderGroups
				.Select(g =>
				{
					var orderedIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
					var distinctStores = orderedIncidents
						.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
						.Select(i => i.StoreName!.Trim())
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.Count();
					if (distinctStores < 2)
					{
						return null;
					}

					var movements = new List<MovementEventDto>();
					for (var idx = 0; idx < orderedIncidents.Count; idx++)
					{
						var incident = orderedIncidents[idx];
						var store = incident.StoreName?.Trim() ?? string.Empty;
						var stolenProducts = BuildLinkedStolenProducts(incident);
						var visit = new MovementEventDto
						{
							StoreName = store,
							Date = incident.DateOfIncident.ToString("yyyy-MM-dd"),
							DateTimeLabel = FormatIncidentDateTimeLabel(incident),
							IncidentType = incident.IncidentType,
							IncidentId = incident.IncidentId.ToString(),
							StolenProductsSummary = BuildStolenProductsSummary(stolenProducts),
							Value = GetIncidentLostValue(incident),
						};

						if (idx > 0)
						{
							var previousStore = orderedIncidents[idx - 1].StoreName?.Trim() ?? string.Empty;
							if (!string.Equals(previousStore, store, StringComparison.OrdinalIgnoreCase))
							{
								visit.PreviousStore = previousStore;
								visit.FromStore = previousStore;
								visit.ToStore = store;
							}
						}

						movements.Add(visit);
					}

					var movementDisplayName = orderedIncidents
						.Select(AnalyticsRules.GetOffenderDisplayName)
						.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
						?? "Unidentified";

					return new CrossStoreMovementDto
					{
						OffenderId = g.Key,
						OffenderName = movementDisplayName,
						Movements = movements,
						TotalStores = distinctStores,
					};
				})
				.Where(m => m != null)
				.Cast<CrossStoreMovementDto>()
				.OrderByDescending(m => m.TotalStores)
				.ThenByDescending(m => m.Movements.Count)
				.ToList();

			var networkMap = BuildOffenderNetwork(mostActive, incidents);

			return new RepeatOffenderDataDto
			{
				MostActive = mostActive,
				CrossStoreMovements = crossStoreMovements,
				NetworkMap = networkMap,
				TotalOffenders = offenderGroups.Count
			};
		}

		private static string BuildStoreNodeId(string storeName)
		{
			var slug = System.Text.RegularExpressions.Regex.Replace(
				storeName.Trim().ToLowerInvariant(),
				@"[^a-z0-9]+",
				"-").Trim('-');
			return string.IsNullOrEmpty(slug) ? "store-unknown" : $"store-{slug}";
		}

		private static OffenderNetworkDataDto BuildOffenderNetwork(List<OffenderProfileDto> offenders, List<Incident> incidents)
		{
			var nodes = new List<OffenderNetworkNodeDto>();
			var links = new List<OffenderNetworkLinkDto>();

			var repeatOffenders = offenders
				.Where(o => o.IncidentCount >= 2)
				.OrderByDescending(o => o.IncidentCount)
				.ThenByDescending(o => o.TotalValue)
				.Take(15)
				.ToList();

			if (repeatOffenders.Count == 0)
			{
				return new OffenderNetworkDataDto { Nodes = nodes, Links = links };
			}

			var allStores = repeatOffenders
				.SelectMany(o => o.StoresTargeted)
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			var centerX = 300.0;
			var centerY = 300.0;

			for (var i = 0; i < repeatOffenders.Count; i++)
			{
				var angle = repeatOffenders.Count > 1 ? (double)i / repeatOffenders.Count * 2 * Math.PI : 0;
				nodes.Add(new OffenderNetworkNodeDto
				{
					Id = $"offender-{repeatOffenders[i].OffenderId}",
					Name = repeatOffenders[i].Name,
					Type = "offender",
					X = centerX + 200 * Math.Cos(angle),
					Y = centerY + 200 * Math.Sin(angle)
				});
			}

			for (var i = 0; i < allStores.Count; i++)
			{
				var angle = allStores.Count > 1 ? (double)i / allStores.Count * 2 * Math.PI : 0;
				nodes.Add(new OffenderNetworkNodeDto
				{
					Id = BuildStoreNodeId(allStores[i]),
					Name = allStores[i],
					Type = "store",
					X = centerX + 110 * Math.Cos(angle),
					Y = centerY + 110 * Math.Sin(angle)
				});
			}

			var storeNodeIds = nodes.Where(n => n.Type == "store").Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
			foreach (var offender in repeatOffenders)
			{
				foreach (var store in offender.StoresTargeted)
				{
					var storeNodeId = BuildStoreNodeId(store);
					if (!storeNodeIds.Contains(storeNodeId))
					{
						continue;
					}

					var storeIncidentCount = offender.Incidents.Count(i =>
						string.Equals(i.StoreName, store, StringComparison.OrdinalIgnoreCase));
					if (storeIncidentCount == 0)
					{
						storeIncidentCount = 1;
					}

					links.Add(new OffenderNetworkLinkDto
					{
						Source = $"offender-{offender.OffenderId}",
						Target = storeNodeId,
						Strength = Math.Min(storeIncidentCount / 5.0, 1.0),
						IncidentCount = storeIncidentCount
					});
				}
			}

			return new OffenderNetworkDataDto { Nodes = nodes, Links = links };
		}

		private static List<HotLocationDto> BuildHotLocationsForDeployment(List<Incident> incidents, DateTime periodEnd)
		{
			return incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.GroupBy(i => new { i.StoreName, i.RegionName })
				.Select(g =>
				{
					var siteIncidents = g.ToList();
					var siteValue = siteIncidents.Sum(GetIncidentLostValue);
					return new HotLocationDto
					{
						SiteName = g.Key.StoreName!,
						RegionName = g.Key.RegionName,
						IncidentCount = siteIncidents.Count,
						TotalValue = siteValue,
						RiskScore = CalculateLocationRiskScore(siteIncidents, periodEnd)
					};
				})
				.OrderByDescending(h => h.RiskScore)
				.ThenByDescending(h => h.IncidentCount)
				.ThenByDescending(h => h.TotalValue)
				.Take(10)
				.ToList();
		}

		private static DeploymentRecommendationDto BuildDeploymentRecommendations(
			List<Incident> incidents,
			List<HotLocationDto> hotLocations,
			DateTime periodEnd)
		{
			var periodStart30 = periodEnd.AddDays(-30);
			var periodStart60 = periodEnd.AddDays(-60);
			var totalIncidents = incidents.Count;

			var timeSlotGroups = incidents
				.Select(i => new { Incident = i, Day = i.DateOfIncident.DayOfWeek, Hour = ParseHour(i.TimeOfIncident) })
				.Where(x => x.Hour.HasValue)
				.GroupBy(x => new { x.Day, Hour = x.Hour!.Value })
				.ToList();

			var maxCount = timeSlotGroups.Any() ? timeSlotGroups.Max(g => g.Count()) : 1;

			var bestTimes = timeSlotGroups
				.OrderByDescending(g => g.Count())
				.Take(20)
				.Select(g =>
				{
					var slotIncidents = g.Select(x => x.Incident).ToList();
					var count = slotIncidents.Count;
					var ratio = (double)count / maxCount;
					var hourLabel = FormatHourLabel(g.Key.Hour);
					var day = g.Key.Day.ToString();

					return new TimeDeploymentRecommendationDto
					{
						Day = day,
						Hour = g.Key.Hour,
						HourLabel = hourLabel,
						RecommendedOfficers = Math.Max(1, (int)Math.Ceiling(count / 3.0)),
						OfficerType = AnalyticsRules.StoreDetectivesOfficerType,
						RecommendedLpm = AnalyticsRules.RequiresLpm(slotIncidents),
						Priority = AnalyticsRules.ToDeploymentPriority(ratio),
						Reason = AnalyticsRules.BuildTimeSlotReason(count, totalIncidents, day, hourLabel, slotIncidents),
						ReasonDetails = AnalyticsRules.BuildTimeSlotReasonDetails(slotIncidents, totalIncidents),
						ExpectedIncidents = count
					};
				})
				.ToList();

			var storeRankings = hotLocations
				.Select((h, idx) =>
				{
					var storeIncidents = incidents.Where(i => i.StoreName == h.SiteName).ToList();
					var recentCount = storeIncidents.Count(i =>
						i.DateOfIncident >= periodStart30 && i.DateOfIncident <= periodEnd);
					var prevCount = storeIncidents.Count(i =>
						i.DateOfIncident >= periodStart60 && i.DateOfIncident < periodStart30);

					var trend = AnalyticsRules.ComputeTrend(recentCount, prevCount);
					var breakdown = AnalyticsRules.BuildLocationRiskBreakdown(
						storeIncidents,
						periodEnd,
						GetIncidentLostValue);

					int.TryParse(storeIncidents.FirstOrDefault()?.SiteId, out var sId);

					var peakHours = storeIncidents
						.Select(i => ParseHour(i.TimeOfIncident))
						.Where(hour => hour.HasValue)
						.GroupBy(hour => hour!.Value)
						.OrderByDescending(hourGroup => hourGroup.Count())
						.Take(3)
						.Select(hourGroup => FormatHourLabel(hourGroup.Key))
						.ToList();

					var recommendedLpm = AnalyticsRules.RequiresLpm(storeIncidents);
					var rank = idx + 1;

					return new StoreRiskRankingDto
					{
						StoreId = sId,
						StoreName = h.SiteName,
						RiskScore = breakdown.Score,
						RiskLevel = breakdown.Level,
						IncidentCount = h.IncidentCount,
						Trend = trend,
						RecommendedOfficerType = AnalyticsRules.StoreDetectivesOfficerType,
						RecommendedLpm = recommendedLpm,
						RecommendedHours = peakHours,
						Priority = rank,
						Reason = AnalyticsRules.BuildStoreRiskReason(
							breakdown, rank, trend, recentCount, prevCount, peakHours, recommendedLpm),
						ReasonDetails = AnalyticsRules.BuildStoreRiskReasonDetails(breakdown),
						RiskFactors = breakdown.Factors
					};
				})
				.ToList();

			var topDay = timeSlotGroups.Any()
				? timeSlotGroups
					.GroupBy(g => g.Key.Day)
					.OrderByDescending(dayGroup => dayGroup.Sum(g => g.Count()))
					.First()
					.Key
					.ToString()
				: "unknown";

			var topHour = timeSlotGroups.Any()
				? FormatHourLabel(
					timeSlotGroups.OrderByDescending(g => g.Count()).First().Key.Hour)
				: "unknown";

			var overallStrategy = totalIncidents == 0
				? "No incidents in the selected period."
				: $"{totalIncidents} incidents, {hotLocations.Count} site(s). Peak {topDay} {topHour}. " +
				  $"{AnalyticsRules.StoreDetectivesOfficerType}." +
				  (storeRankings.Any(s => s.RiskLevel is "high" or "critical")
					  ? $" Focus: {string.Join(", ", storeRankings.Where(s => s.RiskLevel is "high" or "critical").Take(3).Select(s => s.StoreName))}."
					  : " Moderate risk across stores.");

			return new DeploymentRecommendationDto
			{
				BestTimes = bestTimes,
				StoreRankings = storeRankings,
				OverallStrategy = overallStrategy,
				LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
			};
		}

		private static CrimeLinkingDataDto BuildCrimeLinking(List<Incident> incidents, string fromStr, string toStr)
		{
			var clusters = new List<IncidentClusterDto>();

			// Offender-based clusters (same offender identity across multiple incidents)
			var offenderIdClusters = incidents
				.Where(AnalyticsRules.IncidentHasIdentifiedOffender)
				.GroupBy(AnalyticsRules.BuildOffenderGroupingKey)
				.Where(g => g.Count() >= 2)
				.Take(10);

			foreach (var g in offenderIdClusters)
			{
				var clusterIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
				var commonFeatures = BuildCommonFeatures(clusterIncidents);
				var totalVal = SumIncidentValue(clusterIncidents);

				var offenderConfidence = ComputeOffenderClusterConfidence(clusterIncidents);

				var offenderDisplayName = clusterIncidents
					.Select(AnalyticsRules.GetOffenderDisplayName)
					.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
					?? "Unidentified";

				clusters.Add(new IncidentClusterDto
				{
					ClusterId = $"cluster-offender-{g.Key}",
					Title = $"{offenderDisplayName} · {clusterIncidents.Count} linked incidents",
					Incidents = clusterIncidents
						.Select(i => ToLinkedIncident(i, commonFeatures, offenderConfidence))
						.ToList(),
					CommonFeatures = commonFeatures,
					SuspectedOffender = new SuspectedOffenderDto
					{
						Id = g.Key,
						Name = clusterIncidents
							.Select(AnalyticsRules.GetOffenderDisplayName)
							.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
							?? "Unidentified",
						Confidence = offenderConfidence
					},
					TotalValue = totalVal,
					DateRange = new DateRangeDto
					{
						Start = clusterIncidents.First().DateOfIncident.ToString("yyyy-MM-dd"),
						End = clusterIncidents.Last().DateOfIncident.ToString("yyyy-MM-dd")
					},
					Reason =
						$"Linked by matching offender identity ({clusterIncidents.Count} incidents, confidence {offenderConfidence:P0})."
				});
			}

			// Pattern-based clusters (same type + store, no identified offender)
			var patternClusters = incidents
				.Where(i => !AnalyticsRules.IncidentHasIdentifiedOffender(i))
				.GroupBy(i => new
				{
					Type = string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType,
					Store = i.StoreName ?? string.Empty
				})
				.Where(g => g.Count() >= 3)
				.Take(5);

			foreach (var g in patternClusters)
			{
				var clusterIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
				var commonFeatures = new List<string>
				{
					$"Same incident type: {g.Key.Type}",
					$"Same location: {g.Key.Store}"
				};

				var patternConfidence = Math.Min(0.5 + clusterIncidents.Count * 0.05, 0.85);

				clusters.Add(new IncidentClusterDto
				{
					ClusterId = $"cluster-pattern-{g.Key.Type.Replace(" ", "-")}-{g.Key.Store.Replace(" ", "-")}",
					Title = $"{g.Key.Type} at {g.Key.Store} · {clusterIncidents.Count} incidents",
					Incidents = clusterIncidents
						.Select(i => ToLinkedIncident(i, commonFeatures, patternConfidence))
						.ToList(),
					CommonFeatures = commonFeatures,
					SuspectedOffender = null,
					TotalValue = SumIncidentValue(clusterIncidents),
					DateRange = new DateRangeDto
					{
						Start = clusterIncidents.First().DateOfIncident.ToString("yyyy-MM-dd"),
						End = clusterIncidents.Last().DateOfIncident.ToString("yyyy-MM-dd")
					},
					Reason =
						$"Pattern cluster: {clusterIncidents.Count} '{g.Key.Type}' incidents at {g.Key.Store} with no named offender (confidence {patternConfidence:P0})."
				});
			}

			var offenderChains = incidents
				.Where(AnalyticsRules.IncidentHasIdentifiedOffender)
				.GroupBy(AnalyticsRules.BuildOffenderGroupingKey)
				.Where(g => g.Count() >= 2)
				.Take(10)
				.Select(g =>
				{
					var chainIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
					var stores = chainIncidents.Where(i => !string.IsNullOrWhiteSpace(i.StoreName)).Select(i => i.StoreName!).Distinct().ToList();
					var types = chainIncidents.Select(i => i.IncidentType).Distinct().ToList();

					var pattern = stores.Count == 1
						? $"Repeatedly targets {stores[0]}"
						: types.Count == 1
							? $"Consistent MO: {types[0]} across multiple stores"
							: $"Mobile offender — active across {stores.Count} store{(stores.Count > 1 ? "s" : "")}";

					var matchingFeatures = new List<string> { "Same offender" };

					var chainDisplayName = chainIncidents
						.Select(AnalyticsRules.GetOffenderDisplayName)
						.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
						?? "Unidentified";

					return new OffenderChainDto
					{
						ChainId = $"chain-{g.Key}",
						OffenderId = g.Key,
						OffenderName = chainDisplayName,
						Incidents = chainIncidents.Select(i => ToLinkedIncident(i, matchingFeatures, 0.9)).ToList(),
						Timeline = chainIncidents.Select(ToChainTimelineEvent).ToList(),
						TotalValue = SumIncidentValue(chainIncidents),
						Pattern = pattern
					};
				})
				.ToList();

			var linkedIncidentIds = clusters
				.SelectMany(c => c.Incidents.Select(i => i.IncidentId))
				.Concat(offenderChains.SelectMany(c => c.Incidents.Select(i => i.IncidentId)))
				.Distinct()
				.Count();

			return new CrimeLinkingDataDto
			{
				Clusters = clusters,
				OffenderChains = offenderChains,
				TotalLinkedIncidents = linkedIncidentIds,
				Period = new DateRangeDto { Start = fromStr, End = toStr }
			};
		}

		// ============================================================================
		// Private Helpers
		// ============================================================================

		private static int? ParseHour(string? timeString)
		{
			if (string.IsNullOrWhiteSpace(timeString)) return null;
			var parts = timeString.Trim().Split(':');
			if (parts.Length >= 1 && int.TryParse(parts[0], out var h) && h >= 0 && h <= 23)
				return h;
			return null;
		}

		private static string FormatHourLabel(int hour)
		{
			var period = hour < 12 ? "AM" : "PM";
			var displayH = hour % 12;
			if (displayH == 0) displayH = 12;
			return $"{displayH}{period}";
		}

		private static AnalyticsFinancialSummaryDto BuildFinancialSummary(List<Incident> incidents)
		{
			var totalStolenValue = incidents.Sum(GetIncidentStolenValue);
			var totalRecoveredValue = incidents.Sum(GetIncidentRecoveredValue);
			var totalLostValue = incidents.Sum(GetIncidentLostValue);
			var totalRecoveredQuantity = incidents.Sum(GetIncidentRecoveredQuantity);
			var totalLostQuantity = incidents.Sum(GetIncidentLostQuantity);

			return new AnalyticsFinancialSummaryDto
			{
				TotalStolenValue = totalStolenValue,
				TotalRecoveredValue = totalRecoveredValue,
				TotalLostValue = totalLostValue,
				RecoveryRate = CalculateRecoveryRate(totalRecoveredValue, totalStolenValue),
				TotalRecoveredQuantity = totalRecoveredQuantity,
				TotalLostQuantity = totalLostQuantity
			};
		}

		private static List<StoreRecoveryComparisonDto> BuildStoreRecoveryComparisons(List<Incident> incidents)
		{
			return incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.GroupBy(i => i.StoreName!)
				.Select(g =>
				{
					int.TryParse(g.First().SiteId, out var siteId);
					var storeIncidents = g.ToList();
					var stolenValue = storeIncidents.Sum(GetIncidentStolenValue);
					var recoveredValue = storeIncidents.Sum(GetIncidentRecoveredValue);
					var lostValue = storeIncidents.Sum(GetIncidentLostValue);

					return new StoreRecoveryComparisonDto
					{
						StoreId = siteId,
						StoreName = g.Key,
						IncidentCount = storeIncidents.Count,
						TotalStolenValue = stolenValue,
						TotalRecoveredValue = recoveredValue,
						TotalLostValue = lostValue,
						RecoveryRate = CalculateRecoveryRate(recoveredValue, stolenValue),
						TotalRecoveredQuantity = storeIncidents.Sum(GetIncidentRecoveredQuantity),
						TotalLostQuantity = storeIncidents.Sum(GetIncidentLostQuantity)
					};
				})
				.OrderByDescending(x => x.TotalLostValue)
				.ThenByDescending(x => x.IncidentCount)
				.ToList();
		}

		private static List<RecoveryTrendPointDto> BuildRecoveryTrend(List<Incident> incidents, string fromStr, string toStr)
		{
			var startDate = DateTime.Parse(fromStr);
			var endDate = DateTime.Parse(toStr);
			var daySpan = (endDate - startDate).TotalDays;

			if (daySpan <= 14)
			{
				return incidents
					.GroupBy(i => i.DateOfIncident.Date)
					.Select(g => new RecoveryTrendPointDto
					{
						Period = g.Key.ToString("yyyy-MM-dd"),
						IncidentCount = g.Count(),
						StolenValue = g.Sum(GetIncidentStolenValue),
						RecoveredValue = g.Sum(GetIncidentRecoveredValue),
						LostValue = g.Sum(GetIncidentLostValue)
					})
					.OrderBy(x => x.Period)
					.ToList();
			}

			var trends = new List<RecoveryTrendPointDto>();
			var cursor = startDate.Date;
			while (cursor <= endDate.Date)
			{
				var next = cursor.AddDays(7);
				var windowIncidents = incidents
					.Where(i => i.DateOfIncident >= cursor && i.DateOfIncident < next)
					.ToList();

				trends.Add(new RecoveryTrendPointDto
				{
					Period = $"W/C {cursor:dd MMM}",
					IncidentCount = windowIncidents.Count,
					StolenValue = windowIncidents.Sum(GetIncidentStolenValue),
					RecoveredValue = windowIncidents.Sum(GetIncidentRecoveredValue),
					LostValue = windowIncidents.Sum(GetIncidentLostValue)
				});

				cursor = next;
			}

			return trends;
		}

		private static decimal SumIncidentValue(List<Incident> incidents) =>
			incidents.Sum(GetIncidentLostValue);

		private static List<IncidentTypeDataDto> BuildIncidentTypeBreakdown(List<Incident> incidents)
		{
			var total = incidents.Count;
			return incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType)
				.Select(t => new IncidentTypeDataDto
				{
					Type = t.Key,
					Count = t.Count(),
					Percentage = total > 0 ? Math.Round((double)t.Count() / total * 100, 1) : 0,
					TotalValue = t.Sum(GetIncidentLostValue),
				})
				.OrderByDescending(t => t.Count)
				.ToList();
		}

		private static decimal GetIncidentStolenValue(Incident incident) =>
			(incident.TotalStolenValue ?? 0) > 0
				? incident.TotalStolenValue ?? 0
				: incident.StolenItems?.Sum(GetItemStolenValue) ?? 0;

		private static decimal GetIncidentRecoveredValue(Incident incident) =>
			(incident.TotalRecoveredValue ?? 0) > 0
				? incident.TotalRecoveredValue ?? 0
				: incident.StolenItems?.Sum(GetItemRecoveredValue) ?? 0;

		private static decimal GetIncidentLostValue(Incident incident)
		{
			if ((incident.TotalLostValue ?? 0) > 0)
			{
				return incident.TotalLostValue ?? 0;
			}

			var stolenValue = GetIncidentStolenValue(incident);
			var recoveredValue = GetIncidentRecoveredValue(incident);
			var calculatedLostValue = stolenValue - recoveredValue;
			return calculatedLostValue > 0 ? calculatedLostValue : 0;
		}

		private static int GetIncidentRecoveredQuantity(Incident incident) =>
			(incident.TotalRecoveredQuantity ?? 0) > 0
				? incident.TotalRecoveredQuantity ?? 0
				: incident.StolenItems?.Sum(item => item.RecoveredQuantity) ?? 0;

		private static int GetIncidentLostQuantity(Incident incident)
		{
			var totalQuantity = incident.StolenItems?.Sum(item => item.Quantity) ?? 0;
			var recoveredQuantity = GetIncidentRecoveredQuantity(incident);
			return Math.Max(totalQuantity - recoveredQuantity, 0);
		}

		private static decimal GetItemStolenValue(StolenItem item) =>
			item.TotalAmount > 0 ? item.TotalAmount : item.Cost * item.Quantity;

		private static decimal GetItemRecoveredValue(StolenItem item) =>
			item.RecoveredAmount > 0 ? item.RecoveredAmount : item.Cost * item.RecoveredQuantity;

		private static decimal GetItemLostValue(StolenItem item)
		{
			var calculatedLostValue = GetItemStolenValue(item) - GetItemRecoveredValue(item);
			return calculatedLostValue > 0 ? calculatedLostValue : 0;
		}

		private static double CalculateRecoveryRate(decimal recoveredValue, decimal stolenValue)
		{
			if (stolenValue <= 0) return 0;
			return Math.Round((double)(recoveredValue / stolenValue) * 100, 1);
		}

		private static double ComputeOffenderClusterConfidence(List<Incident> incidents)
		{
			var baseScore = 0.7;
			if (incidents.Count >= 3)
			{
				baseScore += 0.1;
			}

			if (incidents.Any(i => i.OffenderDOB.HasValue))
			{
				baseScore += 0.05;
			}

			var sameStore = incidents.Select(i => i.StoreName).Distinct().Count() == 1;
			if (sameStore)
			{
				baseScore += 0.05;
			}

			return Math.Round(Math.Min(baseScore, 0.98), 2);
		}

		private static List<string> BuildCommonFeatures(List<Incident> incidents)
		{
			var features = new List<string>();
			var commonTypes = incidents.GroupBy(i => i.IncidentType).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
			if (commonTypes.Any()) features.Add($"Common type: {string.Join(", ", commonTypes)}");
			var commonStores = incidents.GroupBy(i => i.StoreName ?? "").Where(g => g.Key != "" && g.Count() > 1).Select(g => g.Key).ToList();
			if (commonStores.Any()) features.Add($"Repeat stores: {string.Join(", ", commonStores)}");
			if (!features.Any()) features.Add("Same offender identified");
			return features;
		}

		private static List<LinkedIncidentStolenProductDto> BuildLinkedStolenProducts(Incident incident) =>
			(incident.StolenItems ?? Array.Empty<StolenItem>())
				.Where(item => item.Quantity > 0 || GetItemStolenValue(item) > 0)
				.Select(item => new LinkedIncidentStolenProductDto
				{
					ProductName = item.ProductName ?? item.Description ?? "Unknown product",
					Barcode = item.Barcode?.Trim() ?? string.Empty,
					Quantity = item.Quantity,
					LostValue = GetItemLostValue(item),
				})
				.OrderByDescending(item => item.LostValue)
				.ThenByDescending(item => item.Quantity)
				.ToList();

		private static string BuildStolenProductsSummary(IReadOnlyList<LinkedIncidentStolenProductDto> products)
		{
			if (products.Count == 0)
			{
				return "No stolen product lines recorded";
			}

			return string.Join(
				"; ",
				products.Take(4).Select(product =>
					string.IsNullOrWhiteSpace(product.Barcode)
						? $"{product.ProductName} (×{product.Quantity}, £{product.LostValue:N0} lost)"
						: $"{product.ProductName} [{product.Barcode}] (×{product.Quantity}, £{product.LostValue:N0} lost)"));
		}

		private static string FormatIncidentDateTimeLabel(Incident incident)
		{
			var date = incident.DateOfIncident.ToString("yyyy-MM-dd");
			if (string.IsNullOrWhiteSpace(incident.TimeOfIncident))
			{
				return date;
			}

			var hour = ParseHour(incident.TimeOfIncident);
			return hour.HasValue
				? $"{date} {FormatHourLabel(hour.Value)}"
				: $"{date} {incident.TimeOfIncident.Trim()}";
		}

		private static OffenderIncidentSummaryDto ToOffenderIncidentSummary(Incident incident)
		{
			var stolenProducts = BuildLinkedStolenProducts(incident);
			return new OffenderIncidentSummaryDto
			{
				IncidentId = incident.IncidentId.ToString(),
				Date = incident.DateOfIncident.ToString("yyyy-MM-dd"),
				TimeOfIncident = incident.TimeOfIncident?.Trim() ?? string.Empty,
				DateTimeLabel = FormatIncidentDateTimeLabel(incident),
				StoreName = incident.StoreName ?? string.Empty,
				IncidentType = incident.IncidentType,
				Value = GetIncidentLostValue(incident),
				StolenProducts = stolenProducts,
				StolenProductsSummary = BuildStolenProductsSummary(stolenProducts),
			};
		}

		private static ChainTimelineEventDto ToChainTimelineEvent(Incident incident)
		{
			var stolenProducts = BuildLinkedStolenProducts(incident);
			return new ChainTimelineEventDto
			{
				Date = incident.DateOfIncident.ToString("yyyy-MM-dd"),
				TimeOfIncident = incident.TimeOfIncident?.Trim() ?? string.Empty,
				DateTimeLabel = FormatIncidentDateTimeLabel(incident),
				Store = incident.StoreName ?? string.Empty,
				IncidentType = incident.IncidentType,
				StolenProducts = stolenProducts,
				StolenProductsSummary = BuildStolenProductsSummary(stolenProducts),
			};
		}

		private static LinkedIncidentDto ToLinkedIncident(Incident incident, List<string> matchingFeatures, double similarityScore)
		{
			var stolenProducts = BuildLinkedStolenProducts(incident);
			var displayName = AnalyticsRules.GetOffenderDisplayName(incident);
			return new LinkedIncidentDto
			{
				IncidentId = incident.IncidentId.ToString(),
				Date = incident.DateOfIncident.ToString("yyyy-MM-dd"),
				TimeOfIncident = incident.TimeOfIncident?.Trim() ?? string.Empty,
				DateTimeLabel = FormatIncidentDateTimeLabel(incident),
				StoreName = incident.StoreName ?? string.Empty,
				IncidentType = incident.IncidentType,
				OffenderId = incident.OffenderId,
				OffenderName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
				Value = GetIncidentLostValue(incident),
				SimilarityScore = similarityScore,
				MatchingFeatures = matchingFeatures,
				StolenProducts = stolenProducts,
				StolenProductsSummary = BuildStolenProductsSummary(stolenProducts),
			};
		}
	}
}
