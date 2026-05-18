using AIPBackend.Models.DTOs;

namespace AIPBackend.Services;

public interface IAuthCookieService
{
	void SetAuthCookies(HttpResponse response, string accessToken, string refreshToken, DateTime accessTokenExpiresAtUtc);

	void ClearAuthCookies(HttpResponse response);

	string? GetRefreshTokenFromRequest(HttpRequest request);

	string? GetAccessTokenFromRequest(HttpRequest request);

	void ApplyTokenVisibility(LoginResponseDto response);

	void ApplyTokenVisibility(RefreshTokenResponseDto response);

	SameSiteMode GetSameSiteMode();
}
