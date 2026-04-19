import React, { lazy, Suspense, useEffect, useRef } from 'react';
import { createBrowserRouter, Outlet, useNavigate, useLocation } from 'react-router-dom';
import ProtectedRoute from '@/components/ProtectedRoute';
import { Layout } from '@/components/layout/Layout';
import LoginPage from '@/pages/LoginPage';
import Index from '@/pages/Index';
import CustomerDetailPage from '@/pages/customer/CustomerDetailPage';
import NotFoundPage from '@/pages/NotFoundPage';
import { UserRole } from '@/types/user';
import { PageAccessProvider } from '@/contexts/PageAccessContext';
import { CustomerSelectionUrlSync } from '@/components/customer/CustomerSelectionUrlSync';
import { LoadingFallback } from '@/components/LoadingFallback';
import { SessionTimeoutManager } from '@/components/session/SessionTimeoutManager';
import { RootRedirect } from '@/components/RootRedirect';

// Component to normalize paths and fix double slashes
const PathNormalizer = () => {
	const location = useLocation();
	const navigate = useNavigate();

	useEffect(() => {
		const pathname = location.pathname;
		// Check if pathname has double or more slashes anywhere
		if (pathname.includes('//')) {
			// Replace multiple consecutive slashes with a single slash
			// This ensures we always have exactly one / at the start
			const normalized = pathname.replace(/\/+/g, '/');
			if (normalized !== pathname) {
				console.warn('🔧 [PathNormalizer] Fixing double slash in path:', pathname, '->', normalized);
				// Use replace: true to avoid adding to history and prevent navigation loops
				const newPath = normalized + (location.search || '') + (location.hash || '');
				navigate(newPath, { replace: true });
				return; // Exit early to prevent further processing
			}
		}
	}, [location.pathname, location.search, location.hash, navigate]);

	return null;
};

// Ensure every navigation starts at top of page
const ScrollToTop = () => {
	const location = useLocation();

	useEffect(() => {
		window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
	}, [location.pathname, location.search]);

	return null;
};

// Navigation tracker component for end-to-end logging
const NavigationTracker = () => {
	const location = useLocation();
	const previousLocationRef = useRef<{ pathname: string; search: string } | null>(null);
	const navigationHistoryRef = useRef<Array<{ from: string; to: string; timestamp: number }>>([]);

	useEffect(() => {
		const currentPath = location.pathname + location.search;
		const previousPath = previousLocationRef.current 
			? previousLocationRef.current.pathname + previousLocationRef.current.search 
			: null;

		// Only log if path actually changed
		if (previousPath !== currentPath) {
			const timestamp = Date.now();
			
			console.group('🔄 [Navigation] Route Change Detected');
			console.log('📍 Navigation Details:', {
				from: previousPath || '(initial load)',
				to: currentPath,
				timestamp: new Date(timestamp).toISOString(),
				pathname: location.pathname,
				search: location.search,
				hash: location.hash || '(none)'
			});
			
			// Track navigation history (keep last 10)
			if (previousPath) {
				navigationHistoryRef.current.push({
					from: previousPath,
					to: currentPath,
					timestamp
				});
				if (navigationHistoryRef.current.length > 10) {
					navigationHistoryRef.current.shift();
				}
			}
			
			// Log navigation chain for redirect loops (pathname only; ignore search/hash to avoid false positives)
			if (navigationHistoryRef.current.length >= 3) {
				const recent = navigationHistoryRef.current.slice(-3);
				const getPathname = (p: string) => p.split('?')[0].split('#')[0];
				const chainConnected = recent.every((nav, idx) =>
					idx === 0 || nav.from === recent[idx - 1].to
				);
				const pathnameLoop = getPathname(recent[0].from) === getPathname(recent[recent.length - 1].to);
				const pathnamesInChain = [...new Set(recent.flatMap((n) => [getPathname(n.from), getPathname(n.to)]))];
				const hasActualRouteChange = pathnamesInChain.length > 1;
				if (chainConnected && pathnameLoop && hasActualRouteChange) {
					console.warn('🔄 [Navigation] Potential redirect loop:', {
						chain: recent.map((n) => n.to),
						timestamps: recent.map((n) => new Date(n.timestamp).toISOString())
					});
				}
			}
			
			console.log('📚 Recent Navigation History:', navigationHistoryRef.current.slice(-5));
			console.groupEnd();
			
			previousLocationRef.current = {
				pathname: location.pathname,
				search: location.search
			};
		}
	}, [location.pathname, location.search, location.hash]);

	return null;
};

// Lazy load all route pages for better code splitting and performance
// Only critical pages (Login, Layout, Index) are loaded immediately

// Administration pages
const UserSetup = lazy(() => import('@/pages/administration/UserSetup'));
const EmployeeRegistration = lazy(() => import('@/pages/administration/EmployeeRegistration'));
const CustomerSetup = lazy(() => import('@/pages/administration/CustomerSetup'));

