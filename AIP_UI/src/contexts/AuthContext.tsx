import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { User } from '@/types/user';
import { api, tryRefreshAccessToken, AUTH_REQUEST_TIMEOUT_MS } from '@/config/api';
import { sessionStore } from '@/state/sessionStore';
import { ApiResponse } from '@/types/api';

// Backend ApiResponseDto structure (capital case)
interface BackendApiResponse<T> {
	Success: boolean;
	Message: string;
	Data: T;
	Errors?: string[];
	Timestamp?: string;
}

type LoginResponsePayload = {
	AccessToken: string;
	RefreshToken?: string;
	ExpiresAt?: string;
	User: User;
	Success?: boolean;
	Message?: string;
}

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  error: string | null;
  login: (username: string, password: string) => Promise<User>;
  logout: () => void;
  clearError: () => void;
  updateProfilePicture: (dataUrl: string | null) => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Initialize user from localStorage immediately to prevent login on refresh
  const [user, setUser] = useState<User | null>(() => sessionStore.getUser());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCurrentUser = useCallback(async () => {
    const token = sessionStore.getToken();
    if (!token) {
      setUser(null);
      sessionStore.setUser(null);
      setError(null); // Clear any stale errors when there's no token
      setIsLoading(false);
      return;
    }

    // Restore user from localStorage first for immediate UI rendering
    const cachedUser = sessionStore.getUser();
    if (cachedUser) {
      setUser(cachedUser);
      setIsLoading(false);
    }

    try {
      setIsLoading(true);
      const response = await api.get<BackendApiResponse<User>>('/Auth/me', {
        timeout: AUTH_REQUEST_TIMEOUT_MS,
      });
      // Backend returns ApiResponseDto with capital Data property
      const apiResponse = response.data;
      if (apiResponse.Success && apiResponse.Data) {
        sessionStore.setUser(apiResponse.Data);
        setUser(sessionStore.getUser());
        setError(null);
      } else {
        throw new Error(apiResponse.Message || 'Failed to fetch user data');
      }
    } catch (err: any) {
      // Only clear token if it's a 401 (unauthorized) - token is invalid
      // For other errors (network, 500, etc.), keep the cached user
      const isUnauthorized = err?.response?.status === 401;
      const isTimeout = err?.code === 'ECONNABORTED' || err?.message?.includes('timeout');
      
      if (isUnauthorized) {
        console.error('Failed to fetch current user - unauthorized:', err);
        sessionStore.clearToken();
        setUser(null);
        sessionStore.setUser(null);
        setError(null); // Clear error on unauthorized
      } else if (isTimeout) {
        // For timeout errors, don't set error state - just log and keep cached user
        console.warn('Failed to fetch current user - timeout (keeping cached user):', err);
        setError(null); // Clear any stale timeout errors
      } else {
        // For other errors, keep the cached user and just log the error
        console.warn('Failed to fetch current user (keeping cached user):', err);
        setError(null); // Don't persist errors from fetchCurrentUser
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const handleUserAssignmentsUpdate = (event: CustomEvent<User>) => {
      const updatedUser = event.detail;
      if (updatedUser && updatedUser.id === user?.id) {
        console.log('🔄 [AuthContext] Updating user assignments from event');
        sessionStore.setUser(updatedUser);
        setUser(sessionStore.getUser());
      }
    };

    window.addEventListener('user-assignments-updated', handleUserAssignmentsUpdate as EventListener);
    
    return () => {
      window.removeEventListener('user-assignments-updated', handleUserAssignmentsUpdate as EventListener);
    };
  }, [user]);

  useEffect(() => {
    fetchCurrentUser();
  }, [fetchCurrentUser]);

  useEffect(() => {
    let isMounted = true

    const refreshIfNearExpiry = async () => {
      const expiresAt = sessionStore.getTokenExpiresAt()
      const refreshToken = sessionStore.getRefreshToken()
      const hasSession = !!sessionStore.getToken() && !!sessionStore.getUser()

      if (!expiresAt || !refreshToken || !hasSession) {
        return
      }

      const expiresAtMs = new Date(expiresAt).getTime()
      if (Number.isNaN(expiresAtMs)) {
        return
      }

      const remainingMs = expiresAtMs - Date.now()
      const proactiveWindowMs = 5 * 60 * 1000 // refresh 5 minutes before expiry

      if (remainingMs > proactiveWindowMs) {
        return
      }

      const newToken = await tryRefreshAccessToken()
      if (isMounted && newToken) {
        setUser(sessionStore.getUser())
      }
    }

    const intervalId = window.setInterval(() => {
      void refreshIfNearExpiry()
    }, 60 * 1000)

    void refreshIfNearExpiry()

    return () => {
      isMounted = false
      window.clearInterval(intervalId)
    }
  }, [])

  const login = async (username: string, password: string): Promise<User> => {
    try {
      setIsLoading(true);
      setError(null);

      let response;
      try {
        response = await api.post<any>(
          '/Auth/login',
          {
            email: username,
            password,
          },
          { timeout: AUTH_REQUEST_TIMEOUT_MS },
        );
      } catch (axiosError: any) {
        // Check if it's a timeout error
        const isTimeout = axiosError?.code === 'ECONNABORTED' || axiosError?.message?.includes('timeout');
        
        if (isTimeout) {
          const timeoutMessage = 'Connection timeout. Please check your internet connection and try again.';
          setError(timeoutMessage);
          console.error('❌ [AuthContext] Login failed - timeout:', {
            message: axiosError.message,
            code: axiosError.code
          });
          throw new Error(timeoutMessage);
        }
        
        // Axios throws errors for non-2xx responses, but we might have error data
        const errorResponse = axiosError?.response;
        if (errorResponse?.data) {
          // Backend returns error in ApiResponseDto format
          const errorData = errorResponse.data;
          const errorMessage = errorData?.Message ?? errorData?.message ?? 'Invalid email or password';
          
          console.error('❌ [AuthContext] Login failed (HTTP error):', {
            status: errorResponse.status,
            message: errorMessage,
            errorData
          });
          
          setError(errorMessage);
          throw new Error(errorMessage);
        }
        // Re-throw if we can't extract error message
        throw axiosError;
      }

      // Log the raw response for debugging
      if (import.meta.env.DEV) {
        console.log('🔍 [AuthContext] Login response:', {
          status: response.status,
          data: response.data,
          dataKeys: response.data ? Object.keys(response.data) : [],
          hasSuccess: 'Success' in (response.data || {}),
          hasData: 'Data' in (response.data || {})
        });
      }

      // Backend returns ApiResponseDto<LoginResponseDto> with capital Data property
      // Handle both capital and lowercase properties for compatibility
      const apiResponse = response.data;
      const isSuccess = apiResponse?.Success ?? apiResponse?.success ?? false;
      const responseData = apiResponse?.Data ?? apiResponse?.data;
      const message = apiResponse?.Message ?? apiResponse?.message ?? 'Invalid response from server';

      if (!isSuccess || !responseData) {
        console.error('❌ [AuthContext] Login failed:', {
          isSuccess,
          hasResponseData: !!responseData,
          message,
          fullResponse: apiResponse
        });
        throw new Error(message);
      }

      // Backend LoginResponseDto has AccessToken (capital A) and User (capital U)
      // Handle both capital and lowercase for compatibility
      const loginData = responseData as LoginResponsePayload;
      const requiresTwoFactor = (loginData as any)?.RequiresTwoFactor ?? (loginData as any)?.requiresTwoFactor ?? false;

      if (requiresTwoFactor) {
        if (import.meta.env.DEV) {
          console.log('🔐 [AuthContext] 2FA required for user:', {
            email: username,
            methods: loginData?.TwoFactorMethods ?? loginData?.twoFactorMethods,
          });
        }
        // Caller (LoginPage) will handle the second step using email + code
        return { requiresTwoFactor: true, email: username } as any;
      }

      const accessToken = loginData?.AccessToken ?? (loginData as any)?.accessToken;
      const refreshToken = loginData?.RefreshToken ?? (loginData as any)?.refreshToken;
      const expiresAt = loginData?.ExpiresAt ?? (loginData as any)?.expiresAt;
      const user = (loginData as any)?.User ?? (loginData as any)?.user;

      if (!accessToken || !user) {
        console.error('❌ [AuthContext] Missing token or user in response:', {
          hasAccessToken: !!accessToken,
          hasUser: !!user,
          loginDataKeys: loginData ? Object.keys(loginData) : [],
          loginData
        });
        throw new Error('Invalid response from server: missing token or user data');
      }

      sessionStore.setToken(accessToken);
      sessionStore.setRefreshToken(refreshToken ?? null);
      sessionStore.setTokenExpiresAt(expiresAt ?? null);
      sessionStore.setUser(user);
      const normalizedUser = sessionStore.getUser()!;
      setUser(normalizedUser);
      setError(null);
      
      if (import.meta.env.DEV) {
        console.log('✅ [AuthContext] Login successful:', {
          username: normalizedUser.username,
          userId: normalizedUser.id,
          role: normalizedUser.role
        });
      }
      
      return normalizedUser;
    } catch (err: any) {
      // Check if error was already handled (timeout or HTTP error)
      if (err instanceof Error && err.message) {
        // Error message was already set in the inner catch block
        console.error('❌ [AuthContext] Login error:', err);
        throw err;
      }
      
      // Otherwise, try to extract error message from response
      const isTimeout = err?.code === 'ECONNABORTED' || err?.message?.includes('timeout');
      const errorMessage = isTimeout
        ? 'Connection timeout. Please check your internet connection and try again.'
        : err?.response?.data?.Message 
          ?? err?.response?.data?.message 
          ?? err?.message 
          ?? 'An error occurred during login';
      
      setError(errorMessage);
      
      // Log detailed error information
      console.error('❌ [AuthContext] Login error:', {
        message: errorMessage,
        error: err,
        response: err?.response?.data,
        status: err?.response?.status,
        statusText: err?.response?.statusText
      });
      
      throw new Error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    sessionStore.clearAll();
    setUser(null);
    setError(null); // Clear error on logout
  };

  const clearError = () => {
    setError(null);
  };

  const updateProfilePicture = (dataUrl: string | null) => {
    sessionStore.setProfilePicture(dataUrl);
    if (user) {
      setUser({ ...user, profilePicture: dataUrl ?? undefined });
    }
  };

  return (
    <AuthContext.Provider value={{ user, isLoading, error, login, logout, clearError, updateProfilePicture }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
} 