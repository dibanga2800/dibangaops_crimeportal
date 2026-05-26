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
			new Mock<IIncidentImageStorageService>().Object,
			new Mock<IImageReferenceContentResolver>().Object);
	}
}
