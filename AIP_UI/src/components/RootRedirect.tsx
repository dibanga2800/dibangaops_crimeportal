import { Navigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { LoadingFallback } from '@/components/LoadingFallback'

/**
 * Handles the bare `/` URL without mounting the authenticated app shell (Layout).
 * - Session still resolving → full-page loading (no dashboard chrome).
 * - No user → `/login`.
 * - User present → `/dashboard` (same home content as before, under Layout).
 */
export const RootRedirect = () => {
	const { user, isLoading } = useAuth()

	if (isLoading) {
		return <LoadingFallback />
	}

	if (!user) {
		return <Navigate to="/login" replace />
	}

	return <Navigate to="/dashboard" replace />
}
