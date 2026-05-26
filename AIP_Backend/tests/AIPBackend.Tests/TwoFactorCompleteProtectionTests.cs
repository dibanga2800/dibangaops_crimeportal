using System.Net;
using System.Net.Http.Json;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AIPBackend.Tests;

public class TwoFactorCompleteProtectionTests : IClassFixture<SecurityWebApplicationFactory>
{
	private readonly SecurityWebApplicationFactory _factory;

	public TwoFactorCompleteProtectionTests(SecurityWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task CompleteTwoFactorLogin_with_valid_hashed_code_succeeds()
	{
		const string email = "twofa-success@test.local";
		const string code = "654321";

		await using var scope = _factory.Services.CreateAsyncScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var hasher = scope.ServiceProvider.GetRequiredService<IPendingTwoFactorCodeHasher>();

		var user = await CreateActiveUserAsync(userManager, email);
		user.PendingTwoFactorCode = hasher.Hash(code, user.Id);
		user.PendingTwoFactorExpiryUtc = DateTime.UtcNow.AddMinutes(10);
		await userManager.UpdateAsync(user);

		var client = _factory.CreateClient();
		var response = await PostCompleteAsync(client, email, code);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task CompleteTwoFactorLogin_locks_out_after_repeated_invalid_codes()
	{
		const string email = "twofa-lockout@test.local";
		const string code = "111222";

		await using var scope = _factory.Services.CreateAsyncScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var hasher = scope.ServiceProvider.GetRequiredService<IPendingTwoFactorCodeHasher>();

		var user = await CreateActiveUserAsync(userManager, email);
		user.PendingTwoFactorCode = hasher.Hash(code, user.Id);
		user.PendingTwoFactorExpiryUtc = DateTime.UtcNow.AddMinutes(10);
		await userManager.UpdateAsync(user);

		var client = _factory.CreateClient();

		for (var attempt = 0; attempt < 5; attempt++)
		{
			var failed = await PostCompleteAsync(client, email, "000000");
			Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
		}

		var blocked = await PostCompleteAsync(client, email, code);
		Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);

		var body = await blocked.Content.ReadFromJsonAsync<ApiResponseDto<LoginResponseDto>>();
		Assert.NotNull(body);
		Assert.Contains("locked", body.Message, StringComparison.OrdinalIgnoreCase);
	}

	private static async Task<ApplicationUser> CreateActiveUserAsync(
		UserManager<ApplicationUser> userManager,
		string email)
	{
		var existing = await userManager.FindByEmailAsync(email);
		if (existing != null)
		{
			await userManager.DeleteAsync(existing);
		}

		var user = new ApplicationUser
		{
			UserName = email,
			Email = email,
			EmailConfirmed = true,
			FirstName = "Two",
			LastName = "Factor",
			Role = "manager",
			PageAccessRole = "manager",
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CreatedBy = "test",
		};

		var result = await userManager.CreateAsync(user, "TestPassword1!");
		Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
		return user;
	}

	private static async Task<HttpResponseMessage> PostCompleteAsync(
		HttpClient client,
		string email,
		string code)
	{
		return await client.PostAsJsonAsync("/api/auth/2fa/complete", new CompleteTwoFactorLoginRequestDto
		{
			Email = email,
			Code = code,
		});
	}
}
