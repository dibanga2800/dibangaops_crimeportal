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
import { useAuth } from '@/hooks/useAuth'
import { getAssignedSiteIds, isSiteScopeEnforcedForUser } from '@/utils/siteAccess'
import { MapPin, Building2 } from 'lucide-react'

const formatCurrencyExact = (value: number) =>
	value.toLocaleString('en-GB', {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	})

const toSiteIdString = (value: unknown): string => String(value ?? '').trim()

const getSiteIdFromSiteOption = (site: Site): string =>
	toSiteIdString((site as any).siteID ?? (site as any).id ?? (site as any).siteId)

const mergeAnalyticsHubData = (datasets: AnalyticsHubData[]): AnalyticsHubData => {
	if (datasets.length === 0) {
		throw new Error('No analytics datasets to merge.')
	}
	if (datasets.length === 1) {
		return datasets[0]
	}

	const priorityWeight: Record<string, number> = {
		low: 1,
		medium: 2,
		high: 3,
		critical: 4,
	}
	const trendWeight: Record<string, number> = {
		decreasing: -1,
		stable: 0,
		increasing: 1,
	}

	const mergeIncidentTypes = (
		items: Array<{ type: string; count: number; totalValue: number }>
	): Array<{ type: string; count: number; totalValue: number; percentage: number }> => {
		const map = new Map<string, { type: string; count: number; totalValue: number }>()
		items.forEach((it) => {
			const key = String(it?.type ?? 'Unknown')
			const prev = map.get(key) ?? { type: key, count: 0, totalValue: 0 }
			map.set(key, {
				type: key,
				count: prev.count + Number(it?.count ?? 0),
				totalValue: prev.totalValue + Number(it?.totalValue ?? 0),
			})
		})
		const totalCount = Array.from(map.values()).reduce((sum, it) => sum + it.count, 0)
		return Array.from(map.values())
			.map((it) => ({
				...it,
				percentage: totalCount > 0 ? (it.count / totalCount) * 100 : 0,
			}))
			.sort((a, b) => b.count - a.count)
	}

	const aggregateByKey = <T extends Record<string, any>>(
		rows: T[],
		keyFn: (row: T) => string,
		initial: () => Record<string, any>,
		mergeFn: (acc: Record<string, any>, row: T) => void
	) => {
		const map = new Map<string, Record<string, any>>()
		rows.forEach((row) => {
			const key = keyFn(row)
			if (!key) return
			const acc = map.get(key) ?? initial()
			mergeFn(acc, row)
			map.set(key, acc)
		})
		return Array.from(map.values())
	}

	const mergedStoreDrilldown: Record<string, any> = {}
	datasets.forEach((dataset) => {
		Object.values(dataset.crimeTrends.storeDrilldown || {}).forEach((store: any) => {
			const storeName = String(store?.storeName ?? `Store ${store?.storeId ?? 'Unknown'}`)
			const prev = mergedStoreDrilldown[storeName]
			if (!prev) {
				mergedStoreDrilldown[storeName] = {
					...store,
					storeName,
					incidentTypes: mergeIncidentTypes(store.incidentTypes || []),
				}
				return
			}

			const totalStolenValue = Number(prev.totalStolenValue ?? 0) + Number(store.totalStolenValue ?? 0)
			const totalRecoveredValue =
				Number(prev.totalRecoveredValue ?? 0) + Number(store.totalRecoveredValue ?? 0)
			const totalLostValue = Number(prev.totalLostValue ?? 0) + Number(store.totalLostValue ?? 0)
			const incidents = Number(prev.incidents ?? 0) + Number(store.incidents ?? 0)

			mergedStoreDrilldown[storeName] = {
				...prev,
				...store,
				storeName,
				incidents,
				totalStolenValue,
				totalRecoveredValue,
				totalLostValue,
				recoveryRate: totalStolenValue > 0 ? (totalRecoveredValue / totalStolenValue) * 100 : 0,
				peakDay: incidents >= Number(prev.incidents ?? 0) ? store.peakDay ?? prev.peakDay : prev.peakDay,
				peakHour: incidents >= Number(prev.incidents ?? 0) ? store.peakHour ?? prev.peakHour : prev.peakHour,
				incidentTypes: mergeIncidentTypes([
					...(prev.incidentTypes || []),
					...(store.incidentTypes || []),
				]),
			}
		})
	})

	const storeDrilldownValues = Object.values(mergedStoreDrilldown)
	const totalIncidents = storeDrilldownValues.reduce((sum: number, store: any) => sum + Number(store.incidents ?? 0), 0)
	const mergedIncidentTypes = mergeIncidentTypes(
		storeDrilldownValues.flatMap((store: any) => store.incidentTypes || [])
	)

	const mergedDayOfWeek = aggregateByKey(
		datasets.flatMap((d) => d.crimeTrends.dayOfWeek || []),
		(row) => String(row.day ?? ''),
		() => ({ day: '', incidents: 0, stores: 0, percentage: 0 }),
		(acc, row) => {
			acc.day = row.day
			acc.incidents += Number(row.incidents ?? 0)
			acc.stores += Number(row.stores ?? 0)
		}
	).map((row) => ({
		...row,
		percentage: totalIncidents > 0 ? (Number(row.incidents ?? 0) / totalIncidents) * 100 : 0,
	}))

	const mergedTimeOfDay = aggregateByKey(
		datasets.flatMap((d) => d.crimeTrends.timeOfDay || []),
		(row) => String(row.hour ?? ''),
		() => ({ hour: 0, label: '', incidents: 0, percentage: 0 }),
		(acc, row) => {
			acc.hour = Number(row.hour ?? 0)
			acc.label = String(row.label ?? `${row.hour}:00`)
			acc.incidents += Number(row.incidents ?? 0)
		}
	)
		.map((row) => ({
			...row,
			percentage: totalIncidents > 0 ? (Number(row.incidents ?? 0) / totalIncidents) * 100 : 0,
		}))
		.sort((a, b) => Number(a.hour) - Number(b.hour))

	const mergedRecoveryTrend = aggregateByKey(
		datasets.flatMap((d) => d.crimeTrends.recoveryTrend || []),
		(row) => String(row.period ?? ''),
		() => ({ period: '', incidentCount: 0, stolenValue: 0, recoveredValue: 0, lostValue: 0 }),
		(acc, row) => {
			acc.period = String(row.period ?? '')
			acc.incidentCount += Number(row.incidentCount ?? 0)
			acc.stolenValue += Number(row.stolenValue ?? 0)
			acc.recoveredValue += Number(row.recoveredValue ?? 0)
			acc.lostValue += Number(row.lostValue ?? 0)
		}
	)

	const mergeProducts = (products: any[]) => {
		const map = new Map<string, any>()
		products.forEach((product) => {
			const key = `${String(product?.barcode ?? '')}|${String(product?.productName ?? '')}`
			const prev = map.get(key)
			if (!prev) {
				map.set(key, {
					...product,
					frequency: Number(product?.frequency ?? 0),
					totalValue: Number(product?.totalValue ?? 0),
					stolenValue: Number(product?.stolenValue ?? 0),
					recoveredValue: Number(product?.recoveredValue ?? 0),
					lostValue: Number(product?.lostValue ?? 0),
					storesAffected: Number(product?.storesAffected ?? 0),
				})
				return
			}

			const stolenValue = Number(prev.stolenValue ?? 0) + Number(product?.stolenValue ?? 0)
			const recoveredValue = Number(prev.recoveredValue ?? 0) + Number(product?.recoveredValue ?? 0)
			map.set(key, {
				...prev,
				frequency: Number(prev.frequency ?? 0) + Number(product?.frequency ?? 0),
				totalValue: Number(prev.totalValue ?? 0) + Number(product?.totalValue ?? 0),
				stolenValue,
				recoveredValue,
				lostValue: Number(prev.lostValue ?? 0) + Number(product?.lostValue ?? 0),
				storesAffected: Number(prev.storesAffected ?? 0) + Number(product?.storesAffected ?? 0),
				recoveryRate: stolenValue > 0 ? (recoveredValue / stolenValue) * 100 : 0,
			})
		})
		return Array.from(map.values())
	}

	const mergedTopProducts = mergeProducts(datasets.flatMap((d) => d.hotProducts.topProducts || []))
	const mergedTopRecoveredProducts = [...mergedTopProducts]
		.sort((a, b) => Number(b.recoveredValue ?? 0) - Number(a.recoveredValue ?? 0))
		.slice(0, 10)
	const mergedWorstRecoveryProducts = [...mergedTopProducts]
		.sort((a, b) => Number(b.lostValue ?? 0) - Number(a.lostValue ?? 0))
		.slice(0, 10)

	const mergedStoreHeatmapMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.hotProducts.storeHeatmap || []).forEach((store) => {
			const key = toSiteIdString((store as any).storeId)
			if (!key) return
			const prev = mergedStoreHeatmapMap.get(key)
			if (!prev) {
				mergedStoreHeatmapMap.set(key, {
					...store,
					products: mergeProducts((store as any).products || []),
				})
				return
			}

			const totalValueStolen = Number(prev.totalValueStolen ?? 0) + Number((store as any).totalValueStolen ?? 0)
			const totalValueRecovered =
				Number(prev.totalValueRecovered ?? 0) + Number((store as any).totalValueRecovered ?? 0)
			const totalValueLost = Number(prev.totalValueLost ?? 0) + Number((store as any).totalValueLost ?? 0)
			const riskLevel =
				priorityWeight[String((store as any).riskLevel ?? 'low')] >=
				priorityWeight[String(prev.riskLevel ?? 'low')]
					? (store as any).riskLevel
					: prev.riskLevel

			mergedStoreHeatmapMap.set(key, {
				...prev,
				...(store as any),
				totalIncidents: Number(prev.totalIncidents ?? 0) + Number((store as any).totalIncidents ?? 0),
				totalValueStolen,
				totalValueRecovered,
				totalValueLost,
				riskLevel,
				recoveryRate: totalValueStolen > 0 ? (totalValueRecovered / totalValueStolen) * 100 : 0,
				products: mergeProducts([...(prev.products || []), ...((store as any).products || [])]),
			})
		})
	})
	const mergedStoreHeatmap = Array.from(mergedStoreHeatmapMap.values())

	const mergedStoreRecoveryComparisons = storeDrilldownValues.map((store: any) => ({
		storeId: Number(store.storeId),
		storeName: String(store.storeName ?? `Store ${store.storeId}`),
		incidentCount: Number(store.incidents ?? 0),
		totalStolenValue: Number(store.totalStolenValue ?? 0),
		totalRecoveredValue: Number(store.totalRecoveredValue ?? 0),
		totalLostValue: Number(store.totalLostValue ?? 0),
		recoveryRate:
			Number(store.totalStolenValue ?? 0) > 0
				? (Number(store.totalRecoveredValue ?? 0) / Number(store.totalStolenValue ?? 0)) * 100
				: 0,
		totalRecoveredQuantity: Number(store.totalRecoveredQuantity ?? 0),
		totalLostQuantity: Number(store.totalLostQuantity ?? 0),
	}))

	const financialSummary = mergedStoreRecoveryComparisons.reduce(
		(acc, row) => {
			acc.totalStolenValue += row.totalStolenValue
			acc.totalRecoveredValue += row.totalRecoveredValue
			acc.totalLostValue += row.totalLostValue
			acc.totalRecoveredQuantity += row.totalRecoveredQuantity
			acc.totalLostQuantity += row.totalLostQuantity
			return acc
		},
		{
			totalStolenValue: 0,
			totalRecoveredValue: 0,
			totalLostValue: 0,
			recoveryRate: 0,
			totalRecoveredQuantity: 0,
			totalLostQuantity: 0,
		}
	)
	financialSummary.recoveryRate =
		financialSummary.totalStolenValue > 0
			? (financialSummary.totalRecoveredValue / financialSummary.totalStolenValue) * 100
			: 0

	const mergedBestTimesMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.deploymentRecommendations.bestTimes || []).forEach((rec) => {
			const key = `${rec.day}|${rec.hour}|${rec.officerType}|${rec.recommendedLPM ? '1' : '0'}`
			const prev = mergedBestTimesMap.get(key)
			if (!prev) {
				mergedBestTimesMap.set(key, { ...rec })
				return
			}

			const prevPriority = String(prev.priority ?? 'low')
			const nextPriority = String(rec.priority ?? 'low')
			mergedBestTimesMap.set(key, {
				...prev,
				expectedIncidents: Number(prev.expectedIncidents ?? 0) + Number(rec.expectedIncidents ?? 0),
				recommendedOfficers: Math.max(
					Number(prev.recommendedOfficers ?? 0),
					Number(rec.recommendedOfficers ?? 0)
				),
				priority:
					priorityWeight[nextPriority] >= priorityWeight[prevPriority]
						? rec.priority
						: prev.priority,
				reason:
					String(prev.reason ?? '') === String(rec.reason ?? '')
						? prev.reason
						: `${String(prev.reason ?? '')}; ${String(rec.reason ?? '')}`,
			})
		})
	})
	const mergedBestTimes = Array.from(mergedBestTimesMap.values()).sort(
		(a, b) => Number(b.expectedIncidents ?? 0) - Number(a.expectedIncidents ?? 0)
	)

	const mergedStoreRankingsMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.deploymentRecommendations.storeRankings || []).forEach((ranking) => {
			const key = toSiteIdString(ranking.storeId)
			const prev = mergedStoreRankingsMap.get(key)
			if (!prev) {
				mergedStoreRankingsMap.set(key, { ...ranking })
				return
			}

			const prevCount = Number(prev.incidentCount ?? 0)
			const nextCount = Number(ranking.incidentCount ?? 0)
			const totalCount = prevCount + nextCount
			const weightedRisk =
				totalCount > 0
					? ((Number(prev.riskScore ?? 0) * prevCount) + (Number(ranking.riskScore ?? 0) * nextCount)) /
					  totalCount
					: Math.max(Number(prev.riskScore ?? 0), Number(ranking.riskScore ?? 0))
			const prevRisk = String(prev.riskLevel ?? 'low')
			const nextRisk = String(ranking.riskLevel ?? 'low')

			mergedStoreRankingsMap.set(key, {
				...prev,
				...ranking,
				incidentCount: totalCount,
				riskScore: weightedRisk,
				riskLevel: priorityWeight[nextRisk] >= priorityWeight[prevRisk] ? ranking.riskLevel : prev.riskLevel,
				trend:
					trendWeight[String(ranking.trend ?? 'stable')] >=
					trendWeight[String(prev.trend ?? 'stable')]
						? ranking.trend
						: prev.trend,
				recommendedHours: Array.from(
					new Set([...(prev.recommendedHours || []), ...(ranking.recommendedHours || [])])
				),
			})
		})
	})
	const mergedStoreRankings = Array.from(mergedStoreRankingsMap.values()).sort(
		(a, b) => Number(b.riskScore ?? 0) - Number(a.riskScore ?? 0)
	)

	const mergedMostActiveOffendersMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.repeatOffenders.mostActive || []).forEach((offender) => {
			const key = String(offender.offenderId ?? offender.name ?? '')
			if (!key) return
			const prev = mergedMostActiveOffendersMap.get(key)
			if (!prev) {
				mergedMostActiveOffendersMap.set(key, { ...offender })
				return
			}
			mergedMostActiveOffendersMap.set(key, {
				...prev,
				incidentCount: Number(prev.incidentCount ?? 0) + Number(offender.incidentCount ?? 0),
				totalValue: Number(prev.totalValue ?? 0) + Number(offender.totalValue ?? 0),
				storesTargeted: Array.from(new Set([...(prev.storesTargeted || []), ...(offender.storesTargeted || [])])),
				modusOperandi: Array.from(new Set([...(prev.modusOperandi || []), ...(offender.modusOperandi || [])])),
			})
		})
	})

	const merged = datasets[0]
	const metadataStart = datasets
		.map((d) => d.metadata?.dateRange?.start)
		.filter(Boolean)
		.sort()[0] ?? merged.metadata.dateRange.start
	const metadataEnd = datasets
		.map((d) => d.metadata?.dateRange?.end)
		.filter(Boolean)
		.sort()
		.slice(-1)[0] ?? merged.metadata.dateRange.end

	return {
		...merged,
		crimeTrends: {
			...merged.crimeTrends,
			dayOfWeek: mergedDayOfWeek,
			timeOfDay: mergedTimeOfDay,
			incidentTypes: mergedIncidentTypes,
			storeDrilldown: mergedStoreDrilldown,
			recoveryTrend: mergedRecoveryTrend,
			totalIncidents,
		},
		hotProducts: {
			...merged.hotProducts,
			topProducts: mergedTopProducts,
			topRecoveredProducts: mergedTopRecoveredProducts,
			worstRecoveryProducts: mergedWorstRecoveryProducts,
			storeHeatmap: mergedStoreHeatmap,
			totalValueStolen: financialSummary.totalStolenValue,
			totalValueRecovered: financialSummary.totalRecoveredValue,
			totalValueLost: financialSummary.totalLostValue,
			recoveryRate: financialSummary.recoveryRate,
		},
		financialSummary,
		storeRecoveryComparisons: mergedStoreRecoveryComparisons,
		deploymentRecommendations: {
			...merged.deploymentRecommendations,
			bestTimes: mergedBestTimes,
			storeRankings: mergedStoreRankings,
			overallStrategy:
				datasets.map((d) => d.deploymentRecommendations.overallStrategy).find((s) => String(s).trim().length > 0) ??
				merged.deploymentRecommendations.overallStrategy,
			lastUpdated: datasets
				.map((d) => d.deploymentRecommendations.lastUpdated)
				.filter(Boolean)
				.sort()
				.slice(-1)[0] ?? merged.deploymentRecommendations.lastUpdated,
		},
		repeatOffenders: {
			...merged.repeatOffenders,
			mostActive: Array.from(mergedMostActiveOffendersMap.values()).sort(
				(a, b) => Number(b.incidentCount ?? 0) - Number(a.incidentCount ?? 0)
			),
			totalOffenders: mergedMostActiveOffendersMap.size,
		},
		metadata: {
			...merged.metadata,
			dateRange: {
				start: metadataStart,
				end: metadataEnd,
			},
			generatedAt: new Date().toISOString(),
		},
	}
}

