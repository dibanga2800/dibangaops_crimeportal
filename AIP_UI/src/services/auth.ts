import { sessionStore } from '@/state/sessionStore'
import { User } from '@/types/user'

export const logout = (): void => {
	try {
		sessionStore.clearAll()
		window.dispatchEvent(new Event('session-cleared'))
	} catch (error) {
		console.error('Logout error:', error)
	}
}

export const getToken = (): string | null => {
	return sessionStore.getToken()
}

export const getUser = (): User | null => {
	return sessionStore.getUser()
}

export const isAuthenticated = (): boolean => {
	return Boolean(sessionStore.getToken() && sessionStore.getUser())
}

export const getAuthHeaders = (): HeadersInit => {
	const token = sessionStore.getToken()
	return token
		? { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
		: { 'Content-Type': 'application/json' }
}