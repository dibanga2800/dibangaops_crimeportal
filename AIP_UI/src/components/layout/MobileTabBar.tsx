import React from 'react'
import { Link, useLocation } from 'react-router-dom'
import { BarChart3, FileWarning, LayoutGrid, Menu, Plus } from 'lucide-react'
import { PageAccessContext } from '@/contexts/PageAccessContext'
import { cn } from '@/lib/utils'

interface MobileTabBarProps {
	onOpenMenu: () => void
}

/**
 * Phone-only bottom navigation. Store teams mostly use the portal on a phone
 * to log incidents, so "New report" sits in the thumb zone at the centre.
 * Every destination respects page access; "Menu" opens the full sidebar.
 */
export function MobileTabBar({ onOpenMenu }: MobileTabBarProps) {
	const location = useLocation()
	const pageAccess = React.useContext(PageAccessContext)
	const can = (path: string) => (pageAccess ? pageAccess.hasAccess(path) : false)

	const items = [
		{ to: '/dashboard', label: 'Home', icon: LayoutGrid, show: can('/dashboard'), active: location.pathname === '/' || location.pathname === '/dashboard' },
		{ to: '/operations/incident-report', label: 'Incidents', icon: FileWarning, show: can('/operations/incident-report'), active: location.pathname.startsWith('/operations/incident-report') },
		{ to: '/analytics/data-analytics-hub', label: 'Analytics', icon: BarChart3, show: can('/analytics/data-analytics-hub'), active: location.pathname.startsWith('/analytics') },
	].filter((item) => item.show)

	const canReport = can('/operations/incident-report')
	const left = items.slice(0, 2)
	const right = items.slice(2)

	const tab = (item: (typeof items)[number]) => {
		const Icon = item.icon
		return (
			<Link
				key={item.to}
				to={item.to}
				aria-current={item.active ? 'page' : undefined}
				className={cn(
					'flex flex-1 flex-col items-center justify-center gap-1 py-1.5 text-[11px] font-medium transition-colors',
					item.active ? 'text-foreground' : 'text-muted-foreground hover:text-foreground',
				)}
			>
				<span className={cn('flex h-7 w-12 items-center justify-center rounded-full', item.active && 'bg-accent text-accent-foreground')}>
					<Icon className="h-[18px] w-[18px]" aria-hidden />
				</span>
				{item.label}
			</Link>
		)
	}

	return (
		<nav
			aria-label="Quick navigation"
			className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-card/95 pb-[env(safe-area-inset-bottom)] backdrop-blur supports-[backdrop-filter]:bg-card/85 lg:hidden"
		>
			<div className="mx-auto flex h-16 max-w-md items-stretch px-2">
				{left.map(tab)}
				{canReport && (
					<div className="flex flex-1 items-start justify-center">
						<Link
							to="/operations/incident-report?open=new"
							aria-label="New incident report"
							className="-mt-5 flex h-14 w-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-float ring-4 ring-background transition-transform active:scale-95"
						>
							<Plus className="h-6 w-6" aria-hidden />
						</Link>
					</div>
				)}
				{right.map(tab)}
				<button
					type="button"
					onClick={onOpenMenu}
					className="!flex flex-1 !flex-col items-center !justify-center gap-1 !rounded-none !border-0 !px-0 !py-1.5 text-[11px] font-medium text-muted-foreground hover:text-foreground"
				>
					<span className="flex h-7 w-12 items-center justify-center rounded-full">
						<Menu className="h-[18px] w-[18px]" aria-hidden />
					</span>
					Menu
				</button>
			</div>
		</nav>
	)
}
