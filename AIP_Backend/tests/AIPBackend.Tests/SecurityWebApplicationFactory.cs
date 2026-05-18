using AIPBackend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AIPBackend.Tests;

public class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
	private readonly string _databaseName = $"integration-tests-{Guid.NewGuid():N}";
	private readonly InMemoryDatabaseRoot _databaseRoot = new();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureAppConfiguration((_, configBuilder) =>
		{
			configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Security:RunMigrationsOnStartup"] = "false",
				["Security:RunPageAccessInitializationOnStartup"] = "false",
				["Security:EnableRateLimiting"] = "false"
			});
		});

		builder.ConfigureServices(services =>
		{
			services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseInMemoryDatabase(_databaseName, _databaseRoot));

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
}
