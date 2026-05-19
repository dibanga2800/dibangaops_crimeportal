import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { useContext } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { sessionStore } from '@/state/sessionStore'
import { isCsrfForbidden } from '@/utils/apiErrors'
import { getCsrfToken, applyCsrfHeader } from '@/utils/csrf'
import ProtectedRoute from '@/components/ProtectedRoute'
import { AuthProvider, AuthContext } from '@/contexts/AuthContext'
import * as apiModule from '@/config/api'

const apiGetMock = vi.fn()
const tryRefreshMock = vi.fn()

function AuthProbe() {
	const { user, isLoading } = useContext(AuthContext)!
	return (
		<div>
			<span data-testid="loading">{String(isLoading)}</span>
			<span data-testid="user-id">{user?.id ?? 'none'}</span>
		</div>
	)
}

describe('isCsrfForbidden', () => {
	it('returns true for 403 with CSRF message', () => {
		expect(isCsrfForbidden(403, { message: 'CSRF validation failed.' })).toBe(true)
		expect(isCsrfForbidden(403, { Message: 'CSRF cookie missing.' })).toBe(true)
	})

	it('returns false for 403 without CSRF wording', () => {
		expect(isCsrfForbidden(403, { message: 'Forbidden' })).toBe(false)
	})

	it('returns false for 401 and non-object bodies', () => {
		expect(isCsrfForbidden(401, { message: 'CSRF validation failed.' })).toBe(false)
		expect(isCsrfForbidden(403, null)).toBe(false)
		expect(isCsrfForbidden(403, 'CSRF')).toBe(false)
	})
})

describe('same-origin CSRF (cookie-first)', () => {
	beforeEach(() => {
		sessionStorage.clear()
		sessionStore.clearAll()
		document.cookie = 'aip_csrf=; Max-Age=0; path=/'
	})

	it('falls back to sessionStorage when cookie is absent', () => {
		sessionStore.setCsrfToken('session-only-csrf')
		expect(getCsrfToken()).toBe('session-only-csrf')
	})

	it('applyCsrfHeader uses cookie token on same origin', () => {
		document.cookie = 'aip_csrf=cookie-csrf; path=/'
		sessionStore.setCsrfToken('session-csrf')

		const headers = applyCsrfHeader({})
		expect(headers['X-CSRF-TOKEN']).toBe('cookie-csrf')
	})

	it('omits CSRF header when no token is available', () => {
		expect(applyCsrfHeader({ Accept: 'application/json' })).toEqual({
			Accept: 'application/json',
		})
	})
})

describe('api axios client (same-origin)', () => {
	it('uses withCredentials for cookie sessions', async () => {
		const { api } = await import('@/config/api')
		expect(api.defaults.withCredentials).toBe(true)
	})

	it('resolves public auth paths without CSRF header on login', async () => {
		const { api } = await import('@/config/api')
		const adapter = vi.fn().mockResolvedValue({
			data: {},
			status: 200,
			statusText: 'OK',
			headers: {},
			config: { headers: {} },
		})
		api.defaults.adapter = adapter

		await api.post('/Auth/login', { email: 'a@b.com', password: 'x' })

		const sentHeaders = adapter.mock.calls[0][0].headers as Record<string, string>
		expect(sentHeaders['X-CSRF-TOKEN']).toBeUndefined()
	})

	it('attaches CSRF header on protected endpoints when token exists', async () => {
		const { api } = await import('@/config/api')
		sessionStore.setCsrfToken('mutating-csrf')
		const adapter = vi.fn().mockResolvedValue({
			data: {},
			status: 200,
			statusText: 'OK',
			headers: {},
			config: { headers: {} },
		})
		api.defaults.adapter = adapter

		await api.get('/incidents')

		const sentHeaders = adapter.mock.calls[0][0].headers as Record<string, string>
		expect(sentHeaders['X-CSRF-TOKEN']).toBe('mutating-csrf')
	})
})

describe('api 403 CSRF interceptor', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStorage.clear()
		sessionStore.clearAll()
		delete (window as unknown as { location?: { href: string; pathname: string } }).location
		;(window as unknown as { location: { href: string; pathname: string } }).location = {
			href: '',
			pathname: '/dashboard',
		}
	})

	const getResponseInterceptor = async () => {
		const { api } = await import('@/config/api')
		const handlers = (api.interceptors.response as unknown as { handlers: { rejected: (e: unknown) => Promise<unknown> }[] })
			.handlers
		return handlers[0]?.rejected
	}

	it('does not clear session or redirect on CSRF 403', async () => {
		sessionStore.setUser({ id: '1', role: 'administrator' } as never)
		const rejected = await getResponseInterceptor()
		expect(rejected).toBeDefined()

		const error = {
			response: { status: 403, data: { message: 'CSRF validation failed.' } },
			config: { url: '/incidents', method: 'post', headers: {}, _retry: true },
			message: 'Forbidden',
		}

		await expect(rejected!(error)).rejects.toBe(error)
		expect(sessionStore.getUser()).not.toBeNull()
		expect(window.location.href).not.toBe('/login')
	})

	it('still redirects on 401 after failed refresh when session exists', async () => {
		sessionStore.setUser({ id: '1', role: 'administrator' } as never)
		const rejected = await getResponseInterceptor()
		const error = {
			response: { status: 401, data: {} },
			config: { url: '/incidents', method: 'get', headers: {}, _retry: true },
			message: 'Unauthorized',
		}

		try {
			await rejected!(error)
		} catch {
			// expected
		}

		expect(sessionStore.getUser()).toBeNull()
		expect(window.location.href).toBe('/login')
	})
})