const DataAnalyticsHub = () => {
	const [searchParams, setSearchParams] = useSearchParams()
	const { toast } = useToast()
	const { user } = useAuth()

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
	const enforceSiteScope = useMemo(() => isSiteScopeEnforcedForUser(user), [user])
	const assignedSiteIds = useMemo(() => getAssignedSiteIds(user), [user])
	const assignedSiteIdSet = useMemo(() => new Set(assignedSiteIds), [assignedSiteIds])

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
				const scopedSites = !enforceSiteScope
					? sitesData
					: sitesData.filter((site) => assignedSiteIdSet.has(getSiteIdFromSiteOption(site)))
				const scopedRegionIdSet = new Set(
					scopedSites
						.map((site) =>
							toSiteIdString((site as any).regionId ?? (site as any).fkRegionID ?? (site as any).regionID)
						)
						.filter((id) => id.length > 0)
				)
				const scopedRegions = !enforceSiteScope
					? regionsData
					: regionsData.filter((region) => scopedRegionIdSet.has(toSiteIdString(region.id)))

				setRegions(scopedRegions)
				setSites(scopedSites)
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
	}, [effectiveCustomerId, isAdmin, toast, enforceSiteScope, assignedSiteIdSet])

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
			const baseParams = {
				startDate: dateRange?.from ? format(dateRange.from, 'yyyy-MM-dd') : undefined,
				endDate: dateRange?.to ? format(dateRange.to, 'yyyy-MM-dd') : undefined,
				customerId: effectiveCustomerId,
				regionIds: selectedRegionId !== 'all' ? [Number(selectedRegionId)] : undefined,
			}

			if (enforceSiteScope) {
				const scopedStoreIds = (
					selectedStoreId !== 'all'
						? [selectedStoreId]
						: filteredSites.map((site) => getSiteIdFromSiteOption(site))
				).filter((id) => assignedSiteIdSet.has(id))

				if (scopedStoreIds.length === 0) {
					setData(null)
					setError('No assigned stores available for the selected filters.')
					return
				}

				const scopedDatasets = await Promise.all(
					scopedStoreIds.map((siteId) =>
						analyticsService.getAnalyticsHub({
							...baseParams,
							storeIds: [Number(siteId)],
						})
					)
				)
				setData(mergeAnalyticsHubData(scopedDatasets))
				return
			}

			const analyticsData = await analyticsService.getAnalyticsHub({
				...baseParams,
				storeIds: selectedStoreId !== 'all' ? [Number(selectedStoreId)] : undefined,
			})
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
	}, [
		dateRange,
		selectedRegionId,
		selectedStoreId,
		effectiveCustomerId,
		toast,
		enforceSiteScope,
		filteredSites,
		assignedSiteIdSet,
	])

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
				`${highestLossStore.storeName} has the highest unrecovered loss at £${formatCurrencyExact(highestLossStore.totalLostValue)}`
			)
		}

		if (bestRecoveredProduct) {
			insights.push(
				`${bestRecoveredProduct.productName} leads recovered value at £${formatCurrencyExact(bestRecoveredProduct.recoveredValue)}`
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
								<div className="mt-4 rounded-xl border border-indigo-300/60 bg-gradient-to-r from-indigo-100/90 via-indigo-50/85 to-sky-100/85 dark:border-indigo-800/60 dark:from-indigo-950/55 dark:via-indigo-900/45 dark:to-sky-950/45 p-3 sm:p-4 shadow-sm">
									<div className="grid grid-cols-2 lg:grid-cols-6 gap-3 sm:gap-4 w-full">
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
												£{formatCurrencyExact(data.financialSummary.totalStolenValue)}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Value Saved
											</div>
											<div className="text-xs sm:text-sm font-semibold text-emerald-700 dark:text-emerald-300 truncate">
												£{formatCurrencyExact(data.financialSummary.totalRecoveredValue)}
											</div>
										</div>
										<div className="text-center sm:text-left min-w-0">
											<div className="text-[11px] sm:text-xs font-semibold uppercase tracking-wide text-muted-foreground truncate">
												Value Lost
											</div>
											<div className="text-xs sm:text-sm font-semibold text-rose-700 dark:text-rose-300 truncate">
												£{formatCurrencyExact(data.financialSummary.totalLostValue)}
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
													className="rounded-lg border border-border bg-background/90 px-3 py-2 text-xs sm:text-sm text-foreground"
												>
													{insight}
												</div>
											))}
										</div>
									)}
								</div>
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
																	£{formatCurrencyExact(store.totalStolenValue)}
																</TableCell>
																<TableCell className="text-right text-emerald-700 dark:text-emerald-300">
																	£{formatCurrencyExact(store.totalRecoveredValue)}
																</TableCell>
																<TableCell className="text-right text-rose-700 dark:text-rose-300">
																	£{formatCurrencyExact(store.totalLostValue)}
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

