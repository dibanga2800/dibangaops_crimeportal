import { describe, it, expect, beforeEach } from 'vitest'
import { sessionStore } from '@/state/sessionStore'
import {
	applyLoginPayload,
	persistAuthMetadataFromApiEnvelope,
} from '@/utils/authSession'

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
})
