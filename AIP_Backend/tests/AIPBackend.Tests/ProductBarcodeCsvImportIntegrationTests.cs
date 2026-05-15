using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIPBackend.Tests;

public class ProductBarcodeCsvImportIntegrationTests : IClassFixture<SecurityWebApplicationFactory>
{
	private const string StandardHeaders = "barcode,Department,VMECode,ProductName,RetailPrice";

	private readonly SecurityWebApplicationFactory _factory;

	public ProductBarcodeCsvImportIntegrationTests(SecurityWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_ReturnsUnauthorized_WhenAnonymous()
	{
		var client = _factory.CreateClient();
		using var content = BuildMultipart("a.csv", $"{StandardHeaders}\n1,PROVISIONS,A,B,1\n");
		var response = await client.PostAsync("/api/ProductImport/barcode-csv", content);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_ReturnsForbidden_WhenStoreUser()
	{
		var client = CreateAuthenticatedClient("u1", "store");
		await ResetDatabaseAsync();

		using var content = BuildMultipart("a.csv", $"{StandardHeaders}\n1,PROVISIONS,A,B,1\n");
		var response = await client.PostAsync("/api/ProductImport/barcode-csv", content);
		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_CreatesProduct_WhenManager()
	{
		var client = CreateAuthenticatedClient("mgr-1", "manager");
		await ResetDatabaseAsync();

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,PROVISIONS,VME-001,Milk 1L,3.99\n";
		using var multipart = BuildMultipart("catalog.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_CreatesProduct_WhenAdministrator()
	{
		var client = CreateAuthenticatedClient("admin-1", "administrator");
		await ResetDatabaseAsync();

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,PROVISIONS,VME-001,Milk 1L,3.99\n";
		using var multipart = BuildMultipart("catalog.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var product = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("Milk 1L", product.ProductName);
		Assert.Equal("PROVISIONS", product.Department);
		Assert.Equal("VME-001", product.Description);
		Assert.Equal(3.99m, product.Price);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_LastDuplicateRowWins_WhenSameBarcodeAppearsTwiceInFile()
	{
		var client = CreateAuthenticatedClient("admin-dup", "administrator");
		await ResetDatabaseAsync();

		const string csv =
			"barcode,Department,VMECode,ProductName,RetailPrice\n" +
			"5012345678900,BAKERY,VME-OLD,Old name,1.00\n" +
			"5012345678900,GROCERY 1,VME-NEW,New name,2.00\n";
		using var multipart = BuildMultipart("dup.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var product = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("New name", product.ProductName);
		Assert.Equal("VME-NEW", product.Description);
		Assert.Equal("GROCERY 1", product.Department);
		Assert.Equal(2.00m, product.Price);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_PreservesPriceOnUpdate_WhenRetailPriceCellEmpty()
	{
		var client = CreateAuthenticatedClient("admin-preserve", "administrator");
		await ResetDatabaseAsync();
		await SeedProductAsync("5012345678900", "Old", "OldVme", "OtherDept");

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var product = db.Products.Single(p => p.EAN == "5012345678900");
			product.Price = 4.50m;
			await db.SaveChangesAsync();
		}

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,,VME-NEW,New name,\n";
		using var multipart = BuildMultipart("update-price.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope2 = _factory.Services.CreateScope();
		var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var updated = db2.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("New name", updated.ProductName);
		Assert.Equal(4.50m, updated.Price);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_PreservesDepartmentOnUpdate_WhenDepartmentCellEmpty()
	{
		var client = CreateAuthenticatedClient("admin-dept-preserve", "administrator");
		await ResetDatabaseAsync();
		await SeedProductAsync("5012345678900", "Old", "OldVme", "BEERS/WINES/SPIRITS");

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,,VME-NEW,New name,5.00\n";
		using var multipart = BuildMultipart("update-dept.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var updated = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("New name", updated.ProductName);
		Assert.Equal("BEERS/WINES/SPIRITS", updated.Department);
		Assert.Equal(5.00m, updated.Price);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_UpdatesDepartment_WhenProvided()
	{
		var client = CreateAuthenticatedClient("admin-dept-update", "administrator");
		await ResetDatabaseAsync();
		await SeedProductAsync("5012345678900", "Old", "OldVme", "BAKERY");

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,FROZEN FOODS,VME-NEW,New name,9.99\n";
		using var multipart = BuildMultipart("update-dept2.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var product = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("FROZEN FOODS", product.Department);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_PreservesDescriptionOnUpdate_WhenVmeCodeCellEmpty()
	{
		var client = CreateAuthenticatedClient("admin-vme-preserve", "administrator");
		await ResetDatabaseAsync();
		await SeedProductAsync("5012345678900", "Old", "KEEP-VME", "PROVISIONS");

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,PROVISIONS,,New name,5.00\n";
		using var multipart = BuildMultipart("update-vme.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var updated = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("New name", updated.ProductName);
		Assert.Equal("KEEP-VME", updated.Description);
		Assert.Equal(5.00m, updated.Price);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_PreservesDepartmentOnUpdate_WhenDepartmentCellWhitespaceOnly()
	{
		var client = CreateAuthenticatedClient("admin-dept-ws", "administrator");
		await ResetDatabaseAsync();
		await SeedProductAsync("5012345678900", "Old", "VME", "BEERS/WINES/SPIRITS");

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,   ,VME-NEW,New name,5.00\n";
		using var multipart = BuildMultipart("update-dept-ws.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var updated = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("BEERS/WINES/SPIRITS", updated.Department);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_CreatesProductWithNullPrice_WhenRetailPriceCellEmpty()
	{
		var client = CreateAuthenticatedClient("admin-null-price", "administrator");
		await ResetDatabaseAsync();

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678901,BAKERY,V1,No price item,\n";
		using var multipart = BuildMultipart("create-null-price.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var product = db.Products.Single(p => p.EAN == "5012345678901");
		Assert.Null(product.Price);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_ReturnsBadRequest_WhenFileTooLarge()
	{
		var client = CreateAuthenticatedClient("admin-large", "administrator");
		await ResetDatabaseAsync();

		var bytes = new byte[(5 * 1024 * 1024) + 1];
		using var multipart = BuildMultipartFromBytes("big.csv", bytes);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_ReturnsBadRequest_WhenNotCsvExtension()
	{
		var client = CreateAuthenticatedClient("admin-ext", "administrator");
		await ResetDatabaseAsync();

		using var multipart = BuildMultipart("catalog.txt", $"{StandardHeaders}\n1,PROVISIONS,V,N,1\n");
		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_ReturnsBadRequest_WhenExceedsMaxRows()
	{
		var client = CreateAuthenticatedClient("admin-maxrows", "administrator");
		await ResetDatabaseAsync();

		var sb = new StringBuilder(StandardHeaders).Append('\n');
		for (var i = 0; i < 10_001; i++)
		{
			sb.Append($"EAN{i},PROVISIONS,V,Name{i},1.00\n");
		}

		using var multipart = BuildMultipart("too-many.csv", sb.ToString());
		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_Processes501RowsAcrossChunks_WhenAdministrator()
	{
		var client = CreateAuthenticatedClient("admin-chunk", "administrator");
		await ResetDatabaseAsync();

		var sb = new StringBuilder(StandardHeaders).Append('\n');
		for (var i = 0; i < 501; i++)
		{
			sb.Append($"EAN{i:D6},PROVISIONS,VME{i},Product {i},1.00\n");
		}

		using var multipart = BuildMultipart("chunk-501.csv", sb.ToString());
		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		Assert.Equal(501, await db.Products.CountAsync());
	}

	[Fact]
	public async Task BarcodeCsvImport_Post_UpdatesExisting_WhenAdministrator()
	{
		var client = CreateAuthenticatedClient("admin-2", "administrator");
		await ResetDatabaseAsync();
		await SeedProductAsync("5012345678900", "Old", "OldVme", "OtherDept");

		const string csv = "barcode,Department,VMECode,ProductName,RetailPrice\n5012345678900,OtherDept,VME-NEW,New name,9.99\n";
		using var multipart = BuildMultipart("update.csv", csv);

		var response = await client.PostAsync("/api/ProductImport/barcode-csv", multipart);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var product = db.Products.Single(p => p.EAN == "5012345678900");
		Assert.Equal("New name", product.ProductName);
		Assert.Equal("VME-NEW", product.Description);
		Assert.Equal("OtherDept", product.Department);
		Assert.Equal(9.99m, product.Price);
	}

	private HttpClient CreateAuthenticatedClient(string userId, string role)
	{
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
		client.DefaultRequestHeaders.Add("X-Test-Role", role);
		return client;
	}

	private async Task ResetDatabaseAsync()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		await db.Database.EnsureDeletedAsync();
		await db.Database.EnsureCreatedAsync();
	}

	private async Task SeedProductAsync(string ean, string name, string description, string department)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		db.Products.Add(new Product
		{
			EAN = ean,
			ProductName = name,
			Description = description,
			Department = department,
			Price = 9.99m,
			CreatedAt = DateTime.UtcNow,
			CreatedBy = "seed",
			IsActive = true,
		});
		await db.SaveChangesAsync();
	}

	private static MultipartFormDataContent BuildMultipart(string fileName, string csvBody)
	{
		return BuildMultipartFromBytes(fileName, Encoding.UTF8.GetBytes(csvBody));
	}

	private static MultipartFormDataContent BuildMultipartFromBytes(string fileName, byte[] bytes)
	{
		var content = new MultipartFormDataContent();
		var stream = new MemoryStream(bytes);
		var streamContent = new StreamContent(stream);
		streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
		content.Add(streamContent, "file", fileName);
		return content;
	}
}
