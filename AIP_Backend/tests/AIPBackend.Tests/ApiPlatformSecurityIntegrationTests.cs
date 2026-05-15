using System.Net;
using System.Text;
using System.Text.Json;

namespace AIPBackend.Tests;

public class ApiPlatformSecurityIntegrationTests : IClassFixture<SecurityWebApplicationFactory>
{
	private readonly SecurityWebApplicationFactory _factory;

	public ApiPlatformSecurityIntegrationTests(SecurityWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task CorsPreflight_AllowsConfiguredOrigin()
	{
		var client = _factory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Options, "/api/incidents");
		request.Headers.Add("Origin", "https://www.dibangops.com");
		request.Headers.Add("Access-Control-Request-Method", "GET");
		request.Headers.Add("Access-Control-Request-Headers", "Authorization");

		var response = await client.SendAsync(request);

		Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
		Assert.Contains("https://www.dibangops.com", values);
	}

	[Fact]
	public async Task CorsPreflight_RejectsUntrustedOrigin()
	{
		var client = _factory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Options, "/api/incidents");
		request.Headers.Add("Origin", "https://evil.example");
		request.Headers.Add("Access-Control-Request-Method", "GET");
		request.Headers.Add("Access-Control-Request-Headers", "Authorization");

		var response = await client.SendAsync(request);

		var hasCorsHeader = response.Headers.TryGetValues("Access-Control-Allow-Origin", out _);
		Assert.False(hasCorsHeader);
	}

	[Fact]
	public async Task RefreshTokenEndpoint_RejectsInvalidToken()
	{
		var client = _factory.CreateClient();
		var payload = JsonSerializer.Serialize(new { refreshToken = "not-base64" });
		using var content = new StringContent(payload, Encoding.UTF8, "application/json");

		var response = await client.PostAsync("/api/Auth/refresh", content);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
