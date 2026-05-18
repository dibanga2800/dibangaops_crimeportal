using AIPBackend.Models.DTOs;

namespace AIPBackend.Services;

public interface IAuthCookieService
{
	/// <returns>CSRF token (also set as a non-HttpOnly cookie for same-site clients).</returns>
	string SetAuthCookies(HttpResponse response, string accessToken, string refreshToken, DateTime accessTokenExpiresAtUtc);

	void ClearAuthCookies(HttpResponse response);

	string? GetRefreshTokenFromRequest(HttpRequest request);

	string? GetAccessTokenFromRequest(HttpRequest request);

	void ApplyTokenVisibility(LoginResponseDto response);

	void ApplyTokenVisibility(RefreshTokenResponseDto response);

	SameSiteMode GetSameSiteMode();
}
