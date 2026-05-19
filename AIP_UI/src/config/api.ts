import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { sessionStore } from '@/state/sessionStore'
import { applyCsrfHeader } from '@/utils/csrf'
import { applyBearerHeader } from '@/utils/bearerAuth'
import { persistAuthMetadataFromApiEnvelope } from '@/utils/authSession'
import { isCsrfForbidden } from '@/utils/apiErrors'
import { API_BASE_URL, isDevelopment } from './env'

export const BASE_API_URL = API_BASE_URL

export const DEFAULT_API_TIMEOUT_MS = 30_000
export const AUTH_REQUEST_TIMEOUT_MS = 90_000

export const api = axios.create({
  baseURL: BASE_API_URL,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  timeout: DEFAULT_API_TIMEOUT_MS,
  withCredentials: true,
})

const PUBLIC_AUTH_ENDPOINTS = new Set([
  '/auth/2fa/complete',
  '/auth/login',
  '/auth/refresh',
  '/auth/forgot-password',
  '/auth/reset-password',
])

const isPublicEndpoint = (url?: string): boolean => {
  if (!url) return false

  try {
    const normalizedPath = new URL(url, BASE_API_URL).pathname.toLowerCase()
    return Array.from(PUBLIC_AUTH_ENDPOINTS).some(endpoint =>
      normalizedPath.endsWith(endpoint)
    )
  } catch {
    return false
  }
}

api.interceptors.request.use(
  (config) => {
    if (isDevelopment) {
      console.log('🔄 [API Interceptor] Making request', {
        url: config.url,
        method: config.method,
        baseURL: config.baseURL,
      })
    }

    let headers = (config.headers ?? {}) as Record<string, string>
    headers = applyBearerHeader(headers)

    if (!isPublicEndpoint(config.url)) {
      headers = applyCsrfHeader(headers)
    }

    config.headers = headers
    return config
  },
  (error) => {
    console.error('❌ [API Interceptor] Request error:', error)
    return Promise.reject(error)
  }
)

let isRefreshing = false
let refreshPromise: Promise<boolean> | null = null

type AxiosRequestConfigWithRetry = InternalAxiosRequestConfig & {
  _retry?: boolean
  _skipAuthRedirect?: boolean
}

const AUTH_RESPONSE_PATH_SUFFIXES = [
  '/auth/login',
  '/auth/2fa/complete',
  '/auth/refresh',
] as const

const isAuthSessionResponse = (url?: string): boolean => {
  if (!url) return false
  try {
    const path = new URL(url, BASE_API_URL).pathname.toLowerCase()
    return AUTH_RESPONSE_PATH_SUFFIXES.some(suffix => path.endsWith(suffix))
  } catch {
    return false
  }
}

const refreshAccessToken = async (): Promise<boolean> => {
  try {
    const storedRefreshToken = sessionStore.getRefreshToken()
    const response = await api.post(
      '/Auth/refresh',
      storedRefreshToken ? { refreshToken: storedRefreshToken } : {},
      {
        timeout: AUTH_REQUEST_TIMEOUT_MS,
      }
    )

    const apiResponse = response.data
    const isSuccess = apiResponse?.Success ?? apiResponse?.success ?? false
    const data = apiResponse?.Data ?? apiResponse?.data

    if (!isSuccess || !data) {
      if (isDevelopment) {
        console.warn('⚠️ [Auth] Refresh token response invalid or unsuccessful:', apiResponse)
      }
      return false
    }

    const refreshData = data as Record<string, unknown>
    const expiresAt =
      (refreshData?.ExpiresAt as string | undefined) ??
      (refreshData?.expiresAt as string | undefined)
    const user = (refreshData?.User ?? refreshData?.user) as Parameters<typeof sessionStore.setUser>[0] | undefined
    if (user) {
      sessionStore.setUser(user)
    }

    persistAuthMetadataFromApiEnvelope({ Data: refreshData })

    if (isDevelopment) {
      console.log('✅ [Auth] Session refreshed successfully')
    }

    return true
  } catch (error) {
    if (isDevelopment) {
      console.error('❌ [Auth] Failed to refresh session:', error)
    }
    return false
  }
}

