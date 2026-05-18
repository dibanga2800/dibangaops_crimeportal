import { User } from '@/types/user'
import { sessionStore } from '@/state/sessionStore'

export const applyLoginPayload = (loginData: Record<string, unknown>): User => {
	const user = (loginData.User ?? loginData.user) as User | undefined
	if (!user) {
		throw new Error('Invalid response from server: missing user data')
	}

	const expiresAt =
		(loginData.ExpiresAt as string | undefined) ??
		(loginData.expiresAt as string | undefined) ??
		null

	const csrfToken =
		(loginData.CsrfToken as string | undefined) ??
		(loginData.csrfToken as string | undefined) ??
		null

	sessionStore.setTokenExpiresAt(expiresAt)
	sessionStore.setCsrfToken(csrfToken)
	sessionStore.setUser(user)

	const normalizedUser = sessionStore.getUser()
	if (!normalizedUser) {
		throw new Error('Failed to persist user session')
	}

	return normalizedUser
}
