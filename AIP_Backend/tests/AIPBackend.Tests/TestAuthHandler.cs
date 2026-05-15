using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public const string SchemeName = "TestAuth";

	public TestAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder)
		: base(options, logger, encoder)
	{
	}

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader) ||
			string.IsNullOrWhiteSpace(userIdHeader))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var userId = userIdHeader.ToString();
		var role = Request.Headers.TryGetValue("X-Test-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader)
			? roleHeader.ToString()
			: "manager";

		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, userId),
			new(ClaimTypes.Name, $"test-{userId}"),
			new(ClaimTypes.Role, role)
		};

		AddClaimIfPresent("X-Test-CustomerId", "CustomerId", claims);
		AddClaimIfPresent("X-Test-AssignedCustomerIds", "AssignedCustomerIds", claims);
		AddClaimIfPresent("X-Test-PrimarySiteId", "PrimarySiteId", claims);
		AddClaimIfPresent("X-Test-AssignedSiteIds", "AssignedSiteIds", claims);

		var identity = new ClaimsIdentity(claims, SchemeName);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, SchemeName);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}

	private void AddClaimIfPresent(string headerName, string claimType, ICollection<Claim> claims)
	{
		if (Request.Headers.TryGetValue(headerName, out var headerValue) &&
			!string.IsNullOrWhiteSpace(headerValue))
		{
			claims.Add(new Claim(claimType, headerValue.ToString()));
		}
	}
}
