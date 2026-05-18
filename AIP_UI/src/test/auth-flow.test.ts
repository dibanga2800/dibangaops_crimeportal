import { describe, it, expect, beforeEach, vi } from 'vitest'
import { sessionStore } from '@/state/sessionStore'

describe('sessionStore (cookie auth)', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
	})

	it('does not persist JWTs in localStorage', () => {
		sessionStore.setUser({ id: '1', role: 'administrator' } as any)
		expect(localStorage.getItem('authToken')).toBeNull()
		expect(localStorage.getItem('refreshToken')).toBeNull()
	})

	it('clearAll removes user and expiry metadata', () => {
		sessionStore.setUser({ id: '1', role: 'administrator' } as any)
		sessionStore.setTokenExpiresAt(new Date().toISOString())

		sessionStore.clearAll()

		expect(sessionStore.getUser()).toBeNull()
		expect(sessionStore.getTokenExpiresAt()).toBeNull()
	})

	it('hasSession reflects cached user', () => {
		expect(sessionStore.hasSession()).toBe(false)
		sessionStore.setUser({ id: '1', role: 'administrator' } as any)
		expect(sessionStore.hasSession()).toBe(true)
	})
})

describe('services/auth', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
		vi.restoreAllMocks()
	})

	it('isAuthenticated returns true when user is cached', async () => {
		const { isAuthenticated } = await import('@/services/auth')
		expect(isAuthenticated()).toBe(false)

		sessionStore.setUser({ id: '1', role: 'administrator' } as any)
		expect(isAuthenticated()).toBe(true)
	})

	it('getAuthFetchInit includes credentials and CSRF when cookie is present', async () => {
		const { getAuthFetchInit } = await import('@/services/auth')
		document.cookie = 'aip_csrf=test-csrf-value'

		const init = getAuthFetchInit({ method: 'POST' })
		expect(init.credentials).toBe('include')
		expect((init.headers as Record<string, string>)['X-CSRF-TOKEN']).toBe('test-csrf-value')
	})
})

describe('api 401 interceptor', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
		delete (window as any).location
		;(window as any).location = { href: '', pathname: '/dashboard' }
	})

	it('clears session and redirects on 401 from /Auth/me when user was cached', async () => {
		const { api } = await import('@/config/api')
		sessionStore.setUser({ id: '1', role: 'administrator' } as any)

		const interceptor = (api.interceptors.response as any).handlers?.[0]
		if (interceptor?.rejected) {
			const mockError = {
				response: { status: 401, statusText: 'Unauthorized', data: {} },
				config: { url: '/Auth/me', method: 'get', headers: {} },
			}
			try {
				await interceptor.rejected(mockError)
			} catch {
				// expected
			}
		}

		expect(sessionStore.getUser()).toBeNull()
		expect((window as any).location.href).toBe('/login')
	})
})