export const tryRefreshAccessToken = async (): Promise<boolean> => refreshAccessToken()

api.interceptors.response.use(
  (response) => {
    if (isAuthSessionResponse(response.config.url)) {
      persistAuthMetadataFromApiEnvelope(response.data)
    }

    if (isDevelopment) {
      console.log('✅ [API Interceptor] Response received', {
        url: response.config.url,
        status: response.status
      })
    }
    return response
  },
  async (error: AxiosError) => {
    const status = error.response?.status;
    const url = (error.config as AxiosRequestConfigWithRetry)?.url || '';

    const isExpectedError = status === 404 || status === 403;
    const shouldLogVerbose = isDevelopment;
    const shouldLogWarningOnly = !shouldLogVerbose && isExpectedError;

    if (shouldLogWarningOnly) {
      console.warn(`⚠️ [API Interceptor] ${status} ${error.config?.method?.toUpperCase()} ${url}`);
    } else if (shouldLogVerbose) {
      const isNetworkError = !error.response && (error.message === 'Network Error' || error.message.includes('Failed to fetch'))
      if (!isNetworkError) {
        console.error('❌ [API Interceptor] Response error:', {
          url,
          method: error.config?.method,
          status,
          message: error.message,
          responseData: error.response?.data,
        })
      }
    }

    if (status === 403 && isCsrfForbidden(status, error.response?.data)) {
      if (isDevelopment) {
        console.warn('⚠️ [API] CSRF validation failed — re-login required (no session refresh loop)')
      }
      return Promise.reject(error)
    }

    if (status === 401) {
      const config = (error.config || {}) as AxiosRequestConfigWithRetry
      const isLoginPage = window.location.pathname.includes('/login')
      const isLoginEndpoint = url.includes('/Auth/login')
      const isRefreshEndpoint = url.includes('/Auth/refresh')
      const hasSession = sessionStore.hasSession()

      if (!isLoginEndpoint && !isRefreshEndpoint && !config._retry) {
        config._retry = true

        try {
          if (!isRefreshing) {
            isRefreshing = true
            refreshPromise = refreshAccessToken().finally(() => {
              isRefreshing = false
            })
          }

          const refreshed = await refreshPromise!
          if (refreshed) {
            return api(config)
          }
        } catch (refreshError) {
          if (isDevelopment) {
            console.error('❌ [API] Error during session refresh:', refreshError)
          }
        }
      }

      const shouldForceLogout =
        !config._skipAuthRedirect &&
        config._retry &&
        !isLoginEndpoint &&
        !isRefreshEndpoint &&
        hasSession

      if (shouldForceLogout) {
        console.warn('⚠️ [API 401] Session invalid after refresh — clearing session and redirecting to /login')
        sessionStore.clearAll()
        if (!isLoginPage) {
          window.location.href = '/login'
        }
      }
    }
    return Promise.reject(error)
  }
)

export const EMPLOYEE_ENDPOINTS = {
  LIST: '/employee',
  DETAIL: (id: string) => `/employee/${id}`,
  CREATE: '/employee',
  UPDATE: (id: string) => `/employee/${id}`,
  DELETE: (id: string) => `/employee/${id}`,
  REGISTER: '/employee',
  STATISTICS: '/employee/statistics',
  ACTIVE: '/employee/active',
} as const

export const CUSTOMER_ENDPOINTS = {
  LIST: '/customer',
  DETAIL: (id: string) => `/customer/${id}`,
  CREATE: '/customer',
  UPDATE: (id: string) => `/customer/${id}`,
  DELETE: (id: string) => `/customer/${id}`,
  STATISTICS: '/customer/statistics',
  PAGE_ASSIGNMENTS: (id: string) => `/customer/${id}/page-assignments`,
} as const

