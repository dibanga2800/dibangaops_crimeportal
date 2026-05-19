import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { flushSync } from 'react-dom';
import { User } from '@/types/user';
import { api, tryRefreshAccessToken, AUTH_REQUEST_TIMEOUT_MS } from '@/config/api';
import { sessionStore } from '@/state/sessionStore';
import { applyLoginPayload } from '@/utils/authSession';
import { COOKIE_REQUIRED_MESSAGE, isUnauthorizedStatus } from '@/utils/authCookieHelp';
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
  completeSessionFromPayload: (loginData: Record<string, unknown>) => Promise<User>;
  logout: () => void;
  clearError: () => void;
  updateProfilePicture: (dataUrl: string | null) => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCurrentUser = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    const loadMe = async (): Promise<User | null> => {
      const response = await api.get<BackendApiResponse<User>>('/Auth/me', {
        timeout: AUTH_REQUEST_TIMEOUT_MS,
        _skipAuthRedirect: true,
      } as Parameters<typeof api.get>[1]);
      // ApiResponseDto: backend JSON uses camelCase (success/data) per Program.cs
      const apiResponse = response.data as BackendApiResponse<User> & {
        success?: boolean;
        data?: User;
        message?: string;
      };
      const isSuccess = apiResponse?.Success ?? apiResponse?.success ?? false;
      const userData = apiResponse?.Data ?? apiResponse?.data;
      const message = apiResponse?.Message ?? apiResponse?.message;

      if (isSuccess && userData) {
        return userData;
      }

      throw new Error(message || 'Failed to fetch user data');
    };

    try {
      const userData = await loadMe();
      sessionStore.setUser(userData);
      setUser(sessionStore.getUser());
    } catch (err: unknown) {
      const axiosErr = err as { response?: { status?: number }; code?: string; message?: string };
      const status = axiosErr?.response?.status;
      const isUnauthorized = status === 401;
      const isTimeout =
        axiosErr?.code === 'ECONNABORTED' || axiosErr?.message?.includes('timeout');
      const cachedUser = sessionStore.getUser();

      if (isUnauthorized) {
        const refreshed = await tryRefreshAccessToken();
        if (refreshed) {
          try {
            const userData = await loadMe();
            sessionStore.setUser(userData);
            setUser(sessionStore.getUser());
            return;
          } catch (retryErr) {
            if (import.meta.env.DEV) {
              console.warn('Failed to fetch current user after refresh:', retryErr);
            }
          }
        }

        sessionStore.clearAll();
        setUser(null);
      } else if (isTimeout && cachedUser) {
        console.warn('Failed to fetch current user - timeout (using cached profile until next refresh)');
        setUser(cachedUser);
      } else {
        sessionStore.clearAll();
        setUser(null);
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
      const hasSession = sessionStore.hasSession()

      if (!expiresAt || !hasSession) {
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

      const refreshed = await tryRefreshAccessToken()
      if (isMounted && refreshed) {
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
        const twoFactorUser = (loginData as any)?.User ?? (loginData as any)?.user;
        const accountEmail =
          twoFactorUser?.Email ??
          twoFactorUser?.email ??
          username;
        const twoFactorEmailSent =
          (loginData as any)?.TwoFactorEmailSent ??
          (loginData as any)?.twoFactorEmailSent ??
          true;
        const twoFactorDeliveryMessage =
          (loginData as any)?.TwoFactorDeliveryMessage ??
          (loginData as any)?.twoFactorDeliveryMessage;

        if (import.meta.env.DEV) {
          console.log('🔐 [AuthContext] 2FA required for user:', {
            email: accountEmail,
            emailSent: twoFactorEmailSent,
            methods: loginData?.TwoFactorMethods ?? loginData?.twoFactorMethods,
          });
        }

        return {
          requiresTwoFactor: true,
          email: accountEmail,
          twoFactorEmailSent,
          twoFactorDeliveryMessage,
        } as any;
      }

      const normalizedUser = applyLoginPayload(responseData as Record<string, unknown>);
      flushSync(() => {
        setUser(normalizedUser);
        setError(null);
      });

      const verification = await verifyAuthenticatedSession();
      if (!verification.ok) {
        sessionStore.clearAll();
        flushSync(() => {
          setUser(null);
        });
        const sessionMessage = verification.likelyCookieBlocked
          ? COOKIE_REQUIRED_MESSAGE
          : 'Session could not be established. Please try again or contact support if the problem continues.';
        setError(sessionMessage);
        throw new Error(sessionMessage);
      }
      
      if (import.meta.env.DEV) {
        console.log('✅ [AuthContext] Login successful:', {
          username: normalizedUser.username,
          userId: normalizedUser.id,
          role: normalizedUser.role
        });
      }
      
      return sessionStore.getUser() ?? normalizedUser;
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

  const verifyAuthenticatedSession = async (): Promise<{
    ok: boolean;
    likelyCookieBlocked: boolean;
  }> => {
    let probeStatus: number | undefined;

    try {
      const response = await api.get<BackendApiResponse<User>>('/Auth/me', {
        timeout: AUTH_REQUEST_TIMEOUT_MS,
        _skipAuthRedirect: true,
      } as Parameters<typeof api.get>[1]);

      const apiResponse = response.data as BackendApiResponse<User> & {
        success?: boolean;
        data?: User;
      };
      const isSuccess = apiResponse?.Success ?? apiResponse?.success ?? false;
      const userData = apiResponse?.Data ?? apiResponse?.data;

      if (isSuccess && userData) {
        sessionStore.setUser(userData);
        flushSync(() => {
          setUser(sessionStore.getUser());
        });
        return { ok: true, likelyCookieBlocked: false };
      }
    } catch (err: unknown) {
      const axiosErr = err as { response?: { status?: number } };
      probeStatus = axiosErr?.response?.status;
    }

    const refreshed = await tryRefreshAccessToken();
    if (refreshed) {
      flushSync(() => {
        setUser(sessionStore.getUser());
      });
      return { ok: true, likelyCookieBlocked: false };
    }

    // Login returned csrfToken but /Auth/me could not authenticate — cookies not stored/sent (incognito, blockers).
    const likelyCookieBlocked =
      isUnauthorizedStatus(probeStatus) &&
      Boolean(sessionStore.getCsrfToken()) &&
      !sessionStore.getAccessToken();

    return { ok: false, likelyCookieBlocked };
  };

  const completeSessionFromPayload = async (
    loginData: Record<string, unknown>,
  ): Promise<User> => {
    const normalizedUser = applyLoginPayload(loginData);

    flushSync(() => {
      setUser(normalizedUser);
      setError(null);
    });

    const verification = await verifyAuthenticatedSession();
    if (!verification.ok) {
      sessionStore.clearAll();
      flushSync(() => {
        setUser(null);
      });
      throw new Error(
        verification.likelyCookieBlocked
          ? COOKIE_REQUIRED_MESSAGE
          : 'Session could not be established. Please try again or contact support if the problem continues.',
      );
    }

    return sessionStore.getUser() ?? normalizedUser;
  };

  const logout = () => {
    void (async () => {
      try {
        await api.post('/Auth/logout', {});
      } catch {
        // Local session is cleared even when the API is unreachable.
      } finally {
        sessionStore.clearAll();
        setUser(null);
        setError(null);
      }
    })();
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
    <AuthContext.Provider value={{ user, isLoading, error, login, completeSessionFromPayload, logout, clearError, updateProfilePicture }}>
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