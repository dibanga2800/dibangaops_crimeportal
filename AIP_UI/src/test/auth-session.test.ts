import { describe, it, expect, beforeEach } from 'vitest'
import { sessionStore } from '@/state/sessionStore'
import {
	applyLoginPayload,
	persistAuthMetadataFromApiEnvelope,
} from '@/utils/authSession'
import { COOKIE_REQUIRED_MESSAGE, isUnauthorizedStatus } from '@/utils/authCookieHelp'

describe('authSession', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStorage.clear()
		sessionStore.clearAll()
	})

	it('persistAuthMetadataFromApiEnvelope stores csrfToken from nested Data payload', () => {
		persistAuthMetadataFromApiEnvelope({
			success: true,
			data: {
				csrfToken: 'csrf-from-api',
				expiresAt: '2026-12-31T00:00:00.000Z',
			},
		})

		expect(sessionStore.getCsrfToken()).toBe('csrf-from-api')
		expect(sessionStore.getTokenExpiresAt()).toBe('2026-12-31T00:00:00.000Z')
	})

	it('applyLoginPayload does not clear an existing CSRF token when response omits it', () => {
		sessionStore.setCsrfToken('existing-csrf')

		const user = applyLoginPayload({
			user: { id: '1', role: 'store', username: 'store1' },
			expiresAt: '2026-12-31T00:00:00.000Z',
		})

		expect(sessionStore.getCsrfToken()).toBe('existing-csrf')
		expect(user.role).toBe('store')
	})

	it('applyLoginPayload updates CSRF when response includes csrfToken', () => {
		sessionStore.setCsrfToken('old-csrf')

		applyLoginPayload({
			user: { id: '1', role: 'manager', username: 'mgr' },
			csrfToken: 'new-csrf',
		})

		expect(sessionStore.getCsrfToken()).toBe('new-csrf')
	})

	it('cookie help treats 401 and 403 as unauthorized probe statuses', () => {
		expect(isUnauthorizedStatus(401)).toBe(true)
		expect(isUnauthorizedStatus(403)).toBe(true)
		expect(isUnauthorizedStatus(500)).toBe(false)
	})

	it('persistAuthMetadataFromApiEnvelope stores bearer tokens', () => {
		persistAuthMetadataFromApiEnvelope({
			data: {
				accessToken: 'access-abc',
				refreshToken: 'refresh-xyz',
				csrfToken: 'csrf-123',
			},
		})

		expect(sessionStore.getAccessToken()).toBe('access-abc')
		expect(sessionStore.getRefreshToken()).toBe('refresh-xyz')
		expect(sessionStore.getCsrfToken()).toBe('csrf-123')
	})

	it('applyLoginPayload stores access token for bearer auth fallback', () => {
		applyLoginPayload({
			user: { id: '1', role: 'store', username: 'store1' },
			accessToken: 'jwt-token',
			refreshToken: 'refresh-token',
		})

		expect(sessionStore.getAccessToken()).toBe('jwt-token')
		expect(sessionStore.usesBearerAuth()).toBe(true)
	})
})
