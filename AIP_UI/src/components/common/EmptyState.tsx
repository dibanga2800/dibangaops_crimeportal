import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface EmptyStateProps {
	icon?: ReactNode
	title: ReactNode
	description?: ReactNode
	action?: ReactNode
	className?: string
}

/** Quiet placeholder for panels with nothing to show; always says what to do next when it can. */
export function EmptyState({ icon, title, description, action, className }: EmptyStateProps) {
	return (
		<div className={cn('flex flex-col items-center justify-center gap-2 px-6 py-10 text-center', className)} role="status">
			{icon ? (
				<div className="mb-1 flex h-10 w-10 items-center justify-center rounded-full bg-muted text-muted-foreground [&>svg]:h-5 [&>svg]:w-5">
					{icon}
				</div>
			) : null}
			<p className="text-sm font-medium text-foreground">{title}</p>
			{description ? <p className="max-w-sm text-[13px] leading-5 text-muted-foreground">{description}</p> : null}
			{action ? <div className="mt-2">{action}</div> : null}
		</div>
	)
}
