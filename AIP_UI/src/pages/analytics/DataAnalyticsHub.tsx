/**
 * Data Analytics Hub Page
 * 
 * Main page that orchestrates all analytics modules:
 * - Crime Trend Explorer
 * - Hot Products Dashboard
 * - Repeat Offender Analysis
 * - Resource Deployment Engine
 * - Crime Linking Panel
 */

import { useState, useEffect, useMemo, useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { useToast } from '@/components/ui/use-toast'
import ErrorBoundary from '@/components/ErrorBoundary'
import { analyticsService } from '@/services/analyticsService'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import type { AnalyticsHubData } from '@/types/analytics'
import { Badge } from '@/components/ui/badge'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import { CrimeTrendExplorer } from './components/CrimeTrendExplorer'
import { HotProductsDashboard } from './components/HotProductsDashboard'
import { RepeatOffenderAnalysis } from './components/RepeatOffenderAnalysis'
import { ResourceDeploymentEngine } from './components/ResourceDeploymentEngine'
import { CrimeLinkingPanel } from './components/CrimeLinkingPanel'
import {
	BarChart3,
	RefreshCw,
	Calendar as CalendarIcon,
	Download,
	Filter,
	AlertCircle,
	Shield,
} from 'lucide-react'
import { format, subDays } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import { cn } from '@/lib/utils'
import { customerDashboardService } from '@/services/dashboardService'
import type { Region, Site } from '@/types/dashboard'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { useAvailableCustomers } from '@/hooks/useAvailableCustomers'
import { MapPin, Building2 } from 'lucide-react'

const DataAnalyticsHub = () => {
	const [searchParams, setSearchParams] = useSearchParams()
	const { toast } = useToast()

	const [loading, setLoading] = useState(true)
	const [error, setError] = useState<string | null>(null)
	const [data, setData] = useState<AnalyticsHubData | null>(null)
	const [dateRange, setDateRange] = useState<DateRange | undefined>({
		from: subDays(new Date(), 90),
		to: new Date(),
	})
	const [regions, setRegions] = useState<Region[]>([])
	const [sites, setSites] = useState<Site[]>([])
	const [selectedRegionId, setSelectedRegionId] = useState<string>('all')
	const [selectedStoreId, setSelectedStoreId] = useState<string>('all')
	const [loadingFilters, setLoadingFilters] = useState(true)
	const { isAdmin, selectedCustomerId, setSelectedCustomerId } = useCustomerSelection()
	const { availableCustomers, isLoading: loadingCustomers } = useAvailableCustomers()
	const [selectedCustomerForAdmin, setSelectedCustomerForAdmin] = useState<number | null>(null)

	// Sync effective customer for admins based on URL or context
	const urlCustomerId = searchParams.get('customerId')

	const effectiveCustomerId = useMemo(() => {
		if (urlCustomerId) {
			const id = parseInt(urlCustomerId, 10)
			return Number.isNaN(id) ? undefined : id
		}
		return selectedCustomerId ?? undefined
	}, [urlCustomerId, selectedCustomerId])

	useEffect(() => {
		if (!isAdmin) return
		if (!effectiveCustomerId) return
		setSelectedCustomerForAdmin(effectiveCustomerId)
		if (selectedCustomerId !== effectiveCustomerId) {
			setSelectedCustomerId(effectiveCustomerId)
		}
	}, [isAdmin, effectiveCustomerId, selectedCustomerId, setSelectedCustomerId])

	// Load filters (regions and sites) scoped to the effective customer when available.
	useEffect(() => {
		const loadFilters = async () => {
			setLoadingFilters(true)
			try {
				console.log('🔄 Loading regions and sites for analytics...', {
					effectiveCustomerId,
					isAdmin,
				})
				const [regionsData, sitesData] = await Promise.all([
					customerDashboardService.getRegions(undefined, effectiveCustomerId ?? null),
					customerDashboardService.getSites(undefined, effectiveCustomerId ?? null),
				])
				console.log('✅ Loaded regions:', regionsData.length)
				console.log('✅ Loaded sites:', sitesData.length)
				setRegions(regionsData)
				setSites(sitesData)
			} catch (err) {
				console.error('Failed to load filter options:', err)
				toast({
					title: 'Warning',
					description: 'Failed to load filter options. Some filters may not be available.',
					variant: 'destructive',
				})
			} finally {
				setLoadingFilters(false)
			}
		}

		loadFilters()
	}, [effectiveCustomerId, isAdmin, toast])

	// Customer switch should reset dependent region/store filters to avoid stale selections.
	useEffect(() => {
		setSelectedRegionId('all')
		setSelectedStoreId('all')
	}, [effectiveCustomerId])

	// Filter sites by selected region
	const filteredSites = useMemo(() => {
		if (selectedRegionId === 'all') {
			return sites
		}
		return sites.filter((site) => {
			// Handle different Site type structures
			const siteRegionId = (site as any).regionId || (site as any).fkRegionID || (site as any).regionID
			return String(siteRegionId) === selectedRegionId
		})
	}, [sites, selectedRegionId])

	// Load analytics data - wrapped in useCallback to prevent infinite loops
	const loadData = useCallback(async () => {
		setLoading(true)
		setError(null)

		try {
			const params = {
				startDate: dateRange?.from ? format(dateRange.from, 'yyyy-MM-dd') : undefined,
				endDate: dateRange?.to ? format(dateRange.to, 'yyyy-MM-dd') : undefined,
				customerId: effectiveCustomerId,
				regionIds: selectedRegionId !== 'all' ? [Number(selectedRegionId)] : undefined,
				storeIds: selectedStoreId !== 'all' ? [Number(selectedStoreId)] : undefined,
			}

			const analyticsData = await analyticsService.getAnalyticsHub(params)
			console.log(
				'✅ Analytics data loaded, storeDrilldown count:',
				Object.keys(analyticsData.crimeTrends.storeDrilldown).length
			)
			setData(analyticsData)
		} catch (err) {
			console.error('Failed to load analytics data:', err)
			setError(err instanceof Error ? err.message : 'Failed to load analytics data')
			toast({
				title: 'Error',
				description: 'Failed to load analytics data. Please try again.',
				variant: 'destructive',
			})
		} finally {
			setLoading(false)
		}
	}, [dateRange, selectedRegionId, selectedStoreId, effectiveCustomerId, toast])

	useEffect(() => {
		// Load analytics data immediately; filters (regions/sites) load in parallel
		loadData()
	}, [loadData])

	// Reset store selection when region changes
	useEffect(() => {
		if (selectedRegionId === 'all') {
			return
		}
		const regionSites = filteredSites
		const currentStoreInRegion = regionSites.some((s) => {
			const siteId = (s as any).siteID || (s as any).id
			return String(siteId) === selectedStoreId
		})
		if (selectedStoreId !== 'all' && !currentStoreInRegion) {
			setSelectedStoreId('all')
		}
	}, [selectedRegionId, filteredSites, selectedStoreId])

	const handleDateRangeChange = (range: DateRange | undefined) => {
		setDateRange(range)
	}

	const handleRefresh = () => {
		loadData()
	}

	const handleExport = () => {
		// TODO: Implement export functionality
		toast({
			title: 'Export',
			description: 'Export functionality will be implemented soon.',
		})
	}

	const aiRiskSummary = useMemo(() => {
		if (!data) return null

		const rankings = data.deploymentRecommendations?.storeRankings || []
		if (!rankings.length) return null

		const high = rankings.filter((r) => r.riskLevel === 'high' || r.riskLevel === 'critical')
		const medium = rankings.filter((r) => r.riskLevel === 'medium')
		const low = rankings.filter((r) => r.riskLevel === 'low')

		const top = rankings[0]

		const overallLevel =
			high.length > 0 ? 'high' : medium.length > 0 ? 'medium' : low.length > 0 ? 'low' : 'low'

		const levelLabelMap: Record<string, string> = {
			low: 'Overall risk: Low',
			medium: 'Overall risk: Medium',
			high: 'Overall risk: High',
			critical: 'Overall risk: Critical',
		}

		const topLabelMap: Record<string, string> = {
			low: 'Low',
			medium: 'Medium',
			high: 'High',
			critical: 'Critical',
		}

		return {
			overallLevel,
			overallLabel: levelLabelMap[overallLevel] ?? 'Overall risk: Unknown',
			highCount: high.length,
			mediumCount: medium.length,
			lowCount: low.length,
			topStoreName: top.storeName,
			topStoreLevelLabel: topLabelMap[top.riskLevel] ?? top.riskLevel,
		}
	}, [data])

	const recoveryInsights = useMemo(() => {
		if (!data?.storeRecoveryComparisons?.length) return []

		const comparisons = data.storeRecoveryComparisons
		const networkAverage =
			comparisons.reduce((sum, store) => sum + store.recoveryRate, 0) / comparisons.length
		const bestStore = [...comparisons].sort((a, b) => b.recoveryRate - a.recoveryRate)[0]
		const highestLossStore = [...comparisons].sort((a, b) => b.totalLostValue - a.totalLostValue)[0]
		const bestRecoveredProduct = data.hotProducts.topRecoveredProducts[0]

		const insights = []

		if (bestStore) {
			insights.push(
				`${bestStore.storeName} recovered ${bestStore.recoveryRate.toFixed(1)}% of stolen value vs network average ${networkAverage.toFixed(1)}%`
			)
		}

		if (highestLossStore) {
			insights.push(
				`${highestLossStore.storeName} has the highest unrecovered loss at £${highestLossStore.totalLostValue.toLocaleString('en-GB', {
					minimumFractionDigits: 0,
					maximumFractionDigits: 0,
				})}`
			)
		}

		if (bestRecoveredProduct) {
			insights.push(
				`${bestRecoveredProduct.productName} leads recovered value at £${bestRecoveredProduct.recoveredValue.toLocaleString('en-GB', {
					minimumFractionDigits: 0,
					maximumFractionDigits: 0,
				})}`
			)
		}

		return insights
	}, [data])

	if (error && !data) {
		return (
			<div className="container mx-auto px-3 sm:px-4 md:px-6 py-3 sm:py-4 md:py-6">
				<Card>
					<CardHeader className="p-4 sm:p-6">
						<CardTitle className="text-lg sm:text-xl">Data Analytics Hub</CardTitle>
						<CardDescription className="text-xs sm:text-sm">Error loading analytics data</CardDescription>
					</CardHeader>
					<CardContent className="p-4 sm:p-6">
						<div className="flex flex-col items-center justify-center py-8 sm:py-12 space-y-4">
							<AlertCircle className="h-10 w-10 sm:h-12 sm:w-12 text-red-500" />
							<p className="text-sm sm:text-base text-red-600 text-center">{error}</p>
							<Button onClick={handleRefresh} className="text-sm">
								<RefreshCw className="h-4 w-4 mr-2" />
								Retry
							</Button>
						</div>
					</CardContent>
				</Card>
			</div>
		)
	}

	return (
		<ErrorBoundary>
			<div className="min-h-screen w-full max-w-[100vw] overflow-x-hidden bg-gradient-to-br from-background via-muted/30 to-background">
				<div className="container mx-auto px-3 sm:px-4 md:px-6 py-3 sm:py-4 md:py-6 space-y-4 sm:space-y-6 md:space-y-8 max-w-screen-2xl">
				{/* Header */}
				<Card className="overflow-hidden border-border shadow-sm bg-card">
					<CardHeader className="p-4 sm:p-6 overflow-x-hidden">
						<div className="flex flex-col gap-4 w-full">
							<div className="min-w-0">
								<div className="flex flex-col gap-2 sm:gap-3">
									<CardTitle className="flex items-center gap-2 text-lg sm:text-xl md:text-2xl text-card-foreground">
										<BarChart3 className="h-5 w-5 sm:h-6 sm:w-6 text-indigo-500" />
										Crime Analytics &amp; AI Hub
									</CardTitle>
									<CardDescription className="text-xs sm:text-sm text-muted-foreground">
										Comprehensive crime analytics, AI-driven risk insights, and repeat offender intelligence.
										{(selectedRegionId !== 'all' || selectedStoreId !== 'all') && (
											<span className="block mt-1 text-xs text-indigo-500 dark:text-indigo-300">
												Filters: {selectedRegionId !== 'all' && 'Region • '}
												{selectedStoreId !== 'all' && 'Store'}
											</span>
										)}
									</CardDescription>
									{isAdmin && (
										<div className="mt-1 max-w-xs">
											<p className="text-[11px] sm:text-xs font-medium text-muted-foreground mb-1">
												Customer
											</p>
											<Select
												disabled={loadingCustomers || availableCustomers.length === 0}
												value={selectedCustomerForAdmin?.toString() ?? ''}
												onValueChange={value => {
													const id = parseInt(value, 10)
													setSelectedCustomerForAdmin(id)
													setSelectedCustomerId(id)
													const params = new URLSearchParams(searchParams)
													params.set('customerId', value)
													setSearchParams(params, { replace: true })
													loadData()
												}}
											>
												<SelectTrigger className="h-9 text-xs sm:text-sm w-full sm:w-64">
													<SelectValue placeholder={loadingCustomers ? 'Loading customers…' : 'Select customer'} />
												</SelectTrigger>
												<SelectContent>
													{availableCustomers.map(c => (
														<SelectItem key={c.id} value={c.id.toString()}>
															{c.name}
														</SelectItem>
													))}
												</SelectContent>
											</Select>
										</div>
									)}
								</div>
							</div>
							
							{/* Filters Section */}
							<div className="flex flex-col gap-3 w-full min-w-0">
								{/* Date Range: Start and End date inputs */}
								<div className="w-full min-w-0 grid grid-cols-1 sm:grid-cols-2 gap-2">
									<div className="space-y-1">
										<p className="text-xs font-medium text-muted-foreground">Start date</p>
										<Input
											type="date"
											value={dateRange?.from ? format(dateRange.from, 'yyyy-MM-dd') : ''}
											onChange={(e) => {
												const value = e.target.value
												setDateRange((prev) => ({
													from: value ? new Date(value) : undefined,
													to: prev?.to,
												}))
											}}
											className="h-9 text-xs sm:text-sm"
										/>
									</div>
									<div className="space-y-1">
										<p className="text-xs font-medium text-muted-foreground">End date</p>
										<Input
											type="date"
											value={dateRange?.to ? format(dateRange.to, 'yyyy-MM-dd') : ''}
											onChange={(e) => {
												const value = e.target.value
												setDateRange((prev) => ({
													from: prev?.from,
													to: value ? new Date(value) : undefined,
												}))
											}}
											className="h-9 text-xs sm:text-sm"
										/>
									</div>
								</div>

								{/* Region and Store Filters */}
								<div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3 w-full">
									<div className="min-w-0">
										<Select
											value={selectedRegionId}
											onValueChange={setSelectedRegionId}
											disabled={loadingFilters}
										>
											<SelectTrigger className="w-full text-xs sm:text-sm">
												<Building2 className="h-4 w-4 mr-2 flex-shrink-0" />
												<SelectValue placeholder="All Regions" />
											</SelectTrigger>
											<SelectContent>
												<SelectItem value="all">All Regions</SelectItem>
												{regions.map((region) => (
													<SelectItem key={region.id} value={String(region.id)}>
														{region.name}
													</SelectItem>
												))}
											</SelectContent>
										</Select>
									</div>
									<div className="min-w-0">
										<Select
											value={selectedStoreId}
											onValueChange={setSelectedStoreId}
											disabled={loadingFilters || filteredSites.length === 0}
										>
											<SelectTrigger className="w-full text-xs sm:text-sm">
												<MapPin className="h-4 w-4 mr-2 flex-shrink-0" />
												<SelectValue placeholder="All Stores" />
											</SelectTrigger>
											<SelectContent>
												<SelectItem value="all">All Stores</SelectItem>
												{filteredSites.map((site) => {
													const siteId = (site as any).siteID || (site as any).id
													const siteName = (site as any).locationName || (site as any).name || `Store ${siteId}`
													return (
														<SelectItem key={siteId} value={String(siteId)}>
															{siteName}
														</SelectItem>
													)
												})}
											</SelectContent>
										</Select>
									</div>
								</div>

								{/* Action Buttons */}
								<div className="flex flex-wrap gap-2 w-full">
									<Button
										variant="outline"
										onClick={handleRefresh}
										disabled={loading}
										size="sm"
										className="flex-1 sm:flex-none text-xs sm:text-sm"
									>
										<RefreshCw
											className={`h-3.5 w-3.5 sm:h-4 sm:w-4 mr-1.5 ${loading ? 'animate-spin' : ''}`}
										/>
										Refresh
									</Button>
									<Button 
										variant="outline" 
										onClick={handleExport}
										size="sm"
										className="flex-1 sm:flex-none text-xs sm:text-sm"
									>
										<Download className="h-3.5 w-3.5 sm:h-4 sm:w-4 mr-1.5" />
										Export
									</Button>
									{(selectedRegionId !== 'all' || selectedStoreId !== 'all') && (
										<Button
											variant="outline"
											onClick={() => {
												setSelectedRegionId('all')
												setSelectedStoreId('all')
											}}
											size="sm"
											className="w-full sm:w-auto text-xs sm:text-sm"
										>
											<Filter className="h-3.5 w-3.5 sm:h-4 sm:w-4 mr-1.5" />
											Clear Filters
										</Button>
									)}
								</div>
							</div>

							{data && (
								<>
									<div className="mt-4 grid grid-cols-2 lg:grid-cols-6 gap-3 sm:gap-4 w-full">
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Date Range
											</div>
											<div className="text-xs sm:text-sm font-semibold text-card-foreground truncate">
												{format(new Date(data.metadata.dateRange.start), 'MMM dd')} -{' '}
												{format(new Date(data.metadata.dateRange.end), 'MMM dd, yy')}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Total Incidents
											</div>
											<div className="text-xs sm:text-sm font-semibold text-card-foreground truncate">
												{data.crimeTrends.totalIncidents.toLocaleString()}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Total Stolen
											</div>
											<div className="text-xs sm:text-sm font-semibold text-card-foreground truncate">
												£{data.financialSummary.totalStolenValue.toLocaleString('en-GB', {
													minimumFractionDigits: 0,
													maximumFractionDigits: 0,
												})}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Value Saved
											</div>
											<div className="text-xs sm:text-sm font-semibold text-emerald-700 dark:text-emerald-300 truncate">
												£{data.financialSummary.totalRecoveredValue.toLocaleString('en-GB', {
													minimumFractionDigits: 0,
													maximumFractionDigits: 0,
												})}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Value Lost
											</div>
											<div className="text-xs sm:text-sm font-semibold text-rose-700 dark:text-rose-300 truncate">
												£{data.financialSummary.totalLostValue.toLocaleString('en-GB', {
													minimumFractionDigits: 0,
													maximumFractionDigits: 0,
												})}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Recovery Rate
											</div>
											<div className="text-xs sm:text-sm font-semibold text-indigo-700 dark:text-indigo-300 truncate">
												{data.financialSummary.recoveryRate.toFixed(1)}%
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Offenders Tracked
											</div>
											<div className="text-xs sm:text-sm font-semibold text-card-foreground truncate">
												{data.repeatOffenders.totalOffenders}
											</div>
										</div>
									</div>

									{aiRiskSummary && (
										<div className="mt-3 sm:mt-4 flex flex-col sm:flex-row gap-2 sm:gap-3 items-start sm:items-center w-full">
											<div className="inline-flex items-center gap-2 rounded-full bg-indigo-600 text-white px-3 py-1 text-[10px] sm:text-xs">
												<Shield className="h-3 w-3 text-emerald-200" />
												<span className="font-semibold tracking-wide uppercase">
													AI Risk Engine
												</span>
												<span className="text-[11px] font-medium">
													{aiRiskSummary.overallLabel}
												</span>
											</div>
											<div className="flex flex-wrap gap-1 text-[10px] sm:text-xs text-foreground/80">
												<span className="inline-flex items-center rounded-full bg-red-50 px-2 py-0.5 text-red-700 border border-red-100 dark:bg-red-950/50 dark:text-red-200 dark:border-red-900">
													{aiRiskSummary.highCount} high
												</span>
												<span className="inline-flex items-center rounded-full bg-amber-50 px-2 py-0.5 text-amber-800 border border-amber-100 dark:bg-amber-950/40 dark:text-amber-200 dark:border-amber-900">
													{aiRiskSummary.mediumCount} medium
												</span>
												<span className="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-emerald-800 border border-emerald-100 dark:bg-emerald-950/40 dark:text-emerald-200 dark:border-emerald-900">
													{aiRiskSummary.lowCount} low
												</span>
												<span className="ml-1">
													Top store:{' '}
													<span className="font-semibold">
														{aiRiskSummary.topStoreName} ({aiRiskSummary.topStoreLevelLabel})
													</span>
												</span>
											</div>
										</div>
									)}

									{recoveryInsights.length > 0 && (
										<div className="mt-3 grid gap-2">
											{recoveryInsights.map((insight) => (
												<div
													key={insight}
													className="rounded-lg border border-border bg-muted/40 px-3 py-2 text-xs sm:text-sm text-foreground"
												>
													{insight}
												</div>
											))}
										</div>
									)}
								</>
							)}
						</div>
					</CardHeader>
				</Card>

				{/* Loading State */}
				{loading && !data && (
					<div className="space-y-4 sm:space-y-6">
						{Array.from({ length: 5 }).map((_, i) => (
							<Card key={i}>
								<CardHeader className="p-4 sm:p-6">
									<Skeleton className="h-5 sm:h-6 w-32 sm:w-48" />
									<Skeleton className="h-3 sm:h-4 w-48 sm:w-64 mt-2" />
								</CardHeader>
								<CardContent className="p-4 sm:p-6 pt-0">
									<Skeleton className="h-64 sm:h-80 md:h-96 w-full" />
								</CardContent>
							</Card>
						))}
					</div>
				)}

				{/* Analytics Modules - single focused view at a time */}
				{data && (
					<Card className="overflow-hidden">
						<CardHeader className="p-4 sm:p-6 pb-3 sm:pb-4">
							<CardTitle className="text-sm sm:text-base">
								Select an analytics view to focus on a single insight at a time
							</CardTitle>
							<CardDescription className="text-xs sm:text-sm">
								All views respect the date range, region and store filters from the header above.
							</CardDescription>
						</CardHeader>
						<CardContent className="p-4 sm:p-6 pt-0 overflow-x-hidden">
							<Tabs defaultValue="crime-trends" className="w-full">
								<div className="overflow-x-auto -mx-4 sm:mx-0 px-4 sm:px-0">
									<TabsList className="w-full h-auto grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-1 sm:gap-0 mb-4 p-1 min-w-max sm:min-w-0 rounded-xl bg-muted border border-border">
										<TabsTrigger
											value="crime-trends"
											className="text-[11px] sm:text-sm py-2 whitespace-nowrap text-muted-foreground data-[state=active]:bg-indigo-600 data-[state=active]:text-white data-[state=active]:shadow-sm data-[state=active]:shadow-indigo-500/40 hover:bg-accent hover:text-accent-foreground"
										>
											Crime Trends
										</TabsTrigger>
										<TabsTrigger
											value="deployment"
											className="text-[11px] sm:text-sm py-2 whitespace-nowrap text-muted-foreground data-[state=active]:bg-indigo-600 data-[state=active]:text-white data-[state=active]:shadow-sm data-[state=active]:shadow-indigo-500/40 hover:bg-accent hover:text-accent-foreground"
										>
											Deployment
										</TabsTrigger>
										<TabsTrigger
											value="hot-products"
											className="text-[11px] sm:text-sm py-2 whitespace-nowrap text-muted-foreground data-[state=active]:bg-indigo-600 data-[state=active]:text-white data-[state=active]:shadow-sm data-[state=active]:shadow-indigo-500/40 hover:bg-accent hover:text-accent-foreground"
										>
											Hot Products
										</TabsTrigger>
										<TabsTrigger
											value="recovery-performance"
											className="text-[11px] sm:text-sm py-2 whitespace-nowrap text-muted-foreground data-[state=active]:bg-indigo-600 data-[state=active]:text-white data-[state=active]:shadow-sm data-[state=active]:shadow-indigo-500/40 hover:bg-accent hover:text-accent-foreground"
										>
											Recovery
										</TabsTrigger>
										<TabsTrigger
											value="repeat-offenders"
											className="text-[11px] sm:text-sm py-2 whitespace-nowrap text-muted-foreground data-[state=active]:bg-indigo-600 data-[state=active]:text-white data-[state=active]:shadow-sm data-[state=active]:shadow-indigo-500/40 hover:bg-accent hover:text-accent-foreground"
										>
											Offenders
										</TabsTrigger>
										<TabsTrigger
											value="crime-linking"
											className="text-[11px] sm:text-sm py-2 whitespace-nowrap text-muted-foreground data-[state=active]:bg-indigo-600 data-[state=active]:text-white data-[state=active]:shadow-sm data-[state=active]:shadow-indigo-500/40 hover:bg-accent hover:text-accent-foreground"
										>
											Crime Linking
										</TabsTrigger>
									</TabsList>
								</div>

								<TabsContent value="crime-trends" className="mt-3 sm:mt-4 overflow-x-hidden">
									<ErrorBoundary>
										<CrimeTrendExplorer data={data.crimeTrends} loading={loading} />
									</ErrorBoundary>
								</TabsContent>

								<TabsContent value="deployment" className="mt-3 sm:mt-4 overflow-x-hidden">
									<ErrorBoundary>
										<ResourceDeploymentEngine
											data={data.deploymentRecommendations}
											loading={loading}
										/>
									</ErrorBoundary>
								</TabsContent>

								<TabsContent value="hot-products" className="mt-3 sm:mt-4 overflow-x-hidden">
									<ErrorBoundary>
										<HotProductsDashboard data={data.hotProducts} loading={loading} />
									</ErrorBoundary>
								</TabsContent>

								<TabsContent value="recovery-performance" className="mt-3 sm:mt-4 overflow-x-hidden">
									<Card className="border-border">
										<CardHeader className="p-4 sm:p-6 pb-3">
											<CardTitle className="text-base sm:text-lg">Saved vs Lost by Store</CardTitle>
											<CardDescription>
												Compare recovered value, unrecovered loss, and recovery efficiency across the selected stores.
											</CardDescription>
										</CardHeader>
										<CardContent className="p-4 sm:p-6 pt-0">
											<div className="rounded-lg border border-border overflow-hidden">
												<Table>
													<TableHeader>
														<TableRow>
															<TableHead>Store</TableHead>
															<TableHead>Incidents</TableHead>
															<TableHead className="text-right">Stolen</TableHead>
															<TableHead className="text-right">Saved</TableHead>
															<TableHead className="text-right">Lost</TableHead>
															<TableHead className="text-right">Recovery Rate</TableHead>
														</TableRow>
													</TableHeader>
													<TableBody>
														{data.storeRecoveryComparisons.slice(0, 12).map((store) => (
															<TableRow key={`${store.storeId}-${store.storeName}`}>
																<TableCell className="font-medium">{store.storeName}</TableCell>
																<TableCell>{store.incidentCount}</TableCell>
																<TableCell className="text-right">
																	£{store.totalStolenValue.toLocaleString('en-GB', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}
																</TableCell>
																<TableCell className="text-right text-emerald-700 dark:text-emerald-300">
																	£{store.totalRecoveredValue.toLocaleString('en-GB', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}
																</TableCell>
																<TableCell className="text-right text-rose-700 dark:text-rose-300">
																	£{store.totalLostValue.toLocaleString('en-GB', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}
																</TableCell>
																<TableCell className="text-right">
																	<Badge variant="outline">{store.recoveryRate.toFixed(1)}%</Badge>
																</TableCell>
															</TableRow>
														))}
													</TableBody>
												</Table>
											</div>
										</CardContent>
									</Card>
								</TabsContent>

								<TabsContent value="repeat-offenders" className="mt-3 sm:mt-4 overflow-x-hidden">
									<ErrorBoundary>
										<RepeatOffenderAnalysis
											data={data.repeatOffenders}
											loading={loading}
										/>
									</ErrorBoundary>
								</TabsContent>

								<TabsContent value="crime-linking" className="mt-3 sm:mt-4 overflow-x-hidden">
									<ErrorBoundary>
										<CrimeLinkingPanel data={data.crimeLinking} loading={loading} />
									</ErrorBoundary>
								</TabsContent>
							</Tabs>
						</CardContent>
					</Card>
				)}

				{/* Error State (partial data) */}
				{error && data && (
					<Card className="border-yellow-200 bg-yellow-50 overflow-hidden dark:border-yellow-900 dark:bg-yellow-950/30">
						<CardContent className="p-3 sm:p-4">
							<div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3 text-yellow-800 dark:text-yellow-200">
								<div className="flex items-center gap-2 flex-1 min-w-0">
									<AlertCircle className="h-4 w-4 flex-shrink-0" />
									<span className="text-xs sm:text-sm break-words">
										Some data may be outdated. Error: {error}
									</span>
								</div>
								<Button
									variant="ghost"
									size="sm"
									onClick={handleRefresh}
									className="w-full sm:w-auto text-xs sm:text-sm flex-shrink-0"
								>
									<RefreshCw className="h-3.5 w-3.5 sm:h-4 sm:w-4 mr-1.5" />
									Retry
								</Button>
							</div>
						</CardContent>
					</Card>
				)}
				</div>
			</div>
		</ErrorBoundary>
	)
}

export default DataAnalyticsHub

