import { User } from '@/types/user'
import { allowsBearerAuthFallback } from '@/config/authPolicy'
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

const readAccessTokenFromPayload = (payload: Record<string, unknown>): string | null => {
	const token =
		(payload.AccessToken as string | undefined) ??
		(payload.accessToken as string | undefined)
	if (typeof token === 'string' && token.length > 0) {
		return token
	}
	return null
}

const readRefreshTokenFromPayload = (payload: Record<string, unknown>): string | null => {
	const token =
		(payload.RefreshToken as string | undefined) ??
		(payload.refreshToken as string | undefined)
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

const persistAuthFieldsFromPayload = (payload: Record<string, unknown>): void => {
	const csrfToken = readCsrfFromPayload(payload)
	if (csrfToken) {
		sessionStore.setCsrfToken(csrfToken)
	}

	if (allowsBearerAuthFallback) {
		const accessToken = readAccessTokenFromPayload(payload)
		if (accessToken) {
			sessionStore.setAccessToken(accessToken)
		}

		const refreshToken = readRefreshTokenFromPayload(payload)
		if (refreshToken) {
			sessionStore.setRefreshToken(refreshToken)
		}
	}

	const expiresAt = readExpiresAtFromPayload(payload)
	if (expiresAt) {
		sessionStore.setTokenExpiresAt(expiresAt)
	}
}

/** Persist CSRF, bearer tokens, and expiry from ApiResponseDto envelope or bare login payload. */
export const persistAuthMetadataFromApiEnvelope = (apiBody: unknown): void => {
	if (!apiBody || typeof apiBody !== 'object') {
		return
	}

	const envelope = apiBody as Record<string, unknown>
	const inner = (envelope.Data ?? envelope.data) as Record<string, unknown> | undefined
	const payloads = inner ? [inner, envelope] : [envelope]

	for (const payload of payloads) {
		persistAuthFieldsFromPayload(payload)
	}
}

export const applyLoginPayload = (loginData: Record<string, unknown>): User => {
	const user = (loginData.User ?? loginData.user) as User | undefined
	if (!user) {
		throw new Error('Invalid response from server: missing user data')
	}

	persistAuthFieldsFromPayload(loginData)
	sessionStore.setUser(user)

	const normalizedUser = sessionStore.getUser()
	if (!normalizedUser) {
		throw new Error('Failed to persist user session')
	}

	return normalizedUser
}
