using System.Globalization;
using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using AIPBackend.Services;
using AIPBackend.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AIPBackend.Tests;

public class IncidentAnalyticsStatsTests
{
	[Fact]
	public void IncidentFinancials_recovered_and_lost_match_expected_rules()
	{
		var incident = new Incident
		{
			TotalStolenValue = 500m,
			TotalRecoveredValue = 200m,
			TotalLostValue = 300m,
			StolenItems = new List<StolenItem>
			{
				new() { TotalAmount = 500m, RecoveredAmount = 200m, Quantity = 2 },
			},
		};

		Assert.Equal(200m, IncidentFinancials.GetRecoveredValue(incident));
		Assert.Equal(300m, IncidentFinancials.GetLostValue(incident));
	}

	[Fact]
	public async Task GetIncidentsAsync_clamps_page_size_while_insights_return_full_count()
	{
		const int customerId = 7;
		const int seedCount = 150;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);
		for (var i = 0; i < seedCount; i++)
		{
			context.Incidents.Add(new Incident
			{
				CustomerId = customerId,
				StoreName = $"Store {i % 5}",
				StaffMemberName = "Officer",
				StaffMemberRole = "uniform officer",
				DateOfIncident = DateTime.UtcNow.AddDays(-i % 30),
				DateInputted = DateTime.UtcNow,
				IncidentType = "Shoplifting",
				CreatedBy = "test-user",
			});
		}

		await context.SaveChangesAsync();

		var service = CreateIncidentService(context);
		var fromDate = DateTime.UtcNow.AddYears(-1);
		var toDate = DateTime.UtcNow;

		var paged = await service.GetIncidentsAsync(new GetIncidentsQueryDto
		{
			Page = 1,
			PageSize = 250,
			CustomerId = customerId.ToString(),
		});

		var insights = await service.GetCrimeInsightsAsync(new CrimeIntelligenceQueryDto
		{
			CustomerId = customerId,
			StartDate = fromDate,
			EndDate = toDate,
		});

		Assert.Equal(100, paged.Pagination.PageSize);
		Assert.Equal(100, paged.Data.Count);
		Assert.Equal(seedCount, paged.Pagination.TotalCount);
		Assert.Equal("150", insights.HeroMetrics.First(m => m.Title == "Total Incidents").Value);

