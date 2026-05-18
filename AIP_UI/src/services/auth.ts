import { api } from '@/config/api'
import { sessionStore } from '@/state/sessionStore'
import { applyCsrfHeader } from '@/utils/csrf'
import { applyBearerHeader } from '@/utils/bearerAuth'
import { User } from '@/types/user'

export const logout = async (): Promise<void> => {
	try {
		await api.post('/Auth/logout', {})
	} catch (error) {
		console.warn('Server logout failed; clearing local session anyway.', error)
	} finally {
		sessionStore.clearAll()
		window.dispatchEvent(new Event('session-cleared'))
	}
}

export const getUser = (): User | null => sessionStore.getUser()

export const isAuthenticated = (): boolean => sessionStore.hasSession()

export const getAuthFetchInit = (init: RequestInit = {}): RequestInit => {
	const headers = applyCsrfHeader(
		applyBearerHeader({
			'Content-Type': 'application/json',
			...(init.headers as Record<string, string> | undefined),
		}),
	)

	return {
		...init,
		credentials: 'include',
		headers,
	}
}
