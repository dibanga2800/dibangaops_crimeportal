import { Link } from 'react-router-dom'
import { ChevronRight } from 'lucide-react'
import { formatMoney } from '@/lib/money'
import { cn } from '@/lib/utils'

export interface StoreLeaderboardRow {
	store: string
	incidents: number
	lost: number
	recovered: number
	/** Weekly incident counts, oldest first. */
	trend: number[]
}

function Spark({ values }: { values: number[] }) {
	if (values.length < 2 || values.every((v) => v === 0)) return <span className="text-xs text-muted-foreground">—</span>
	const w = 64
	const h = 20
	const max = Math.max(...values, 1)
	const step = w / (values.length - 1)
	const d = values.map((v, i) => `${i ? 'L' : 'M'}${(i * step).toFixed(1)},${(h - 2 - (v / max) * (h - 4)).toFixed(1)}`).join(' ')
	const rising = values.slice(-3).reduce((a, b) => a + b, 0) > values.slice(0, 3).reduce((a, b) => a + b, 0)
	return (
		<svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} aria-hidden className="overflow-visible">
			<path d={d} fill="none" strokeWidth={1.5} strokeLinecap="round" strokeLinejoin="round" className={rising ? 'stroke-loss' : 'stroke-muted-foreground'} />
		</svg>
	)
}

/** Stores ranked by incidents, with what each is losing and its recent direction. */
export function StoreLeaderboard({ rows, className }: { rows: StoreLeaderboardRow[]; className?: string }) {
	const maxLost = Math.max(1, ...rows.map((r) => r.lost))
	return (
		<div className={cn('overflow-x-auto', className)}>
			<table className="w-full text-sm">
				<thead>
					<tr className="border-b border-border text-left text-xs text-muted-foreground">
						<th scope="col" className="py-2 pl-4 pr-2 font-medium sm:pl-5">Store</th>
						<th scope="col" className="px-2 py-2 text-right font-medium">Incidents</th>
						<th scope="col" className="hidden px-2 py-2 font-medium sm:table-cell">Lost</th>
						<th scope="col" className="hidden px-2 py-2 text-right font-medium md:table-cell">Recovered</th>
						<th scope="col" className="py-2 pl-2 pr-4 font-medium sm:pr-5">8 weeks</th>
					</tr>
				</thead>
				<tbody className="divide-y divide-border">
					{rows.map((row) => {
						const rate = row.lost + row.recovered > 0 ? Math.round((row.recovered / (row.lost + row.recovered)) * 100) : 0
						return (
							<tr key={row.store} className="group">
								<td className="py-2.5 pl-4 pr-2 sm:pl-5">
									<Link
										to={`/operations/incident-report?search=${encodeURIComponent(row.store)}`}
										className="inline-flex max-w-[14rem] items-center gap-1 truncate font-medium text-foreground hover:underline"
									>
										<span className="truncate">{row.store}</span>
										<ChevronRight className="h-3.5 w-3.5 shrink-0 opacity-0 transition-opacity group-hover:opacity-60" aria-hidden />
									</Link>
								</td>
								<td className="figures px-2 py-2.5 text-right font-semibold">{row.incidents}</td>
								<td className="hidden px-2 py-2.5 sm:table-cell">
									<div className="flex items-center gap-2">
										<div className="h-1.5 w-20 overflow-hidden rounded-full bg-muted" aria-hidden>
											<div className="h-full rounded-full bg-loss" style={{ width: `${Math.max(4, (row.lost / maxLost) * 100)}%` }} />
										</div>
										<span className="figures text-loss">{formatMoney(row.lost, 'headline')}</span>
									</div>
								</td>
								<td className="figures hidden px-2 py-2.5 text-right text-muted-foreground md:table-cell">
									<span className="text-recovered">{formatMoney(row.recovered, 'headline')}</span> · {rate}%
								</td>
								<td className="py-2.5 pl-2 pr-4 sm:pr-5">
									<Spark values={row.trend} />
								</td>
							</tr>
						)
					})}
				</tbody>
			</table>
		</div>
	)
}
