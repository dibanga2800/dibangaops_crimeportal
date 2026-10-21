/**
 * Crime Linking Panel Module
 * 
 * Displays incident clusters linked to same offender/features
 * and chains of incidents showing patterns.
 */

import { useState, useMemo } from 'react'
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
import { SUBTABS_LIST, SUBTABS_TRIGGER } from './moduleStyles'
import type { CrimeLinkingData } from '@/types/analytics'
import { formatOffenderDisplayName } from '../offenderDisplay'
import { formatConfidencePercent, getClusterTitle } from '../crimeLinkingDisplay'
import { LinkedIncidentProducts } from './LinkedIncidentProducts'
import {
	Link as LinkIcon,
	Network,
	ArrowRight,
	Calendar,
	MapPin,
	DollarSign,
	Eye,
	EyeOff,
} from 'lucide-react'

interface CrimeLinkingPanelProps {
	data: CrimeLinkingData
	loading?: boolean
}

export const CrimeLinkingPanel = ({
	data,
	loading = false,
}: CrimeLinkingPanelProps) => {
	const [expandedClusters, setExpandedClusters] = useState<Set<string>>(new Set())
	const [expandedChains, setExpandedChains] = useState<Set<string>>(new Set())

	const toggleCluster = (clusterId: string) => {
		const newExpanded = new Set(expandedClusters)
		if (newExpanded.has(clusterId)) {
			newExpanded.delete(clusterId)
		} else {
			newExpanded.add(clusterId)
		}
		setExpandedClusters(newExpanded)
	}

	const toggleChain = (chainId: string) => {
		const newExpanded = new Set(expandedChains)
		if (newExpanded.has(chainId)) {
			newExpanded.delete(chainId)
		} else {
			newExpanded.add(chainId)
		}
		setExpandedChains(newExpanded)
	}

	const sortedClusters = useMemo(() => {
		return [...data.clusters].sort((a, b) => b.incidents.length - a.incidents.length)
	}, [data.clusters])

	const sortedChains = useMemo(() => {
		return [...data.offenderChains].sort((a, b) => b.incidents.length - a.incidents.length)
	}, [data.offenderChains])

	if (loading) {
		return (
			<Card>
				<CardHeader>
					<CardTitle>Crime Linking Panel</CardTitle>
					<CardDescription>Loading crime linking data...</CardDescription>
				</CardHeader>
			</Card>
		)
	}

	return (
		<Card className="w-full">
			<CardHeader className="p-4 pb-3 sm:p-5 sm:pb-3">
				<CardTitle className="flex items-center gap-2 text-base">
					<LinkIcon className="h-4 w-4 text-muted-foreground" aria-hidden />
					Linked incidents
				</CardTitle>
				<CardDescription>
					Incidents that look connected by offender, description or products, and the chains they form.
				</CardDescription>
			</CardHeader>
			<CardContent className="p-4 pt-0 sm:p-5 sm:pt-0">
				{/* Summary */}
				<dl className="mb-4 grid grid-cols-3 overflow-hidden rounded-md border border-border">
	<div className="px-3 py-2.5 shadow-[1px_0_0_0_hsl(var(--border))] sm:px-4">
		<dt className="truncate text-xs text-muted-foreground">Total clusters</dt>
		<dd className="figures font-display text-xl font-semibold text-foreground">{data.clusters.length}</dd>
	</div>
	<div className="px-3 py-2.5 shadow-[1px_0_0_0_hsl(var(--border))] sm:px-4">
		<dt className="truncate text-xs text-muted-foreground">Offender chains</dt>
		<dd className="figures font-display text-xl font-semibold text-foreground">{data.offenderChains.length}</dd>
	</div>
	<div className="px-3 py-2.5 shadow-[1px_0_0_0_hsl(var(--border))] sm:px-4">
		<dt className="truncate text-xs text-muted-foreground">Linked incidents</dt>
		<dd className="figures font-display text-xl font-semibold text-foreground">{data.totalLinkedIncidents}</dd>
	</div>
</dl>

				<Tabs defaultValue="clusters" className="w-full">
					<TabsList className={SUBTABS_LIST}>
						<TabsTrigger className={SUBTABS_TRIGGER} value="clusters">Clusters</TabsTrigger>
						<TabsTrigger className={SUBTABS_TRIGGER} value="chains">Offender chains</TabsTrigger>
					</TabsList>

					<TabsContent value="clusters" className="space-y-4">
						{sortedClusters.length === 0 && (
							<div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
								No incident clusters in the selected period.
							</div>
						)}
						{sortedClusters.map((cluster) => {
							const isExpanded = expandedClusters.has(cluster.clusterId)

							return (
								<Card key={cluster.clusterId}>
									<CardHeader>
										<div className="flex items-center justify-between">
											<div className="flex-1">
												<CardTitle className="text-base">{getClusterTitle(cluster)}</CardTitle>
												<CardDescription>
													{cluster.incidents.length} linked incidents •{' '}
													{new Date(cluster.dateRange.start).toLocaleDateString()} -{' '}
													{new Date(cluster.dateRange.end).toLocaleDateString()}
												</CardDescription>
												{cluster.reason && (
													<p className="text-sm text-muted-foreground mt-2">{cluster.reason}</p>
												)}
											</div>
											<div className="flex items-center gap-2">
												{cluster.suspectedOffender && (
													<Badge variant="outline">
														{formatOffenderDisplayName(
															cluster.suspectedOffender.name,
															cluster.suspectedOffender.id,
														)}{' '}
														({formatConfidencePercent(cluster.suspectedOffender.confidence)} confidence)
													</Badge>
												)}
												<Badge variant="secondary">
													£{cluster.totalValue.toFixed(2)}
												</Badge>
												<Button
													variant="ghost"
													size="sm"
													onClick={() => toggleCluster(cluster.clusterId)}
												>
													{isExpanded ? (
														<>
															<EyeOff className="h-4 w-4 mr-2" />
															Hide
														</>
													) : (
														<>
															<Eye className="h-4 w-4 mr-2" />
															View Details
														</>
													)}
												</Button>
											</div>
										</div>
									</CardHeader>
									{isExpanded && (
										<CardContent className="space-y-4">
											<div>
												<h4 className="font-semibold mb-2">Common features</h4>
												<div className="flex flex-wrap gap-2">
													{cluster.commonFeatures.map((feature) => (
														<Badge key={feature} variant="outline">
															{feature}
														</Badge>
													))}
												</div>
											</div>
											<div>
												<h4 className="font-semibold mb-2">Linked incidents</h4>
												<div className="border rounded-lg">
													<Table>
														<TableHeader>
															<TableRow>
																<TableHead>Incident ID</TableHead>
																<TableHead>Date & time</TableHead>
																<TableHead>Store</TableHead>
																<TableHead>Stolen products</TableHead>
																<TableHead>Type</TableHead>
																<TableHead>Value</TableHead>
																<TableHead>Similarity</TableHead>
															</TableRow>
														</TableHeader>
														<TableBody>
															{cluster.incidents.map((incident) => (
																<TableRow key={incident.incidentId}>
																	<TableCell className="font-mono text-xs">
																		{incident.incidentId}
																	</TableCell>
																	<TableCell className="text-sm whitespace-nowrap">
																		{incident.dateTimeLabel || incident.date || '—'}
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
																	<TableCell>£{incident.value.toFixed(2)}</TableCell>
																	<TableCell>
																		<Badge variant="outline">
																			{(incident.similarityScore * 100).toFixed(0)}%
																		</Badge>
																	</TableCell>
																</TableRow>
															))}
														</TableBody>
													</Table>
												</div>
											</div>
										</CardContent>
									)}
								</Card>
							)
						})}
					</TabsContent>

					<TabsContent value="chains" className="space-y-6 mt-6">
						{sortedChains.length === 0 && (
							<div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
								No offender chains (repeat offenders with 2+ incidents) in this period.
							</div>
						)}
						{sortedChains.map((chain) => {
							const isExpanded = expandedChains.has(chain.chainId)

							return (
								<Card key={chain.chainId}>
									<CardHeader>
										<div className="flex items-center justify-between">
											<div className="flex-1">
												<CardTitle className="text-base">
													{formatOffenderDisplayName(chain.offenderName, chain.offenderId)}
												</CardTitle>
												<CardDescription>
													{chain.chainId} • {chain.incidents.length} incidents • Pattern:{' '}
													{chain.pattern}
												</CardDescription>
											</div>
											<div className="flex items-center gap-2">
												<Badge variant="secondary">
													£{chain.totalValue.toFixed(2)}
												</Badge>
												<Button
													variant="ghost"
													size="sm"
													onClick={() => toggleChain(chain.chainId)}
												>
													{isExpanded ? (
														<>
															<EyeOff className="h-4 w-4 mr-2" />
															Hide Timeline
														</>
													) : (
														<>
															<Eye className="h-4 w-4 mr-2" />
															View Timeline
														</>
													)}
												</Button>
											</div>
										</div>
									</CardHeader>
									{isExpanded && (
										<CardContent className="space-y-4">
											{/* Timeline */}
											<div>
												<h4 className="font-semibold mb-3">Incident timeline</h4>
												<div className="space-y-2">
													{chain.timeline.map((event, index) => (
														<div
															key={index}
															className="flex items-start gap-4 p-3 border rounded-lg hover:bg-muted/50 transition-colors"
														>
															<div className="flex items-center gap-2 text-sm text-muted-foreground min-w-[140px] shrink-0">
																<Calendar className="h-4 w-4" />
																<span>{event.dateTimeLabel || event.date}</span>
															</div>
															<ArrowRight className="h-4 w-4 text-muted-foreground/80 shrink-0 mt-0.5" />
															<div className="flex-1 min-w-0 space-y-1">
																<div className="flex items-center gap-2 flex-wrap">
																	<MapPin className="h-4 w-4 text-muted-foreground/80 shrink-0" />
																	<span className="font-medium">{event.store || '—'}</span>
																	<Badge variant="outline" className="text-xs">
																		{event.incidentType}
																	</Badge>
																</div>
																<LinkedIncidentProducts
																	summary={event.stolenProductsSummary}
																	products={event.stolenProducts}
																/>
															</div>
														</div>
													))}
												</div>
											</div>

											{/* Detailed Incidents */}
											<div>
												<h4 className="font-semibold mb-2">All incidents</h4>
												<div className="border rounded-lg">
													<Table>
														<TableHeader>
															<TableRow>
																<TableHead>Incident ID</TableHead>
																<TableHead>Date & time</TableHead>
																<TableHead>Store</TableHead>
																<TableHead>Stolen products</TableHead>
																<TableHead>Type</TableHead>
																<TableHead className="text-right">Value</TableHead>
															</TableRow>
														</TableHeader>
														<TableBody>
															{chain.incidents.map((incident) => (
																<TableRow key={incident.incidentId}>
																	<TableCell className="font-mono text-xs">
																		{incident.incidentId}
																	</TableCell>
																	<TableCell className="text-sm whitespace-nowrap">
																		{incident.dateTimeLabel || incident.date || '—'}
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
										</CardContent>
									)}
								</Card>
							)
						})}
					</TabsContent>
				</Tabs>
			</CardContent>
		</Card>
	)
}

