import React from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Settings as SettingsIcon, LayoutGrid } from 'lucide-react'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion'
import { Button } from '../ui/button'
import { cn } from '@/lib/utils'
import { usePageAccess } from '@/contexts/PageAccessContext'
import { PageAccessContext } from '@/contexts/PageAccessContext'
import { useAuth } from '@/contexts/AuthContext'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { CustomerSelector } from '@/components/customer/CustomerSelector'
import {
	SIDEBAR_SECTIONS,
	SIDEBAR_TOP_LINKS,
	type SidebarGuardContext,
	type SidebarNavLink,
	type SidebarSection,
} from '@/config/navigation/sidebar'

interface SidebarNavigationProps {
	onNavigate?: () => void
	isMobileOpen?: boolean
	onMobileClose?: () => void
}

interface NavItemProps {
  to: string
  icon: React.ReactNode
  label: string
  onClick?: () => void
  className?: string
	bypassAccessCheck?: boolean
}

const NavItem = ({ to, icon, label, onClick, className, bypassAccessCheck }: NavItemProps) => {
	const { hasAccess } = usePageAccess()
	const { selectedCustomerId, isAdmin } = useCustomerSelection()
	const navigate = useNavigate()
	const location = useLocation()

	// Normalize path - ensure it starts with / and doesn't have double slashes
	const normalizePath = (path: string) => {
		if (!path) return '/'
		// Remove any leading/trailing whitespace
		const trimmed = path.trim()
		// Split by ? to handle query parameters
		const [pathPart, queryPart] = trimmed.split('?')
		// Replace multiple consecutive slashes with a single slash
		// This preserves the path structure but removes double slashes
		const normalized = pathPart.replace(/\/+/g, '/')
		// Ensure it starts with exactly one / (not //)
		const finalPath = normalized.startsWith('//') ? normalized.substring(1) : normalized
		// Reattach query string if it exists
		return queryPart ? `${finalPath}?${queryPart}` : finalPath
	}

	const normalizedTo = normalizePath(to)
	const isCustomerPage = normalizedTo.startsWith('/customer/')
	const finalTo = isCustomerPage && isAdmin && selectedCustomerId ? `${normalizedTo}?customerId=${selectedCustomerId}` : normalizedTo
	const isActive = location.pathname === normalizedTo || (isCustomerPage && location.pathname === normalizedTo.split('?')[0])
  
	// Access check is already handled by canDisplayLink in the parent component
	// The parent filters links before passing them to NavItem, so we don't need to check again here
	// bypassAccessCheck is only used for special cases where NavItem might be used outside the normal flow
  
  const handleClick = (e: React.MouseEvent) => {
		e.preventDefault()
		e.stopPropagation()
    
    console.group('🖱️ [SidebarNavigation] Link Click');
    console.log('📍 Click Details:', {
      label,
      originalTo: to,
      normalizedTo,
      finalTo,
      isCustomerPage,
      isAdmin,
      selectedCustomerId
    });
    
    if (isCustomerPage && isAdmin && !selectedCustomerId) {
      console.warn('⚠️ [SidebarNavigation] Navigation blocked: Customer page requires customer selection');
      console.log('📋 Block Details:', {
        reason: 'Customer page accessed by admin without selectedCustomerId',
        action: 'No navigation performed'
      });
      console.groupEnd();
			return
    }
    
		// Ensure path is normalized before navigation
		const normalizedFinalTo = normalizePath(finalTo)
    
    console.log('🚀 [SidebarNavigation] Executing navigation:', {
      from: location.pathname + location.search,
      to: normalizedFinalTo,
      timestamp: new Date().toISOString()
    });
    console.groupEnd();
    
		navigate(normalizedFinalTo)
		onClick?.()
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
		if (e.key === 'Enter' || e.key === ' ') {
			e.preventDefault()
			e.stopPropagation()
      
      if (isCustomerPage && isAdmin && !selectedCustomerId) {
				return
      }
      
			// Ensure path is normalized before navigation
			const normalizedFinalTo = normalizePath(finalTo)
			navigate(normalizedFinalTo)
			onClick?.()
    }
  }

  return (
    <a
      href="#"
      className={cn(
				'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
				'hover:bg-accent hover:text-accent-foreground',
				'focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
				isActive && 'bg-accent text-accent-foreground',
				className,
      )}
      onClick={handleClick}
      tabIndex={0}
      onKeyDown={handleKeyDown}
      aria-label={`Navigate to ${label}`}
    >
      {icon}
      <span>{label}</span>
    </a>
  )
}

