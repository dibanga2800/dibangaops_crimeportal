const CSRF_COOKIE_NAME = 'aip_csrf'

export const getCsrfToken = (): string | null => {
	if (typeof document === 'undefined') {
		return null
	}

	const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${CSRF_COOKIE_NAME}=([^;]*)`))
	return match?.[1] ? decodeURIComponent(match[1]) : null
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
