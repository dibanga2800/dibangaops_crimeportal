using System.Security.Cryptography;
using AIPBackend.Models.DTOs;
using AIPBackend.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services;

public class AuthCookieService : IAuthCookieService
{
	private readonly AuthCookieOptions _options;
	private readonly IConfiguration _configuration;

	public AuthCookieService(IOptions<AuthCookieOptions> options, IConfiguration configuration)
	{
		_options = options.Value;
		_configuration = configuration;
	}

	public string SetAuthCookies(HttpResponse response, string accessToken, string refreshToken, DateTime accessTokenExpiresAtUtc)
	{
		var sameSite = GetSameSiteMode();
		var secure = _options.Secure;
		var domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain.Trim();

		response.Cookies.Append(
			_options.AccessTokenCookieName,
			accessToken,
			BuildCookieOptions(
				httpOnly: true,
				expires: accessTokenExpiresAtUtc,
				path: "/",
				sameSite,
				secure,
				domain));

		var refreshDays = int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var configuredDays)
			? configuredDays
			: 7;

		response.Cookies.Append(
			_options.RefreshTokenCookieName,
			refreshToken,
			BuildCookieOptions(
				httpOnly: true,
				expires: DateTimeOffset.UtcNow.AddDays(refreshDays),
				path: _options.RefreshTokenPath,
				sameSite,
				secure,
				domain));

		var csrfToken = GenerateCsrfToken();
		response.Cookies.Append(
			_options.CsrfCookieName,
			csrfToken,
			BuildCookieOptions(
				httpOnly: false,
				expires: accessTokenExpiresAtUtc,
				path: "/",
				sameSite,
				secure,
				domain));

		return csrfToken;
	}

	public void ClearAuthCookies(HttpResponse response)
	{
		var sameSite = GetSameSiteMode();
		var secure = _options.Secure;
		var domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain.Trim();

		void Delete(string name, string path)
		{
			response.Cookies.Delete(name, BuildCookieOptions(
				httpOnly: true,
				expires: DateTimeOffset.UnixEpoch,
				path,
				sameSite,
				secure,
				domain));
		}

		Delete(_options.AccessTokenCookieName, "/");
		Delete(_options.RefreshTokenCookieName, _options.RefreshTokenPath);
		Delete(_options.CsrfCookieName, "/");
	}

	public string? GetRefreshTokenFromRequest(HttpRequest request) =>
		request.Cookies.TryGetValue(_options.RefreshTokenCookieName, out var cookieValue) &&
		!string.IsNullOrWhiteSpace(cookieValue)
			? cookieValue
			: null;

	public string? GetAccessTokenFromRequest(HttpRequest request) =>
		request.Cookies.TryGetValue(_options.AccessTokenCookieName, out var cookieValue) &&
		!string.IsNullOrWhiteSpace(cookieValue)
			? cookieValue
			: null;

	public void ApplyTokenVisibility(LoginResponseDto response)
	{
		if (_options.ExposeTokensInResponse)
		{
			return;
		}

		response.AccessToken = string.Empty;
		response.RefreshToken = string.Empty;
	}

	public void ApplyTokenVisibility(RefreshTokenResponseDto response)
	{
		if (_options.ExposeTokensInResponse)
		{
			return;
		}

		response.AccessToken = string.Empty;
		response.RefreshToken = string.Empty;
	}

	public SameSiteMode GetSameSiteMode() =>
		_options.SameSite.Trim().ToLowerInvariant() switch
		{
			"none" => SameSiteMode.None,
			"strict" => SameSiteMode.Strict,
			_ => SameSiteMode.Lax,
		};

	private static string GenerateCsrfToken()
	{
		var bytes = new byte[32];
		RandomNumberGenerator.Fill(bytes);
		return Convert.ToBase64String(bytes);
	}

	private CookieOptions BuildCookieOptions(
		bool httpOnly,
		DateTimeOffset expires,
		string path,
		SameSiteMode sameSite,
		bool secure,
		string? domain)
	{
		var options = new CookieOptions
		{
			HttpOnly = httpOnly,
			Secure = secure,
			SameSite = sameSite,
			Path = path,
			Expires = expires,
			IsEssential = true,
		};

		if (!string.IsNullOrWhiteSpace(domain))
		{
			options.Domain = domain;
		}

		return options;
	}
}