export const REGION_ENDPOINTS = {
  LIST: '/region',
  DETAIL: (id: string) => `/region/${id}`,
  CREATE: '/region',
  UPDATE: (id: string) => `/region/${id}`,
  DELETE: (id: string) => `/region/${id}`,
  BY_CUSTOMER: (customerId: string) => `/region/customer/${customerId}`,
} as const

export const SITE_ENDPOINTS = {
  LIST: '/site',
  DETAIL: (id: string) => `/site/${id}`,
  CREATE: '/site',
  UPDATE: (id: string) => `/site/${id}`,
  DELETE: (id: string) => `/site/${id}`,
  BY_CUSTOMER: (customerId: string) => `/site/customer/${customerId}`,
  BY_REGION: (regionId: string) => `/site/region/${regionId}`,
} as const

export const USER_ENDPOINTS = {
  LIST: '/user',
  DETAIL: (id: string) => `/user/${id}`,
  CREATE: '/user',
  UPDATE: (id: string) => `/user/${id}`,
  DELETE: (id: string) => `/user/${id}`,
  ASSIGN_CUSTOMERS: (id: string) => `/user/${id}/assign-customers`,
} as const

export const STOCK_ENDPOINTS = {
  LIST: '/Stock',
  DETAIL: (id: string) => `/Stock/${id}`,
  CREATE: '/Stock',
  UPDATE: (id: string) => `/Stock/${id}`,
  DELETE: (id: string) => `/Stock/${id}`,
  ISSUE: (id: string) => `/Stock/${id}/issue`,
  ADD: (id: string) => `/Stock/${id}/add`,
  LOW_STOCK: '/Stock/low-stock',
  CHECK_LOW_STOCK: '/Stock/check-low-stock',
  TEST_EMAIL: '/Stock/test-email',
} as const

export const CLASSIFICATION_ENDPOINTS = {
  CLASSIFY: '/Classification/classify',
  CLASSIFY_EXISTING: (id: number) => `/Classification/classify/${id}`,
} as const

export const ANALYTICS_ENDPOINTS = {
  SUMMARY: '/Analytics/summary',
  HUB: '/Analytics/hub',
  AI_PATTERNS: '/AiAnalytics/patterns',
  AI_RISK_SCORES: '/AiAnalytics/risk-scores',
} as const

export const EVIDENCE_ENDPOINTS = {
  REGISTER: (incidentId: number) => `/Evidence/incidents/${incidentId}/evidence`,
  BY_INCIDENT: (incidentId: number) => `/Evidence/incidents/${incidentId}`,
  DETAIL: (id: number) => `/Evidence/${id}`,
  SCAN: '/Evidence/scan',
  CUSTODY_EVENT: (id: number) => `/Evidence/${id}/custody`,
} as const

export const ALERT_INSTANCE_ENDPOINTS = {
  LIST: '/alerts',
  DETAIL: (id: number) => `/alerts/${id}`,
  SUMMARY: '/alerts/summary',
  ACKNOWLEDGE: (id: number) => `/alerts/${id}/acknowledge`,
  ESCALATE: (id: number) => `/alerts/${id}/escalate`,
  RESOLVE: (id: number) => `/alerts/${id}/resolve`,
} as const

export const API_HEADERS = {
  'Content-Type': 'application/json',
  'Accept': 'application/json'
} as const

export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errors?: string[]
}

export const handleApiError = (error: any): string => {
  const data = error?.response?.data

  if (data?.message && typeof data.message === 'string') {
    return data.message
  }

  if (data?.errors) {
    const errors = data.errors

    if (typeof errors === 'string') {
      return errors
    }

    if (Array.isArray(errors)) {
      return errors.join(', ')
    }

    if (typeof errors === 'object') {
      const flattened = Object.values(errors)
        .flat()
        .filter((e: unknown): e is string => typeof e === 'string')

      if (flattened.length > 0) {
        return flattened.join(', ')
      }
    }
  }

  if (error?.message && typeof error.message === 'string') {
    return error.message
  }

  return 'An unexpected error occurred'
}
