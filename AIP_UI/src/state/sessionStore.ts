import { User, UserRole } from '@/types/user'

type Listener = (user: User | null) => void

const TOKEN_KEY = 'authToken'
const REFRESH_TOKEN_KEY = 'refreshToken'
const TOKEN_EXPIRES_AT_KEY = 'tokenExpiresAt'
const USER_KEY = 'user'
const PROFILE_PIC_KEY = 'profilePicture'

const ROLE_MIGRATION_MAP: Record<string, UserRole> = {
	'advantageonehoofficer': 'manager',
	'advantageoneofficer': 'store',
	'customersitemanager': 'store',
	'customerhomanager': 'manager',
}

const normalizeRole = (role: string | undefined): UserRole => {
	if (!role) return 'store'
	const lower = role.toLowerCase().trim()
	return ROLE_MIGRATION_MAP[lower] ?? lower as UserRole
}

const normalizeUser = (user: User): User => {
	// Support both frontend camelCase and backend PascalCase for profile picture and security prefs
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

export const sessionStore = {
	// Token management
	getToken: (): string | null => {
		try {
			return localStorage.getItem(TOKEN_KEY)
		} catch (error) {
			console.error('Error getting token from session storage:', error)
			return null
		}
	},

	getRefreshToken: (): string | null => {
		try {
			return localStorage.getItem(REFRESH_TOKEN_KEY)
		} catch (error) {
			console.error('Error getting refresh token from session storage:', error)
			return null
		}
	},

	setRefreshToken: (refreshToken: string | null): void => {
		try {
			if (refreshToken) {
				localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
			} else {
				localStorage.removeItem(REFRESH_TOKEN_KEY)
			}
		} catch (error) {
			console.error('Error setting refresh token in session storage:', error)
		}
	},

	getTokenExpiresAt: (): string | null => {
		try {
			return localStorage.getItem(TOKEN_EXPIRES_AT_KEY)
		} catch (error) {
			console.error('Error getting token expiry from session storage:', error)
			return null
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

	setToken: (token: string): void => {
		try {
			localStorage.setItem(TOKEN_KEY, token)
		} catch (error) {
			console.error('Error setting token in session storage:', error)
		}
	},

	clearToken: (): void => {
		try {
			localStorage.removeItem(TOKEN_KEY)
		} catch (error) {
			console.error('Error clearing token from session storage:', error)
		}
	},

	// User management
	getUser: (): User | null => {
		try {
			if (currentUser) {
				return currentUser
			}
			const userStr = localStorage.getItem(USER_KEY)
			if (!userStr) return null
			const parsed = JSON.parse(userStr)
			if (!parsed) return null
			const normalized = normalizeUser(parsed)
			const profilePic = localStorage.getItem(PROFILE_PIC_KEY)
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
			const profilePicFromLocalStorage = localStorage.getItem(PROFILE_PIC_KEY)

			// If we already have a cached profile picture, keep it.
			// Otherwise, if the backend/user payload includes a profile picture,
			// persist it into the dedicated PROFILE_PIC_KEY so it survives reloads.
			if (profilePicFromLocalStorage) {
				normalized.profilePicture = profilePicFromLocalStorage
			} else if (normalized.profilePicture) {
				try {
					localStorage.setItem(PROFILE_PIC_KEY, normalized.profilePicture as unknown as string)
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
			localStorage.removeItem(USER_KEY)
			localStorage.removeItem(PROFILE_PIC_KEY)
		} catch (error) {
			console.error('Error clearing user from session storage:', error)
		}
	},

	clearAll: (): void => {
		sessionStore.clearToken()
		sessionStore.clearUser()
		sessionStore.setRefreshToken(null)
		sessionStore.setTokenExpiresAt(null)
	},

	// Profile picture management (stored separately to keep user JSON small)
	getProfilePicture: (): string | null => {
		try {
			return localStorage.getItem(PROFILE_PIC_KEY)
		} catch (error) {
			console.error('Error getting profile picture:', error)
			return null
		}
	},

	setProfilePicture: (dataUrl: string | null): void => {
		try {
			if (dataUrl) {
				localStorage.setItem(PROFILE_PIC_KEY, dataUrl)
			} else {
				localStorage.removeItem(PROFILE_PIC_KEY)
			}
			if (currentUser) {
				currentUser = { ...currentUser, profilePicture: dataUrl ?? undefined }
				listeners.forEach(listener => listener(currentUser))
			}
		} catch (error) {
			console.error('Error setting profile picture:', error)
		}
	},

	// Subscription for user changes
	subscribe: (listener: Listener) => {
		listeners.add(listener)
		return () => listeners.delete(listener)
	}
}

