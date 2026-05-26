using AIPBackend.Services.Auth;
using Microsoft.Extensions.Configuration;

namespace AIPBackend.Tests;

public class PendingTwoFactorCodeHasherTests
{
	private static PendingTwoFactorCodeHasher CreateHasher(string? jwtKey = null)
	{
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Jwt:Key"] = jwtKey ?? TestAppSettings.JwtSigningKey,
			})
			.Build();

		return new PendingTwoFactorCodeHasher(config);
	}

	[Fact]
	public void Hash_and_Verify_round_trip_succeeds()
	{
		var hasher = CreateHasher();
		const string userId = "user-abc";
		const string code = "482917";

		var stored = hasher.Hash(code, userId);

		Assert.NotEqual(code, stored);
		Assert.True(hasher.Verify(code, userId, stored));
		Assert.False(hasher.Verify("000000", userId, stored));
		Assert.False(hasher.Verify(code, "other-user", stored));
	}

	[Fact]
	public void Verify_accepts_legacy_plaintext_code()
	{
		var hasher = CreateHasher();
		const string userId = "user-legacy";
		const string code = "123456";

		Assert.True(hasher.Verify(code, userId, code));
		Assert.False(hasher.Verify("654321", userId, code));
	}

	[Fact]
	public void Hash_uses_different_output_for_different_users()
	{
		var hasher = CreateHasher();
		const string code = "111222";

		var hashA = hasher.Hash(code, "user-a");
		var hashB = hasher.Hash(code, "user-b");

		Assert.NotEqual(hashA, hashB);
	}
}
