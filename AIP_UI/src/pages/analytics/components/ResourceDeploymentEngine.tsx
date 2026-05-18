/**
 * Resource Deployment Engine Module
 * 
 * Displays best time/day recommendations for officer deployment,
 * officer type suggestions (uniform / store detectives), LPM recommendations, and store risk rankings.
 */

import { Fragment, useMemo, useState, useCallback } from 'react'
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
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

const PRIORITY_COLORS = {
	low: '#10b981',
	medium: '#f59e0b',
	high: '#ef4444',
	critical: '#dc2626',
}

const RISK_COLORS = {
	low: '#10b981',
	medium: '#f59e0b',
	high: '#ef4444',
	critical: '#dc2626',
}

const OFFICER_TYPE_COLORS = {
	'store detectives': '#8b5cf6',
}

const LPM_COLOR = '#3b82f6'

const getPriorityColor = (priority: string) => {
	return PRIORITY_COLORS[priority as keyof typeof PRIORITY_COLORS] || PRIORITY_COLORS.low
}

const getRiskColor = (riskLevel: string) => {
	return RISK_COLORS[riskLevel as keyof typeof RISK_COLORS] || RISK_COLORS.low
}

const getTrendIcon = (trend: string) => {
	switch (trend) {
		case 'increasing':
			return <TrendingUp className="h-4 w-4 text-red-500" />
		case 'decreasing':
			return <TrendingDown className="h-4 w-4 text-green-500" />
		default:
			return <Minus className="h-4 w-4 text-gray-500" />
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
			className="rounded-lg border border-border bg-muted/30 p-4 sm:p-5 space-y-4"
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
				<Badge
					variant="outline"
					style={{
						borderColor: getRiskColor(store.riskLevel),
						color: getRiskColor(store.riskLevel),
					}}
				>
					{store.riskLevel.toUpperCase()} · {riskPercent}/100
				</Badge>
			</div>

			{store.reason && (
				<div>
					<p className="text-xs font-medium uppercase tracking-wide text-muted-foreground mb-1">
						Summary
					</p>
					<p className="text-sm text-foreground leading-relaxed">{store.reason}</p>
				</div>
			)}

			{store.riskFactors && store.riskFactors.length > 0 && (
				<div>
					<p className="text-xs font-medium uppercase tracking-wide text-muted-foreground mb-2">
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

	if (loading) {
		return (
			<Card>
				<CardHeader>
					<CardTitle>Resource Deployment Engine</CardTitle>
					<CardDescription>Loading deployment recommendations...</CardDescription>
				</CardHeader>
			</Card>
		)
	}

	return (
		<Card className="w-full shadow-sm border-2">
			<CardHeader className="pb-4">
				<CardTitle className="flex items-center gap-2">
					<Shield className="h-5 w-5" />
					Resource Deployment Engine
				</CardTitle>
				<CardDescription>
					Deployment times and store risk from incidents in the selected date range
				</CardDescription>
			</CardHeader>
			<CardContent className="pt-6">
				{/* Strategy Summary */}
				<Card className="mb-6 bg-blue-50 border-blue-200">
					<CardHeader>
						<CardTitle className="text-base flex items-center gap-2">
							<AlertTriangle className="h-4 w-4 text-blue-600" />
							Overall Strategy
						</CardTitle>
					</CardHeader>
					<CardContent>
						<p className="text-sm text-gray-700">{data.overallStrategy}</p>
						<p className="text-xs text-gray-500 mt-2">
							Last updated: {new Date(data.lastUpdated).toLocaleString()}
						</p>
					</CardContent>
				</Card>

				<Tabs defaultValue="recommendations" className="w-full">
					<TabsList className="grid w-full grid-cols-3">
						<TabsTrigger value="recommendations">Time Recommendations</TabsTrigger>
						<TabsTrigger value="risk-ranking">Store Risk Ranking</TabsTrigger>
						<TabsTrigger value="by-day">Recommendations by Day</TabsTrigger>
					</TabsList>

					<TabsContent value="recommendations" className="space-y-4">
						{/* Critical Priority */}
						{criticalRecommendations.length > 0 && (
							<div>
								<h3 className="font-semibold mb-3 flex items-center gap-2 text-red-600">
									<AlertTriangle className="h-4 w-4" />
									Critical Priority Deployments
								</h3>
								<div className="space-y-2">
									{criticalRecommendations.slice(0, 5).map((rec, index) => (
										<Card key={index} className="border-red-200 bg-red-50">
											<CardContent className="p-4">
												<div className="flex items-center justify-between">
													<div className="flex items-center gap-4">
													<div>
														<div className="font-semibold">{rec.day}</div>
														<div className="text-sm text-gray-600">{rec.hourLabel}</div>
													</div>
													<div className="flex gap-2">
														<Badge
															style={{
																backgroundColor: OFFICER_TYPE_COLORS[rec.officerType],
																color: 'white',
															}}
														>
															{rec.officerType}
														</Badge>
														{rec.recommendedLPM && (
															<Badge
																style={{
																	backgroundColor: LPM_COLOR,
																	color: 'white',
																}}
															>
																LPM
															</Badge>
														)}
													</div>
													<div className="text-sm">
														<span className="font-medium">{rec.recommendedOfficers}</span>{' '}
														officers
													</div>
													<p
														className="text-sm text-gray-600 max-w-md line-clamp-2"
														title={rec.reason}
													>
														{rec.reason}
													</p>
													</div>
													<div className="text-right">
														<div className="text-sm font-semibold text-red-600">
															~{rec.expectedIncidents} incidents expected
														</div>
													</div>
												</div>
											</CardContent>
										</Card>
									))}
								</div>
							</div>
						)}

						{/* High Priority */}
						{highPriorityRecommendations.length > 0 && (
							<div>
								<h3 className="font-semibold mb-3 flex items-center gap-2 text-orange-600">
									<Clock className="h-4 w-4" />
									High Priority Deployments
								</h3>
								<div className="border rounded-lg">
									<Table>
										<TableHeader>
											<TableRow>
												<TableHead>Day</TableHead>
												<TableHead>Time</TableHead>
												<TableHead>Officer Type</TableHead>
												<TableHead>Recommended</TableHead>
												<TableHead>Expected Incidents</TableHead>
												<TableHead>Reason</TableHead>
											</TableRow>
										</TableHeader>
										<TableBody>
											{highPriorityRecommendations.slice(0, 10).map((rec, index) => (
												<TableRow key={index}>
													<TableCell className="font-medium">{rec.day}</TableCell>
													<TableCell>{rec.hourLabel}</TableCell>
													<TableCell>
														<div className="flex gap-2 flex-wrap">
															<Badge
																variant="outline"
																style={{
																	borderColor: OFFICER_TYPE_COLORS[rec.officerType],
																	color: OFFICER_TYPE_COLORS[rec.officerType],
																}}
															>
																{rec.officerType}
															</Badge>
															{rec.recommendedLPM && (
																<Badge
																	variant="outline"
																	style={{
																		borderColor: LPM_COLOR,
																		color: LPM_COLOR,
																	}}
																>
																	LPM
																</Badge>
															)}
														</div>
													</TableCell>
													<TableCell>
														<div className="flex items-center gap-1">
															<Users className="h-4 w-4" />
															{rec.recommendedOfficers}
														</div>
													</TableCell>
													<TableCell>{rec.expectedIncidents}</TableCell>
													<TableCell className="text-sm text-gray-600">
														{rec.reason}
													</TableCell>
												</TableRow>
											))}
										</TableBody>
									</Table>
								</div>
							</div>
						)}
					</TabsContent>

					<TabsContent value="risk-ranking" className="space-y-6 mt-6">
						<p className="text-sm text-muted-foreground">
							Click a store for risk breakdown and deployment detail.
						</p>
						<div className="border rounded-lg overflow-hidden">
							<Table>
								<TableHeader>
									<TableRow>
										<TableHead className="w-10" aria-label="Expand" />
										<TableHead>Rank</TableHead>
										<TableHead>Store</TableHead>
										<TableHead>Risk Score</TableHead>
										<TableHead>Risk Level</TableHead>
										<TableHead>Incidents</TableHead>
										<TableHead>Trend</TableHead>
										<TableHead>Officer Type</TableHead>
										<TableHead>Recommended Hours</TableHead>
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
														'cursor-pointer transition-colors hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
														isExpanded && 'bg-muted/40'
													)}
												>
													<TableCell className="text-muted-foreground">
														{isExpanded ? (
															<ChevronDown className="h-4 w-4" aria-hidden />
														) : (
															<ChevronRight className="h-4 w-4" aria-hidden />
														)}
													</TableCell>
													<TableCell className="font-bold">#{rank}</TableCell>
													<TableCell className="font-medium">{store.storeName}</TableCell>
											<TableCell>
												<div className="flex items-center gap-2">
													<div className="w-24 bg-gray-200 rounded-full h-2">
														<div
															className="h-2 rounded-full"
															style={{
																width: `${Math.min(store.riskScore * 100, 100)}%`,
																backgroundColor: getRiskColor(store.riskLevel),
															}}
														/>
													</div>
													<span className="text-sm font-medium">{(store.riskScore * 100).toFixed(0)}</span>
												</div>
											</TableCell>
											<TableCell>
												<Badge
													variant="outline"
													style={{
														borderColor: getRiskColor(store.riskLevel),
														color: getRiskColor(store.riskLevel),
													}}
												>
													{store.riskLevel.toUpperCase()}
												</Badge>
											</TableCell>
											<TableCell>{store.incidentCount}</TableCell>
											<TableCell>{getTrendIcon(store.trend)}</TableCell>
											<TableCell>
												<div className="flex gap-2 flex-wrap">
													<Badge
														variant="outline"
														style={{
															borderColor: OFFICER_TYPE_COLORS[store.recommendedOfficerType],
															color: OFFICER_TYPE_COLORS[store.recommendedOfficerType],
														}}
													>
														{store.recommendedOfficerType}
													</Badge>
													{store.recommendedLPM && (
														<Badge
															variant="outline"
															style={{
																borderColor: LPM_COLOR,
																color: LPM_COLOR,
															}}
														>
															LPM
														</Badge>
													)}
												</div>
											</TableCell>
											<TableCell>
												<div className="flex flex-wrap gap-1">
													{store.recommendedHours.slice(0, 3).map((hour) => (
														<Badge key={hour} variant="secondary" className="text-xs">
															{hour}
														</Badge>
													))}
													{store.recommendedHours.length > 3 && (
														<Badge variant="secondary" className="text-xs">
															+{store.recommendedHours.length - 3}
														</Badge>
													)}
												</div>
											</TableCell>
												</TableRow>
												{isExpanded && (
													<TableRow key={`${rowKey}-detail`} className="hover:bg-transparent">
														<TableCell colSpan={9} className="p-3 sm:p-4 bg-muted/20">
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
					</TabsContent>

					<TabsContent value="by-day" className="space-y-6 mt-6">
						{Object.entries(recommendationsByDay).map(([day, recommendations]) => (
							<Card key={day}>
								<CardHeader>
									<CardTitle className="text-base">{day}</CardTitle>
									<CardDescription>
										{recommendations.length} deployment recommendations
									</CardDescription>
								</CardHeader>
								<CardContent>
									<div className="space-y-2">
										{recommendations
											.sort((a, b) => b.priority.localeCompare(a.priority))
											.map((rec, index) => (
												<div
													key={index}
													className="flex items-center justify-between p-3 border rounded-lg hover:bg-gray-50 transition-colors"
												>
													<div className="flex items-center gap-4">
														<div className="text-sm font-medium">{rec.hourLabel}</div>
														<div className="flex gap-2">
															<Badge
																style={{
																	backgroundColor: OFFICER_TYPE_COLORS[rec.officerType],
																	color: 'white',
																}}
															>
																{rec.officerType}
															</Badge>
															{rec.recommendedLPM && (
																<Badge
																	style={{
																		backgroundColor: LPM_COLOR,
																		color: 'white',
																	}}
																>
																	LPM
																</Badge>
															)}
														</div>
														<div className="text-sm">
															{rec.recommendedOfficers} officers
														</div>
														<div className="text-xs text-gray-600">{rec.reason}</div>
													</div>
													<Badge
														variant="outline"
														style={{
															borderColor: getPriorityColor(rec.priority),
															color: getPriorityColor(rec.priority),
														}}
													>
														{rec.priority}
													</Badge>
												</div>
											))}
									</div>
								</CardContent>
							</Card>
						))}
					</TabsContent>
				</Tabs>
			</CardContent>
		</Card>
	)
}

