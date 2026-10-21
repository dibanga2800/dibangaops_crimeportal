/**
 * Repeat Offender Analysis Module
 * 
 * Displays most active offenders, cross-store movement patterns,
 * and offender network map visualization.
 */

import { useMemo, useState } from 'react'
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { SUBTABS_LIST, SUBTABS_TRIGGER, RISK_CHIP } from './moduleStyles'
import type { RepeatOffenderData } from '@/types/analytics'
import { formatOffenderDisplayName } from '../offenderDisplay'
import { LinkedIncidentProducts } from './LinkedIncidentProducts'
import { OffenderNetworkGraph } from './OffenderNetworkGraph'
import {
	Users,
	MapPin,
	Network,
	AlertTriangle,
	TrendingUp,
} from 'lucide-react'

interface RepeatOffenderAnalysisProps {
	data: RepeatOffenderData
	loading?: boolean
}

const RISK_COLORS = {
	low: '#0E7C66',
	medium: '#C4680F',
	high: '#B42318',
	critical: '#8F1D14',
}

const getRiskColor = (riskLevel: string) => {
	return RISK_COLORS[riskLevel as keyof typeof RISK_COLORS] || RISK_COLORS.low
}

export const RepeatOffenderAnalysis = ({
	data,
	loading = false,
}: RepeatOffenderAnalysisProps) => {
	const [selectedOffenderId, setSelectedOffenderId] = useState<string | null>(null)

	const repeatOffenderCount = useMemo(
		() => data.mostActive.filter((o) => o.incidentCount >= 2).length,
		[data.mostActive],
	)

	const selectedOffender = useMemo(() => {
		if (!selectedOffenderId) return null
		return data.mostActive.find((o) => o.offenderId === selectedOffenderId) || null
	}, [selectedOffenderId, data.mostActive])

	const selectedOffenderMovements = useMemo(() => {
		if (!selectedOffenderId) return null
		return data.crossStoreMovements.find((m) => m.offenderId === selectedOffenderId) || null
	}, [selectedOffenderId, data.crossStoreMovements])

	if (loading) {
		return (
			<Card>
				<CardHeader>
					<CardTitle>Repeat Offender Analysis</CardTitle>
					<CardDescription>Loading offender data...</CardDescription>
				</CardHeader>
			</Card>
		)
	}

	return (
		<Card className="w-full">
			<CardHeader className="p-4 pb-3 sm:p-5 sm:pb-3">
				<CardTitle className="flex items-center gap-2 text-base">
					<Users className="h-4 w-4 text-muted-foreground" aria-hidden />
					Repeat offenders
				</CardTitle>
				<CardDescription>
					The most active offenders, who moves between stores, and who is seen together.
				</CardDescription>
			</CardHeader>
			<CardContent className="p-4 pt-0 sm:p-5 sm:pt-0">
				<Tabs defaultValue="offenders" className="w-full">
					<TabsList className={SUBTABS_LIST}>
						<TabsTrigger className={SUBTABS_TRIGGER} value="offenders">Most active</TabsTrigger>
						<TabsTrigger className={SUBTABS_TRIGGER} value="movements">Across stores</TabsTrigger>
						<TabsTrigger className={SUBTABS_TRIGGER} value="network">Network</TabsTrigger>
					</TabsList>

					<TabsContent value="offenders" className="space-y-4">
						<dl className="mb-4 grid grid-cols-3 overflow-hidden rounded-md border border-border">
	<div className="px-3 py-2.5 shadow-[1px_0_0_0_hsl(var(--border))] sm:px-4">
		<dt className="truncate text-xs text-muted-foreground">Total offenders</dt>
		<dd className="figures font-display text-xl font-semibold text-foreground">{data.totalOffenders}</dd>
	</div>
	<div className="px-3 py-2.5 shadow-[1px_0_0_0_hsl(var(--border))] sm:px-4">
		<dt className="truncate text-xs text-muted-foreground">Repeat (2+ incidents)</dt>
		<dd className="figures font-display text-xl font-semibold text-foreground">{repeatOffenderCount}</dd>
	</div>
	<div className="px-3 py-2.5 shadow-[1px_0_0_0_hsl(var(--border))] sm:px-4">
		<dt className="truncate text-xs text-muted-foreground">Multi-store</dt>
		<dd className="figures font-display text-xl font-semibold text-foreground">{data.crossStoreMovements.length}</dd>
	</div>
</dl>

						<p className="text-xs text-muted-foreground">
							All identified offenders in the selected period (N/A and placeholders excluded).
							Single-incident offenders show as low risk.
						</p>

						<div className="border rounded-lg">
							<Table>
								<TableHeader>
									<TableRow>
										<TableHead>Offender</TableHead>
										<TableHead>Incidents</TableHead>
										<TableHead>Stores</TableHead>
										<TableHead className="text-right">Value</TableHead>
										<TableHead>Risk</TableHead>
										<TableHead>Last seen</TableHead>
										<TableHead><span className="sr-only">Actions</span></TableHead>
									</TableRow>
								</TableHeader>
								<TableBody>
									{data.mostActive.length === 0 && (
										<TableRow>
											<TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
												No identified offenders in the selected period.
											</TableCell>
										</TableRow>
									)}
									{data.mostActive.map((offender) => (
									<TableRow key={offender.offenderId}>
											<TableCell className="font-medium">
												{formatOffenderDisplayName(offender.name, offender.offenderId)}
											</TableCell>
											<TableCell>
												<Badge variant="outline">
													{offender.incidentCount}
													{offender.incidentCount < 2 ? ' (single)' : ''}
												</Badge>
											</TableCell>
											<TableCell>
												<div className="flex flex-wrap gap-1">
													{offender.storesTargeted.slice(0, 3).map((store) => (
														<Badge key={store} variant="secondary" className="text-xs">
															{store}
														</Badge>
													))}
													{offender.storesTargeted.length > 3 && (
														<Badge variant="secondary" className="text-xs">
															+{offender.storesTargeted.length - 3}
														</Badge>
													)}
												</div>
											</TableCell>
											<TableCell className="figures text-right">
												£{offender.totalValue.toFixed(2)}
											</TableCell>
											<TableCell>
												<span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${RISK_CHIP[offender.riskLevel] ?? RISK_CHIP.low}`}>{offender.riskLevel}</span>
											</TableCell>
											<TableCell className="text-sm text-muted-foreground">
												{new Date(offender.lastIncident).toLocaleDateString()}
											</TableCell>
											<TableCell className="text-right">
												<Button
													variant="ghost"
													size="sm"
													onClick={() => setSelectedOffenderId(offender.offenderId)}
												>
													View Details
												</Button>
											</TableCell>
										</TableRow>
									))}
								</TableBody>
							</Table>
						</div>

						{selectedOffender && (
							<Card className="mt-4">
								<CardHeader>
									<CardTitle>
										{formatOffenderDisplayName(
											selectedOffender.name,
											selectedOffender.offenderId,
										)}
									</CardTitle>
									<CardDescription>Detailed offender profile</CardDescription>
								</CardHeader>
								<CardContent className="space-y-4">
									<div className="grid grid-cols-2 md:grid-cols-4 gap-4">
										<div>
											<div className="text-sm text-muted-foreground">Incident Count</div>
											<div className="text-xl font-bold">{selectedOffender.incidentCount}</div>
										</div>
										<div>
											<div className="text-sm text-muted-foreground">Stores Targeted</div>
											<div className="text-xl font-bold">{selectedOffender.storesTargeted.length}</div>
										</div>
										<div>
											<div className="text-sm text-muted-foreground">Total Value</div>
											<div className="text-xl font-bold">
												£{selectedOffender.totalValue.toFixed(2)}
											</div>
										</div>
										<div>
											<div className="text-sm text-muted-foreground">Risk Level</div>
											<span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${RISK_CHIP[selectedOffender.riskLevel] ?? RISK_CHIP.low}`}>{selectedOffender.riskLevel}</span>
										</div>
									</div>
									<div>
										<div className="text-sm text-muted-foreground mb-2">Stores Targeted</div>
										<div className="flex flex-wrap gap-2">
											{selectedOffender.storesTargeted.map((store) => (
												<Badge key={store} variant="secondary">
													{store}
												</Badge>
											))}
										</div>
									</div>
									<div className="grid grid-cols-2 gap-4">
										<div>
											<div className="text-sm text-muted-foreground">First Incident</div>
											<div className="font-medium">
												{new Date(selectedOffender.firstIncident).toLocaleDateString()}
											</div>
										</div>
										<div>
											<div className="text-sm text-muted-foreground">Last Incident</div>
											<div className="font-medium">
												{new Date(selectedOffender.lastIncident).toLocaleDateString()}
											</div>
										</div>
									</div>
									{(selectedOffender.incidents?.length ?? 0) > 0 && (
										<div>
											<div className="text-sm text-muted-foreground mb-2">Linked incidents</div>
											<div className="border rounded-lg overflow-x-auto">
												<Table>
													<TableHeader>
														<TableRow>
															<TableHead>Date & time</TableHead>
															<TableHead>Store</TableHead>
															<TableHead>Stolen products</TableHead>
															<TableHead>Type</TableHead>
															<TableHead className="text-right">Value</TableHead>
														</TableRow>
													</TableHeader>
													<TableBody>
														{selectedOffender.incidents?.map((incident) => (
															<TableRow key={incident.incidentId}>
																<TableCell className="text-sm whitespace-nowrap">
																	{incident.dateTimeLabel || incident.date}
																</TableCell>
																<TableCell>{incident.storeName || '—'}</TableCell>
																<TableCell>
																	<LinkedIncidentProducts
																		summary={incident.stolenProductsSummary}
																		products={incident.stolenProducts}
																	/>
																</TableCell>
																<TableCell>
																	<Badge variant="secondary" className="text-xs">
																		{incident.incidentType}
																	</Badge>
																</TableCell>
																<TableCell className="text-right">
																	£{incident.value.toFixed(2)}
																</TableCell>
															</TableRow>
														))}
													</TableBody>
												</Table>
											</div>
										</div>
									)}
									<Button
										variant="outline"
										onClick={() => setSelectedOffenderId(null)}
									>
										Close Details
									</Button>
								</CardContent>
							</Card>
						)}
					</TabsContent>

					<TabsContent value="movements" className="space-y-6 mt-6">
						{data.crossStoreMovements.length === 0 && (
							<div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
								No offenders with activity across multiple stores in this period.
							</div>
						)}
						{data.crossStoreMovements.map((movement) => (
							<Card key={movement.offenderId}>
								<CardHeader>
									<div className="flex items-center justify-between">
										<div>
											<CardTitle className="text-base">
												{formatOffenderDisplayName(movement.offenderName, movement.offenderId)}
											</CardTitle>
											<CardDescription>{movement.offenderId}</CardDescription>
										</div>
										<Badge variant="outline">
											{movement.totalStores} stores
										</Badge>
									</div>
								</CardHeader>
								<CardContent>
									<div className="space-y-3">
										{movement.movements.map((move, index) => {
										const storeLabel =
											move.storeName || move.toStore || move.fromStore || '—'
										const showTransition =
											Boolean(move.previousStore || (move.fromStore && move.toStore))

										return (
											<div
												key={move.incidentId || `${movement.offenderId}-${index}`}
												className="flex flex-col gap-2 p-3 border rounded-lg hover:bg-muted/50 transition-colors sm:flex-row sm:items-start sm:gap-4"
											>
												<div className="text-sm text-muted-foreground min-w-[140px] shrink-0">
													{move.dateTimeLabel ||
														(move.date
															? new Date(move.date).toLocaleDateString()
															: '—')}
												</div>
												<div className="flex-1 min-w-0 space-y-1">
													<div className="flex flex-wrap items-center gap-2">
														<MapPin className="h-4 w-4 text-muted-foreground/80 shrink-0" />
														<span className="font-medium">{storeLabel}</span>
														{showTransition && (
															<Badge variant="outline" className="text-xs">
																Store change
																{move.previousStore
																	? ` from ${move.previousStore}`
																	: ''}
															</Badge>
														)}
														<Badge variant="secondary" className="text-xs">
															{move.incidentType}
														</Badge>
														{typeof move.value === 'number' && move.value > 0 && (
															<span className="text-xs text-muted-foreground">
																£{move.value.toFixed(2)} lost
															</span>
														)}
													</div>
													{move.stolenProductsSummary && (
														<p className="text-xs text-muted-foreground">
															{move.stolenProductsSummary}
														</p>
													)}
												</div>
											</div>
										)
									})}
									</div>
								</CardContent>
							</Card>
						))}
					</TabsContent>

					<TabsContent value="network" className="space-y-6 mt-6">
						<Card>
							<CardHeader>
								<CardTitle className="flex items-center gap-2">
									<Network className="h-5 w-5" />
									Offender Network Visualization
								</CardTitle>
								<CardDescription>
									Visual representation of connections between offenders and stores
								</CardDescription>
							</CardHeader>
							<CardContent>
							<OffenderNetworkGraph data={data.networkMap} />
							<div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
								<Card>
									<CardContent className="p-4">
										<div className="text-sm text-muted-foreground">Total Nodes</div>
										<div className="text-2xl font-bold">{data.networkMap.nodes.length}</div>
									</CardContent>
								</Card>
								<Card>
									<CardContent className="p-4">
										<div className="text-sm text-muted-foreground">Total Connections</div>
										<div className="text-2xl font-bold">{data.networkMap.links.length}</div>
									</CardContent>
								</Card>
							</div>
							<div className="mt-3 flex flex-wrap items-center justify-center gap-4 text-sm text-muted-foreground">
								<div className="flex items-center gap-2">
									<div className="h-3 w-3 rounded-full bg-primary" />
									<span>Offenders</span>
								</div>
								<div className="flex items-center gap-2">
									<div className="h-3 w-3 rounded-full bg-recovered" />
									<span>Stores</span>
								</div>
							</div>
							</CardContent>
						</Card>
					</TabsContent>
				</Tabs>
			</CardContent>
		</Card>
	)
}

