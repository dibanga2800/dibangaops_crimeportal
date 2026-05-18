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
	low: '#10b981',
	medium: '#f59e0b',
	high: '#ef4444',
	critical: '#dc2626',
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
		<Card className="w-full shadow-sm border-2">
			<CardHeader className="pb-4">
				<CardTitle className="flex items-center gap-2">
					<Users className="h-5 w-5" />
					Repeat Offender Analysis
				</CardTitle>
				<CardDescription>
					Track most active offenders, cross-store movements, and network patterns
				</CardDescription>
			</CardHeader>
			<CardContent className="pt-6">
				<Tabs defaultValue="offenders" className="w-full">
					<TabsList className="grid w-full grid-cols-3">
						<TabsTrigger value="offenders">Most Active</TabsTrigger>
						<TabsTrigger value="movements">Cross-Store Movement</TabsTrigger>
						<TabsTrigger value="network">Network Map</TabsTrigger>
					</TabsList>

					<TabsContent value="offenders" className="space-y-4">
						<div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
							<Card>
								<CardContent className="p-4">
									<div className="text-sm text-gray-500">Total Offenders</div>
									<div className="text-2xl font-bold">{data.totalOffenders}</div>
								</CardContent>
							</Card>
							<Card>
								<CardContent className="p-4">
									<div className="text-sm text-gray-500">Repeat (2+ incidents)</div>
									<div className="text-2xl font-bold">{repeatOffenderCount}</div>
								</CardContent>
							</Card>
							<Card>
								<CardContent className="p-4">
									<div className="text-sm text-gray-500">Multi-Store</div>
									<div className="text-2xl font-bold">
										{data.crossStoreMovements.length}
									</div>
								</CardContent>
							</Card>
						</div>

						<p className="text-xs text-muted-foreground">
							All identified offenders in the selected period (N/A and placeholders excluded).
							Single-incident offenders show as low risk.
						</p>

						<div className="border rounded-lg">
							<Table>
								<TableHeader>
									<TableRow>
										<TableHead>Offender Name</TableHead>
										<TableHead>Incidents</TableHead>
										<TableHead>Stores Targeted</TableHead>
										<TableHead>Total Value</TableHead>
										<TableHead>Risk Level</TableHead>
										<TableHead>Last Incident</TableHead>
										<TableHead className="text-right">Actions</TableHead>
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
											<TableCell>
												£{offender.totalValue.toFixed(2)}
											</TableCell>
											<TableCell>
												<Badge
													variant="outline"
													style={{
														borderColor: getRiskColor(offender.riskLevel),
														color: getRiskColor(offender.riskLevel),
													}}
												>
													{offender.riskLevel.toUpperCase()}
												</Badge>
											</TableCell>
											<TableCell className="text-sm text-gray-500">
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
											<div className="text-sm text-gray-500">Incident Count</div>
											<div className="text-xl font-bold">{selectedOffender.incidentCount}</div>
										</div>
										<div>
											<div className="text-sm text-gray-500">Stores Targeted</div>
											<div className="text-xl font-bold">{selectedOffender.storesTargeted.length}</div>
										</div>
										<div>
											<div className="text-sm text-gray-500">Total Value</div>
											<div className="text-xl font-bold">
												£{selectedOffender.totalValue.toFixed(2)}
											</div>
										</div>
										<div>
											<div className="text-sm text-gray-500">Risk Level</div>
											<Badge
												variant="outline"
												style={{
													borderColor: getRiskColor(selectedOffender.riskLevel),
													color: getRiskColor(selectedOffender.riskLevel),
												}}
											>
												{selectedOffender.riskLevel.toUpperCase()}
											</Badge>
										</div>
									</div>
									<div>
										<div className="text-sm text-gray-500 mb-2">Stores Targeted</div>
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
											<div className="text-sm text-gray-500">First Incident</div>
											<div className="font-medium">
												{new Date(selectedOffender.firstIncident).toLocaleDateString()}
											</div>
										</div>
										<div>
											<div className="text-sm text-gray-500">Last Incident</div>
											<div className="font-medium">
												{new Date(selectedOffender.lastIncident).toLocaleDateString()}
											</div>
										</div>
									</div>
									{(selectedOffender.incidents?.length ?? 0) > 0 && (
										<div>
											<div className="text-sm text-gray-500 mb-2">Linked incidents</div>
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
							<div className="rounded-lg border border-dashed p-8 text-center text-sm text-gray-500">
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
												className="flex flex-col gap-2 p-3 border rounded-lg hover:bg-gray-50 transition-colors sm:flex-row sm:items-start sm:gap-4"
											>
												<div className="text-sm text-gray-500 min-w-[140px] shrink-0">
													{move.dateTimeLabel ||
														(move.date
															? new Date(move.date).toLocaleDateString()
															: '—')}
												</div>
												<div className="flex-1 min-w-0 space-y-1">
													<div className="flex flex-wrap items-center gap-2">
														<MapPin className="h-4 w-4 text-gray-400 shrink-0" />
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
															<span className="text-xs text-gray-500">
																£{move.value.toFixed(2)} lost
															</span>
														)}
													</div>
													{move.stolenProductsSummary && (
														<p className="text-xs text-gray-600">
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
										<div className="text-sm text-gray-500">Total Nodes</div>
										<div className="text-2xl font-bold">{data.networkMap.nodes.length}</div>
									</CardContent>
								</Card>
								<Card>
									<CardContent className="p-4">
										<div className="text-sm text-gray-500">Total Connections</div>
										<div className="text-2xl font-bold">{data.networkMap.links.length}</div>
									</CardContent>
								</Card>
							</div>
							<div className="mt-3 flex flex-wrap items-center justify-center gap-4 text-sm text-gray-600">
								<div className="flex items-center gap-2">
									<div className="h-3 w-3 rounded-full bg-blue-500" />
									<span>Offenders</span>
								</div>
								<div className="flex items-center gap-2">
									<div className="h-3 w-3 rounded-full bg-green-500" />
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

