import { sessionStore } from '@/state/sessionStore'

export const applyBearerHeader = (headers: Record<string, string>): Record<string, string> => {
	const accessToken = sessionStore.getAccessToken()
	if (!accessToken) {
		return headers
	}

	return {
		...headers,
		Authorization: `Bearer ${accessToken}`,
	}
}
