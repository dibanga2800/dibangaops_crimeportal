import React from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Settings as SettingsIcon, LayoutGrid } from 'lucide-react'
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

/** Shared look for sidebar links: quiet by default, leaf marker when active. */
const sidebarLinkClass = (isActive: boolean) =>
	cn(
		'relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-sidebar-foreground transition-all duration-150',
		'hover:bg-white/[0.08] hover:text-white hover:[&>svg]:opacity-100',
		'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-ring',
		'[&>svg]:h-4 [&>svg]:w-4 [&>svg]:shrink-0 [&>svg]:opacity-100',
		isActive &&
			'sidebar-link-active font-semibold text-white [&>svg]:text-signal [&>svg]:opacity-100 before:absolute before:-left-3 before:top-1.5 before:bottom-1.5 before:w-[3px] before:rounded-r-full before:bg-signal',
	)

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
      href={finalTo}
      className={cn(sidebarLinkClass(isActive), className)}
      onClick={handleClick}
      tabIndex={0}
      onKeyDown={handleKeyDown}
      aria-current={isActive ? 'page' : undefined}
    >
      {icon}
      <span className="truncate">{label}</span>
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
	const pageAccessContext = React.useContext(PageAccessContext);
	const { user } = useAuth()
	const { selectedCustomerId, isAdmin } = useCustomerSelection()
	const navigate = useNavigate()
	const location = useLocation()

	const hasAccess = pageAccessContext?.hasAccess ?? (() => false)
	const currentRole = pageAccessContext?.currentRole ?? null
	const isLoading = pageAccessContext?.isLoading ?? true
	const availablePages = pageAccessContext?.availablePages ?? []
	const pageAccessByRole = pageAccessContext?.pageAccessByRole ?? {}

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

	// Create a key based on pageAccessByRole to force re-render when settings change
	const settingsKey = React.useMemo(() => {
		if (effectiveRole && pageAccessByRole[effectiveRole]) {
			return JSON.stringify(pageAccessByRole[effectiveRole].sort());
		}
		return '';
	}, [effectiveRole, pageAccessByRole]);

	const pages = availablePages || []
	
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
  
  if (!pageAccessContext || isLoading) {
    return (
      <div className="space-y-1.5 px-3 py-4" aria-busy="true" aria-label="Loading navigation">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="h-9 animate-pulse rounded-md bg-sidebar-accent" />
        ))}
      </div>
		)
  }

  // If no user at all, show login message
  if (!user) {
    return (
      <div className="px-3 py-2">
				<div className="text-sm text-sidebar-foreground">Sign in to see navigation</div>
        </div>
		)
  }

  // User exists but role not initialized yet - show loading
  if (!effectiveRole) {
    return (
      <div className="px-3 py-2">
				<div className="text-sm text-sidebar-foreground">Loading navigation…</div>
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

  const isDashboardActive = location.pathname === '/' || location.pathname === '/dashboard'

  return (
    <div className="space-y-6 px-3 py-4">
      <div className="space-y-0.5">
        <p className="px-3 pb-1.5 text-[11px] font-semibold uppercase tracking-[0.08em] text-sidebar-primary/80">Overview</p>
        {hasAccess('/dashboard') && (
          <a
            href="/dashboard"
            onClick={handleNavigation('/dashboard')}
            onKeyDown={handleKeyDown('/dashboard')}
            className={sidebarLinkClass(isDashboardActive)}
            aria-current={isDashboardActive ? 'page' : undefined}
          >
            <LayoutGrid />
            <span>Dashboard</span>
          </a>
        )}

				{topLevelLinks.map((link) => {
					const Icon = link.icon
					return (
          <NavItem
							key={link.path}
							to={link.path}
							icon={<Icon />}
							label={link.label}
            onClick={onNavigate}
							bypassAccessCheck={link.bypassAccessCheck}
          />
					)
				})}
      </div>

        {visibleSections.map((section) => (
          <div key={section.id} className="space-y-0.5" role="group" aria-labelledby={`nav-${section.id}`}>
            <p id={`nav-${section.id}`} className="px-3 pb-1.5 text-[11px] font-semibold uppercase tracking-[0.08em] text-sidebar-primary/80">
              {section.label}
            </p>
            {section.showCustomerSelector && isAdmin && (
              <div className="pb-2">
                <CustomerSelector />
                {!selectedCustomerId && (
                  <p className="px-3 pt-1 text-xs text-warning">Select a company to open company pages</p>
                )}
              </div>
            )}
            {section.links.map((link) => {
              const Icon = link.icon
              return (
                <NavItem
                  key={link.path}
                  to={link.path}
                  icon={<Icon />}
                  label={link.label}
                  onClick={onNavigate}
                  bypassAccessCheck={link.bypassAccessCheck}
                />
              )
            })}
          </div>
        ))}

        {isAdministrator && hasAccess('/settings') && (
          <div className="space-y-0.5 border-t border-sidebar-border pt-4">
            <NavItem
              to="/settings"
              icon={<SettingsIcon />}
              label="Role access"
              onClick={onNavigate}
            />
          </div>
        )}
    </div>
	)
}