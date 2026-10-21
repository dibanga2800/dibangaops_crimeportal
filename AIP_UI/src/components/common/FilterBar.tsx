import { useState, type ReactNode } from 'react'
import { CalendarRange, SlidersHorizontal, X } from 'lucide-react'
import { cn } from '@/lib/utils'

interface FilterBarProps {
	children: ReactNode
	/** Controls aligned to the right end of the bar (apply, clear, export). */
	actions?: ReactNode
	/** Chips describing what is filtered, shown under the controls. */
	summary?: ReactNode
	/**
	 * The one control that stays visible on phones (usually search). When set,
	 * the other controls fold behind a "Filters" button below the sm breakpoint.
	 */
	primary?: ReactNode
	/** Number of filters currently applied, shown on the phone "Filters" button. */
	activeCount?: number
	className?: string
	label?: string
}

/**
 * One horizontal toolbar for page-level filters. Every data page filters in the
 * same place and the same way: controls wrap left to right, actions sit at the
 * end, and an optional summary line says what is currently applied. On phones
 * the controls fold away behind a single button so results stay above the fold.
 */
export function FilterBar({ children, actions, summary, primary, activeCount = 0, className, label = 'Filters' }: FilterBarProps) {
	const [open, setOpen] = useState(false)
	const collapsible = Boolean(primary)

	return (
		<section aria-label={label} className={cn('rounded-lg border border-border bg-card', className)}>
			<div className="flex flex-col gap-2 p-2.5 lg:flex-row lg:items-center">
				{collapsible && (
					<div className="flex items-center gap-2 sm:hidden">
						<div className="min-w-0 flex-1">{primary}</div>
						<button
							type="button"
							onClick={() => setOpen((v) => !v)}
							aria-expanded={open}
							className="!h-9 !gap-1.5 !border-input !px-3 !text-sm !font-medium shadow-sm"
							style={{ borderWidth: 1, background: 'hsl(var(--card))' }}
						>
							<SlidersHorizontal className="h-4 w-4" aria-hidden />
							Filters
							{activeCount > 0 && (
								<span className="figures flex h-5 min-w-[1.25rem] items-center justify-center rounded-full bg-primary px-1 text-[11px] font-semibold text-primary-foreground">
									{activeCount}
								</span>
							)}
						</button>
					</div>
				)}
				<div className={cn('min-w-0 flex-1 flex-wrap items-center gap-2', collapsible ? (open ? 'flex' : 'hidden sm:flex') : 'flex')}>
					{collapsible && <div className="hidden sm:contents">{primary}</div>}
					{children}
				</div>
				{actions ? (
					<div
						className={cn(
							'shrink-0 flex-wrap items-center gap-2 lg:justify-end',
							collapsible ? (open ? 'flex' : 'hidden sm:flex') : 'flex',
						)}
					>
						{actions}
					</div>
				) : null}
			</div>
			{summary ? (
				<div className="flex flex-wrap items-center gap-1.5 border-t border-border px-3 py-2 text-xs text-muted-foreground">
					{summary}
				</div>
			) : null}
		</section>
	)
}

interface DateRangeFieldProps {
	from: string
	to: string
	onFromChange: (value: string) => void
	onToChange: (value: string) => void
	className?: string
	fromLabel?: string
	toLabel?: string
	min?: string
	max?: string
}

/** Two native date inputs joined into one control, so a range reads as one thing. */
export function DateRangeField({
	from,
	to,
	onFromChange,
	onToChange,
	className,
	fromLabel = 'From date',
	toLabel = 'To date',
	min,
	max,
}: DateRangeFieldProps) {
	const input =
		'h-[34px] min-w-0 flex-1 rounded-none border-0 bg-transparent px-2 text-sm shadow-none focus:outline-none focus:ring-0 [color-scheme:inherit]'
	return (
		<div
			className={cn(
				'flex h-9 w-full items-center rounded-md border border-input bg-card pl-2.5 transition-colors focus-within:border-ring focus-within:ring-2 focus-within:ring-ring/20 hover:border-foreground/30 sm:w-auto',
				className,
			)}
		>
			<CalendarRange className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
			<input
				type="date"
				value={from}
				min={min}
				max={to || max}
				onChange={(e) => onFromChange(e.target.value)}
				aria-label={fromLabel}
				className={cn(input, 'sm:w-[8.75rem] sm:flex-none')}
				style={{ borderWidth: 0, boxShadow: 'none' }}
			/>
			<span className="px-0.5 text-xs text-muted-foreground" aria-hidden>
				to
			</span>
			<input
				type="date"
				value={to}
				min={from || min}
				max={max}
				onChange={(e) => onToChange(e.target.value)}
				aria-label={toLabel}
				className={cn(input, 'sm:w-[8.75rem] sm:flex-none')}
				style={{ borderWidth: 0, boxShadow: 'none' }}
			/>
		</div>
	)
}

interface FilterChipProps {
	children: ReactNode
	onRemove?: () => void
	removeLabel?: string
}

/** A single applied filter in the FilterBar summary line. */
export function FilterChip({ children, onRemove, removeLabel = 'Remove filter' }: FilterChipProps) {
	return (
		<span className="inline-flex items-center gap-1 rounded-full border border-border bg-muted/60 py-0.5 pl-2.5 pr-1 text-xs font-medium text-foreground">
			{children}
			{onRemove ? (
				<button
					type="button"
					onClick={onRemove}
					aria-label={removeLabel}
					className="!rounded-full !border-0 !p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground"
				>
					<X className="h-3 w-3" />
				</button>
			) : (
				<span className="w-1.5" />
			)}
		</span>
	)
}
