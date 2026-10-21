import { useMemo } from 'react'
import { cn } from '@/lib/utils'

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'] as const
const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const
/** Trading-day blocks; overnight is grouped because few stores trade then. */
const SLOTS = [
	{ label: '06–09', from: 6, to: 9 },
	{ label: '09–12', from: 9, to: 12 },
	{ label: '12–15', from: 12, to: 15 },
	{ label: '15–18', from: 15, to: 18 },
	{ label: '18–21', from: 18, to: 21 },
	{ label: '21–24', from: 21, to: 24 },
	{ label: 'Night', from: 0, to: 6 },
] as const

const slotFor = (hour: number) => SLOTS.findIndex((s) => hour >= s.from && hour < s.to)

export interface WeekHourGridProps {
	/** When each incident happened. */
	times: Date[]
	className?: string
}

/**
 * When incidents happen, by day of week and time of day. The darkest cell is
 * when store detective cover pays off most.
 */
export function WeekHourGrid({ times, className }: WeekHourGridProps) {
	const { grid, max, peak, total } = useMemo(() => {
		const g = DAYS.map(() => SLOTS.map(() => 0))
		let n = 0
		for (const t of times) {
			if (Number.isNaN(t.getTime())) continue
			const day = (t.getDay() + 6) % 7
			const slot = slotFor(t.getHours())
			if (slot < 0) continue
			g[day][slot] += 1
			n += 1
		}
		let m = 0
		let p: { day: number; slot: number } | null = null
		g.forEach((row, d) =>
			row.forEach((v, s) => {
				if (v > m) {
					m = v
					p = { day: d, slot: s }
				}
			}),
		)
		return { grid: g, max: m, peak: p as { day: number; slot: number } | null, total: n }
	}, [times])

	if (total === 0) return null

	return (
		<div className={cn('space-y-3', className)}>
			<div className="overflow-x-auto">
				<table className="w-full min-w-[420px] border-separate" style={{ borderSpacing: 3 }}>
					<caption className="sr-only">
						Incidents by day of week and time of day.
						{peak ? ` Busiest: ${DAY_NAMES[peak.day]} ${SLOTS[peak.slot].label}, ${max} incidents.` : ''}
					</caption>
					<thead>
						<tr>
							<th scope="col" className="w-10" />
							{SLOTS.map((slot) => (
								<th key={slot.label} scope="col" className="figures pb-1 text-center text-[11px] font-medium text-muted-foreground">
									{slot.label}
								</th>
							))}
						</tr>
					</thead>
					<tbody>
						{DAYS.map((day, d) => (
							<tr key={day}>
								<th scope="row" className="pr-1 text-left text-xs font-medium text-muted-foreground">
									{day}
								</th>
								{SLOTS.map((slot, s) => {
									const v = grid[d][s]
									const strength = max ? v / max : 0
									const isPeak = peak && peak.day === d && peak.slot === s
									return (
										<td
											key={slot.label}
											title={`${DAY_NAMES[d]} ${slot.label}: ${v} ${v === 1 ? 'incident' : 'incidents'}`}
											className={cn(
												'figures h-9 rounded-[4px] text-center text-xs font-semibold transition-colors',
												v === 0 && 'bg-muted/60 text-transparent',
												strength > 0.55 ? 'text-primary-foreground' : 'text-foreground',
												isPeak && 'ring-2 ring-signal ring-offset-1 ring-offset-card',
											)}
											style={v ? { backgroundColor: `hsl(var(--primary) / ${0.12 + strength * 0.88})` } : undefined}
										>
											{v || '·'}
										</td>
									)
								})}
							</tr>
						))}
					</tbody>
				</table>
			</div>
			<div className="flex flex-wrap items-center justify-between gap-2 text-xs text-muted-foreground">
				{peak ? (
					<p>
						Busiest: <span className="font-semibold text-foreground">{DAY_NAMES[peak.day]} {SLOTS[peak.slot].label}</span> ({max}{' '}
						{max === 1 ? 'incident' : 'incidents'})
					</p>
				) : (
					<span />
				)}
				<span className="flex items-center gap-1.5" aria-hidden>
					Fewer
					<span className="h-2 w-16 rounded-full" style={{ background: 'linear-gradient(90deg, hsl(var(--primary) / 0.12), hsl(var(--primary)))' }} />
					More
				</span>
			</div>
		</div>
	)
}
