using System.IdentityModel.Tokens.Jwt;
using AIPBackend.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AIPBackend.Middleware;

/// <summary>
/// Validates double-submit CSRF token for cookie-authenticated mutating requests.
/// </summary>
public class CsrfValidationMiddleware
{
	private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
	{
		HttpMethods.Get,
		HttpMethods.Head,
		HttpMethods.Options,
		HttpMethods.Trace,
	};

	private static readonly string[] ExemptPathPrefixes =
	[
		"/api/auth/login",
		"/api/auth/refresh",
		"/api/auth/2fa/complete",
		"/api/auth/forgot-password",
		"/api/auth/reset-password",
	];

	private readonly RequestDelegate _next;
	private readonly AuthCookieOptions _options;
	private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;

	public CsrfValidationMiddleware(
		RequestDelegate next,
		IOptions<AuthCookieOptions> options,
		IOptionsMonitor<JwtBearerOptions> jwtOptions)
	{
		_next = next;
		_options = options.Value;
		_jwtOptions = jwtOptions;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (SafeMethods.Contains(context.Request.Method))
		{
			await _next(context);
			return;
		}

		var path = context.Request.Path.Value ?? string.Empty;
		if (ExemptPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
		{
			await _next(context);
			return;
		}

		// Bearer-only clients (e.g. Swagger) skip CSRF only when a valid JWT is presented.
		if (TryGetBearerToken(context.Request.Headers.Authorization, out var bearerToken) &&
		    IsValidBearerToken(bearerToken))
		{
			await _next(context);
			return;
		}

		if (!context.Request.Cookies.TryGetValue(_options.AccessTokenCookieName, out var accessCookie) ||
		    string.IsNullOrWhiteSpace(accessCookie))
		{
			await _next(context);
			return;
		}

		if (!context.Request.Cookies.TryGetValue(_options.CsrfCookieName, out var csrfCookie) ||
		    string.IsNullOrWhiteSpace(csrfCookie))
		{
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			await context.Response.WriteAsJsonAsync(new { message = "CSRF cookie missing." });
			return;
		}

		if (!context.Request.Headers.TryGetValue("X-CSRF-TOKEN", out var csrfHeader) ||
		    string.IsNullOrWhiteSpace(csrfHeader) ||
		    !string.Equals(csrfHeader.ToString(), csrfCookie, StringComparison.Ordinal))
		{
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			await context.Response.WriteAsJsonAsync(new { message = "CSRF validation failed." });
			return;
		}

		await _next(context);
	}

	private bool IsValidBearerToken(string token)
	{
		var validationParameters = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
		if (validationParameters == null)
		{
			return false;
		}

		try
		{
			var handler = new JwtSecurityTokenHandler();
			handler.ValidateToken(token, validationParameters.Clone(), out _);
			return true;
		}
		catch (SecurityTokenException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static bool TryGetBearerToken(string? authorizationHeader, out string token)
	{
		token = string.Empty;
		if (string.IsNullOrWhiteSpace(authorizationHeader))
		{
			return false;
		}

		const string bearerPrefix = "Bearer ";
		if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		token = authorizationHeader[bearerPrefix.Length..].Trim();
		return !string.IsNullOrWhiteSpace(token);
	}
}
