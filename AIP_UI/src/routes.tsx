import React, { lazy, useEffect, useRef } from 'react';
import { createBrowserRouter, Outlet, useNavigate, useLocation } from 'react-router-dom';
import ProtectedRoute from '@/components/ProtectedRoute';
import { Layout } from '@/components/layout/Layout';
import LoginPage from '@/pages/LoginPage';
import Index from '@/pages/Index';
import CustomerDetailPage from '@/pages/customer/CustomerDetailPage';
import ActionCalendar from '@/pages/ActionCalendar';
import { UserRole } from '@/types/user';
import { PageAccessProvider } from '@/contexts/PageAccessContext';
import { CustomerSelectionUrlSync } from '@/components/customer/CustomerSelectionUrlSync';

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
			
			// Log navigation chain for redirect loops
			if (navigationHistoryRef.current.length >= 3) {
				const recent = navigationHistoryRef.current.slice(-3);
				const isLoop = recent.every((nav, idx) => 
					idx === 0 || nav.from === recent[idx - 1].to
				);
				if (isLoop && recent[0].from === recent[recent.length - 1].to) {
					console.error('🔄 [Navigation] ⚠️ POTENTIAL REDIRECT LOOP DETECTED!', {
						chain: recent.map(n => n.to),
						timestamps: recent.map(n => new Date(n.timestamp).toISOString())
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

// Import all the necessary pages
import UserSetup from '@/pages/administration/UserSetup';
import EmployeeRegistration from '@/pages/administration/EmployeeRegistration';
import CustomerSetup from '@/pages/administration/CustomerSetup';
import IncidentReportPage from '@/pages/operations/IncidentReportPage';
import AlertRulesPage from '@/pages/operations/AlertRulesPage';
import DataAnalyticsHub from '@/pages/analytics/DataAnalyticsHub';
import Settings from '@/pages/Settings';
import Profile from '@/pages/Profile';

// Import operations pages (moved from customer)
const IncidentGraphPage = lazy(() => import('./pages/customer/IncidentGraph').then(module => ({ default: module.default })));
const CustomerCrimeIntelligencePage = lazy(() => import('./pages/customer/CustomerCrimeIntelligence'));
import BarcodeTestPage from './pages/test/BarcodeTestPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <PageAccessProvider>
        <PathNormalizer />
        <NavigationTracker />
        <CustomerSelectionUrlSync />
        <Outlet />
      </PageAccessProvider>
    ),
    children: [
      {
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'test/barcode',
        element: <BarcodeTestPage />,
      },
      {
        path: '/',
        element: <Layout />,
        children: [
          {
            path: '/',
            element: (
              <ProtectedRoute>
                <Index />
              </ProtectedRoute>
            ),
          },
          {
            path: 'dashboard',
            element: (
              <ProtectedRoute>
                <Index />
              </ProtectedRoute>
            ),
          },
          {
            path: 'action-calendar',
            element: (
              <ProtectedRoute>
                <ActionCalendar />
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
            path: 'settings',
            element: (
              <ProtectedRoute allowedRoles={['administrator'] as UserRole[]}>
                <Settings />
              </ProtectedRoute>
            ),
          },
          // Administration routes
          {
            path: 'administration/user-setup',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'advantageonehoofficer'] as UserRole[]}>
                <UserSetup />
              </ProtectedRoute>
            ),
          },
          {
            path: 'administration/employee-registration',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'advantageonehoofficer'] as UserRole[]}>
                <EmployeeRegistration />
              </ProtectedRoute>
            ),
          },
          {
            path: 'administration/customer-setup',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'advantageonehoofficer'] as UserRole[]}>
                <CustomerSetup />
              </ProtectedRoute>
            ),
          },
          // Operations routes
          {
            path: 'operations/incident-report',
            element: (
              <ProtectedRoute>
                <IncidentReportPage />
              </ProtectedRoute>
            ),
          },
          {
            path: 'operations/incident-graph',
            element: (
              <ProtectedRoute allowedRoles={['administrator', 'advantageoneofficer', 'customerhomanager', 'customersitemanager'] as UserRole[]}>
                <IncidentGraphPage />
              </ProtectedRoute>
            ),
          },
          {
            path: 'operations/crime-intelligence',
            element: (
              <ProtectedRoute 
                allowedRoles={['administrator', 'advantageonehoofficer', 'advantageoneofficer', 'customerhomanager', 'customersitemanager'] as UserRole[]}
                accessPath="/operations/crime-intelligence"
              >
                <CustomerCrimeIntelligencePage />
              </ProtectedRoute>
            ),
          },
          {
            path: 'operations/alert-rules',
            element: (
              <ProtectedRoute 
                allowedRoles={['administrator', 'advantageonehoofficer', 'customersitemanager', 'customerhomanager'] as UserRole[]}
                accessPath="/operations/alert-rules"
              >
                <AlertRulesPage />
              </ProtectedRoute>
            ),
          },
          {
            path: 'analytics/data-analytics-hub',
            element: (
              <ProtectedRoute 
                allowedRoles={['administrator', 'advantageonehoofficer', 'customerhomanager'] as UserRole[]}
                accessPath="/analytics/data-analytics-hub"
                enforcePageAccess={false}
              >
                <DataAnalyticsHub />
              </ProtectedRoute>
            ),
          },
          {
            path: 'customer/:customerId/*',
            element: (
              <ProtectedRoute enforcePageAccess={false}>
                <CustomerDetailPage />
              </ProtectedRoute>
            ),
          },
        ]
      }
    ]
  }
]);

export default router;
