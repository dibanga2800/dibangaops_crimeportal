using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AIPBackend.Models;
using AIPBackend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AIPBackend.Tests;

public class JwtServiceRolloverTests
{
	[Fact]
	public void GetPrincipalFromExpiredToken_AcceptsTokenSignedWithPreviousKey()
	{
		var currentKey = "CurrentSigningKeyThatIsAtLeastThirtyTwoCharsLong_2026";
		var previousKey = "PreviousSigningKeyThatIsAtLeastThirtyTwoChars_2025";
		var service = CreateJwtService(currentKey, previousKey);

		var expiredToken = CreateExpiredToken(
			signingKey: previousKey,
			userId: "user-123",
			issuer: "AIPBackend",
			audience: "AIPFrontend");

		var principal = service.GetPrincipalFromExpiredToken(expiredToken);

		Assert.Equal("user-123", principal.FindFirstValue(ClaimTypes.NameIdentifier));
	}

	[Fact]
	public void GetPrincipalFromExpiredToken_RejectsTokenWithUnknownKey()
	{
		var service = CreateJwtService(
			currentKey: "CurrentSigningKeyThatIsAtLeastThirtyTwoCharsLong_2026",
			previousKey: "PreviousSigningKeyThatIsAtLeastThirtyTwoChars_2025");

		var expiredToken = CreateExpiredToken(
			signingKey: "UnknownSigningKeyThatShouldFailValidation_2024",
			userId: "user-123",
			issuer: "AIPBackend",
			audience: "AIPFrontend");

		Assert.ThrowsAny<Exception>(() => service.GetPrincipalFromExpiredToken(expiredToken));
	}

	private static JwtService CreateJwtService(string currentKey, string previousKey)
	{
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Jwt:Key"] = currentKey,
				["Jwt:PreviousKeys:0"] = previousKey,
				["Jwt:Issuer"] = "AIPBackend",
				["Jwt:Audience"] = "AIPFrontend",
				["Jwt:AccessTokenExpirationMinutes"] = "60",
				["Jwt:RefreshTokenExpirationDays"] = "7"
			})
			.Build();

		var userStore = new Mock<IUserStore<ApplicationUser>>();
		var userManager = new UserManager<ApplicationUser>(
			userStore.Object,
			null!,
			null!,
			null!,
			null!,
			null!,
			null!,
			null!,
			null!);

		return new JwtService(
			config,
			userManager,
			new Mock<ILogger<JwtService>>().Object);
	}

	private static string CreateExpiredToken(string signingKey, string userId, string issuer, string audience)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: audience,
			claims: new[]
			{
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Role, "administrator")
			},
			expires: DateTime.UtcNow.AddMinutes(-5),
			signingCredentials: credentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
