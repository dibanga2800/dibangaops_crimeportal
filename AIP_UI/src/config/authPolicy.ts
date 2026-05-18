/**
 * Bearer tokens in sessionStorage are disabled in production builds.
 * Pentest / production posture: HttpOnly cookies only (ExposeTokensInResponse=false on API).
 * Enable locally via DEV or VITE_ALLOW_BEARER_AUTH=true when the API exposes tokens (Development only).
 */
export const allowsBearerAuthFallback =
	import.meta.env.DEV === true || import.meta.env.VITE_ALLOW_BEARER_AUTH === 'true'
