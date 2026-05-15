using System.Text;
using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIPBackend.Tests;

public class ProductBarcodeCsvImportServiceTests
{
	private const string StandardHeaders = "barcode,Department,VMECode,ProductName,RetailPrice";

	[Fact]
	public async Task ImportAsync_CapsRowErrorsReturned_At200()
	{
		await using var db = CreateContext();
		var service = CreateService(db);

		var sb = new StringBuilder(StandardHeaders).Append('\n');
		for (var i = 0; i < 250; i++)
		{
			sb.Append($",PROVISIONS,V,Name{i},1.00\n");
		}

		using var stream = ToStream(sb.ToString());
		var result = await service.ImportAsync(stream, "errors.csv", "tester");

		Assert.Equal(250, result.InvalidRows);
		Assert.Equal(200, result.RowErrorsReturned);
		Assert.Equal(200, result.RowErrors.Count);
	}

	[Fact]
	public async Task ImportAsync_ProcessesMoreThanOneChunk()
	{
		await using var db = CreateContext();
		var service = CreateService(db);

		var sb = new StringBuilder(StandardHeaders).Append('\n');
		for (var i = 0; i < 501; i++)
		{
			sb.Append($"EAN{i:D6},PROVISIONS,VME{i},Product {i},1.00\n");
		}

		using var stream = ToStream(sb.ToString());
		var result = await service.ImportAsync(stream, "chunk.csv", "tester");

		Assert.True(result.ImportCompleted);
		Assert.Equal(501, result.ValidRows);
		Assert.Equal(501, result.CreatedCount);
		Assert.Equal(501, await db.Products.CountAsync());
	}

	[Fact]
	public async Task ImportAsync_PreservesDescriptionAndDepartment_WhenUpdateCellsEmptyOrWhitespace()
	{
		await using var db = CreateContext();
		db.Products.Add(new Product
		{
			EAN = "5012345678900",
			ProductName = "Old",
			Description = "KEEP-VME",
			Department = "BEERS/WINES/SPIRITS",
			Price = 4.50m,
			CreatedAt = DateTime.UtcNow,
			CreatedBy = "seed",
			IsActive = true,
		});
		await db.SaveChangesAsync();

		var service = CreateService(db);
		const string csv =
			"barcode,Department,VMECode,ProductName,RetailPrice\n" +
			"5012345678900,   ,,Updated name,\n";

		using var stream = ToStream(csv);
		var result = await service.ImportAsync(stream, "preserve.csv", "tester");

		var product = await db.Products.SingleAsync(p => p.EAN == "5012345678900");
		Assert.Equal("Updated name", product.ProductName);
		Assert.Equal("KEEP-VME", product.Description);
		Assert.Equal("BEERS/WINES/SPIRITS", product.Department);
		Assert.Equal(4.50m, product.Price);
		Assert.Equal(1, result.UpdatedCount);
	}

	private static ProductBarcodeCsvImportService CreateService(ApplicationDbContext db) =>
		new(db, NullLogger<ProductBarcodeCsvImportService>.Instance);

	private static ApplicationDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
			.Options;
		return new ApplicationDbContext(options);
	}

	private static MemoryStream ToStream(string csv)
	{
		var bytes = Encoding.UTF8.GetBytes(csv);
		return new MemoryStream(bytes);
	}
}