// Operations pages
const IncidentReportPage = lazy(() => import('@/pages/operations/IncidentReportPage'));
const AlertRulesPage = lazy(() => import('@/pages/operations/AlertRulesPage'));
const IncidentGraphPage = lazy(() => import('./pages/customer/IncidentGraph'));
const CustomerCrimeIntelligencePage = lazy(() => import('./pages/customer/CustomerCrimeIntelligence'));

// Analytics pages
const DataAnalyticsHub = lazy(() => import('@/pages/analytics/DataAnalyticsHub'));

// User pages
const Settings = lazy(() => import('@/pages/Settings'));
const Profile = lazy(() => import('@/pages/Profile'));

// Other pages
const BarcodeTestPage = lazy(() => import('./pages/test/BarcodeTestPage'));
const AboutPage = lazy(() => import('@/pages/AboutPage'));
const PrivacyPage = lazy(() => import('@/pages/PrivacyPage'));
const TermsPage = lazy(() => import('@/pages/TermsPage'));
const ContactPage = lazy(() => import('@/pages/ContactPage'));

const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <PageAccessProvider>
        <PathNormalizer />
        <ScrollToTop />
        <NavigationTracker />
        <SessionTimeoutManager />
        <CustomerSelectionUrlSync />
        <Outlet />
      </PageAccessProvider>
    ),
    children: [
      {
        index: true,
        element: <RootRedirect />,
      },
      {
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'about',
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <AboutPage />
          </Suspense>
        ),
      },
      {
        path: 'privacy',
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <PrivacyPage />
          </Suspense>
        ),
      },
      {
        path: 'terms',
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <TermsPage />
          </Suspense>
        ),
      },
      {
        path: 'test/barcode',
        element: <BarcodeTestPage />,
      },
      {
        element: <Layout />,
        children: [
          {
            path: 'dashboard',
            element: (
              <ProtectedRoute>
                <Suspense fallback={<LoadingFallback />}>
                  <Index />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'profile',
            element: (
              <ProtectedRoute>
                <Profile />
              </ProtectedRoute>
            ),
          },
          {
            path: 'contact',
            element: (
              <ProtectedRoute enforcePageAccess={false}>
                <Suspense fallback={<LoadingFallback />}>
                  <ContactPage />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'settings',
            element: (
              <ProtectedRoute allowedRoles={['administrator'] as UserRole[]}>
                <Suspense fallback={<LoadingFallback />}>
                  <Settings />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          // Administration routes
          {
            path: 'administration/user-setup',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'manager'] as UserRole[]}>
                <UserSetup />
              </ProtectedRoute>
            ),
          },
          {
            path: 'administration/employee-registration',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'manager'] as UserRole[]}>
                <Suspense fallback={<LoadingFallback />}>
                  <EmployeeRegistration />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'administration/customer-setup',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'manager'] as UserRole[]}>
                <CustomerSetup />
              </ProtectedRoute>
            ),
          },
          // Operations routes
          {
            path: 'operations/incident-report',
            element: (
              <ProtectedRoute>
                <Suspense fallback={<LoadingFallback />}>
                  <IncidentReportPage />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'operations/incident-graph',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'manager', 'store', 'security-officer'] as UserRole[]}>
                <Suspense fallback={<LoadingFallback />}>
                  <IncidentGraphPage />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'operations/crime-intelligence',
            element: (
              <ProtectedRoute 
                allowedRoles={['administrator', 'manager', 'store', 'security-officer'] as UserRole[]}
                accessPath="/operations/crime-intelligence"
              >
                <Suspense fallback={<LoadingFallback />}>
                  <CustomerCrimeIntelligencePage />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'operations/alert-rules',
            element: (
              <ProtectedRoute 
                allowedRoles={['administrator', 'manager', 'store', 'security-officer'] as UserRole[]}
                accessPath="/operations/alert-rules"
              >
                <Suspense fallback={<LoadingFallback />}>
                  <AlertRulesPage />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'analytics/data-analytics-hub',
            element: (
              <ProtectedRoute
                allowedRoles={['administrator', 'manager', 'store', 'security-officer'] as UserRole[]}
                accessPath="/analytics/data-analytics-hub"
              >
                <Suspense fallback={<LoadingFallback />}>
                  <DataAnalyticsHub />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: 'customer/:customerId/*',
            element: (
              <ProtectedRoute enforcePageAccess={false}>
                <Suspense fallback={<LoadingFallback />}>
                  <CustomerDetailPage />
                </Suspense>
              </ProtectedRoute>
            ),
          },
          {
            path: '*',
            element: <NotFoundPage />,
          },
        ]
      }
    ]
  },
  {
    path: '*',
    element: <NotFoundPage />,
  }
]);

export default router;
