import { cn } from '@/lib/utils'

interface SegmentedProps<T extends string> {
	value: T
	options: readonly T[] | ReadonlyArray<{ value: T; label: string }>
	onChange: (value: T) => void
	ariaLabel: string
	className?: string
	size?: 'sm' | 'default'
}

/** A compact single-choice switch (e.g. Daily / Weekly / Monthly). */
export function Segmented<T extends string>({ value, options, onChange, ariaLabel, className, size = 'default' }: SegmentedProps<T>) {
	const items = options.map((o) => (typeof o === 'string' ? { value: o as T, label: o as string } : o))
	return (
		<div
			role="radiogroup"
			aria-label={ariaLabel}
			className={cn('inline-flex items-center rounded-md border border-border bg-muted/70 p-0.5', className)}
		>
			{items.map((item) => {
				const active = item.value === value
				return (
					<button
						key={item.value}
						type="button"
						role="radio"
						aria-checked={active}
						onClick={() => onChange(item.value)}
						className={cn(
							'!rounded-[5px] !border-0 whitespace-nowrap font-medium transition-colors',
							size === 'sm' ? '!px-2.5 !py-1 !text-xs' : '!px-3 !py-1 !text-[13px]',
							active
								? 'bg-card text-foreground shadow-[0_1px_2px_hsl(var(--foreground)/0.08)]'
								: 'text-muted-foreground hover:text-foreground',
						)}
					>
						{item.label}
					</button>
				)
			})}
		</div>
	)
}
