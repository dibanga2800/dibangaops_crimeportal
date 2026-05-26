import { User, UserRole } from '@/types/user'

type Listener = (user: User | null) => void

const LEGACY_TOKEN_KEY = 'authToken'
const LEGACY_REFRESH_TOKEN_KEY = 'refreshToken'
const ACCESS_TOKEN_KEY = 'accessToken'
const REFRESH_TOKEN_KEY = 'refreshToken'
const TOKEN_EXPIRES_AT_KEY = 'tokenExpiresAt'
const CSRF_TOKEN_KEY = 'csrfToken'
const USER_KEY = 'user'
const PROFILE_PIC_KEY = 'profilePicture'

const ROLE_MIGRATION_MAP: Record<string, UserRole> = {
	'advantageonehoofficer': 'manager',
	'advantageoneofficer': 'store',
	'customersitemanager': 'store',
	'customerhomanager': 'manager',
	'store user': 'store',
	'store-user': 'store',
	'store_user': 'store',
	'security officer': 'security-officer',
	'security_officer': 'security-officer',
}

const normalizeRole = (role: string | undefined): UserRole => {
	if (!role) return 'store'
	const lower = role.toLowerCase().trim()
	return ROLE_MIGRATION_MAP[lower] ?? lower as UserRole
}

const normalizeUser = (user: User): User => {
	const anyUser = user as any
	const profilePicture =
		anyUser.profilePicture !== undefined
			? anyUser.profilePicture
			: anyUser.ProfilePicture
	const twoFactorEnabled = anyUser.twoFactorEnabled ?? anyUser.TwoFactorEnabled ?? false
	const emailNotificationsEnabled = anyUser.emailNotificationsEnabled ?? anyUser.EmailNotificationsEnabled ?? true
	const loginAlertsEnabled = anyUser.loginAlertsEnabled ?? anyUser.LoginAlertsEnabled ?? true

	return {
		...user,
		role: normalizeRole(user.role),
		pageAccessRole: normalizeRole(user.pageAccessRole ?? user.role),
		...(profilePicture !== undefined ? { profilePicture } : {}),
		twoFactorEnabled,
		emailNotificationsEnabled,
		loginAlertsEnabled,
	}
}

let currentUser: User | null = null
const listeners = new Set<Listener>()

const purgeLegacyTokenStorage = (): void => {
	try {
		localStorage.removeItem(LEGACY_TOKEN_KEY)
		localStorage.removeItem(LEGACY_REFRESH_TOKEN_KEY)
	} catch (error) {
		console.error('Error clearing legacy auth tokens:', error)
	}
}

purgeLegacyTokenStorage()

