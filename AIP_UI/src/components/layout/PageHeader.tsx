import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface PageHeaderProps {
	title: ReactNode
	description?: ReactNode
	/** Buttons or controls aligned to the right of the title on wide screens. */
	actions?: ReactNode
	/** Small inline context next to the title, e.g. a scope badge. */
	meta?: ReactNode
	/** Optional element above the title, such as a back link. */
	leading?: ReactNode
	className?: string
}

/**
 * Standard page heading used by every screen inside the app shell.
 * Title and description sit left; actions wrap below on narrow screens.
 */
export function PageHeader({ title, description, actions, meta, leading, className }: PageHeaderProps) {
	return (
		<header className={cn('flex flex-col gap-4 pb-6 md:flex-row md:items-end md:justify-between', className)}>
			<div className="min-w-0 space-y-1.5">
				{leading}
				<div className="flex flex-wrap items-center gap-x-3 gap-y-1">
					<h1 className="text-[1.75rem] font-semibold leading-[1.1] tracking-tight text-foreground sm:text-[2.125rem]">
						{title}
					</h1>
					{meta}
				</div>
				{description ? (
					<p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">{description}</p>
				) : null}
			</div>
			{actions ? <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div> : null}
		</header>
	)
}

interface SectionHeadingProps {
	title: ReactNode
	description?: ReactNode
	actions?: ReactNode
	icon?: ReactNode
	className?: string
}

/** Heading for a panel or a group of panels inside a page. */
export function SectionHeading({ title, description, actions, icon, className }: SectionHeadingProps) {
	return (
		<div className={cn('flex flex-wrap items-start justify-between gap-3', className)}>
			<div className="min-w-0">
				<h2 className="flex items-center gap-2 text-base font-semibold leading-6 text-foreground">
					{icon ? <span className="text-muted-foreground [&>svg]:h-4 [&>svg]:w-4">{icon}</span> : null}
					{title}
				</h2>
				{description ? <p className="mt-0.5 text-[13px] leading-5 text-muted-foreground">{description}</p> : null}
			</div>
			{actions ? <div className="flex shrink-0 items-center gap-2">{actions}</div> : null}
		</div>
	)
}
