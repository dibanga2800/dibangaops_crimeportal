#nullable enable

using System.Globalization;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
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
			var fromDate = (from ?? DateTime.UtcNow.AddDays(-90)).Date;
			var toDateInclusive = (to ?? DateTime.UtcNow).Date;
			var effectiveFrom = fromDate;
			var effectiveTo = toDateInclusive.AddDays(1).AddTicks(-1);

			var incidents = await _repository.GetAllForStatsAsync(customerId, siteId, regionId);
			var filtered = incidents
				.Where(i => i.DateOfIncident >= effectiveFrom && i.DateOfIncident <= effectiveTo)
				.ToList();

			var totalValue = filtered.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);

			var repeatOffenders = filtered
				.Where(i => !string.IsNullOrWhiteSpace(i.OffenderName))
				.GroupBy(i => i.OffenderName!.ToLowerInvariant().Trim())
				.Where(g => g.Count() > 1)
				.Count();

			var hotLocations = filtered
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.GroupBy(i => new { i.StoreName, i.RegionName })
				.Select(g =>
				{
					var siteIncidents = g.ToList();
					var siteValue = siteIncidents.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
					return new HotLocationDto
					{
						SiteName = g.Key.StoreName!,
						RegionName = g.Key.RegionName,
						IncidentCount = siteIncidents.Count,
						TotalValue = siteValue,
						RiskScore = CalculateLocationRiskScore(siteIncidents)
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

		private static double CalculateLocationRiskScore(List<Incident> incidents)
		{
			var countWeight = Math.Min(incidents.Count / 20.0, 0.4);
			var valueWeight = Math.Min((double)(incidents.Sum(i => i.TotalValueRecovered ?? 0) / 5000m), 0.3);
			var policeWeight = incidents.Any(i => i.PoliceInvolvement) ? 0.2 : 0;
			var recencyWeight = incidents.Any(i => i.DateOfIncident >= DateTime.UtcNow.AddDays(-7)) ? 0.1 : 0;

			return Math.Round(Math.Min(countWeight + valueWeight + policeWeight + recencyWeight, 1.0), 2);
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
						Value = g.Sum(i => i.TotalValueRecovered ?? 0)
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
					Value = weekIncidents.Sum(i => i.TotalValueRecovered ?? 0)
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
					TotalValue = g.Sum(i => i.TotalValueRecovered ?? 0)
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
			var fromDate = (from ?? DateTime.UtcNow.AddDays(-90)).Date;
			var toDateInclusive = (to ?? DateTime.UtcNow).Date;
			var effectiveFrom = fromDate;
			var effectiveTo = toDateInclusive.AddDays(1).AddTicks(-1);

			var filtered = await _repository.GetAllForStatsAsync(customerId, siteId, regionId, effectiveFrom, effectiveTo);

			var total = filtered.Count;
			var fromStr = fromDate.ToString("yyyy-MM-dd");
			var toStr = toDateInclusive.ToString("yyyy-MM-dd");

			var crimeTrends = BuildCrimeTrends(filtered, total, fromStr, toStr);
			var hotProducts = BuildHotProducts(filtered, fromStr, toStr);
			var repeatOffenders = BuildRepeatOffenders(filtered);
			var hotLocations = BuildHotLocationsForDeployment(filtered);
			var deployment = BuildDeploymentRecommendations(filtered, hotLocations);
			var crimeLinking = BuildCrimeLinking(filtered, fromStr, toStr);

			_logger.LogInformation(
				"Analytics hub generated: {Count} incidents, {Offenders} repeat offenders, {Clusters} clusters",
				total, repeatOffenders.TotalOffenders, crimeLinking.Clusters.Count);

			return new AnalyticsHubDto
			{
				CrimeTrends = crimeTrends,
				HotProducts = hotProducts,
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

			var timeOfDay = Enumerable.Range(7, 16).Select(h =>
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
					TotalValue = g.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0)
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

					return new StoreDrilldownDataDto
					{
						StoreId = siteIdInt,
						StoreName = g.Key,
						Incidents = storeTotal,
						IncidentTypes = storeIncidents
							.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType)
							.Select(t => new IncidentTypeDataDto
							{
								Type = t.Key,
								Count = t.Count(),
								Percentage = storeTotal > 0 ? Math.Round((double)t.Count() / storeTotal * 100, 1) : 0,
								TotalValue = t.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0)
							})
							.OrderByDescending(t => t.Count)
							.ToList(),
						PeakDay = peakDay,
						PeakHour = peakHour
					};
				});

			return new CrimeTrendDataDto
			{
				DayOfWeek = dayOfWeek,
				TimeOfDay = timeOfDay,
				IncidentTypes = incidentTypes,
				StoreDrilldown = storeDrilldown,
				TotalIncidents = total,
				DateRange = new DateRangeDto { Start = fromStr, End = toStr }
			};
		}

		private static HotProductsDataDto BuildHotProducts(List<Incident> incidents, string fromStr, string toStr)
		{
			var allItems = incidents
				.SelectMany(i => i.StolenItems.Select(si => new { Incident = i, Item = si }))
				.ToList();

			var totalValueLost = allItems.Sum(x => x.Item.TotalAmount);

			var topProducts = allItems
				.GroupBy(x =>
				{
					var key = !string.IsNullOrWhiteSpace(x.Item.Barcode)
						? x.Item.Barcode.Trim()
						: (!string.IsNullOrWhiteSpace(x.Item.ProductName) ? x.Item.ProductName.Trim() : "Unknown");
					return key;
				})
				.Select(g => new ProductFrequencyDataDto
				{
					Barcode = g.Key,
					ProductName = g.Select(x => x.Item.ProductName ?? x.Item.Description ?? g.Key).First(s => !string.IsNullOrWhiteSpace(s)) ?? g.Key,
					Frequency = g.Count(),
					TotalValue = g.Sum(x => x.Item.TotalAmount),
					StoresAffected = g.Select(x => x.Incident.StoreName).Distinct().Count()
				})
				.OrderByDescending(p => p.Frequency)
				.Take(20)
				.ToList();

			var storeHeatmap = allItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Incident.StoreName))
				.GroupBy(x => x.Incident.StoreName!)
				.Select(g =>
				{
					int.TryParse(g.First().Incident.SiteId, out var sId);
					var storeIncidentCount = incidents.Count(i => i.StoreName == g.Key);

					var products = g
						.GroupBy(x =>
						{
							var key = !string.IsNullOrWhiteSpace(x.Item.Barcode)
								? x.Item.Barcode.Trim()
								: (!string.IsNullOrWhiteSpace(x.Item.ProductName) ? x.Item.ProductName.Trim() : "Unknown");
							return key;
						})
						.Select(pg => new StoreProductItemDto
						{
							Barcode = pg.Key,
							ProductName = pg.Select(x => x.Item.ProductName ?? x.Item.Description ?? pg.Key).First(s => !string.IsNullOrWhiteSpace(s)) ?? pg.Key,
							Frequency = pg.Count(),
							Value = pg.Sum(x => x.Item.TotalAmount)
						})
						.OrderByDescending(p => p.Frequency)
						.Take(5)
						.ToList();

					var riskLevel = storeIncidentCount >= 10 ? "critical"
						: storeIncidentCount >= 5 ? "high"
						: storeIncidentCount >= 2 ? "medium"
						: "low";

					return new StoreProductHeatmapDataDto
					{
						StoreId = sId,
						StoreName = g.Key,
						Products = products,
						TotalIncidents = storeIncidentCount,
						RiskLevel = riskLevel
					};
				})
				.OrderByDescending(s => s.TotalIncidents)
				.ToList();

			return new HotProductsDataDto
			{
				TopProducts = topProducts,
				StoreHeatmap = storeHeatmap,
				TotalValueLost = totalValueLost,
				Period = new DateRangeDto { Start = fromStr, End = toStr }
			};
		}

		private static string BuildOffenderKey(Incident incident)
		{
			var name = (incident.OffenderName ?? string.Empty).Trim().ToLowerInvariant();
			
			var genderSource = !string.IsNullOrWhiteSpace(incident.Gender)
				? incident.Gender
				: incident.OffenderSex;
			var genderPart = (genderSource ?? string.Empty).Trim().ToLowerInvariant();
			var dobPart = incident.OffenderDOB.HasValue
				? incident.OffenderDOB.Value.ToString("yyyy-MM-dd")
				: string.Empty;

			if (string.IsNullOrWhiteSpace(name))
			{
				// If we have no name at all, fall back to a placeholder that still
				// incorporates any available DOB/gender so we don't merge everything.
				name = "unknown";
			}

			// Build a compact key using only the attributes that are actually present,
			// so incidents with/without DOB or gender but the same name can still group.
			var parts = new List<string> { name };
			if (!string.IsNullOrEmpty(dobPart))
			{
				parts.Add(dobPart);
			}
			if (!string.IsNullOrEmpty(genderPart))
			{
				parts.Add(genderPart);
			}

			return string.Join("|", parts);
		}

		private static RepeatOffenderDataDto BuildRepeatOffenders(List<Incident> incidents)
		{
			// Group all offenders (by OffenderId when present, otherwise by normalised OffenderName)
			// We no longer require 2+ incidents to appear in the analytics; single-incident offenders
			// are included with a low risk level, so they can still be analysed and tracked.
			var offenderGroups = incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.OffenderName))
				.GroupBy(BuildOffenderKey)
				.ToList();

			var mostActive = offenderGroups
				.Select(g =>
				{
					var offenderIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
					var totalVal = offenderIncidents.Sum(i =>
						i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
					var storesTargeted = offenderIncidents
						.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
						.Select(i => i.StoreName!)
						.Distinct()
						.ToList();

					var moList = offenderIncidents
						.Where(i => !string.IsNullOrWhiteSpace(i.ModusOperandi))
						.SelectMany(i =>
						{
							try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(i.ModusOperandi!) ?? new List<string>(); }
							catch { return new List<string>(); }
						})
						.Distinct()
						.ToList();

					var incCount = offenderIncidents.Count;
					var riskLevel = incCount >= 5 || totalVal >= 1000m ? "critical"
						: incCount >= 3 || totalVal >= 500m ? "high"
						: incCount >= 2 ? "medium"
						: "low";

					return new OffenderProfileDto
					{
						OffenderId = g.Key,
						Name = offenderIncidents.First().OffenderName!,
						IncidentCount = incCount,
						FirstIncident = offenderIncidents.First().DateOfIncident.ToString("yyyy-MM-dd"),
						LastIncident = offenderIncidents.Last().DateOfIncident.ToString("yyyy-MM-dd"),
						StoresTargeted = storesTargeted,
						TotalValue = totalVal,
						RiskLevel = riskLevel,
						ModusOperandi = moList
					};
				})
				.OrderByDescending(o => o.IncidentCount)
				.Take(20)
				.ToList();

			var crossStoreMovements = offenderGroups
				.Where(g => g.Select(i => i.StoreName).Distinct().Count() > 1)
				.Select(g =>
				{
					var orderedIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
					var movements = new List<MovementEventDto>();
					for (var idx = 1; idx < orderedIncidents.Count; idx++)
					{
						if (orderedIncidents[idx].StoreName != orderedIncidents[idx - 1].StoreName)
						{
							movements.Add(new MovementEventDto
							{
								FromStore = orderedIncidents[idx - 1].StoreName ?? string.Empty,
								ToStore = orderedIncidents[idx].StoreName ?? string.Empty,
								Date = orderedIncidents[idx].DateOfIncident.ToString("yyyy-MM-dd"),
								IncidentType = orderedIncidents[idx].IncidentType
							});
						}
					}
					return new CrossStoreMovementDto
					{
						OffenderId = g.Key,
						OffenderName = orderedIncidents.First().OffenderName!,
						Movements = movements,
						TotalStores = orderedIncidents.Select(i => i.StoreName).Distinct().Count()
					};
				})
				.ToList();

			var networkMap = BuildOffenderNetwork(mostActive.Take(10).ToList(), incidents);

			return new RepeatOffenderDataDto
			{
				MostActive = mostActive,
				CrossStoreMovements = crossStoreMovements,
				NetworkMap = networkMap,
				TotalOffenders = offenderGroups.Count
			};
		}

		private static OffenderNetworkDataDto BuildOffenderNetwork(List<OffenderProfileDto> offenders, List<Incident> incidents)
		{
			var nodes = new List<OffenderNetworkNodeDto>();
			var links = new List<OffenderNetworkLinkDto>();

			var allStores = offenders.SelectMany(o => o.StoresTargeted).Distinct().ToList();
			var centerX = 300.0;
			var centerY = 300.0;

			for (var i = 0; i < offenders.Count; i++)
			{
				var angle = offenders.Count > 1 ? (double)i / offenders.Count * 2 * Math.PI : 0;
				nodes.Add(new OffenderNetworkNodeDto
				{
					Id = $"offender-{offenders[i].OffenderId}",
					Name = offenders[i].Name,
					Type = "offender",
					X = centerX + 200 * Math.Cos(angle),
					Y = centerY + 200 * Math.Sin(angle)
				});
			}

			for (var i = 0; i < allStores.Count; i++)
			{
				var angle = allStores.Count > 1 ? (double)i / allStores.Count * 2 * Math.PI : 0;
				var storeId = $"store-{allStores[i].ToLowerInvariant().Replace(" ", "-")}";
				nodes.Add(new OffenderNetworkNodeDto
				{
					Id = storeId,
					Name = allStores[i],
					Type = "store",
					X = centerX + 110 * Math.Cos(angle),
					Y = centerY + 110 * Math.Sin(angle)
				});
			}

			var storeNodeIds = nodes.Where(n => n.Type == "store").Select(n => n.Id).ToHashSet();
			foreach (var offender in offenders)
			{
				foreach (var store in offender.StoresTargeted)
				{
					var storeNodeId = $"store-{store.ToLowerInvariant().Replace(" ", "-")}";
					if (storeNodeIds.Contains(storeNodeId))
					{
						links.Add(new OffenderNetworkLinkDto
						{
							Source = $"offender-{offender.OffenderId}",
							Target = storeNodeId,
							Strength = Math.Min(offender.IncidentCount / 10.0, 1.0),
							IncidentCount = offender.IncidentCount
						});
					}
				}
			}

			return new OffenderNetworkDataDto { Nodes = nodes, Links = links };
		}

		private static List<HotLocationDto> BuildHotLocationsForDeployment(List<Incident> incidents)
		{
			return incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.GroupBy(i => new { i.StoreName, i.RegionName })
				.Select(g =>
				{
					var siteIncidents = g.ToList();
					var siteValue = siteIncidents.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);
					return new HotLocationDto
					{
						SiteName = g.Key.StoreName!,
						RegionName = g.Key.RegionName,
						IncidentCount = siteIncidents.Count,
						TotalValue = siteValue,
						RiskScore = CalculateLocationRiskScore(siteIncidents)
					};
				})
				.OrderByDescending(h => h.RiskScore)
				.Take(10)
				.ToList();
		}

		private static DeploymentRecommendationDto BuildDeploymentRecommendations(
			List<Incident> incidents,
			List<HotLocationDto> hotLocations)
		{
			var dayOrder = new[]
			{
				DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
				DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
			};

			var timeSlotCounts = incidents
				.Select(i => new { Day = i.DateOfIncident.DayOfWeek, Hour = ParseHour(i.TimeOfIncident) })
				.Where(x => x.Hour.HasValue)
				.GroupBy(x => new { x.Day, Hour = x.Hour!.Value })
				.Select(g => new { g.Key.Day, g.Key.Hour, Count = g.Count() })
				.ToList();

			var maxCount = timeSlotCounts.Any() ? timeSlotCounts.Max(x => x.Count) : 1;

			var bestTimes = timeSlotCounts
				.OrderByDescending(x => x.Count)
				.Take(20)
				.Select(x =>
				{
					var ratio = (double)x.Count / maxCount;
					var priority = ratio >= 0.7 ? "critical"
						: ratio >= 0.4 ? "high"
						: ratio >= 0.2 ? "medium"
						: "low";

					return new TimeDeploymentRecommendationDto
					{
						Day = x.Day.ToString(),
						Hour = x.Hour,
						HourLabel = FormatHourLabel(x.Hour),
						RecommendedOfficers = Math.Max(1, (int)Math.Ceiling(x.Count / 3.0)),
						OfficerType = x.Count >= 5 || x.Hour >= 17 ? "store detectives" : "uniform",
						RecommendedLpm = x.Count >= 3,
						Priority = priority,
						Reason = $"{x.Count} incident{(x.Count != 1 ? "s" : "")} recorded at this time",
						ExpectedIncidents = x.Count
					};
				})
				.ToList();

			var storeRankings = hotLocations
				.Select((h, idx) =>
				{
					var recentCount = incidents.Count(i =>
						i.StoreName == h.SiteName && i.DateOfIncident >= DateTime.UtcNow.AddDays(-30));
					var prevCount = incidents.Count(i =>
						i.StoreName == h.SiteName &&
						i.DateOfIncident >= DateTime.UtcNow.AddDays(-60) &&
						i.DateOfIncident < DateTime.UtcNow.AddDays(-30));

					var trend = recentCount > prevCount * 1.1 ? "increasing"
						: recentCount < prevCount * 0.9 ? "decreasing"
						: "stable";

					int.TryParse(incidents.FirstOrDefault(i => i.StoreName == h.SiteName)?.SiteId, out var sId);

					var peakHours = incidents
						.Where(i => i.StoreName == h.SiteName)
						.Select(i => ParseHour(i.TimeOfIncident))
						.Where(h2 => h2.HasValue)
						.GroupBy(h2 => h2!.Value)
						.OrderByDescending(g => g.Count())
						.Take(3)
						.Select(g => $"{FormatHourLabel(g.Key)}")
						.ToList();

					var riskLevel = h.RiskScore >= 0.7 ? "critical"
						: h.RiskScore >= 0.4 ? "high"
						: h.RiskScore >= 0.2 ? "medium"
						: "low";

					return new StoreRiskRankingDto
					{
						StoreId = sId,
						StoreName = h.SiteName,
						RiskScore = h.RiskScore,
						RiskLevel = riskLevel,
						IncidentCount = h.IncidentCount,
						Trend = trend,
						RecommendedOfficerType = h.IncidentCount >= 10 ? "store detectives" : "uniform",
						RecommendedLpm = h.RiskScore >= 0.4,
						RecommendedHours = peakHours,
						Priority = idx + 1
					};
				})
				.ToList();

			var topDay = timeSlotCounts.Any()
				? timeSlotCounts.GroupBy(x => x.Day).OrderByDescending(g => g.Sum(x => x.Count)).First().Key.ToString()
				: "unknown";

			var topHour = timeSlotCounts.Any()
				? FormatHourLabel(timeSlotCounts.OrderByDescending(x => x.Count).First().Hour)
				: "unknown";

			var overallStrategy = incidents.Count == 0
				? "No incident data available for the selected period."
				: $"Based on {incidents.Count} incidents across {hotLocations.Count} locations. " +
				  $"Peak activity on {topDay}s at {topHour}. " +
				  (storeRankings.Any(s => s.RiskLevel is "high" or "critical")
					  ? $"Prioritise deployment at {string.Join(", ", storeRankings.Where(s => s.RiskLevel is "high" or "critical").Take(3).Select(s => s.StoreName))}."
					  : "Maintain standard deployment across all sites.");

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
				.Where(i => !string.IsNullOrWhiteSpace(i.OffenderName))
				.GroupBy(BuildOffenderKey)
				.Where(g => g.Count() >= 2)
				.Take(10);

			foreach (var g in offenderIdClusters)
			{
				var clusterIncidents = g.OrderBy(i => i.DateOfIncident).ToList();
				var commonFeatures = BuildCommonFeatures(clusterIncidents);
				var totalVal = SumIncidentValue(clusterIncidents);

				clusters.Add(new IncidentClusterDto
				{
					ClusterId = $"cluster-offender-{g.Key}",
					Incidents = clusterIncidents.Select(i => ToLinkedIncident(i, commonFeatures, 0.95)).ToList(),
					CommonFeatures = commonFeatures,
					SuspectedOffender = new SuspectedOffenderDto
					{
						Id = g.Key,
						Name = clusterIncidents.First().OffenderName ?? "Unknown",
						Confidence = 0.95
					},
					TotalValue = totalVal,
					DateRange = new DateRangeDto
					{
						Start = clusterIncidents.First().DateOfIncident.ToString("yyyy-MM-dd"),
						End = clusterIncidents.Last().DateOfIncident.ToString("yyyy-MM-dd")
					}
				});
			}

			// Pattern-based clusters (same type + store, no identified offender)
			var patternClusters = incidents
				.Where(i => string.IsNullOrWhiteSpace(i.OffenderId))
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

				clusters.Add(new IncidentClusterDto
				{
					ClusterId = $"cluster-pattern-{g.Key.Type.Replace(" ", "-")}-{g.Key.Store.Replace(" ", "-")}",
					Incidents = clusterIncidents.Select(i => ToLinkedIncident(i, commonFeatures, 0.65)).ToList(),
					CommonFeatures = commonFeatures,
					SuspectedOffender = null,
					TotalValue = SumIncidentValue(clusterIncidents),
					DateRange = new DateRangeDto
					{
						Start = clusterIncidents.First().DateOfIncident.ToString("yyyy-MM-dd"),
						End = clusterIncidents.Last().DateOfIncident.ToString("yyyy-MM-dd")
					}
				});
			}

			var offenderChains = incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.OffenderName))
				.GroupBy(BuildOffenderKey)
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

					return new OffenderChainDto
					{
						ChainId = $"chain-{g.Key}",
						OffenderId = g.Key,
						OffenderName = chainIncidents.First().OffenderName!,
						Incidents = chainIncidents.Select(i => ToLinkedIncident(i, matchingFeatures, 0.9)).ToList(),
						Timeline = chainIncidents.Select(i => new ChainTimelineEventDto
						{
							Date = i.DateOfIncident.ToString("yyyy-MM-dd"),
							Store = i.StoreName ?? string.Empty,
							IncidentType = i.IncidentType
						}).ToList(),
						TotalValue = SumIncidentValue(chainIncidents),
						Pattern = pattern
					};
				})
				.ToList();

			var linkedIncidentIds = clusters
				.SelectMany(c => c.Incidents.Select(i => i.IncidentId))
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

		private static decimal SumIncidentValue(List<Incident> incidents) =>
			incidents.Sum(i => i.TotalValueRecovered ?? i.StolenItems?.Sum(s => s.TotalAmount) ?? 0);

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

		private static LinkedIncidentDto ToLinkedIncident(Incident incident, List<string> matchingFeatures, double similarityScore) =>
			new()
			{
				IncidentId = incident.IncidentId.ToString(),
				Date = incident.DateOfIncident.ToString("yyyy-MM-dd"),
				StoreName = incident.StoreName ?? string.Empty,
				IncidentType = incident.IncidentType,
				OffenderId = incident.OffenderId,
				OffenderName = incident.OffenderName,
				Value = incident.TotalValueRecovered ?? incident.StolenItems?.Sum(s => s.TotalAmount) ?? 0,
				SimilarityScore = similarityScore,
				MatchingFeatures = matchingFeatures
			};
	}
}
