import { sessionStore } from '@/state/sessionStore'

const CSRF_COOKIE_NAME = 'aip_csrf'

const readCsrfFromCookie = (): string | null => {
	if (typeof document === 'undefined') {
		return null
	}

	const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${CSRF_COOKIE_NAME}=([^;]*)`))
	return match?.[1] ? decodeURIComponent(match[1]) : null
}

/** Prefer first-party cookie (same-origin); fall back to sessionStorage from login JSON. */
export const getCsrfToken = (): string | null => {
	const fromCookie = readCsrfFromCookie()
	if (fromCookie) {
		return fromCookie
	}

	return sessionStore.getCsrfToken()
}

export const applyCsrfHeader = (headers: Record<string, string>): Record<string, string> => {
	const csrfToken = getCsrfToken()
	if (!csrfToken) {
		return headers
	}

	return {
		...headers,
		'X-CSRF-TOKEN': csrfToken,
	}
}
