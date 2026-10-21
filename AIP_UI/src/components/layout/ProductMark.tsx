import { Link } from 'react-router-dom'
import { ShieldCheck } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ProductMarkProps {
	className?: string
	/** "dark" for the ink sidebar, "light" for paper surfaces. */
	tone?: 'dark' | 'light'
	to?: string
}

/**
 * DibangOps product lockup — the same shield-on-ink mark used on the sign-in
 * screen, so the product reads as one system from login to every page.
 */
export function ProductMark({ className, tone = 'dark', to = '/dashboard' }: ProductMarkProps) {
	return (
		<Link
			to={to}
			className={cn(
				'group flex min-w-0 items-center gap-2.5 rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-signal',
				className,
			)}
		>
			<span
				className={cn(
					'flex h-8 w-8 shrink-0 items-center justify-center rounded-md',
					tone === 'dark' ? 'bg-white/[0.07] ring-1 ring-inset ring-white/10' : 'bg-ink',
				)}
			>
				<ShieldCheck className="h-4 w-4 text-signal" aria-hidden="true" />
			</span>
			<span className="min-w-0 leading-tight">
				<span className={cn('block truncate text-sm font-semibold', tone === 'dark' ? 'text-white' : 'text-foreground')}>
					DibangOps
				</span>
				<span className={cn('block truncate text-xs', tone === 'dark' ? 'text-sidebar-foreground' : 'text-muted-foreground')}>
					Crime Portal
				</span>
			</span>
		</Link>
	)
}
