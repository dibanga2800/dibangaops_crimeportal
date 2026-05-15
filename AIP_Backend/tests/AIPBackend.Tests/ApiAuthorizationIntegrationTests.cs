using System.Net;
using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AIPBackend.Tests;

public class ApiAuthorizationIntegrationTests : IClassFixture<SecurityWebApplicationFactory>
{
	private readonly SecurityWebApplicationFactory _factory;

	public ApiAuthorizationIntegrationTests(SecurityWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task CrimeInsightsEndpoint_RejectsAnonymousRequests()
	{
		var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/incidents/insights?customerId=100");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task EvidenceById_ReturnsForbidden_ForCrossTenantAccess()
	{
		var (incidentId, evidenceId) = await SeedIncidentAndEvidenceAsync(customerId: 100);
		var client = CreateAuthenticatedClient(
			userId: "user-2",
			role: "manager",
			customerId: 200);

		var response = await client.GetAsync($"/api/Evidence/{evidenceId}");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task EvidenceById_ReturnsOk_ForAuthorizedTenant()
	{
		var (_, evidenceId) = await SeedIncidentAndEvidenceAsync(customerId: 100);
		var client = CreateAuthenticatedClient(
			userId: "user-1",
			role: "manager",
			customerId: 100);

		var response = await client.GetAsync($"/api/Evidence/{evidenceId}");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	private HttpClient CreateAuthenticatedClient(string userId, string role, int customerId)
	{
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
		client.DefaultRequestHeaders.Add("X-Test-Role", role);
		client.DefaultRequestHeaders.Add("X-Test-CustomerId", customerId.ToString());
		return client;
	}

	private async Task<(int IncidentId, int EvidenceId)> SeedIncidentAndEvidenceAsync(int customerId)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		await db.Database.EnsureDeletedAsync();
		await db.Database.EnsureCreatedAsync();

		var incident = new Incident
		{
			CustomerId = customerId,
			StoreName = "Site Alpha",
			StaffMemberName = "Officer Jane",
			DateOfIncident = DateTime.UtcNow.AddDays(-1),
			DateInputted = DateTime.UtcNow.AddDays(-1),
			IncidentType = "theft",
			CreatedBy = "creator-user-1"
		};
		db.Incidents.Add(incident);
		await db.SaveChangesAsync();

		var evidence = new EvidenceItem
		{
			IncidentId = incident.IncidentId,
			Barcode = $"EVID-{Guid.NewGuid():N}",
			EvidenceType = "image",
			Status = "registered",
			RegisteredBy = "creator-user-1",
			RegisteredAt = DateTime.UtcNow
		};
		db.EvidenceItems.Add(evidence);
		await db.SaveChangesAsync();

		return (incident.IncidentId, evidence.EvidenceItemId);
	}
}
