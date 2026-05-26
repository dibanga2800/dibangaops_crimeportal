using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Tests;

/// <summary>
/// Locks in the include/exclude policy for the periodic backfill query in
/// <see cref="IncidentRepository.GetIncidentsNeedingClassificationAsync"/>.
///
/// The bug that motivated these tests: rows tagged
/// "rule-based-fallback (inline-error)" were stranded forever because the
/// WHERE clause only listed "rule-based-fallback (inline-timeout)". The
/// inline path tags transient/unexpected exceptions with the inline-error
/// version, so excluding it from backfill leaves a recovering classifier
/// no way to refine those rows.
/// </summary>
public class IncidentRepositoryBackfillTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("rule-based-v1")]
	[InlineData("rule-based-v2")]
	[InlineData("rule-based-v3")]
	[InlineData("azure-openai-v1")]
	[InlineData("rule-based-fallback (inline-timeout)")]
	[InlineData("rule-based-fallback (inline-error)")]
	public async Task Backfill_picks_up_stale_or_transient_versions(string? version)
	{
		await using var context = CreateDbContext();
		var repository = new IncidentRepository(context);

		context.Incidents.Add(BuildIncident(classificationVersion: version));
		await context.SaveChangesAsync();

		var picked = await repository.GetIncidentsNeedingClassificationAsync(limit: 50);

		Assert.Single(picked);
	}

	[Theory]
	[InlineData("rule-based-v4")]
	[InlineData("azure-openai-v2")]
	[InlineData("rule-based-fallback (azure-error)")]
	[InlineData("rule-based-fallback (disabled)")]
	public async Task Backfill_skips_current_and_stable_fallback_versions(string version)
	{
		await using var context = CreateDbContext();
		var repository = new IncidentRepository(context);

		context.Incidents.Add(BuildIncident(classificationVersion: version));
		await context.SaveChangesAsync();

		var picked = await repository.GetIncidentsNeedingClassificationAsync(limit: 50);

		Assert.Empty(picked);
	}

	[Fact]
	public async Task Backfill_skips_soft_deleted_rows_even_if_version_is_stale()
	{
		await using var context = CreateDbContext();
		var repository = new IncidentRepository(context);

		var staleAndDeleted = BuildIncident(classificationVersion: "rule-based-fallback (inline-error)");
		staleAndDeleted.RecordIsDeletedYN = true;
		context.Incidents.Add(staleAndDeleted);
		await context.SaveChangesAsync();

		var picked = await repository.GetIncidentsNeedingClassificationAsync(limit: 50);

		Assert.Empty(picked);
	}

	[Fact]
	public async Task Backfill_picks_up_rows_missing_any_ai_field_regardless_of_version()
	{
		await using var context = CreateDbContext();
		var repository = new IncidentRepository(context);

		var missingCategory = BuildIncident(classificationVersion: "rule-based-v4");
		missingCategory.IncidentCategory = null;
		context.Incidents.Add(missingCategory);

		var missingRiskLevel = BuildIncident(classificationVersion: "rule-based-v4");
		missingRiskLevel.RiskLevel = null;
		context.Incidents.Add(missingRiskLevel);

		var missingRiskScore = BuildIncident(classificationVersion: "rule-based-v4");
		missingRiskScore.RiskScore = null;
		context.Incidents.Add(missingRiskScore);

		await context.SaveChangesAsync();

		var picked = await repository.GetIncidentsNeedingClassificationAsync(limit: 50);

		Assert.Equal(3, picked.Count);
	}

	[Fact]
	public async Task Backfill_respects_limit_and_orders_newest_first()
	{
		await using var context = CreateDbContext();
		var repository = new IncidentRepository(context);

		for (var i = 0; i < 5; i++)
		{
			context.Incidents.Add(BuildIncident(
				classificationVersion: "rule-based-v3",
				storeName: $"Store {i}"));
		}
		await context.SaveChangesAsync();

		var picked = await repository.GetIncidentsNeedingClassificationAsync(limit: 2);

		Assert.Equal(2, picked.Count);
		Assert.True(picked[0].IncidentId > picked[1].IncidentId);
	}

	private static ApplicationDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
			.Options;

		return new ApplicationDbContext(options);
	}

	private static Incident BuildIncident(string? classificationVersion, string storeName = "Test Store")
	{
		return new Incident
		{
			CustomerId = 1,
			StoreName = storeName,
			StaffMemberName = "Test Officer",
			DateOfIncident = DateTime.UtcNow,
			DateInputted = DateTime.UtcNow,
			IncidentType = "Shoplifting",
			IncidentCategory = "Shoplifting",
			IncidentCategoryConfidence = 0.80,
			RiskLevel = "low",
			RiskScore = 0.10,
			ClassificationVersion = classificationVersion,
			RecordIsDeletedYN = false,
			CreatedBy = "test-user",
		};
	}
}
