import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';

interface RouteConfig {
  path: string;
  roles?: string[];
  requireAuth?: boolean;
  redirectTo?: string;
}

const defaultRouteConfig: RouteConfig[] = [
  { path: '/login', requireAuth: false },
  { path: '/dashboard', requireAuth: true },
  { path: '/admin/*', roles: ['administrator'], requireAuth: true },
  { path: '/customer/*', roles: ['manager', 'store'], requireAuth: true },
  { path: '/officer/*', roles: ['store', 'manager'], requireAuth: true },
];

/**
 * Lightweight route-protection hook. Not currently wired into the router
 * (ProtectedRoute handles auth gating). Kept aligned with AuthContext's API
 * for potential future use.
 */
export function useRouteProtection(customConfig: RouteConfig[] = []) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  
  const routeConfig = [...defaultRouteConfig, ...customConfig];

  useEffect(() => {
    const currentPath = location.pathname;
    const matchingRoute = routeConfig.find(route => 
      currentPath.startsWith(route.path.replace('/*', ''))
    );

    if (!matchingRoute) return;

    if (matchingRoute.requireAuth && !user) {
      navigate('/login', { state: { from: location }, replace: true });
      return;
    }

    if (matchingRoute.roles && user?.role && 
        !matchingRoute.roles.includes(user.role)) {
      navigate(matchingRoute.redirectTo || '/dashboard', { replace: true });
    }
  }, [location, user, navigate, routeConfig]);
}

export function usePermissions(requiredPermissions: string[]) {
  const { user } = useAuth();
  
  return {
    hasPermission: !!user && requiredPermissions.includes(user.role),
    isLoading: false,
    error: null
  };
} 