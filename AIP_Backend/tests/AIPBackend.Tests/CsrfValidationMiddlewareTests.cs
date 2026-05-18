using AIPBackend.Middleware;
using AIPBackend.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AIPBackend.Tests;

public class CsrfValidationMiddlewareTests
{
	private const string SigningKey = "TestSigningKeyForCsrfMiddlewareTests_Min32Chars!";

	[Fact]
	public async Task InvokeAsync_RejectsCookieAuth_WhenAuthorizationHeaderIsInvalidBearer()
	{
		var nextCalled = false;
		var context = CreateContext(
			HttpMethods.Post,
			"/api/incidents",
			accessCookie: "access-token",
			csrfCookie: "csrf-token",
			csrfHeader: null,
			authorization: "Bearer not-a-valid-jwt");

		await CreateMiddleware(() => nextCalled = true).InvokeAsync(context);

		Assert.False(nextCalled);
		Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
	}

	[Fact]
	public async Task InvokeAsync_AllowsCookieAuth_WhenCsrfHeaderMatches()
	{
		var nextCalled = false;
		var context = CreateContext(
			HttpMethods.Post,
			"/api/incidents",
			accessCookie: "access-token",
			csrfCookie: "csrf-token",
			csrfHeader: "csrf-token",
			authorization: null);

		await CreateMiddleware(() => nextCalled = true).InvokeAsync(context);

		Assert.True(nextCalled);
	}

	[Fact]
	public async Task InvokeAsync_SkipsCsrf_WhenValidBearerTokenPresent()
	{
		var nextCalled = false;
		var validToken = CreateValidJwt();
		var context = CreateContext(
			HttpMethods.Post,
			"/api/incidents",
			accessCookie: "access-token",
			csrfCookie: "csrf-token",
			csrfHeader: null,
			authorization: $"Bearer {validToken}");

		await CreateMiddleware(() => nextCalled = true).InvokeAsync(context);

		Assert.True(nextCalled);
	}

	private CsrfValidationMiddleware CreateMiddleware(Action onNext)
	{
		var jwtOptions = new Mock<IOptionsMonitor<JwtBearerOptions>>();
		jwtOptions
			.Setup(m => m.Get(JwtBearerDefaults.AuthenticationScheme))
			.Returns(new JwtBearerOptions
			{
				TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = "AIPBackend",
					ValidAudience = "AIPFrontend",
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
					ClockSkew = TimeSpan.Zero
				}
			});

		var cookieOptions = Microsoft.Extensions.Options.Options.Create(new AuthCookieOptions
		{
			AccessTokenCookieName = "aip_access",
			CsrfCookieName = "aip_csrf"
		});

		return new CsrfValidationMiddleware(_ =>
		{
			onNext();
			return Task.CompletedTask;
		}, cookieOptions, jwtOptions.Object);
	}

	private static HttpContext CreateContext(
		string method,
		string path,
		string? accessCookie,
		string? csrfCookie,
		string? csrfHeader,
		string? authorization)
	{
		var context = new DefaultHttpContext();
		context.Request.Method = method;
		context.Request.Path = path;
		context.Response.Body = new MemoryStream();

		if (!string.IsNullOrEmpty(accessCookie))
		{
			var cookieDict = new Dictionary<string, string> { ["aip_access"] = accessCookie };
			if (!string.IsNullOrEmpty(csrfCookie))
			{
				cookieDict["aip_csrf"] = csrfCookie;
			}

			context.Request.Cookies = new MockRequestCookieCollection(cookieDict);
		}

		if (!string.IsNullOrEmpty(csrfHeader))
		{
			context.Request.Headers["X-CSRF-TOKEN"] = csrfHeader;
		}

		if (!string.IsNullOrEmpty(authorization))
		{
			context.Request.Headers.Authorization = authorization;
		}

		return context;
	}

	private static string CreateValidJwt()
	{
		var credentials = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
			SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: "AIPBackend",
			audience: "AIPFrontend",
			claims: new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
			expires: DateTime.UtcNow.AddMinutes(5),
			signingCredentials: credentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private sealed class MockRequestCookieCollection(Dictionary<string, string> cookies) : IRequestCookieCollection
	{
		public string? this[string key] => cookies.TryGetValue(key, out var value) ? value : null;
		public int Count => cookies.Count;
		public ICollection<string> Keys => cookies.Keys;
		public bool ContainsKey(string key) => cookies.ContainsKey(key);
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => cookies.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		bool IRequestCookieCollection.TryGetValue(string key, out string? value)
		{
			if (cookies.TryGetValue(key, out var cookieValue))
			{
				value = cookieValue;
				return true;
			}

			value = null;
			return false;
		}
	}
}
