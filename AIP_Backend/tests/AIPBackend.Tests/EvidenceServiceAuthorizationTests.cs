using AIPBackend.Data;
using AIPBackend.Exceptions;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIPBackend.Tests;

public class EvidenceServiceAuthorizationTests
{
	[Fact]
	public async Task GetByIdAsync_EnforcesIncidentScopeAndReturnsEvidence()
	{
		var context = CreateDbContext();
		var incident = await SeedIncidentAsync(context);
		var evidence = await SeedEvidenceAsync(context, incident.IncidentId, "EVID-1001");

		var userContext = new Mock<IUserContextService>(MockBehavior.Strict);
		userContext
			.Setup(service => service.EnsureCanAccessRecord(incident.CustomerId, incident.CreatedBy));

		var service = new EvidenceService(
			context,
			new Mock<ILogger<EvidenceService>>().Object,
			userContext.Object);

		var result = await service.GetByIdAsync(evidence.EvidenceItemId);

		Assert.Equal(evidence.EvidenceItemId, result.EvidenceItemId);
		Assert.Equal("EVID-1001", result.Barcode);
		userContext.Verify(service => service.EnsureCanAccessRecord(incident.CustomerId, incident.CreatedBy), Times.Once);
	}

	[Fact]
	public async Task ScanBarcodeAsync_ThrowsForbidden_WhenUserCannotAccessIncident()
	{
		var context = CreateDbContext();
		var incident = await SeedIncidentAsync(context);
		await SeedEvidenceAsync(context, incident.IncidentId, "EVID-2002");

		var userContext = new Mock<IUserContextService>(MockBehavior.Strict);
		userContext
			.Setup(service => service.EnsureCanAccessRecord(incident.CustomerId, incident.CreatedBy))
			.Throws(new ForbiddenAccessException("Cross-tenant access denied"));

		var service = new EvidenceService(
			context,
			new Mock<ILogger<EvidenceService>>().Object,
			userContext.Object);

		var request = new BarcodeScanDto
		{
			Barcode = "EVID-2002"
		};

		await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ScanBarcodeAsync(request));
	}

	[Fact]
	public async Task RecordCustodyEventAsync_UpdatesStatusAfterAuthorization()
	{
		var context = CreateDbContext();
		var incident = await SeedIncidentAsync(context);
		var evidence = await SeedEvidenceAsync(context, incident.IncidentId, "EVID-3003");

		var userContext = new Mock<IUserContextService>(MockBehavior.Strict);
		userContext
			.Setup(service => service.EnsureCanAccessRecord(incident.CustomerId, incident.CreatedBy));

		var service = new EvidenceService(
			context,
			new Mock<ILogger<EvidenceService>>().Object,
			userContext.Object);

		var custodyEvent = await service.RecordCustodyEventAsync(
			evidence.EvidenceItemId,
			new RecordCustodyEventDto
			{
				EventType = "received",
				Location = "Evidence Locker A"
			},
			"user-ops");

		var persistedEvidence = await context.EvidenceItems.FirstAsync(item => item.EvidenceItemId == evidence.EvidenceItemId);
		Assert.Equal("in-storage", persistedEvidence.Status);
		Assert.Equal("received", custodyEvent.EventType);
		userContext.Verify(service => service.EnsureCanAccessRecord(incident.CustomerId, incident.CreatedBy), Times.Once);
	}

	private static ApplicationDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
			.Options;

		return new ApplicationDbContext(options);
	}

	private static async Task<Incident> SeedIncidentAsync(ApplicationDbContext context)
	{
		var incident = new Incident
		{
			CustomerId = 42,
			StoreName = "Site Alpha",
			StaffMemberName = "Officer Jane",
			DateOfIncident = DateTime.UtcNow.AddDays(-1),
			DateInputted = DateTime.UtcNow.AddDays(-1),
			IncidentType = "theft",
			CreatedBy = "creator-user-1"
		};

		context.Incidents.Add(incident);
		await context.SaveChangesAsync();
		return incident;
	}

	private static async Task<EvidenceItem> SeedEvidenceAsync(ApplicationDbContext context, int incidentId, string barcode)
	{
		var evidence = new EvidenceItem
		{
			IncidentId = incidentId,
			Barcode = barcode,
			EvidenceType = "image",
			Status = "registered",
			RegisteredBy = "creator-user-1",
			RegisteredAt = DateTime.UtcNow
		};

		context.EvidenceItems.Add(evidence);
		await context.SaveChangesAsync();
		return evidence;
	}
}
