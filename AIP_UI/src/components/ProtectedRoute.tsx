import React, { useContext, useEffect, useRef } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { PageAccessContext } from '@/contexts/PageAccessContext';
import { UserRole } from '@/types/user';

interface ProtectedRouteProps {
	children: React.ReactNode;
	allowedRoles?: UserRole[];
	accessPath?: string;
	enforcePageAccess?: boolean;
}

const normalizePath = (path: string): string => {
	if (!path) return '/';
	const trimmed = path.trim();
	const hasLeadingSlash = trimmed.startsWith('/');
	const normalized = trimmed.replace(/\/+/g, '/');
	return hasLeadingSlash ? normalized : `/${normalized}`;
};

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
	children,
	allowedRoles,
	accessPath,
	enforcePageAccess = true,
}) => {
	const { user, isLoading: authLoading } = useAuth();
	const pageAccess = useContext(PageAccessContext);
	const location = useLocation();
	const previousPathRef = useRef<string>('');

	const pathToCheck = normalizePath(accessPath ?? location.pathname);
	
	useEffect(() => {
		if (!import.meta.env.DEV) return;

		if (previousPathRef.current && previousPathRef.current !== pathToCheck) {
			console.log('🛡️ [ProtectedRoute] Path changed:', previousPathRef.current, '->', pathToCheck);
		}
		previousPathRef.current = pathToCheck;
	}, [pathToCheck]);
	
	// Show loading only during initial auth check (not page access loading to prevent loops)
	if (authLoading) {
		return (
			<div className="flex min-h-[200px] items-center justify-center text-sm text-muted-foreground">
				Checking permissions…
			</div>
		);
	}

	if (!user) {
		if (import.meta.env.DEV) {
			console.warn('🚫 [ProtectedRoute] Not authenticated — redirecting to /login from', pathToCheck);
		}
		return <Navigate to="/login" state={{ from: location }} replace />;
	}

	if (allowedRoles && !allowedRoles.includes(user.role)) {
		if (import.meta.env.DEV) {
			console.warn('🚫 [ProtectedRoute] Role blocked:', user.role, 'not in', allowedRoles, '— redirecting to /dashboard from', pathToCheck);
		}
		return <Navigate to="/dashboard" replace />;
	}

	// Check page access settings (only if enforcePageAccess is true)
	if (enforcePageAccess && pageAccess) {
		// If still loading settings, allow access temporarily to prevent redirect loops
		// The hasAccess function already handles this case
		if (pageAccess.status === 'loading') {
			return <>{children}</>;
		}

		// If offline, still allow access but log warning
		if (pageAccess.status === 'offline') {
			if (import.meta.env.DEV) {
				console.warn('⚠️ [ProtectedRoute] Backend offline, allowing access:', pathToCheck);
			}
			return <>{children}</>;
		}

		// Check access using page access settings
		const hasAccessResult = pageAccess.hasAccess(pathToCheck);
		
		if (!hasAccessResult) {
			if (import.meta.env.DEV) {
				console.warn('🚫 [ProtectedRoute] Page access denied for', pathToCheck, '— redirecting to /dashboard');
			}
			return <Navigate to="/dashboard" replace />;
		}
	}

	return <>{children}</>;
};

export default ProtectedRoute;