import { ArrowRight, Clock, MapPin, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { formatMoney } from '@/lib/money'
import type { AnalyticsHubData } from '@/types/analytics'

export interface HubRiskSummary {
	overallLevel: string
	highCount: number
	mediumCount: number
	lowCount: number
	topStoreName: string
	topStoreLevelLabel: string
}

interface HubBriefingProps {
	data: AnalyticsHubData
	risk: HubRiskSummary | null
	insights: string[]
	periodLabel: string
	onOpenTab: (tab: string) => void
}

const PRIORITY_TONE: Record<string, string> = {
	critical: 'bg-loss-soft text-loss',
	high: 'bg-loss-soft text-loss',
	medium: 'bg-warning-soft text-warning',
	low: 'bg-recovered-soft text-recovered',
}

/**
 * The hub's opening read: one sentence on the period, the single most useful
 * action (where and when to put cover), and the three findings worth knowing.
 * Everything links into the tab that explains it.
 */
export function HubBriefing({ data, risk, insights, periodLabel, onOpenTab }: HubBriefingProps) {
	const { financialSummary: money, crimeTrends } = data
	const best = data.deploymentRecommendations?.bestTimes?.[0]
	const topStore = data.deploymentRecommendations?.storeRankings?.[0]
	const riskTotal = risk ? risk.highCount + risk.mediumCount + risk.lowCount : 0

	return (
		<section
			aria-labelledby="briefing-title"
			className="overflow-hidden rounded-xl border border-border bg-card"
		>
			<div className="grid lg:grid-cols-[minmax(0,7fr)_minmax(0,5fr)]">
				{/* Headline and actions */}
				<div className="relative p-5 sm:p-6">
					<div
						aria-hidden
						className="pointer-events-none absolute inset-0 bg-[radial-gradient(70%_80%_at_0%_0%,hsl(var(--primary)/0.07),transparent_70%)]"
					/>
					<p id="briefing-title" className="relative flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-[0.08em] text-primary">
						<Sparkles className="h-3.5 w-3.5" aria-hidden />
						Briefing, {periodLabel}
					</p>
					<p className="relative mt-2 max-w-2xl font-display text-xl font-semibold leading-snug tracking-tight text-foreground sm:text-[22px]">
						<span className="text-loss">{formatMoney(money.totalLostValue, 'headline')}</span> lost across{' '}
						{crimeTrends.totalIncidents.toLocaleString('en-GB')} incidents. Stores got back{' '}
						<span className="text-recovered">{money.recoveryRate.toFixed(0)}%</span> of what was taken.
					</p>

					<div className="relative mt-5 grid gap-3 sm:grid-cols-2">
						{best && (
							<div className="flex flex-col rounded-lg border border-primary/20 bg-accent/60 p-4">
								<div className="flex items-center justify-between gap-2">
									<span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
										<Clock className="h-3.5 w-3.5" aria-hidden />
										Recommended cover
									</span>
									<span className={cn('rounded-full px-2 py-0.5 text-[11px] font-semibold first-letter:uppercase', PRIORITY_TONE[best.priority])}>
										{best.priority} priority
									</span>
								</div>
								<p className="mt-2 font-display text-lg font-semibold text-foreground">
									{best.day}, {best.hourLabel}
								</p>
								<p className="mt-0.5 text-[13px] leading-5 text-muted-foreground">
									{best.recommendedOfficers} {best.recommendedOfficers === 1 ? 'store detective' : 'store detectives'}
									{best.recommendedLPM ? ' plus LPM' : ''}. {best.reason}
								</p>
								<Button
									size="sm"
									className="mt-3 self-start"
									onClick={() => onOpenTab('deployment')}
								>
									Open deployment plan
									<ArrowRight className="h-3.5 w-3.5" />
								</Button>
							</div>
						)}
						{topStore && (
							<div className="flex flex-col rounded-lg border border-loss/20 bg-loss-soft/50 p-4">
								<div className="flex items-center justify-between gap-2">
									<span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
										<MapPin className="h-3.5 w-3.5" aria-hidden />
										Store to watch
									</span>
									<span className={cn('rounded-full px-2 py-0.5 text-[11px] font-semibold first-letter:uppercase', PRIORITY_TONE[topStore.riskLevel])}>
										{topStore.riskLevel} risk
									</span>
								</div>
								<p className="mt-2 font-display text-lg font-semibold text-foreground">{topStore.storeName}</p>
								<p className="mt-0.5 text-[13px] leading-5 text-muted-foreground">
									Risk score {Math.round(topStore.riskScore)} of 100, {topStore.incidentCount} incidents
									{topStore.trend === 'increasing' ? ' and rising' : topStore.trend === 'decreasing' ? ' and falling' : ''}.
								</p>
								<Button variant="outline" size="sm" className="mt-3 self-start bg-card" onClick={() => onOpenTab('hot-products')}>
									See what's taken there
									<ArrowRight className="h-3.5 w-3.5" />
								</Button>
							</div>
						)}
					</div>
				</div>

				{/* Findings and risk spread */}
				<div className="flex flex-col border-t border-border bg-muted/30 p-5 sm:p-6 lg:border-l lg:border-t-0">
					<h2 className="text-sm font-semibold text-foreground">What stands out</h2>
					{insights.length > 0 ? (
						<ol className="mt-3 space-y-3">
							{insights.map((insight, index) => (
								<li key={insight} className="flex gap-3">
									<span className="figures mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-primary text-[11px] font-semibold text-primary-foreground">
										{index + 1}
									</span>
									<p className="text-sm leading-6 text-foreground">{insight}</p>
								</li>
							))}
						</ol>
					) : (
						<p className="mt-3 text-sm text-muted-foreground">Nothing unusual in this period.</p>
					)}

					{risk && riskTotal > 0 && (
						<div className="mt-auto pt-5">
							<div className="flex items-baseline justify-between gap-2 border-t border-border pt-4">
								<span className="text-xs font-medium text-muted-foreground">Store risk spread</span>
								<span className="figures text-xs text-muted-foreground">
									{riskTotal} {riskTotal === 1 ? 'store' : 'stores'}
								</span>
							</div>
							<div
								className="mt-2 flex h-2 gap-0.5 overflow-hidden rounded-full"
								role="img"
								aria-label={`${risk.highCount} high, ${risk.mediumCount} medium and ${risk.lowCount} low risk stores`}
							>
								<span className="bg-loss" style={{ width: `${(risk.highCount / riskTotal) * 100}%` }} />
								<span className="bg-warning" style={{ width: `${(risk.mediumCount / riskTotal) * 100}%` }} />
								<span className="bg-recovered" style={{ width: `${(risk.lowCount / riskTotal) * 100}%` }} />
							</div>
							<dl className="figures mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs">
								{[
									{ label: 'High', value: risk.highCount, dot: 'bg-loss' },
									{ label: 'Medium', value: risk.mediumCount, dot: 'bg-warning' },
									{ label: 'Low', value: risk.lowCount, dot: 'bg-recovered' },
								].map((row) => (
									<div key={row.label} className="flex items-center gap-1.5">
										<span className={cn('h-2 w-2 rounded-full', row.dot)} aria-hidden />
										<dt className="text-muted-foreground">{row.label}</dt>
										<dd className="font-semibold text-foreground">{row.value}</dd>
									</div>
								))}
							</dl>
						</div>
					)}
				</div>
			</div>
		</section>
	)
}
