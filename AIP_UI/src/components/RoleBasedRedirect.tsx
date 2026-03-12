import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { usePageAccess } from '@/contexts/PageAccessContext'
import { useAuth } from '@/contexts/AuthContext'

const RoleBasedRedirect = () => {
  const navigate = useNavigate()
  const { currentRole } = usePageAccess()
  const { user } = useAuth()
  
  useEffect(() => {
    
    // Only redirect if we have a valid role and user
    if (currentRole && user) {
      let redirectPath = '/dashboard' // default
      
      if (user.role === 'administrator' || user.role === 'manager') {
        redirectPath = '/dashboard'
      } else if (['security-officer', 'store'].includes(user.role ?? '')) {
        redirectPath = '/dashboard'
      }
      
      console.log('🔄 Role-based redirect to:', redirectPath)
      navigate(redirectPath, { replace: true })
    }
  }, [currentRole, navigate])
  
  return (
    <div className="flex h-screen w-full items-center justify-center">
      <div className="h-8 w-8 animate-spin rounded-full border-b-2 border-primary"></div>
    </div>
  )
}

export default RoleBasedRedirect 