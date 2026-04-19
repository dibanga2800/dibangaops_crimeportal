import { describe, it, expect, beforeEach, vi } from 'vitest'
import { sessionStore } from '@/state/sessionStore'

/**
 * Regression tests for the login / auth stabilisation work.
 *
 * These are *unit* tests that validate the token-storage layer and the
 * Axios 401 interceptor behaviour, which were the root causes of the
 * "login takes 3 attempts" bug.
 */

// ── sessionStore ──────────────────────────────────────────────────

describe('sessionStore', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
	})

	it('stores and retrieves a token via the canonical key', () => {
		sessionStore.setToken('jwt-abc')
		expect(sessionStore.getToken()).toBe('jwt-abc')
		expect(localStorage.getItem('authToken')).toBe('jwt-abc')
	})

	it('clearAll removes both token and user', () => {
		sessionStore.setToken('jwt-abc')
		sessionStore.setUser({ id: '1', role: 'administrator' } as any)

		sessionStore.clearAll()

		expect(sessionStore.getToken()).toBeNull()
		expect(sessionStore.getUser()).toBeNull()
	})

	it('getUser falls back to localStorage when in-memory cache is empty', () => {
		const user = { id: '1', role: 'administrator', username: 'test' }
		localStorage.setItem('user', JSON.stringify(user))

		// In-memory cache is empty after clearAll, so it should fall back
		const retrieved = sessionStore.getUser()
		expect(retrieved).toMatchObject({
			id: '1',
			role: 'administrator',
			username: 'test',
			pageAccessRole: 'administrator',
			twoFactorEnabled: false,
			emailNotificationsEnabled: true,
			loginAlertsEnabled: true,
		})
	})
})

// ── services/auth ─────────────────────────────────────────────────

describe('services/auth', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
	})

	it('getToken delegates to sessionStore', async () => {
		const { getToken } = await import('@/services/auth')
		sessionStore.setToken('tok-123')
		expect(getToken()).toBe('tok-123')
	})

	it('isAuthenticated returns true only when both token and user are present', async () => {
		const { isAuthenticated } = await import('@/services/auth')
		expect(isAuthenticated()).toBe(false)

		sessionStore.setToken('tok')
		expect(isAuthenticated()).toBe(false)

		sessionStore.setUser({ id: '1', role: 'administrator' } as any)
		expect(isAuthenticated()).toBe(true)
	})

	it('getAuthHeaders includes bearer token when authenticated', async () => {
		const { getAuthHeaders } = await import('@/services/auth')
		sessionStore.setToken('tok-456')
		const headers = getAuthHeaders()
		expect(headers).toEqual({
			'Authorization': 'Bearer tok-456',
			'Content-Type': 'application/json',
		})
	})

	it('getAuthHeaders omits Authorization when no token', async () => {
		const { getAuthHeaders } = await import('@/services/auth')
		const headers = getAuthHeaders()
		expect(headers).toEqual({ 'Content-Type': 'application/json' })
	})

	it('logout clears sessionStore', async () => {
		const { logout } = await import('@/services/auth')
		sessionStore.setToken('tok')
		sessionStore.setUser({ id: '1', role: 'administrator' } as any)

		logout()

		expect(sessionStore.getToken()).toBeNull()
		expect(sessionStore.getUser()).toBeNull()
	})
})

// ── Axios 401 interceptor behaviour ───────────────────────────────

describe('api 401 interceptor', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
		// Prevent actual navigation
		delete (window as any).location
		;(window as any).location = { href: '', pathname: '/dashboard' }
	})

	it('does NOT clear session or redirect on 401 from a data endpoint', async () => {
		const { api } = await import('@/config/api')
		sessionStore.setToken('valid-token')

		// Simulate a 401 from a data endpoint (e.g. /region)
		try {
			// The interceptor runs on error responses; we can test it by
			// inspecting sessionStore state after a rejection
			const interceptor = (api.interceptors.response as any).handlers?.[0]
			if (interceptor?.rejected) {
				const mockError = {
					response: { status: 401, statusText: 'Unauthorized', data: {} },
					config: { url: '/region', method: 'get', headers: {} },
				}
				try { await interceptor.rejected(mockError) } catch { /* expected */ }
			}
		} catch { /* expected */ }

		// Token should still be present — the 401 was NOT from an auth endpoint
		expect(sessionStore.getToken()).toBe('valid-token')
		expect((window as any).location.href).not.toContain('/login')
	})

	it('clears session and redirects on 401 from /Auth/me', async () => {
		const { api } = await import('@/config/api')
		sessionStore.setToken('expired-token')

		try {
			const interceptor = (api.interceptors.response as any).handlers?.[0]
			if (interceptor?.rejected) {
				const mockError = {
					response: { status: 401, statusText: 'Unauthorized', data: {} },
					config: { url: '/Auth/me', method: 'get', headers: {} },
				}
				try { await interceptor.rejected(mockError) } catch { /* expected */ }
			}
		} catch { /* expected */ }

		expect(sessionStore.getToken()).toBeNull()
		expect((window as any).location.href).toBe('/login')
	})
})