const canDisplayLink = (link: SidebarNavLink, context: SidebarGuardContext, availablePages: Array<{ path: string }>, currentRole?: string | null) => {
	// If bypassAccessCheck is set, always show (for special cases like officer customer reporting)
	// Check this FIRST before any other logic
	if (link.bypassAccessCheck) {
		// Still respect the guard if it exists, but bypass the Settings check
		if (link.guard) {
			return link.guard(context)
		}
		return true
	}

	// For administrators, ALWAYS show all items from config
	if (context.isAdministrator || currentRole === 'administrator') {
		return true
	}

	// If link has a custom guard, use it
	if (link.guard) {
		const guardResult = link.guard(context)
		// Guard should check hasAccess internally, so just return its result
		return guardResult
	}

	// For all other roles, ONLY use Settings-based access control
	const hasAccess = context.hasAccess(link.path)
	return hasAccess
}

export const SidebarNavigation: React.FC<SidebarNavigationProps> = ({ onNavigate, onMobileClose }) => {
	// Try to get context, but handle gracefully if not available
	let pageAccessContext;
	try {
		pageAccessContext = React.useContext(PageAccessContext);
	} catch (error) {
		// Context not available
		pageAccessContext = undefined;
	}
	
	// Safety check - if context is not available, return loading state
	if (!pageAccessContext) {
		return (
			<div className="px-3 py-2">
				<div className="text-sm text-gray-500 dark:text-gray-400">Loading navigation...</div>
			</div>
		);
	}
	
	const { hasAccess, currentRole, isLoading, availablePages, pageAccessByRole } = pageAccessContext;
	const { user } = useAuth(); // Check user from AuthContext
	const { selectedCustomerId, isAdmin } = useCustomerSelection()
	const navigate = useNavigate()
	const location = useLocation()

	// Derive an effective role for navigation visibility:
	// - Prefer the authenticated user's primary role
	// - Fall back to pageAccessRole
	// - Finally fall back to PageAccessContext.currentRole so behavior degrades gracefully
	const effectiveRole = React.useMemo(() => {
		if (user) {
			const rawRole =
				user.role ||
				(user as any).pageAccessRole ||
				null;
			if (rawRole) {
				return String(rawRole).trim().toLowerCase();
			}
		}
		return currentRole;
	}, [user, currentRole]);
	
	// Compute guard context and sections (must be before early returns)
	// Store: only incident page. Security Officer: incident + Settings-configurable. Manager/Admin: full access
	const isCustomerRole = effectiveRole === 'store' || effectiveRole === 'manager' || effectiveRole === 'security-officer'
	const isAdministrator = effectiveRole === 'administrator'
	const isManager = effectiveRole === 'manager'
	const isOfficerRole = effectiveRole === 'security-officer'
	const isStoreUser = effectiveRole === 'store'

	const guardContext: SidebarGuardContext = React.useMemo(() => ({
		hasAccess,
		isCustomerRole,
		isAdministrator,
		isOfficerRole,
		isStoreUser,
		isManager
	}), [hasAccess, isCustomerRole, isAdministrator, isOfficerRole, isStoreUser, isManager]);

	// Debug: Log current page access for officer role
	React.useEffect(() => {
		if (effectiveRole === 'store' && import.meta.env.DEV) {
			const storePages = pageAccessByRole[effectiveRole] || [];
			const customerReportingPages = storePages.filter(id => 
				id === 'management-customer-reporting' || id.includes('customer-reporting')
			);
			const customerReportingPage = availablePages.find(p => 
				p.path === '/management/customer-reporting' || 
				p.id === 'management-customer-reporting'
			);
			
			console.log(`🔍 [Sidebar] Store role page access:`, {
				totalPages: storePages.length,
				customerReporting: {
					enabled: customerReportingPages.length > 0,
					pageIds: customerReportingPages,
					pageInAvailablePages: !!customerReportingPage,
					pageId: customerReportingPage?.id,
					hasAccess: customerReportingPage ? hasAccess('/management/customer-reporting') : false
				}
			});
		}
	}, [effectiveRole, pageAccessByRole, availablePages, hasAccess]);

	// Create a key based on pageAccessByRole to force re-render when settings change
	const settingsKey = React.useMemo(() => {
		if (effectiveRole && pageAccessByRole[effectiveRole]) {
			return JSON.stringify(pageAccessByRole[effectiveRole].sort());
		}
		return '';
	}, [effectiveRole, pageAccessByRole]);

	const pages = availablePages || []
	
	// Debug: Log available pages and page access for Customer Reporting
	React.useEffect(() => {
		if (effectiveRole === 'store' && import.meta.env.DEV) {
			const customerReportingPage = pages.find(p => 
				p.path === '/management/customer-reporting' || 
				p.id === 'management-customer-reporting'
			);
			const storePages = pageAccessByRole[effectiveRole] || [];
			const hasCustomerReporting = storePages.includes('management-customer-reporting') || 
			                               storePages.some(id => id.includes('customer-reporting'));
			
			console.log(`🔍 [Sidebar] Available pages and access check:`, {
				totalAvailablePages: pages.length,
				customerReportingPage: customerReportingPage ? {
					id: customerReportingPage.id,
					path: customerReportingPage.path,
					title: customerReportingPage.title
				} : 'NOT FOUND',
				storeHasAccess: hasCustomerReporting,
				storeAllowedPages: storePages.slice(0, 5),
				willShowInSidebar: customerReportingPage ? guardContext.hasAccess('/management/customer-reporting') : false
			});
		}
	}, [pages, effectiveRole, pageAccessByRole, guardContext]);
	
	const topLevelLinks = React.useMemo(() => {
		let filtered = SIDEBAR_TOP_LINKS.filter((link) => canDisplayLink(link, guardContext, pages, effectiveRole));

		// Store & security-officer roles: top links come from SIDEBAR_TOP_LINKS and page access

		// Dev-only: log filtering for store/officer to verify Settings-driven visibility
		if (import.meta.env.DEV && (effectiveRole === 'store' || effectiveRole === 'security-officer')) {
			const rolePages = pageAccessByRole[effectiveRole] || [];
			SIDEBAR_TOP_LINKS.forEach((link) => {
				const canDisplay = canDisplayLink(link, guardContext, pages, effectiveRole);
				const hasAccessResult = guardContext.hasAccess(link.path);
				console.log(`🔍 [Sidebar] Top link "${link.label}" (${link.path}):`, {
					canDisplay,
					hasAccess: hasAccessResult,
					currentRole: effectiveRole,
					roleAllowedPagesCount: rolePages.length,
					rolePagesSample: rolePages.slice(0, 5),
				});
			});
		}
		
		return filtered;
	}, [guardContext, pages, effectiveRole, settingsKey, pageAccessByRole]);

	const visibleSections = React.useMemo(() => {
		return SIDEBAR_SECTIONS.reduce<SidebarSection[]>((acc, section) => {
			const guardPassed = section.guard ? section.guard(guardContext) : true
			if (!guardPassed) {
				return acc
			}

			let links = section.links.filter((link) => {
				const canDisplay = canDisplayLink(link, guardContext, pages, effectiveRole)
				
				// Dev-only: log filtering for store/officer to verify Settings-driven visibility
				if (import.meta.env.DEV && (effectiveRole === 'store' || effectiveRole === 'security-officer')) {
					const rolePages = pageAccessByRole[effectiveRole] || [];
					console.log(`🔍 [Sidebar] Section link "${link.label}" (${link.path}):`, {
						canDisplay,
						hasAccess: guardContext.hasAccess(link.path),
						currentRole: effectiveRole,
						rolePagesSample: rolePages.slice(0, 5),
					});
				}
				
				return canDisplay
			})

			// No override: store and security-officer links come only from page access settings (hasAccess via canDisplayLink).

			// Debug logging
			if (import.meta.env.DEV && false) {
				console.log(`🔍 [Sidebar] Section filtered:`, {
					sectionId: section.id,
					totalLinks: section.links.length,
					filteredLinks: links.length,
					willShow: links.length > 0,
					settingsKey
				})
			}
			
			if (links.length === 0) {
				return acc
			}

			acc.push({ ...section, links })
			return acc
		}, [])
	}, [guardContext, pages, effectiveRole, settingsKey, pageAccessByRole])
  
  if (isLoading) {
    return (
      <div className="px-3 py-2">
        <div className="space-y-4">
          <div className="pl-5">
            <div className="w-[180px] h-9 bg-gray-200 dark:bg-gray-700 animate-pulse rounded-[20px]" />
          </div>
          <div className="space-y-2">
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className="h-10 bg-gray-200 dark:bg-gray-700 animate-pulse rounded-lg" />
            ))}
          </div>
        </div>
      </div>
		)
  }

  // If no user at all, show login message
  if (!user) {
    return (
      <div className="px-3 py-2">
				<div className="text-sm text-gray-500 dark:text-gray-400">Please log in to view navigation</div>
        </div>
		)
  }

  // User exists but role not initialized yet - show loading
  if (!effectiveRole) {
    return (
      <div className="px-3 py-2">
				<div className="text-sm text-gray-500 dark:text-gray-400">Loading navigation...</div>
        </div>
		)
  }

  const handleKeyDown = (to: string) => (e: React.KeyboardEvent) => {
		if (e.key === 'Enter' || e.key === ' ') {
			e.preventDefault()

			const isCustomerPage = to.startsWith('/customer/')
			const finalTo = isCustomerPage && isAdmin && selectedCustomerId ? `${to}?customerId=${selectedCustomerId}` : to

      if (isCustomerPage && isAdmin && !selectedCustomerId) {
				return
      }
      
			navigate(finalTo)
			onNavigate?.()
			onMobileClose?.()
    }
	}

  const handleNavigation = (to: string) => (e: React.MouseEvent) => {
		e.preventDefault()

		const isCustomerPage = to.startsWith('/customer/')
		const finalTo = isCustomerPage && isAdmin && selectedCustomerId ? `${to}?customerId=${selectedCustomerId}` : to

    if (isCustomerPage && isAdmin && !selectedCustomerId) {
			return
    }
    
		navigate(finalTo)
		onNavigate?.()
		onMobileClose?.()
  }

  return (
    <div className="px-3 py-2">
      <div className="space-y-4">
        {hasAccess('/dashboard') && (
          <div className="pl-5">
            <Button
              asChild
              className={cn(
								'w-[180px] rounded-[20px] bg-white px-3 text-black hover:bg-white/90 flex h-9 items-center justify-start gap-2',
								(location.pathname === '/' || location.pathname === '/dashboard') && 'bg-white/90',
              )}
            >
              <a 
                href="/dashboard" 
								onClick={handleNavigation('/dashboard')}
								onKeyDown={handleKeyDown('/dashboard')}
                className="flex items-center gap-2"
              >
								<div className="rounded-lg bg-red-500/10 p-1.5">
                  <LayoutGrid className="h-[18px] w-[18px] text-red-500" />
                </div>
                <span className="text-xs font-medium">Dashboard</span>
              </a>
            </Button>
          </div>
        )}

				{topLevelLinks.map((link) => {
					const Icon = link.icon
					return (
          <NavItem
							key={link.path}
							to={link.path}
							icon={<Icon className="h-4 w-4" />}
							label={link.label}
            onClick={onNavigate}
							bypassAccessCheck={link.bypassAccessCheck}
          />
					)
				})}

        <Accordion type="multiple" className="space-y-2" key={settingsKey}>
					{visibleSections.map((section) => {
						const SectionIcon = section.icon
						return (
							<AccordionItem value={section.id} key={section.id}>
              <AccordionTrigger className="text-sm">
                <div className="flex items-center gap-2">
										<SectionIcon className="h-4 w-4" />
										<span>{section.label}</span>
                </div>
              </AccordionTrigger>
              <AccordionContent className="space-y-1 pt-1">
									{section.showCustomerSelector && isAdmin && (
                  <div className="px-3 pb-2">
                    <CustomerSelector />
                    {!selectedCustomerId && (
												<p className="mt-2 text-xs text-amber-600 dark:text-amber-500">
                        Please select a company to access company pages
                      </p>
                    )}
                  </div>
                )}
                
									{section.links.map((link) => {
										const Icon = link.icon
										return (
                  <NavItem
												key={link.path}
												to={link.path}
												icon={<Icon className="h-4 w-4" />}
												label={link.label}
                    onClick={onNavigate}
												bypassAccessCheck={link.bypassAccessCheck}
                  />
										)
									})}
              </AccordionContent>
            </AccordionItem>
						)
					})}
        </Accordion>

        {isAdministrator && hasAccess('/settings') && (
          <NavItem
            to="/settings"
            icon={<SettingsIcon className="h-4 w-4" />}
            label="Settings"
            onClick={onNavigate}
            className="mt-4"
          />
        )}
      </div>
    </div>
	)
}