namespace AIPBackend.Options;

public class AuthCookieOptions
{
	public const string SectionName = "Auth:Cookies";

	public string AccessTokenCookieName { get; set; } = "aip_access";

	public string RefreshTokenCookieName { get; set; } = "aip_refresh";

	public string CsrfCookieName { get; set; } = "aip_csrf";

	/// <summary>
	/// Cookie Domain attribute (e.g. .dibangops.com). Leave null for host-only cookies.
	/// </summary>
	public string? Domain { get; set; }

	/// <summary>
	/// Lax, Strict, or None. Use Lax when API and SPA share a registrable domain (api.example.com + www.example.com).
	/// Use None for cross-site API hosts (requires Secure).
	/// </summary>
	public string SameSite { get; set; } = "Lax";

	public bool Secure { get; set; } = true;

	/// <summary>
	/// When false, login/refresh responses omit token values from JSON (cookies carry tokens).
	/// </summary>
	public bool ExposeTokensInResponse { get; set; } = false;

	public string RefreshTokenPath { get; set; } = "/api/Auth";
}