export const sessionStore = {
	hasSession: (): boolean => Boolean(currentUser ?? sessionStore.getUser()),

	getTokenExpiresAt: (): string | null => {
		try {
			return localStorage.getItem(TOKEN_EXPIRES_AT_KEY)
		} catch (error) {
			console.error('Error getting token expiry from session storage:', error)
			return null
		}
	},

	getAccessToken: (): string | null => {
		try {
			return sessionStorage.getItem(ACCESS_TOKEN_KEY)
		} catch (error) {
			console.error('Error getting access token from session storage:', error)
			return null
		}
	},

	setAccessToken: (token: string | null): void => {
		try {
			if (token) {
				sessionStorage.setItem(ACCESS_TOKEN_KEY, token)
			} else {
				sessionStorage.removeItem(ACCESS_TOKEN_KEY)
			}
		} catch (error) {
			console.error('Error setting access token in session storage:', error)
		}
	},

	getRefreshToken: (): string | null => {
		try {
			return sessionStorage.getItem(REFRESH_TOKEN_KEY)
		} catch (error) {
			console.error('Error getting refresh token from session storage:', error)
			return null
		}
	},

	setRefreshToken: (token: string | null): void => {
		try {
			if (token) {
				sessionStorage.setItem(REFRESH_TOKEN_KEY, token)
			} else {
				sessionStorage.removeItem(REFRESH_TOKEN_KEY)
			}
		} catch (error) {
			console.error('Error setting refresh token in session storage:', error)
		}
	},

	usesBearerAuth: (): boolean => Boolean(sessionStore.getAccessToken()),

	getCsrfToken: (): string | null => {
		try {
			return sessionStorage.getItem(CSRF_TOKEN_KEY)
		} catch (error) {
			console.error('Error getting CSRF token from session storage:', error)
			return null
		}
	},

	setCsrfToken: (token: string | null): void => {
		try {
			if (token) {
				sessionStorage.setItem(CSRF_TOKEN_KEY, token)
			} else {
				sessionStorage.removeItem(CSRF_TOKEN_KEY)
			}
		} catch (error) {
			console.error('Error setting CSRF token in session storage:', error)
		}
	},

	setTokenExpiresAt: (expiresAt: string | null): void => {
		try {
			if (expiresAt) {
				localStorage.setItem(TOKEN_EXPIRES_AT_KEY, expiresAt)
			} else {
				localStorage.removeItem(TOKEN_EXPIRES_AT_KEY)
			}
		} catch (error) {
			console.error('Error setting token expiry in session storage:', error)
		}
	},

	getUser: (): User | null => {
		try {
			if (currentUser) {
				return currentUser
			}
			const userStr = sessionStorage.getItem(USER_KEY)
			if (!userStr) return null
			const parsed = JSON.parse(userStr)
			if (!parsed) return null
			const normalized = normalizeUser(parsed)
			const profilePic = sessionStorage.getItem(PROFILE_PIC_KEY)
			normalized.profilePicture = profilePic ?? undefined
			currentUser = normalized
			return normalized
		} catch (error) {
			console.error('Error getting user from session storage:', error)
			return null
		}
	},

	setUser: (user: User | null) => {
		const normalized = user ? normalizeUser(user) : null
		if (normalized) {
			const profilePicFromLocalStorage = sessionStorage.getItem(PROFILE_PIC_KEY)

			if (profilePicFromLocalStorage) {
				normalized.profilePicture = profilePicFromLocalStorage
			} else if (normalized.profilePicture) {
				try {
					sessionStorage.setItem(PROFILE_PIC_KEY, normalized.profilePicture as unknown as string)
				} catch (error) {
					console.error('Error caching profile picture in session storage:', error)
				}
			}
		}
		currentUser = normalized
		listeners.forEach(listener => listener(normalized))
		try {
			if (normalized) {
				const { profilePicture: _pp, ...userWithoutPic } = normalized
				localStorage.setItem(USER_KEY, JSON.stringify(userWithoutPic))
			} else {
				localStorage.removeItem(USER_KEY)
			}
		} catch (error) {
			console.error('Error setting user in session storage:', error)
		}
	},

	clearUser: (): void => {
		currentUser = null
		listeners.forEach(listener => listener(null))
		try {
			sessionStorage.removeItem(USER_KEY)
			sessionStorage.removeItem(PROFILE_PIC_KEY)
		} catch (error) {
			console.error('Error clearing user from session storage:', error)
		}
	},

	clearAll: (): void => {
		sessionStore.clearUser()
		sessionStore.setTokenExpiresAt(null)
		sessionStore.setCsrfToken(null)
		sessionStore.setAccessToken(null)
		sessionStore.setRefreshToken(null)
		purgeLegacyTokenStorage()
	},

	getProfilePicture: (): string | null => {
		try {
			return sessionStorage.getItem(PROFILE_PIC_KEY)
		} catch (error) {
			console.error('Error getting profile picture:', error)
			return null
		}
	},

	setProfilePicture: (dataUrl: string | null): void => {
		try {
			if (dataUrl) {
				sessionStorage.setItem(PROFILE_PIC_KEY, dataUrl)
			} else {
				sessionStorage.removeItem(PROFILE_PIC_KEY)
			}
			if (currentUser) {
				currentUser = { ...currentUser, profilePicture: dataUrl ?? undefined }
				listeners.forEach(listener => listener(currentUser))
			}
		} catch (error) {
			console.error('Error setting profile picture:', error)
		}
	},

	subscribe: (listener: Listener) => {
		listeners.add(listener)
		return () => listeners.delete(listener)
	}
}
