using AIPBackend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AIPBackend.Tests;

/// <summary>
/// Runs the API against the local COOP SQL database with test authentication.
/// </summary>
public class CoopWebApplicationFactory : WebApplicationFactory<Program>
{
	private const string ConnectionString =
		"Server=localhost\\SQLEXPRESS;Database=COOP;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;";

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Development");

		builder.ConfigureAppConfiguration((_, configBuilder) =>
		{
			configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:DefaultConnection"] = ConnectionString,
				["ConnectionStrings:DefaultDbConnection"] = ConnectionString,
				["Security:RunMigrationsOnStartup"] = "false",
				["Security:RunPageAccessInitializationOnStartup"] = "false",
				["Security:EnableRateLimiting"] = "false",
			});
		});

		builder.ConfigureServices(services =>
		{
			services.RemoveDbContext<ApplicationDbContext>();

			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(ConnectionString));

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
				options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
				options.DefaultScheme = TestAuthHandler.SchemeName;
			}).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
				TestAuthHandler.SchemeName,
				_ => { });
		});
	}

	public HttpClient CreateAuthenticatedClient(string userId, string role)
	{
		var client = CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
		client.DefaultRequestHeaders.Add("X-Test-Role", role);
		return client;
	}
}
