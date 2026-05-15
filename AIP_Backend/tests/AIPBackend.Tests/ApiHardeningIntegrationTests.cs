using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AIPBackend.Tests;

public class ApiHardeningIntegrationTests : IClassFixture<SecurityWebApplicationFactory>
{
	private readonly SecurityWebApplicationFactory _factory;

	public ApiHardeningIntegrationTests(SecurityWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task SwaggerUi_IsDisabledByDefault_ReturnsNotFound()
	{
		var client = _factory.CreateClient();

		var response = await client.GetAsync("/swagger/index.html");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task UploadsPath_IsNotPubliclyServedByDefault()
	{
		var client = _factory.CreateClient();
		var environment = _factory.Services.GetRequiredService<IWebHostEnvironment>();
		var uploadsPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
		Directory.CreateDirectory(uploadsPath);
		var testFileName = $"security-test-{Guid.NewGuid():N}.txt";
		await File.WriteAllTextAsync(Path.Combine(uploadsPath, testFileName), "secured-upload", Encoding.UTF8);

		var response = await client.GetAsync($"/uploads/{testFileName}");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task ProductImport_RejectsDangerousFileExtension()
	{
		var client = CreateAuthenticatedClient("admin-user", "administrator");
		using var multipart = new MultipartFormDataContent();
		var bytes = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
		bytes.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
		multipart.Add(bytes, "file", "malicious.exe");

		var response = await client.PostAsync("/api/ProductImport/excel", multipart);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task ContactSubmit_RejectsDisallowedAttachmentType()
	{
		var client = CreateAuthenticatedClient("ops-user", "manager");
		using var multipart = new MultipartFormDataContent
		{
			{ new StringContent("Ops User"), "name" },
			{ new StringContent("ops@example.com"), "email" },
			{ new StringContent("Operations"), "jobRole" },
			{ new StringContent("Need assistance with portal hardening."), "description" }
		};

		var attachment = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
		attachment.Headers.ContentType = MediaTypeHeaderValue.Parse("image/svg+xml");
		multipart.Add(attachment, "attachment", "payload.svg");

		var response = await client.PostAsync("/api/Contact", multipart);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	private HttpClient CreateAuthenticatedClient(string userId, string role)
	{
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
		client.DefaultRequestHeaders.Add("X-Test-Role", role);
		client.DefaultRequestHeaders.Add("X-Test-CustomerId", "100");
		return client;
	}

}
