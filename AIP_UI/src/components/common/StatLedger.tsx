import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

export type StatTone = 'neutral' | 'loss' | 'recovered' | 'warning' | 'info' | 'signal'

export interface StatDelta {
	/** Percentage change, e.g. 18 for +18%. */
	value: number
	/** Whether an increase is good news (recovery) or bad news (loss, incidents). */
	goodWhenUp: boolean
	/** What the change is measured against, e.g. "previous 4 weeks". */
	against: string
}

export interface StatLedgerItem {
	label: ReactNode
	value: ReactNode
	/** One short line under the value: period, comparison, share. */
	hint?: ReactNode
	tone?: StatTone
	icon?: ReactNode
	/** Recent values, oldest first, drawn as a small trend line. */
	trend?: number[]
	delta?: StatDelta
	onClick?: () => void
	/** Accessible name when the cell is clickable. */
	ariaLabel?: string
}

const toneTick: Record<StatTone, string> = {
	neutral: 'bg-foreground/70',
	loss: 'bg-loss',
	recovered: 'bg-recovered',
	warning: 'bg-warning',
	info: 'bg-info',
	signal: 'bg-signal',
}

const toneValue: Record<StatTone, string> = {
	neutral: 'text-foreground',
	loss: 'text-loss',
	recovered: 'text-recovered',
	warning: 'text-warning',
	info: 'text-foreground',
	signal: 'text-foreground',
}

const toneStroke: Record<StatTone, string> = {
	neutral: 'stroke-foreground/60',
	loss: 'stroke-loss',
	recovered: 'stroke-recovered',
	warning: 'stroke-warning',
	info: 'stroke-info',
	signal: 'stroke-primary',
}

const toneFill: Record<StatTone, string> = {
	neutral: 'fill-foreground/[0.06]',
	loss: 'fill-loss/10',
	recovered: 'fill-recovered/10',
	warning: 'fill-warning/10',
	info: 'fill-info/10',
	signal: 'fill-primary/10',
}

const colsFor = (n: number) =>
	n <= 2
		? 'sm:grid-cols-2'
		: n === 3
			? 'sm:grid-cols-3'
			: n === 4
				? 'grid-cols-2 xl:grid-cols-4'
				: n === 6
					? 'grid-cols-2 md:grid-cols-3 xl:grid-cols-6'
					: 'sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5'

function Sparkline({ values, tone, fluid = false }: { values: number[]; tone: StatTone; fluid?: boolean }) {
	if (values.length < 2) return null
	const w = 96
	const h = 28
	const max = Math.max(...values)
	const min = Math.min(...values)
	const span = max - min || 1
	const step = w / (values.length - 1)
	const points = values.map((v, i) => [i * step, h - 2 - ((v - min) / span) * (h - 4)] as const)
	const line = points.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)},${y.toFixed(1)}`).join(' ')
	const area = `${line} L${w},${h} L0,${h} Z`
	const [lx, ly] = points[points.length - 1]
	return (
		<svg
			viewBox={`0 0 ${w} ${h}`}
			width={fluid ? '100%' : w}
			height={fluid ? 22 : h}
			preserveAspectRatio={fluid ? 'none' : undefined}
			className="shrink-0 overflow-visible"
			aria-hidden
		>
			<path d={area} className={toneFill[tone]} />
			<path
				d={line}
				fill="none"
				strokeWidth={1.75}
				strokeLinejoin="round"
				strokeLinecap="round"
				vectorEffect="non-scaling-stroke"
				className={toneStroke[tone]}
			/>
			{fluid ? null : <circle cx={lx} cy={ly} r={2.5} className={cn(toneStroke[tone], 'fill-card')} strokeWidth={1.75} />}
		</svg>
	)
}

function DeltaText({ delta }: { delta: StatDelta }) {
	const up = delta.value > 0
	const flat = Math.round(delta.value) === 0
	const good = flat ? null : up === delta.goodWhenUp
	return (
		<span className="truncate text-xs text-muted-foreground">
			<span
				className={cn(
					'figures font-semibold',
					good === null ? 'text-muted-foreground' : good ? 'text-recovered' : 'text-loss',
				)}
			>
				{flat ? 'No change' : `${up ? '▲' : '▼'} ${Math.abs(Math.round(delta.value))}%`}
			</span>{' '}
			on {delta.against}
		</span>
	)
}

interface StatLedgerProps {
	items: StatLedgerItem[]
	className?: string
	/** Compact cells for secondary pages. */
	size?: 'default' | 'compact'
	loading?: boolean
}

/**
 * The headline figures for a page, set as one joined ledger rather than
 * separate coloured blocks: condensed figures, a tone tick, and (when the page
 * has history) a small trend line and a change against the previous period.
 */
export function StatLedger({ items, className, size = 'default', loading = false }: StatLedgerProps) {
	// Five or more cells get narrow on laptops: the trend line moves under the figure.
	const dense = items.length >= 5
	return (
		<section
			className={cn(
				'grid overflow-hidden rounded-lg border border-border bg-card',
				items.length === 4 || items.length === 6 ? '' : 'grid-cols-1',
				colsFor(items.length),
				className,
			)}
		>
			{items.map((item, index) => {
				const tone = item.tone ?? 'neutral'
				const Comp = item.onClick ? 'button' : 'div'
				return (
					<Comp
						key={index}
						type={item.onClick ? 'button' : undefined}
						onClick={item.onClick}
						aria-label={item.ariaLabel}
						className={cn(
							// 1px rule drawn right/below each cell; the outer edges are clipped by the frame.
							'group relative flex min-w-0 flex-col items-stretch justify-start text-left shadow-[1px_1px_0_0_hsl(var(--border))]',
							size === 'compact' ? 'gap-1 px-4 py-3.5' : 'gap-2 px-4 py-4 sm:px-5 sm:py-5',
							item.onClick && 'rounded-none transition-colors hover:bg-muted/50',
						)}
						style={item.onClick ? { borderRadius: 0, fontWeight: 'inherit' } : undefined}
					>
						<span className="flex items-center gap-2 text-[13px] font-medium text-muted-foreground">
							<span aria-hidden className={cn('h-3 w-1 shrink-0 rounded-full', toneTick[tone])} />
							<span className="truncate">{item.label}</span>
							{item.icon ? (
								<span className="ml-auto text-muted-foreground/60 transition-colors group-hover:text-muted-foreground [&>svg]:h-4 [&>svg]:w-4">
									{item.icon}
								</span>
							) : null}
						</span>
						<span className="flex items-end justify-between gap-3">
							{loading ? (
								<span className="mt-1 h-8 w-24 animate-pulse rounded bg-muted" />
							) : (
								<span
									className={cn(
										'figures min-w-0 truncate font-display font-semibold leading-none tracking-tight',
										size === 'compact' ? 'text-2xl' : dense ? 'text-[1.75rem] sm:text-[2rem]' : 'text-[1.75rem] sm:text-[2.25rem]',
										toneValue[tone],
									)}
								>
									{item.value}
								</span>
							)}
							{!loading && item.trend && !dense ? (
								<span className="hidden sm:block">
									<Sparkline values={item.trend} tone={tone} />
								</span>
							) : null}
						</span>
						{!loading && item.trend && dense ? (
							<span className="hidden sm:block">
								<Sparkline values={item.trend} tone={tone} fluid />
							</span>
						) : null}
						{item.delta ? (
							<DeltaText delta={item.delta} />
						) : item.hint ? (
							<span className="truncate text-xs text-muted-foreground">{item.hint}</span>
						) : null}
					</Comp>
				)
			})}
		</section>
	)
}
