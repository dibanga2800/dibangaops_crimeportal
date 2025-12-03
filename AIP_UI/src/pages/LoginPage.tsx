import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Eye, EyeOff, User2 } from 'lucide-react'
import { Alert } from '@/components/ui/alert'
import { usePageAccess } from '@/contexts/PageAccessContext'
import { useAuth } from '@/contexts/AuthContext'
import { UserRole } from '@/types/user'

interface LoginError {
  type: 'credentials' | 'network' | 'server' | 'validation';
  message: string;
}

// Remove the roleRedirect function since we handle redirection inline now

const getErrorMessage = (error: LoginError): { title: string; message: string } => {
  switch (error.type) {
    case 'credentials':
      return {
        title: 'Authentication Failed',
        message: error.message
      };
    case 'validation':
      return {
        title: 'Invalid Input',
        message: error.message
      };
    default:
      return {
        title: 'Error',
        message: error.message
      };
  }
};

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<LoginError | null>(null)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const { setCurrentRole } = usePageAccess()
  const { login } = useAuth()

  const validateForm = (): boolean => {
    if (!username.trim()) {
      setError({
        type: 'validation',
        message: 'Username is required'
      });
      return false;
    }
    if (!password.trim()) {
      setError({
        type: 'validation',
        message: 'Password is required'
      });
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    console.log('🔒 Starting login process...', { username })

    if (!validateForm()) {
      console.log('❌ Form validation failed')
      return
    }

    setLoading(true)
    try {
      const loggedInUser = await login(username, password)
      
      console.log('✅ Login successful:', {
        username: loggedInUser.username,
        role: loggedInUser.role,
        timestamp: new Date().toISOString()
      })

      // Set the page access role - use role if pageAccessRole is not available
      const roleToSet = loggedInUser.role
      console.log('🔑 Setting role for page access:', roleToSet)
      console.log('🔍 Full user object from login:', loggedInUser)
      
      // Set role (don't await - let it load in background)
      setCurrentRole(roleToSet).catch(err => {
        console.warn('⚠️ [LoginPage] Error setting role:', err);
      })

      // All users now go to /dashboard
      const redirectPath = '/dashboard'
      console.log('🔄 Redirecting to:', redirectPath)
      
      // Use replace to prevent going back to login page
      navigate(redirectPath, { replace: true })
    } catch (err) {
      console.error('❌ Login error:', err)
      setError({
        type: 'credentials',
        message: err instanceof Error ? err.message : 'An error occurred during login'
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      {/* Mobile-specific background positioning for logo visibility */}
      <style>{`
        .login-container {
          background-image: url(/coop_bg_img.png);
          background-repeat: no-repeat;
        }
        @media (max-width: 640px) {
          .login-container {
            background-size: auto 100%;
            background-position: center top;
          }
        }
        @media (min-width: 641px) {
          .login-container {
            background-size: cover;
            background-position: center center;
          }
        }
      `}</style>
      
      <div 
        className="login-container min-h-screen flex items-center justify-center relative overflow-hidden p-4 sm:p-6"
      >
        {/* Multi-layer overlay for professional appearance */}
        <div className="absolute inset-0 bg-gradient-to-br from-black/35 via-black/25 to-black/35" />
        <div className="absolute inset-0 backdrop-blur-sm" />
      
      {/* Login Card */}
      <Card className="w-full max-w-[95%] sm:max-w-md shadow-2xl relative z-10 border border-white/10 bg-white">
        <CardHeader className="pb-2 px-4 sm:px-6 pt-4 sm:pt-6">
          <CardTitle className="flex items-center gap-2 text-xl sm:text-2xl font-bold text-brand-700">
            <User2 className="h-5 w-5 sm:h-6 sm:w-6 text-brand-500 flex-shrink-0" />
            Sign In
          </CardTitle>
        </CardHeader>
        <CardContent className="px-4 sm:px-6 pb-4 sm:pb-6">
          <form className="space-y-4 sm:space-y-6" onSubmit={handleSubmit}>
            {error && (
              <Alert variant="destructive" className="mb-3 sm:mb-4">
                <h3 className="font-medium text-sm sm:text-base">{getErrorMessage(error).title}</h3>
                <p className="text-xs sm:text-sm mt-1">{getErrorMessage(error).message}</p>
              </Alert>
            )}
            <div>
              <label htmlFor="username" className="block text-xs sm:text-sm font-medium text-gray-700 mb-1.5">Username</label>
              <Input
                id="username"
                name="username"
                autoComplete="username"
                value={username}
                onChange={e => setUsername(e.target.value)}
                className="w-full text-sm sm:text-base h-10 sm:h-11"
                placeholder="Enter your username"
                disabled={loading}
              />
            </div>
            <div>
              <label htmlFor="password" className="block text-xs sm:text-sm font-medium text-gray-700 mb-1.5">Password</label>
              <div className="relative">
                <Input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  className="w-full pr-10 text-sm sm:text-base h-10 sm:h-11"
                  placeholder="Enter your password"
                  disabled={loading}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700 touch-manipulation"
                  disabled={loading}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4 sm:h-5 sm:w-5" />
                  ) : (
                    <Eye className="h-4 w-4 sm:h-5 sm:w-5" />
                  )}
                </button>
              </div>
            </div>
            <Button
              type="submit"
              className="w-full bg-brand hover:bg-brand-600 text-white h-10 sm:h-11 text-sm sm:text-base font-medium mt-2"
              disabled={loading}
            >
              {loading ? (
                <div className="flex items-center justify-center gap-2">
                  <div className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  <span>Signing in...</span>
                </div>
              ) : (
                'Sign In'
              )}
            </Button>
          </form>
        </CardContent>
      </Card>
      </div>
    </>
  )
} 