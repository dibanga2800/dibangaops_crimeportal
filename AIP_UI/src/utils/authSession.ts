import { User } from '@/types/user'
import { sessionStore } from '@/state/sessionStore'

const readCsrfFromPayload = (payload: Record<string, unknown>): string | null => {
	const token =
		(payload.CsrfToken as string | undefined) ??
		(payload.csrfToken as string | undefined)
	if (typeof token === 'string' && token.length > 0) {
		return token
	}
	return null
}

const readExpiresAtFromPayload = (payload: Record<string, unknown>): string | null => {
	const expiresAt =
		(payload.ExpiresAt as string | undefined) ??
		(payload.expiresAt as string | undefined)
	return expiresAt ?? null
}

/** Persist CSRF + expiry from ApiResponseDto envelope or bare login payload. */
export const persistAuthMetadataFromApiEnvelope = (apiBody: unknown): void => {
	if (!apiBody || typeof apiBody !== 'object') {
		return
	}

	const envelope = apiBody as Record<string, unknown>
	const inner = (envelope.Data ?? envelope.data) as Record<string, unknown> | undefined
	const payloads = inner ? [inner, envelope] : [envelope]

	for (const payload of payloads) {
		const csrfToken = readCsrfFromPayload(payload)
		if (csrfToken) {
			sessionStore.setCsrfToken(csrfToken)
		}

		const expiresAt = readExpiresAtFromPayload(payload)
		if (expiresAt) {
			sessionStore.setTokenExpiresAt(expiresAt)
		}
	}
}

export const applyLoginPayload = (loginData: Record<string, unknown>): User => {
	const user = (loginData.User ?? loginData.user) as User | undefined
	if (!user) {
		throw new Error('Invalid response from server: missing user data')
	}

	const expiresAt = readExpiresAtFromPayload(loginData)
	const csrfToken = readCsrfFromPayload(loginData)

	if (expiresAt) {
		sessionStore.setTokenExpiresAt(expiresAt)
	}

	// Only overwrite CSRF when the API returned one (cross-origin SPAs cannot read API-domain cookies).
	if (csrfToken) {
		sessionStore.setCsrfToken(csrfToken)
	}

	sessionStore.setUser(user)

	const normalizedUser = sessionStore.getUser()
	if (!normalizedUser) {
		throw new Error('Failed to persist user session')
	}

	return normalizedUser
}