describe('AuthProvider bootstrap', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStorage.clear()
		sessionStore.clearAll()
		apiGetMock.mockReset()
		tryRefreshMock.mockReset()
		vi.spyOn(apiModule.api, 'get').mockImplementation(apiGetMock as typeof apiModule.api.get)
		vi.spyOn(apiModule, 'tryRefreshAccessToken').mockImplementation(tryRefreshMock)
	})

	afterEach(() => {
		vi.restoreAllMocks()
	})

	it('does not expose cached user until /Auth/me succeeds', async () => {
		sessionStore.setUser({ id: 'cached', role: 'administrator', username: 'admin' } as never)
		apiGetMock.mockResolvedValue({
			data: {
				success: true,
				data: { id: 'verified', role: 'administrator', username: 'admin' },
			},
		})

		render(
			<AuthProvider>
				<AuthProbe />
			</AuthProvider>,
		)

		expect(screen.getByTestId('user-id').textContent).toBe('none')

		await waitFor(() => {
			expect(screen.getByTestId('loading').textContent).toBe('false')
		})

		expect(screen.getByTestId('user-id').textContent).toBe('verified')
		expect(apiGetMock).toHaveBeenCalledWith(
			'/Auth/me',
			expect.objectContaining({ _skipAuthRedirect: true }),
		)
	})

	it('clears cached user when /Auth/me is 401 and refresh fails', async () => {
		sessionStore.setUser({ id: 'cached', role: 'administrator', username: 'admin' } as never)
		apiGetMock.mockRejectedValue({ response: { status: 401 } })
		tryRefreshMock.mockResolvedValue(false)

		render(
			<AuthProvider>
				<AuthProbe />
			</AuthProvider>,
		)

		await waitFor(() => {
			expect(screen.getByTestId('loading').textContent).toBe('false')
		})

		expect(screen.getByTestId('user-id').textContent).toBe('none')
		expect(sessionStore.getUser()).toBeNull()
	})

	it('restores session after silent refresh on 401', async () => {
		sessionStore.setUser({ id: 'cached', role: 'store', username: 'store1' } as never)
		apiGetMock
			.mockRejectedValueOnce({ response: { status: 401 } })
			.mockResolvedValueOnce({
				data: {
					success: true,
					data: { id: 'refreshed', role: 'store', username: 'store1' },
				},
			})
		tryRefreshMock.mockResolvedValue(true)

		render(
			<AuthProvider>
				<AuthProbe />
			</AuthProvider>,
		)

		await waitFor(() => {
			expect(screen.getByTestId('user-id').textContent).toBe('refreshed')
		})

		expect(tryRefreshMock).toHaveBeenCalled()
		expect(apiGetMock).toHaveBeenCalledTimes(2)
	})
})

describe('ProtectedRoute (bootstrap guard)', () => {
	beforeEach(() => {
		localStorage.clear()
		sessionStore.clearAll()
	})

	it('shows loading instead of redirecting while auth is loading', () => {
		sessionStore.setUser({ id: '1', role: 'administrator' } as never)

		render(
			<MemoryRouter initialEntries={['/dashboard']}>
				<Routes>
					<Route
						path="/dashboard"
						element={
							<ProtectedRoute>
								<div data-testid="protected-content">Secret</div>
							</ProtectedRoute>
						}
					/>
					<Route path="/login" element={<div data-testid="login-page">Login</div>} />
				</Routes>
			</MemoryRouter>,
			{
				wrapper: ({ children }) => (
					<AuthContext.Provider
						value={{
							user: null,
							isLoading: true,
							error: null,
							login: vi.fn(),
							completeSessionFromPayload: vi.fn(),
							logout: vi.fn(),
							clearError: vi.fn(),
							updateProfilePicture: vi.fn(),
						}}
					>
						{children}
					</AuthContext.Provider>
				),
			},
		)

		expect(screen.getByText(/checking permissions/i)).toBeInTheDocument()
		expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
		expect(screen.queryByTestId('login-page')).not.toBeInTheDocument()
	})

	it('redirects to login when auth finished and user is null', () => {
		render(
			<MemoryRouter initialEntries={['/dashboard']}>
				<Routes>
					<Route
						path="/dashboard"
						element={
							<ProtectedRoute>
								<div data-testid="protected-content">Secret</div>
							</ProtectedRoute>
						}
					/>
					<Route path="/login" element={<div data-testid="login-page">Login</div>} />
				</Routes>
			</MemoryRouter>,
			{
				wrapper: ({ children }) => (
					<AuthContext.Provider
						value={{
							user: null,
							isLoading: false,
							error: null,
							login: vi.fn(),
							completeSessionFromPayload: vi.fn(),
							logout: vi.fn(),
							clearError: vi.fn(),
							updateProfilePicture: vi.fn(),
						}}
					>
						{children}
					</AuthContext.Provider>
				),
			},
		)

		expect(screen.getByTestId('login-page')).toBeInTheDocument()
	})
})
