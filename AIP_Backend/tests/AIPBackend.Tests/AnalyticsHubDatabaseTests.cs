using System.Net;
using System.Text.Json;
using AIPBackend.Data;
using AIPBackend.Repositories;
using AIPBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIPBackend.Tests;

/// <summary>
/// Validates analytics hub generation against the local COOP database schema.
/// </summary>
public class AnalyticsHubDatabaseTests
{
	private const string ConnectionString =
		"Server=localhost\\SQLEXPRESS;Database=COOP;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;";

	[Fact]
	public async Task GetAnalyticsHubAsync_against_coop_database_succeeds()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseSqlServer(ConnectionString)
			.Options;

		await using var context = new ApplicationDbContext(options);
		var repository = new IncidentRepository(context);
		var service = new IncidentAnalyticsService(repository, NullLogger<IncidentAnalyticsService>.Instance);

		var result = await service.GetAnalyticsHubAsync();

		Assert.NotNull(result);
		Assert.NotNull(result.CrimeTrends);
		Assert.NotNull(result.CrimeLinking.Clusters);

		var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		});
		Assert.False(string.IsNullOrWhiteSpace(json));
	}
}

public class AnalyticsHubApiTests : IClassFixture<CoopWebApplicationFactory>
{
	private readonly CoopWebApplicationFactory _factory;

	public AnalyticsHubApiTests(CoopWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetHub_returns_ok_for_administrator()
	{
		var client = _factory.CreateAuthenticatedClient("admin-test", "administrator");
		var response = await client.GetAsync("/api/Analytics/hub?from=2026-04-18&to=2026-05-18");
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.False(string.IsNullOrWhiteSpace(body), body);
	}

	[Fact]
	public async Task GetHub_returns_ok_with_customer_and_site_filters()
	{
		var client = _factory.CreateAuthenticatedClient("admin-test", "administrator");
		var response = await client.GetAsync(
			"/api/Analytics/hub?from=2026-04-18&to=2026-05-18&customerId=1&siteId=1&regionId=1");
		var body = await response.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.False(string.IsNullOrWhiteSpace(body), body);
	}
}
