import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface FormSectionProps {
	title: ReactNode
	description?: ReactNode
	icon?: ReactNode
	children: ReactNode
	/** Classes for the field area, e.g. a grid. */
	className?: string
}

/**
 * A titled group of fields inside a form. Groups are separated by a rule
 * rather than boxed, so long forms read as one document instead of nested cards.
 */
export function FormSection({ title, description, icon, children, className }: FormSectionProps) {
	return (
		<section className="border-t border-border pt-5 first:border-t-0 first:pt-0">
			<div className="mb-4">
				<h3 className="flex items-center gap-2 text-sm font-semibold text-foreground">
					{icon ? <span className="text-muted-foreground [&>svg]:h-4 [&>svg]:w-4">{icon}</span> : null}
					{title}
				</h3>
				{description ? <p className="mt-0.5 text-[13px] text-muted-foreground">{description}</p> : null}
			</div>
			<div className={className}>{children}</div>
		</section>
	)
}

/** Save/cancel row that stays visible at the bottom of a scrolling dialog. */
export function FormActions({ children, className }: { children: ReactNode; className?: string }) {
	return (
		<div
			className={cn(
				'form-actions sticky bottom-0 z-10 flex flex-col-reverse gap-2 border-t border-border bg-card/95 py-3 backdrop-blur sm:flex-row sm:items-center sm:justify-end',
				className,
			)}
		>
			{children}
		</div>
	)
}
