namespace AIPBackend.Tests;

/// <summary>
/// Shared configuration for WebApplicationFactory integration tests (CI has no appsettings.Local.json).
/// </summary>
internal static class TestAppSettings
{
	public const string JwtSigningKey = "TestSigningKeyThatIsAtLeastThirtyTwoCharactersLong!";

	public static IReadOnlyDictionary<string, string?> Values { get; } = new Dictionary<string, string?>
	{
		["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=IntegrationTests;Trusted_Connection=True;",
		["ConnectionStrings:DefaultDbConnection"] = "Server=(localdb)\\mssqllocaldb;Database=IntegrationTests;Trusted_Connection=True;",
		["ConnectionStrings:StorageAccount"] = "UseDevelopmentStorage=true",
		["Jwt:Key"] = JwtSigningKey,
		["Jwt:Issuer"] = "AIPBackend",
		["Jwt:Audience"] = "AIPFrontend",
		["Jwt:AccessTokenExpirationMinutes"] = "60",
		["Jwt:RefreshTokenExpirationDays"] = "7",
		["IncidentImageStorage:Mode"] = "database",
		["Security:RunMigrationsOnStartup"] = "false",
		["Security:RunPageAccessInitializationOnStartup"] = "false",
		["Security:EnableRateLimiting"] = "false",
		["Security:EnableSwaggerInProduction"] = "false",
		["Security:EnableSwaggerUiInProduction"] = "false",
	};
}