		// The dashboard "Shoplifting" card must reconcile with the
		// summary-driven "Total Incidents" card, so the server-side
		// shoplifting count must span the full filtered set even though
		// the page payload is capped at 100.
		Assert.Equal(seedCount, paged.Summary.TotalIncidents);
		Assert.Equal(seedCount, paged.Summary.ShopliftingIncidents);
	}

	[Fact]
	public async Task GetIncidentsAsync_summary_counts_shoplifting_by_case_insensitive_substring()
	{
		const int customerId = 21;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);

		// Types that name shoplifting as the primary offense — first word
		// "shoplifting" with the canonical or a qualifying tail.
		var shopliftingTypes = new[]
		{
			"Shoplifting",
			"Shoplifting / Theft",
			"shoplifting",
			"Shoplifting (confirmed)",
		};
		// "Attempted Shoplifting" is intentionally in the non-shoplifting
		// bucket: it is a distinct offense (the theft never completed)
		// and would otherwise inflate the dashboard "Shoplifting" card.
		var nonShopliftingTypes = new[]
		{
			"Attempted Shoplifting",
			"Theft",
			"Theft Prevention",
			"Self Scan Tills",
			"Violent Behaviour",
			"Others",
		};

		foreach (var type in shopliftingTypes)
		{
			context.Incidents.Add(BuildIncident(customerId, type));
		}
		foreach (var type in nonShopliftingTypes)
		{
			context.Incidents.Add(BuildIncident(customerId, type));
		}
		await context.SaveChangesAsync();

		var service = CreateIncidentService(context);
		var paged = await service.GetIncidentsAsync(new GetIncidentsQueryDto
		{
			Page = 1,
			PageSize = 50,
			CustomerId = customerId.ToString(),
		});

		Assert.Equal(shopliftingTypes.Length + nonShopliftingTypes.Length, paged.Summary.TotalIncidents);
		Assert.Equal(shopliftingTypes.Length, paged.Summary.ShopliftingIncidents);
	}

	[Fact]
	public async Task GetIncidentsAsync_summary_counts_today_high_priority_pending_and_resolved_across_full_set()
	{
		const int customerId = 31;
		const int seedCount = 200;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);

		var today = DateTime.UtcNow.Date;

		for (var i = 0; i < seedCount; i++)
		{
			// Three deterministic axes drive the assertions below:
			//   - "Today" rows: every 4th incident (i % 4 == 0).
			//   - High-priority rows: every 5th incident (i % 5 == 0).
			//   - Pending vs resolved rows alternate by i % 2.
			// These overlap intentionally so the test also exercises that the
			// counters are independent (one row can be Today + High +
			// Pending all at once).
			var incident = BuildIncident(customerId, "Shoplifting");
			incident.DateOfIncident = i % 4 == 0 ? today : today.AddDays(-((i % 30) + 1));
			incident.Priority = i % 5 == 0 ? "high" : "medium";
			incident.Status = i % 2 == 0 ? "pending" : "resolved";
			context.Incidents.Add(incident);
		}
		await context.SaveChangesAsync();

		var service = CreateIncidentService(context);
		var paged = await service.GetIncidentsAsync(new GetIncidentsQueryDto
		{
			Page = 1,
			PageSize = 1000,
			CustomerId = customerId.ToString(),
		});

		var expectedToday = Enumerable.Range(0, seedCount).Count(i => i % 4 == 0);
		var expectedHighPriority = Enumerable.Range(0, seedCount).Count(i => i % 5 == 0);
		var expectedPending = Enumerable.Range(0, seedCount).Count(i => i % 2 == 0);
		var expectedResolved = seedCount - expectedPending;

		// Page payload is clamped to 100, but the summary spans all 200.
		Assert.Equal(100, paged.Pagination.PageSize);
		Assert.Equal(100, paged.Data.Count);
		Assert.Equal(seedCount, paged.Pagination.TotalCount);
		Assert.Equal(seedCount, paged.Summary.TotalIncidents);
		Assert.Equal(expectedToday, paged.Summary.TodayIncidents);
		Assert.Equal(expectedHighPriority, paged.Summary.HighPriorityIncidents);
		Assert.Equal(expectedPending, paged.Summary.PendingIncidents);
		Assert.Equal(expectedResolved, paged.Summary.ResolvedIncidents);
	}

	[Fact]
	public async Task GetIncidentsAsync_summary_counts_priority_and_status_case_insensitively()
	{
		const int customerId = 41;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);

		// Realistic data has casing drift (LLM output sometimes returns
		// "High", manual edits use "high", legacy rows have "HIGH"). The
		// summary should bucket all of them together.
		var priorityValues = new[] { "high", "High", "HIGH", "medium", "low", "" };
		var pendingStatuses = new[] { "pending", "Pending", "PENDING" };
		var resolvedStatuses = new[] { "resolved", "Resolved", "RESOLVED" };

		foreach (var priority in priorityValues)
		{
			var incident = BuildIncident(customerId, "Shoplifting");
			incident.Priority = priority;
			incident.Status = "active";
			context.Incidents.Add(incident);
		}
		foreach (var status in pendingStatuses.Concat(resolvedStatuses))
		{
			var incident = BuildIncident(customerId, "Shoplifting");
			incident.Priority = "low";
			incident.Status = status;
			context.Incidents.Add(incident);
		}
		await context.SaveChangesAsync();

		var service = CreateIncidentService(context);
		var paged = await service.GetIncidentsAsync(new GetIncidentsQueryDto
		{
			Page = 1,
			PageSize = 50,
			CustomerId = customerId.ToString(),
		});

		// 3 high-priority rows (high, High, HIGH) — the other priority rows
		// in the priority-axis batch are medium/low/empty; the status-axis
		// batch is all "low".
		Assert.Equal(3, paged.Summary.HighPriorityIncidents);
		Assert.Equal(pendingStatuses.Length, paged.Summary.PendingIncidents);
		Assert.Equal(resolvedStatuses.Length, paged.Summary.ResolvedIncidents);
	}

	private static Incident BuildIncident(int customerId, string incidentType)
	{
		return new Incident
		{
			CustomerId = customerId,
			StoreName = "Test Store",
			StaffMemberName = "Officer",
			DateOfIncident = DateTime.UtcNow,
			DateInputted = DateTime.UtcNow,
			IncidentType = incidentType,
			CreatedBy = "test-user",
		};
	}

	[Fact]
	public async Task GetCrimeInsightsAsync_financial_totals_match_IncidentFinancials()
	{
		const int customerId = 13;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);
		context.Incidents.Add(new Incident
		{
			CustomerId = customerId,
			StoreName = "North",
			StaffMemberName = "Officer",
			DateOfIncident = new DateTime(DateTime.UtcNow.Year, 3, 1),
			DateInputted = DateTime.UtcNow,
			IncidentType = "Shoplifting",
			TotalRecoveredValue = 100m,
			TotalLostValue = 40m,
			CreatedBy = "test-user",
		});
		context.Incidents.Add(new Incident
		{
			CustomerId = customerId,
			StoreName = "South",
			StaffMemberName = "Officer",
			DateOfIncident = new DateTime(DateTime.UtcNow.Year, 3, 2),
			DateInputted = DateTime.UtcNow,
			IncidentType = "Theft",
			TotalStolenValue = 200m,
			TotalRecoveredValue = 50m,
			CreatedBy = "test-user",
		});
		await context.SaveChangesAsync();

		var incidents = await context.Incidents
			.Where(i => i.CustomerId == customerId)
			.ToListAsync();
		var expectedRecovered = incidents.Sum(IncidentFinancials.GetRecoveredValue);
		var expectedLost = incidents.Sum(IncidentFinancials.GetLostValue);

		var service = CreateIncidentService(context);
		var result = await service.GetCrimeInsightsAsync(new CrimeIntelligenceQueryDto
		{
			CustomerId = customerId,
			StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
			EndDate = DateTime.UtcNow,
		});

		var valueImpact = result.HeroMetrics
			.First(m => m.Title == "Value Impact")
			.ValueImpact;
		Assert.NotNull(valueImpact);
		var currencyFormat = CultureInfo.CreateSpecificCulture("en-GB");
		Assert.Equal(expectedRecovered.ToString("C0", currencyFormat), valueImpact.Recovered);
		Assert.Equal(expectedLost.ToString("C0", currencyFormat), valueImpact.EstimatedLoss);
	}

	[Fact]
	public async Task GetCrimeInsightsAsync_counts_all_incidents_for_customer()
	{
		const int customerId = 11;
		const int seedCount = 131;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);
		for (var i = 0; i < seedCount; i++)
		{
			context.Incidents.Add(new Incident
			{
				CustomerId = customerId,
				StoreName = "Central",
				StaffMemberName = "Officer",
				StaffMemberRole = "detective",
				DateOfIncident = new DateTime(DateTime.UtcNow.Year, 1, 15).AddDays(i % 10),
				DateInputted = DateTime.UtcNow,
				IncidentType = "Shoplifting",
				TotalRecoveredValue = 10m,
				TotalLostValue = 5m,
				CreatedBy = "test-user",
			});
		}

		await context.SaveChangesAsync();

		var service = CreateIncidentService(context);
		var result = await service.GetCrimeInsightsAsync(new CrimeIntelligenceQueryDto
		{
			CustomerId = customerId,
			StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
			EndDate = DateTime.UtcNow,
		});

		var totalMetric = result.HeroMetrics.First(m => m.Title == "Total Incidents");
		Assert.Equal("131", totalMetric.Value);
	}

	[Fact]
	public async Task GetIncidentGraphAnalyticsAsync_returns_full_incident_count()
	{
		const int customerId = 19;
		const int seedCount = 125;

		await using var context = CreateDbContext();
		SeedCustomer(context, customerId);
		for (var i = 0; i < seedCount; i++)
		{
			context.Incidents.Add(new Incident
			{
				CustomerId = customerId,
				StoreName = $"Location {i % 3}",
				StaffMemberName = "Officer",
				StaffMemberRole = "uniform officer",
				DateOfIncident = new DateTime(DateTime.UtcNow.Year, 2, 1).AddDays(i % 20),
				DateInputted = DateTime.UtcNow,
				IncidentType = "Theft",
				TotalRecoveredValue = 20m,
				TotalLostValue = 10m,
				CreatedBy = "test-user",
			});
		}

		await context.SaveChangesAsync();

		var service = CreateIncidentService(context);
		var result = await service.GetIncidentGraphAnalyticsAsync(new IncidentGraphAnalyticsQueryDto
		{
			CustomerId = customerId,
			FromDate = $"{DateTime.UtcNow.Year}-01-01",
			ToDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
			GraphType = "type",
		});

		Assert.Equal(seedCount, result.Totals.TotalIncidents);
		Assert.Equal(seedCount, result.Types.Sum(t => t.Count));
	}

	private static ApplicationDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
			.Options;

		return new ApplicationDbContext(options);
	}

	private static void SeedCustomer(ApplicationDbContext context, int customerId)
	{
		context.Customers.Add(new Customer
		{
			CustomerId = customerId,
			CompanyName = $"Customer {customerId}",
			CompanyNumber = $"C{customerId:D4}",
		});
	}

	private static IncidentService CreateIncidentService(ApplicationDbContext context)
	{
		var repository = new IncidentRepository(context);
		var userContext = new Mock<IUserContextService>();
		userContext.Setup(u => u.GetCurrentContext()).Returns(new UserRequestContext
		{
			Role = "administrator",
			AccessibleSiteIds = Array.Empty<string>(),
		});
		userContext.Setup(u => u.EnsureCanAccessCustomer(It.IsAny<int>()));
		userContext.Setup(u => u.ResolveCustomerFilter(It.IsAny<int?>()))
			.Returns<int?>(requested => new TenantCustomerFilter
			{
				Unrestricted = true,
				SingleCustomerId = requested,
			});
		userContext.Setup(u => u.ResolveSiteFilter(It.IsAny<string?>()))
			.Returns((string?)null);

		return new IncidentService(
			repository,
			new Mock<ISiteRepository>().Object,
			NullLogger<IncidentService>.Instance,
			userContext.Object,
			new ServiceCollection().BuildServiceProvider(),
			new Mock<IIncidentClassifier>().Object,
			new RuleBasedIncidentClassifier(NullLogger<RuleBasedIncidentClassifier>.Instance),
			new Mock<IIncidentImageStorageService>().Object,
			new Mock<IImageReferenceContentResolver>().Object);
	}
}
