/**
 * Resource Deployment Engine Module
 * 
 * Displays best time/day recommendations for officer deployment,
 * officer type suggestions (uniform / store detectives), LPM recommendations, and store risk rankings.
 */

import { Fragment, useMemo, useState, useCallback } from 'react'
import { Badge } from '@/components/ui/badge'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import { Segmented } from '@/components/common/Segmented'
import type { DeploymentRecommendation, StoreRiskRanking } from '@/types/analytics'
import { cn } from '@/lib/utils'
import {
	Shield,
	Clock,
	TrendingUp,
	TrendingDown,
	Minus,
	AlertTriangle,
	Users,
	ChevronDown,
	ChevronRight,
	MapPin,
} from 'lucide-react'

interface ResourceDeploymentEngineProps {
	data: DeploymentRecommendation
	loading?: boolean
}

const RISK_BG: Record<string, string> = { low: 'bg-recovered', medium: 'bg-warning', high: 'bg-loss', critical: 'bg-loss' }
const RISK_TEXT: Record<string, string> = { low: 'text-recovered', medium: 'text-warning', high: 'text-loss', critical: 'text-loss' }
const PRIORITY_BG = RISK_BG

const getTrendIcon = (trend: string) => {
	switch (trend) {
		case 'increasing':
			return <TrendingUp className="h-4 w-4 text-loss" />
		case 'decreasing':
			return <TrendingDown className="h-4 w-4 text-recovered" />
		default:
			return <Minus className="h-4 w-4 text-muted-foreground" />
	}
}

const getTrendLabel = (trend: string) => {
	switch (trend) {
		case 'increasing':
			return 'Rising vs prior 30 days'
		case 'decreasing':
			return 'Falling vs prior 30 days'
		default:
			return 'Stable vs prior 30 days'
	}
}

const getStoreRowKey = (store: StoreRiskRanking, index: number) =>
	`${store.storeId}-${store.storeName}-${index}`

interface StoreRiskDetailPanelProps {
	store: StoreRiskRanking
	rank: number
}

const StoreRiskDetailPanel = ({ store, rank }: StoreRiskDetailPanelProps) => {
	const riskPercent = Math.round(store.riskScore * 100)

	return (
		<div
			id={`store-risk-detail-${rank}`}
			className="space-y-4 rounded-md border border-border bg-card p-4 sm:p-5"
			role="region"
			aria-label={`Risk details for ${store.storeName}`}
		>
			<div className="flex flex-wrap items-start justify-between gap-3">
				<div>
					<h4 className="font-semibold text-foreground flex items-center gap-2">
						<MapPin className="h-4 w-4 text-muted-foreground" />
						{store.storeName}
					</h4>
					<p className="text-sm text-muted-foreground mt-1">
						Priority rank #{rank} · {store.incidentCount} incident
						{store.incidentCount !== 1 ? 's' : ''} in period
					</p>
				</div>
				<span className={cn('figures text-sm font-semibold capitalize', RISK_TEXT[store.riskLevel] ?? 'text-foreground')}>
					{store.riskLevel} · {riskPercent}/100
				</span>
			</div>

			{store.reason && (
				<div>
					<p className="text-xs font-medium text-muted-foreground mb-1">
						Summary
					</p>
					<p className="text-sm text-foreground leading-relaxed">{store.reason}</p>
				</div>
			)}

			{store.riskFactors && store.riskFactors.length > 0 && (
				<div>
					<p className="text-xs font-medium text-muted-foreground mb-2">
						How the risk score was calculated
					</p>
					<div className="rounded-md border border-border overflow-hidden">
						<Table>
							<TableHeader>
								<TableRow>
									<TableHead className="w-[140px]">Factor</TableHead>
									<TableHead className="w-[72px]">Weight</TableHead>
									<TableHead>Evidence from data</TableHead>
								</TableRow>
							</TableHeader>
							<TableBody>
								{store.riskFactors.map((factor) => (
									<TableRow key={factor.factor}>
										<TableCell className="font-medium capitalize">
											{factor.factor.replace(/_/g, ' ')}
										</TableCell>
										<TableCell>{(factor.score * 100).toFixed(0)}%</TableCell>
										<TableCell className="text-sm text-muted-foreground">
											{factor.description}
										</TableCell>
									</TableRow>
								))}
							</TableBody>
						</Table>
					</div>
				</div>
			)}

			<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
				<div className="rounded-md border border-border bg-background p-3">
					<p className="text-xs font-medium text-muted-foreground mb-1">Trend</p>
					<div className="flex items-center gap-2 text-sm">
						{getTrendIcon(store.trend)}
						<span>{getTrendLabel(store.trend)}</span>
					</div>
				</div>
				<div className="rounded-md border border-border bg-background p-3">
					<p className="text-xs font-medium text-muted-foreground mb-1">Deployment</p>
					<p className="text-sm">{store.recommendedOfficerType}</p>
					<p className="text-xs text-muted-foreground mt-1">
						{store.recommendedLPM ? 'LPM recommended' : 'LPM not required'}
					</p>
				</div>
				<div className="rounded-md border border-border bg-background p-3">
					<p className="text-xs font-medium text-muted-foreground mb-1">Peak hours</p>
					{store.recommendedHours.length > 0 ? (
						<div className="flex flex-wrap gap-1">
							{store.recommendedHours.map((hour) => (
								<Badge key={hour} variant="secondary" className="text-xs">
									{hour}
								</Badge>
							))}
						</div>
					) : (
						<p className="text-sm text-muted-foreground">No peak hours recorded</p>
					)}
				</div>
			</div>

		</div>
	)
}

export const ResourceDeploymentEngine = ({
	data,
	loading = false,
}: ResourceDeploymentEngineProps) => {
	const criticalRecommendations = useMemo(() => {
		return data.bestTimes
			.filter((r) => r.priority === 'critical')
			.sort((a, b) => b.expectedIncidents - a.expectedIncidents)
	}, [data.bestTimes])

	const highPriorityRecommendations = useMemo(() => {
		return data.bestTimes
			.filter((r) => r.priority === 'high')
			.sort((a, b) => b.expectedIncidents - a.expectedIncidents)
	}, [data.bestTimes])

	const recommendationsByDay = useMemo(() => {
		const grouped: Record<string, typeof data.bestTimes> = {}
		data.bestTimes.forEach((rec) => {
			if (!grouped[rec.day]) {
				grouped[rec.day] = []
			}
			grouped[rec.day].push(rec)
		})
		return grouped
	}, [data.bestTimes])

	const [expandedStoreKey, setExpandedStoreKey] = useState<string | null>(null)

	const handleStoreRowClick = useCallback((rowKey: string) => {
		setExpandedStoreKey((current) => (current === rowKey ? null : rowKey))
	}, [])

	const handleStoreRowKeyDown = useCallback(
		(event: React.KeyboardEvent, rowKey: string) => {
			if (event.key === 'Enter' || event.key === ' ') {
				event.preventDefault()
				handleStoreRowClick(rowKey)
			}
		},
		[handleStoreRowClick]
	)

	const [view, setView] = useState<'schedule' | 'ranking' | 'by-day'>('schedule')
	const scheduled = useMemo(
		() => [...criticalRecommendations.slice(0, 5), ...highPriorityRecommendations.slice(0, 10)],
		[criticalRecommendations, highPriorityRecommendations],
	)

	if (loading) {
		return <div className="h-[420px] animate-pulse rounded-lg border border-border bg-card" aria-busy="true" aria-label="Loading deployment recommendations" />
	}

	const officerChips = (type: string, lpm?: boolean) => (
		<span className="flex flex-wrap items-center gap-1.5">
			<span className="rounded border border-border px-1.5 py-0.5 text-xs font-medium capitalize text-foreground">{type}</span>
			{lpm ? <span className="rounded border border-border px-1.5 py-0.5 text-xs font-medium text-foreground">LPM</span> : null}
		</span>
	)

	return (
		<section className="rounded-lg border border-border bg-card">
			<div className="flex flex-wrap items-start justify-between gap-3 p-4 sm:p-5">
				<div>
					<h3 className="flex items-center gap-2 text-base font-semibold text-foreground">
						<Shield className="h-4 w-4 text-muted-foreground" aria-hidden />
						Where and when to deploy
					</h3>
					<p className="mt-0.5 text-[13px] text-muted-foreground">Store detective cover and store risk, from incidents in the selected period.</p>
				</div>
				<Segmented<'schedule' | 'ranking' | 'by-day'>
					ariaLabel="Deployment view"
					size="sm"
					value={view}
					onChange={setView}
					options={[
						{ value: 'schedule', label: 'Priority times' },
						{ value: 'ranking', label: 'Store ranking' },
						{ value: 'by-day', label: 'By day' },
					]}
				/>
			</div>

			{data.overallStrategy ? (
				<figure className="mx-4 mb-4 border-l-2 border-primary pl-4 sm:mx-5">
					<blockquote className="text-[15px] leading-relaxed text-foreground">{data.overallStrategy}</blockquote>
					<figcaption className="mt-1 text-xs text-muted-foreground">
						Recommended strategy · updated {new Date(data.lastUpdated).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' })}
					</figcaption>
				</figure>
			) : null}

			{view === 'schedule' && (
				<div className="border-t border-border">
					{scheduled.length === 0 ? (
						<p className="px-4 py-8 text-center text-sm text-muted-foreground sm:px-5">No critical or high priority times in this period.</p>
					) : (
						<ul className="divide-y divide-border">
							{scheduled.map((rec, index) => {
								const critical = rec.priority === 'critical'
								return (
									<li key={`${rec.day}-${rec.hourLabel}-${index}`} className="grid gap-x-6 gap-y-2 px-4 py-3.5 sm:px-5 md:grid-cols-[9rem_minmax(0,1fr)_auto] md:items-center">
										<div className="flex items-center gap-3 md:block">
											<p className="text-sm font-semibold text-foreground">{rec.day}</p>
											<p className="figures text-sm text-muted-foreground">{rec.hourLabel}</p>
										</div>
										<div className="min-w-0 space-y-1.5">
											<div className="flex flex-wrap items-center gap-2">
												<span
													className={cn(
														'inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-semibold',
														critical ? 'bg-loss-soft text-loss' : 'bg-warning-soft text-warning',
													)}
												>
													<span className={cn('h-1.5 w-1.5 rounded-full', critical ? 'bg-loss' : 'bg-warning')} aria-hidden />
													{critical ? 'Critical' : 'High'}
												</span>
												{officerChips(rec.officerType, rec.recommendedLPM)}
												<span className="figures flex items-center gap-1 text-xs text-muted-foreground">
													<Users className="h-3.5 w-3.5" aria-hidden />
													{rec.recommendedOfficers} {rec.recommendedOfficers === 1 ? 'officer' : 'officers'}
												</span>
											</div>
											<p className="text-[13px] leading-5 text-muted-foreground">{rec.reason}</p>
										</div>
										<p className="figures text-sm md:text-right">
											<span className={cn('font-display text-xl font-semibold', critical ? 'text-loss' : 'text-foreground')}>~{rec.expectedIncidents}</span>
											<span className="ml-1 text-xs text-muted-foreground">incidents expected</span>
										</p>
									</li>
								)
							})}
						</ul>
					)}
				</div>
			)}

			{view === 'ranking' && (
				<div className="border-t border-border">
					<p className="px-4 pt-3 text-[13px] text-muted-foreground sm:px-5">Select a store to see what drives its score.</p>
					<div className="mt-2 overflow-x-auto">
						<Table>
							<TableHeader>
								<TableRow>
									<TableHead className="w-8" aria-label="Expand" />
									<TableHead className="w-12">Rank</TableHead>
									<TableHead>Store</TableHead>
									<TableHead className="min-w-[10rem]">Risk</TableHead>
									<TableHead className="text-right">Incidents</TableHead>
									<TableHead>Trend</TableHead>
									<TableHead>Cover</TableHead>
									<TableHead>Hours</TableHead>
								</TableRow>
							</TableHeader>
							<TableBody>
								{data.storeRankings.map((store, index) => {
									const rowKey = getStoreRowKey(store, index)
									const isExpanded = expandedStoreKey === rowKey
									const rank = index + 1
									return (
										<Fragment key={rowKey}>
											<TableRow
												tabIndex={0}
												role="button"
												aria-expanded={isExpanded}
												aria-controls={`store-risk-detail-${rank}`}
												onClick={() => handleStoreRowClick(rowKey)}
												onKeyDown={(event) => handleStoreRowKeyDown(event, rowKey)}
												className={cn(
													'cursor-pointer transition-colors hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
													isExpanded && 'bg-muted/40',
												)}
											>
												<TableCell className="text-muted-foreground">
													{isExpanded ? <ChevronDown className="h-4 w-4" aria-hidden /> : <ChevronRight className="h-4 w-4" aria-hidden />}
												</TableCell>
												<TableCell className="figures text-muted-foreground">{rank}</TableCell>
												<TableCell className="font-medium">{store.storeName}</TableCell>
												<TableCell>
													<div className="flex items-center gap-2.5">
														<div className="h-1.5 w-20 overflow-hidden rounded-full bg-muted">
															<div className={cn('h-full rounded-full', RISK_BG[store.riskLevel] ?? 'bg-recovered')} style={{ width: `${Math.min(store.riskScore * 100, 100)}%` }} />
														</div>
														<span className="figures w-7 text-right text-sm font-semibold">{(store.riskScore * 100).toFixed(0)}</span>
														<span className={cn('text-xs font-semibold capitalize', RISK_TEXT[store.riskLevel] ?? 'text-muted-foreground')}>{store.riskLevel}</span>
													</div>
												</TableCell>
												<TableCell className="figures text-right">{store.incidentCount}</TableCell>
												<TableCell>
													<span className="flex items-center gap-1.5" title={getTrendLabel(store.trend)}>
														{getTrendIcon(store.trend)}
														<span className="sr-only">{getTrendLabel(store.trend)}</span>
													</span>
												</TableCell>
												<TableCell>{officerChips(store.recommendedOfficerType, store.recommendedLPM)}</TableCell>
												<TableCell>
													<span className="figures text-sm text-muted-foreground">
														{store.recommendedHours.slice(0, 3).join(', ')}
														{store.recommendedHours.length > 3 ? ` +${store.recommendedHours.length - 3}` : ''}
													</span>
												</TableCell>
											</TableRow>
											{isExpanded && (
												<TableRow className="hover:bg-transparent">
													<TableCell colSpan={8} className="bg-muted/20 p-3 sm:p-4">
														<StoreRiskDetailPanel store={store} rank={rank} />
													</TableCell>
												</TableRow>
											)}
										</Fragment>
									)
								})}
							</TableBody>
						</Table>
					</div>
				</div>
			)}

			{view === 'by-day' && (
				<div className="grid gap-3 border-t border-border p-4 sm:p-5 md:grid-cols-2">
					{Object.entries(recommendationsByDay).map(([day, recommendations]) => (
						<div key={day} className="rounded-md border border-border">
							<div className="flex items-baseline justify-between border-b border-border bg-muted/40 px-3 py-2">
								<p className="text-sm font-semibold text-foreground">{day}</p>
								<p className="figures text-xs text-muted-foreground">
									{recommendations.length} {recommendations.length === 1 ? 'slot' : 'slots'}
								</p>
							</div>
							<ul className="divide-y divide-border">
								{[...recommendations]
									.sort((a, b) => b.priority.localeCompare(a.priority))
									.map((rec, index) => (
										<li key={index} className="flex items-start gap-3 px-3 py-2.5">
											<span className={cn('mt-1.5 h-2 w-2 shrink-0 rounded-full', PRIORITY_BG[rec.priority] ?? 'bg-muted-foreground')} title={`${rec.priority} priority`} />
											<div className="min-w-0 flex-1">
												<div className="flex flex-wrap items-center gap-2">
													<span className="figures text-sm font-medium text-foreground">{rec.hourLabel}</span>
													{officerChips(rec.officerType, rec.recommendedLPM)}
													<span className="figures text-xs text-muted-foreground">{rec.recommendedOfficers} officers</span>
												</div>
												<p className="mt-0.5 text-xs text-muted-foreground">{rec.reason}</p>
											</div>
										</li>
									))}
							</ul>
						</div>
					))}
				</div>
			)}
		</section>
	)
}
