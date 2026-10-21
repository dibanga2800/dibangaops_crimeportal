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
import { PageHeader } from '@/components/layout/PageHeader'
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
	RefreshCw,
	Calendar as CalendarIcon,
	Download,
	Filter,
	AlertCircle,
	Shield,
} from 'lucide-react'
import { format, subDays, subMonths, startOfDay } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import { cn } from '@/lib/utils'
import {
	ANALYTICS_DEFAULT_LOOKBACK_DAYS,
	formatAnalyticsDateParam,
	getDefaultAnalyticsDateRange,
	normalizeAnalyticsDateRange,
	parseAnalyticsDateInput,
} from './analyticsDateRange'
import { getOffenderMergeKey } from './offenderDisplay'
import { customerDashboardService } from '@/services/dashboardService'
import type { Region, Site } from '@/types/dashboard'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { useAvailableCustomers } from '@/hooks/useAvailableCustomers'
import { useAuth } from '@/hooks/useAuth'
import { getAssignedSiteIds, isSiteScopeEnforcedForUser } from '@/utils/siteAccess'
import { MapPin, Building2 } from 'lucide-react'
import { FilterBar, DateRangeField, FilterChip } from '@/components/common/FilterBar'
import { Segmented } from '@/components/common/Segmented'
import { StatLedger, type StatLedgerItem } from '@/components/common/StatLedger'
import { EmptyState } from '@/components/common/EmptyState'

type PeriodPreset = '30d' | '6m' | '12m' | 'custom'
const PERIOD_PRESETS: ReadonlyArray<{ value: PeriodPreset; label: string }> = [
	{ value: '30d', label: '30 days' },
	{ value: '6m', label: '6 months' },
	{ value: '12m', label: '12 months' },
]

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

	const mergeCountRecord = (
		left: Record<string, number> = {},
		right: Record<string, number> = {},
	): Record<string, number> => {
		const merged: Record<string, number> = { ...left }
		Object.entries(right).forEach(([key, value]) => {
			merged[key] = (merged[key] ?? 0) + Number(value ?? 0)
		})
		return merged
	}

	const mergeTypesByKey = (
		left: Record<string, any[]> = {},
		right: Record<string, any[]> = {},
	): Record<string, any[]> => {
		const keys = new Set([...Object.keys(left), ...Object.keys(right)])
		const merged: Record<string, any[]> = {}
		keys.forEach((key) => {
			merged[key] = mergeIncidentTypes([...(left[key] ?? []), ...(right[key] ?? [])])
		})
		return merged
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
					incidentsByDay: { ...(store.incidentsByDay || {}) },
					incidentsByHour: { ...(store.incidentsByHour || {}) },
					incidentTypesByDay: { ...(store.incidentTypesByDay || {}) },
					incidentTypesByHour: { ...(store.incidentTypesByHour || {}) },
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
				incidentsByDay: mergeCountRecord(prev.incidentsByDay, store.incidentsByDay),
				incidentsByHour: mergeCountRecord(prev.incidentsByHour, store.incidentsByHour),
				incidentTypesByDay: mergeTypesByKey(prev.incidentTypesByDay, store.incidentTypesByDay),
				incidentTypesByHour: mergeTypesByKey(prev.incidentTypesByHour, store.incidentTypesByHour),
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

	const mergeProductStores = (left: any[] = [], right: any[] = []) => {
		const map = new Map<string, any>()
		;[...left, ...right].forEach((store) => {
			const storeName = String(store?.storeName ?? '').trim()
			if (!storeName) return
			const storeKey = `${Number(store?.storeId ?? 0)}|${storeName}`
			const prev = map.get(storeKey)
			if (!prev) {
				map.set(storeKey, {
					storeId: Number(store?.storeId ?? 0),
					storeName,
					frequency: Number(store?.frequency ?? 0),
					stolenValue: Number(store?.stolenValue ?? 0),
					recoveredValue: Number(store?.recoveredValue ?? 0),
					lostValue: Number(store?.lostValue ?? 0),
				})
				return
			}

			const stolenValue = Number(prev.stolenValue ?? 0) + Number(store?.stolenValue ?? 0)
			const recoveredValue = Number(prev.recoveredValue ?? 0) + Number(store?.recoveredValue ?? 0)
			map.set(storeKey, {
				...prev,
				frequency: Number(prev.frequency ?? 0) + Number(store?.frequency ?? 0),
				stolenValue,
				recoveredValue,
				lostValue: Number(prev.lostValue ?? 0) + Number(store?.lostValue ?? 0),
				recoveryRate: stolenValue > 0 ? (recoveredValue / stolenValue) * 100 : 0,
			})
		})

		return Array.from(map.values()).sort(
			(a, b) => Number(b.lostValue ?? 0) - Number(a.lostValue ?? 0) || Number(b.frequency ?? 0) - Number(a.frequency ?? 0),
		)
	}

	const mergeProducts = (products: any[]) => {
		const map = new Map<string, any>()
		products.forEach((product) => {
			const key = `${String(product?.barcode ?? '')}|${String(product?.productName ?? '')}`
			const prev = map.get(key)
			if (!prev) {
				const stores = mergeProductStores(product?.stores ?? [])
				map.set(key, {
					...product,
					frequency: Number(product?.frequency ?? 0),
					totalValue: Number(product?.totalValue ?? 0),
					stolenValue: Number(product?.stolenValue ?? 0),
					recoveredValue: Number(product?.recoveredValue ?? 0),
					lostValue: Number(product?.lostValue ?? 0),
					stores,
					storesAffected: stores.length,
				})
				return
			}

			const stolenValue = Number(prev.stolenValue ?? 0) + Number(product?.stolenValue ?? 0)
			const recoveredValue = Number(prev.recoveredValue ?? 0) + Number(product?.recoveredValue ?? 0)
			const stores = mergeProductStores(prev.stores, product?.stores)
			map.set(key, {
				...prev,
				frequency: Number(prev.frequency ?? 0) + Number(product?.frequency ?? 0),
				totalValue: Number(prev.totalValue ?? 0) + Number(product?.totalValue ?? 0),
				stolenValue,
				recoveredValue,
				lostValue: Number(prev.lostValue ?? 0) + Number(product?.lostValue ?? 0),
				stores,
				storesAffected: stores.length,
				recoveryRate: stolenValue > 0 ? (recoveredValue / stolenValue) * 100 : 0,
			})
		})
		return Array.from(map.values())
	}

	const mergedTopProducts = mergeProducts(datasets.flatMap((d) => d.hotProducts.topProducts || []))
	const mergedTopRecoveredProducts = [...mergedTopProducts]
		.sort((a, b) => Number(b.recoveredValue ?? 0) - Number(a.recoveredValue ?? 0))
		.slice(0, 10)
	const mergedWorstRecoveryProducts = mergeProducts(
		datasets.flatMap((d) => d.hotProducts.worstRecoveryProducts || []),
	)
		.filter((p) => Number(p.stolenValue ?? 0) > 0)
		.sort(
			(a, b) =>
				Number(a.recoveryRate ?? 0) - Number(b.recoveryRate ?? 0) ||
				Number(b.lostValue ?? 0) - Number(a.lostValue ?? 0),
		)
		.slice(0, 20)

	const mergeRiskFactors = (left: any[] = [], right: any[] = []): any[] => {
		const map = new Map<string, any>()
		;[...left, ...right].forEach((factor) => {
			const key = String(factor?.factor ?? factor?.description ?? '')
			if (!key) return
			const prev = map.get(key)
			if (!prev) {
				map.set(key, { ...factor })
				return
			}
			map.set(key, {
				...prev,
				score: Math.max(Number(prev.score ?? 0), Number(factor?.score ?? 0)),
			})
		})
		return Array.from(map.values())
	}

	const mergedStoreHeatmapMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.hotProducts.storeHeatmap || []).forEach((store) => {
			const storeName = String((store as any).storeName ?? '').trim()
			const siteKey = toSiteIdString((store as any).storeId)
			const key = siteKey && siteKey !== '0' ? siteKey : storeName
			if (!key) return
			const prev = mergedStoreHeatmapMap.get(key)
			if (!prev) {
				mergedStoreHeatmapMap.set(key, {
					...store,
					storeName,
					products: mergeProducts((store as any).products || []),
				})
				return
			}

			const totalValueStolen = Number(prev.totalValueStolen ?? 0) + Number((store as any).totalValueStolen ?? 0)
			const totalValueRecovered =
				Number(prev.totalValueRecovered ?? 0) + Number((store as any).totalValueRecovered ?? 0)
			const totalValueLost = Number(prev.totalValueLost ?? 0) + Number((store as any).totalValueLost ?? 0)
			const mergedRiskScore = Math.max(Number(prev.riskScore ?? 0), Number((store as any).riskScore ?? 0))
			const mergedRiskLevel =
				priorityWeight[String((store as any).riskLevel ?? 'low')] >=
				priorityWeight[String(prev.riskLevel ?? 'low')]
					? (store as any).riskLevel
					: prev.riskLevel
			const mergedProducts = mergeProducts([...(prev.products || []), ...((store as any).products || [])])

			mergedStoreHeatmapMap.set(key, {
				...prev,
				...(store as any),
				storeName: storeName || prev.storeName,
				totalIncidents: Number(prev.totalIncidents ?? 0) + Number((store as any).totalIncidents ?? 0),
				incidentsWithStolenItems:
					Number(prev.incidentsWithStolenItems ?? 0) +
					Number((store as any).incidentsWithStolenItems ?? 0),
				productLineCount: Number(prev.productLineCount ?? 0) + Number((store as any).productLineCount ?? 0),
				productGroupCount: mergedProducts.length,
				totalValueStolen,
				totalValueRecovered,
				totalValueLost,
				riskScore: mergedRiskScore,
				riskLevel: mergedRiskLevel,
				riskFactors: mergeRiskFactors(prev.riskFactors, (store as any).riskFactors),
				riskSummary:
					mergedRiskScore >= Number((store as any).riskScore ?? 0)
						? (store as any).riskSummary ?? prev.riskSummary
						: prev.riskSummary,
				recoveryRate: totalValueStolen > 0 ? (totalValueRecovered / totalValueStolen) * 100 : 0,
				products: mergedProducts,
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
				reasonDetails: Array.from(
					new Set([...(prev.reasonDetails || []), ...(rec.reasonDetails || [])])
				),
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
				reason: ranking.reason || prev.reason,
				reasonDetails: Array.from(
					new Set([...(prev.reasonDetails || []), ...(ranking.reasonDetails || [])])
				),
				riskFactors: (ranking.riskFactors?.length ? ranking.riskFactors : prev.riskFactors) || [],
			})
		})
	})
	const mergedStoreRankings = Array.from(mergedStoreRankingsMap.values()).sort(
		(a, b) => Number(b.riskScore ?? 0) - Number(a.riskScore ?? 0)
	)

	const mergedMostActiveOffendersMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.repeatOffenders.mostActive || []).forEach((offender) => {
			const key = getOffenderMergeKey(offender)
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
				incidents: [...(prev.incidents || []), ...(offender.incidents || [])],
			})
		})
	})

	const mergedCrossStoreMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.repeatOffenders.crossStoreMovements || []).forEach((movement) => {
			const key = getOffenderMergeKey({
				offenderId: movement.offenderId,
				name: movement.offenderName,
			})
			if (!key) return
			const prev = mergedCrossStoreMap.get(key)
			if (!prev) {
				mergedCrossStoreMap.set(key, {
					...movement,
					movements: [...(movement.movements || [])],
				})
				return
			}
			const combinedMovements = [...(prev.movements || []), ...(movement.movements || [])].sort(
				(a, b) =>
					String(a.dateTimeLabel || a.date || '').localeCompare(
						String(b.dateTimeLabel || b.date || ''),
					),
			)
			const storeNames = new Set<string>()
			combinedMovements.forEach((m) => {
				const name = m.storeName || m.toStore || m.fromStore
				if (name) storeNames.add(name)
			})
			mergedCrossStoreMap.set(key, {
				...prev,
				...movement,
				movements: combinedMovements,
				totalStores: Math.max(Number(prev.totalStores ?? 0), storeNames.size),
			})
		})
	})

	const mergedNetworkNodesMap = new Map<string, any>()
	const mergedNetworkLinksMap = new Map<string, any>()
	datasets.forEach((dataset) => {
		(dataset.repeatOffenders.networkMap?.nodes || []).forEach((node) => {
			if (!mergedNetworkNodesMap.has(node.id)) {
				mergedNetworkNodesMap.set(node.id, { ...node })
			}
		})
		;(dataset.repeatOffenders.networkMap?.links || []).forEach((link) => {
			const key = `${link.source}::${link.target}`
			const prev = mergedNetworkLinksMap.get(key)
			if (!prev) {
				mergedNetworkLinksMap.set(key, { ...link })
				return
			}
			mergedNetworkLinksMap.set(key, {
				...prev,
				incidentCount: Number(prev.incidentCount ?? 0) + Number(link.incidentCount ?? 0),
				strength: Math.min(
					1,
					Math.max(Number(prev.strength ?? 0), Number(link.strength ?? 0)),
				),
			})
		})
	})

	const mergedClustersMap = new Map<string, any>()
	const mergedChainsMap = new Map<string, any>()
	const linkedIncidentIdSet = new Set<string>()
	datasets.forEach((dataset) => {
		(dataset.crimeLinking?.clusters || []).forEach((cluster) => {
			const key = String(cluster.clusterId ?? '')
			if (!key) return
			const prev = mergedClustersMap.get(key)
			if (!prev) {
				mergedClustersMap.set(key, {
					...cluster,
					incidents: [...(cluster.incidents || [])],
				})
			} else {
				const incidentMap = new Map<string, any>()
				;[...(prev.incidents || []), ...(cluster.incidents || [])].forEach((incident) => {
					incidentMap.set(String(incident.incidentId), incident)
				})
				mergedClustersMap.set(key, {
					...prev,
					...cluster,
					incidents: Array.from(incidentMap.values()),
					totalValue:
						Number(prev.totalValue ?? 0) + Number(cluster.totalValue ?? 0),
				})
			}
			;(cluster.incidents || []).forEach((i) => linkedIncidentIdSet.add(String(i.incidentId)))
		})
		;(dataset.crimeLinking?.offenderChains || []).forEach((chain) => {
			const key = String(chain.chainId ?? chain.offenderId ?? '')
			if (!key) return
			const prev = mergedChainsMap.get(key)
			if (!prev) {
				mergedChainsMap.set(key, {
					...chain,
					incidents: [...(chain.incidents || [])],
					timeline: [...(chain.timeline || [])],
				})
			} else {
				const incidentMap = new Map<string, any>()
				;[...(prev.incidents || []), ...(chain.incidents || [])].forEach((incident) => {
					incidentMap.set(String(incident.incidentId), incident)
				})
				mergedChainsMap.set(key, {
					...prev,
					...chain,
					incidents: Array.from(incidentMap.values()),
					timeline: [...(prev.timeline || []), ...(chain.timeline || [])].sort((a, b) =>
						String(a.dateTimeLabel || a.date || '').localeCompare(
							String(b.dateTimeLabel || b.date || ''),
						),
					),
					totalValue: Number(prev.totalValue ?? 0) + Number(chain.totalValue ?? 0),
				})
			}
			;(chain.incidents || []).forEach((i) => linkedIncidentIdSet.add(String(i.incidentId)))
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
			crossStoreMovements: Array.from(mergedCrossStoreMap.values()).sort(
				(a, b) => Number(b.totalStores ?? 0) - Number(a.totalStores ?? 0)
			),
			networkMap: {
				nodes: Array.from(mergedNetworkNodesMap.values()),
				links: Array.from(mergedNetworkLinksMap.values()),
			},
			totalOffenders: mergedMostActiveOffendersMap.size,
		},
		crimeLinking: {
			...merged.crimeLinking,
			clusters: Array.from(mergedClustersMap.values()).sort(
				(a, b) => (b.incidents?.length ?? 0) - (a.incidents?.length ?? 0)
			),
			offenderChains: Array.from(mergedChainsMap.values()).sort(
				(a, b) => (b.incidents?.length ?? 0) - (a.incidents?.length ?? 0)
			),
			totalLinkedIncidents: linkedIncidentIdSet.size,
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
	const [dateRange, setDateRange] = useState<DateRange | undefined>(getDefaultAnalyticsDateRange)
	const [regions, setRegions] = useState<Region[]>([])
	const [sites, setSites] = useState<Site[]>([])
	const [selectedRegionId, setSelectedRegionId] = useState<string>('all')
	const [selectedStoreId, setSelectedStoreId] = useState<string>('all')
	const [loadingFilters, setLoadingFilters] = useState(true)
	const [activeTab, setActiveTab] = useState('crime-trends')
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
		if (selectedCustomerId != null) return selectedCustomerId
		if (isAdmin && availableCustomers.length > 0) return availableCustomers[0].id
		return undefined
	}, [urlCustomerId, selectedCustomerId, isAdmin, availableCustomers])

	// Auto-select first customer for admins when none is chosen yet.
	useEffect(() => {
		if (!isAdmin || loadingCustomers) return
		if (urlCustomerId) return
		if (availableCustomers.length === 0) return

		const firstId = availableCustomers[0].id
		setSelectedCustomerForAdmin(firstId)
		if (selectedCustomerId !== firstId) {
			setSelectedCustomerId(firstId)
		}
		const params = new URLSearchParams(searchParams)
		if (params.get('customerId') !== String(firstId)) {
			params.set('customerId', String(firstId))
			setSearchParams(params, { replace: true })
		}
	}, [
		isAdmin,
		loadingCustomers,
		urlCustomerId,
		availableCustomers,
		selectedCustomerId,
		setSelectedCustomerId,
		searchParams,
		setSearchParams,
	])

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
			const effectiveRange = normalizeAnalyticsDateRange(dateRange)
			const baseParams = {
				startDate: formatAnalyticsDateParam(effectiveRange.from!),
				endDate: formatAnalyticsDateParam(effectiveRange.to!),
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
		setDateRange(normalizeAnalyticsDateRange(range))
	}

	const handleRefresh = () => {
		loadData()
	}

	const handleQuickDateRangeDays = (days: number) => {
		const to = startOfDay(new Date())
		setDateRange({
			from: startOfDay(subDays(to, days)),
			to,
		})
	}

	const handleQuickDateRangeMonths = (months: number) => {
		const to = startOfDay(new Date())
		setDateRange({
			from: startOfDay(subMonths(to, months)),
			to,
		})
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
			<div className="space-y-6">
				<PageHeader title="Data analytics hub" className="pb-0" />
				<div className="rounded-lg border border-border bg-card">
					<EmptyState
						icon={<AlertCircle className="h-5 w-5 text-loss" />}
						title="Analytics could not be loaded"
						description={error}
						action={
							<Button onClick={handleRefresh}>
								<RefreshCw className="h-4 w-4" />
								Try again
							</Button>
						}
					/>
				</div>
			</div>
		)
	}

	const fromValue = dateRange?.from ? format(dateRange.from, 'yyyy-MM-dd') : ''
	const toValue = dateRange?.to ? format(dateRange.to, 'yyyy-MM-dd') : ''
	const activePreset: PeriodPreset = (() => {
		if (!dateRange?.from || !dateRange?.to) return 'custom'
		const to = startOfDay(dateRange.to).getTime()
		const from = startOfDay(dateRange.from).getTime()
		if (to !== startOfDay(new Date()).getTime()) return 'custom'
		if (from === startOfDay(subDays(new Date(to), ANALYTICS_DEFAULT_LOOKBACK_DAYS)).getTime()) return '30d'
		if (from === startOfDay(subMonths(new Date(to), 6)).getTime()) return '6m'
		if (from === startOfDay(subMonths(new Date(to), 12)).getTime()) return '12m'
		return 'custom'
	})()
	const applyPreset = (preset: PeriodPreset) => {
		if (preset === '30d') handleQuickDateRangeDays(ANALYTICS_DEFAULT_LOOKBACK_DAYS)
		if (preset === '6m') handleQuickDateRangeMonths(6)
		if (preset === '12m') handleQuickDateRangeMonths(12)
	}
	const selectedRegionName = regions.find((r) => String(r.id) === selectedRegionId)?.name
	const selectedStoreName = (() => {
		const site = filteredSites.find((s) => String((s as any).siteID || (s as any).id) === selectedStoreId) as any
		return site ? site.locationName || site.name : undefined
	})()
	const scopeFilters = (selectedRegionId !== 'all' ? 1 : 0) + (selectedStoreId !== 'all' ? 1 : 0)

	const trend = data?.crimeTrends.recoveryTrend ?? []
	const series = (pick: (p: (typeof trend)[number]) => number) => (trend.length > 2 ? trend.map(pick) : undefined)
	const pounds = (v: number) => `£${Math.round(v).toLocaleString('en-GB')}`
	const ledgerItems: StatLedgerItem[] = data
		? [
				{
					label: 'Incidents',
					value: data.crimeTrends.totalIncidents.toLocaleString('en-GB'),
					hint: `${format(new Date(data.metadata.dateRange.start), 'd MMM')} to ${format(new Date(data.metadata.dateRange.end), 'd MMM yyyy')}`,
					trend: series((p) => p.incidentCount),
				},
				{ label: 'Value stolen', value: pounds(data.financialSummary.totalStolenValue), trend: series((p) => p.stolenValue) },
				{
					label: 'Recovered',
					value: pounds(data.financialSummary.totalRecoveredValue),
					tone: 'recovered',
					trend: series((p) => p.recoveredValue),
				},
				{ label: 'Lost', value: pounds(data.financialSummary.totalLostValue), tone: 'loss', trend: series((p) => p.lostValue) },
				{ label: 'Recovery rate', value: `${data.financialSummary.recoveryRate.toFixed(1)}%`, hint: 'Share of stolen value recovered' },
				{ label: 'Offenders tracked', value: data.repeatOffenders.totalOffenders.toLocaleString('en-GB'), hint: 'Seen in this period' },
			]
		: []

	const riskTotal = aiRiskSummary ? aiRiskSummary.highCount + aiRiskSummary.mediumCount + aiRiskSummary.lowCount : 0
	const riskTone: Record<string, string> = { high: 'text-loss', critical: 'text-loss', medium: 'text-warning', low: 'text-recovered' }

	const tabs = [
		{ value: 'crime-trends', label: 'Crime trends' },
		{ value: 'deployment', label: 'Deployment' },
		{ value: 'hot-products', label: 'Hot products' },
		{ value: 'recovery-performance', label: 'Recovery by store' },
		{ value: 'repeat-offenders', label: 'Offenders' },
		{ value: 'crime-linking', label: 'Crime linking' },
	]

	return (
		<ErrorBoundary>
			<div className="w-full space-y-5">
				<PageHeader
					title="Data analytics hub"
					description="Crime trends, hot products, repeat offenders and where to deploy store detectives, for the dates and stores you choose."
					className="pb-0"
					actions={
						<>
							<Button variant="outline" onClick={handleRefresh} disabled={loading}>
								<RefreshCw className={cn('h-4 w-4', loading && 'animate-spin')} />
								Refresh
							</Button>
							<Button variant="outline" onClick={handleExport}>
								<Download className="h-4 w-4" />
								Export
							</Button>
						</>
					}
				/>

				<FilterBar
					label="Analytics filters"
					activeCount={scopeFilters + (activePreset === 'custom' ? 1 : 0)}
					primary={
						<Segmented<PeriodPreset>
							ariaLabel="Period"
							value={activePreset}
							onChange={applyPreset}
							options={PERIOD_PRESETS}
							className="w-full sm:w-auto [&>button]:flex-1"
						/>
					}
					actions={
						scopeFilters > 0 ? (
							<Button
								variant="ghost"
								size="sm"
								onClick={() => {
									setSelectedRegionId('all')
									setSelectedStoreId('all')
								}}
							>
								Clear region and store
							</Button>
						) : null
					}
					summary={
						<>
							<span>Showing</span>
							<FilterChip>{isAdmin ? availableCustomers.find((c) => c.id === selectedCustomerForAdmin)?.name ?? 'No company selected' : 'Your stores'}</FilterChip>
							<FilterChip>
								{dateRange?.from && dateRange?.to
									? `${format(dateRange.from, 'd MMM yyyy')} to ${format(dateRange.to, 'd MMM yyyy')}`
									: 'All dates'}
							</FilterChip>
							{selectedRegionName ? (
								<FilterChip onRemove={() => setSelectedRegionId('all')} removeLabel="Remove region filter">
									{selectedRegionName}
								</FilterChip>
							) : null}
							{selectedStoreName ? (
								<FilterChip onRemove={() => setSelectedStoreId('all')} removeLabel="Remove store filter">
									{selectedStoreName}
								</FilterChip>
							) : null}
						</>
					}
				>
					{isAdmin && (
						<Select
							disabled={loadingCustomers || availableCustomers.length === 0}
							value={selectedCustomerForAdmin?.toString() ?? ''}
							onValueChange={(value) => {
								const id = parseInt(value, 10)
								setSelectedCustomerForAdmin(id)
								setSelectedCustomerId(id)
								const params = new URLSearchParams(searchParams)
								params.set('customerId', value)
								setSearchParams(params, { replace: true })
								loadData()
							}}
						>
							<SelectTrigger className="h-9 w-full gap-2 sm:w-56" aria-label="Company">
								<Building2 className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
								<SelectValue placeholder={loadingCustomers ? 'Loading companies…' : 'Select company'} />
							</SelectTrigger>
							<SelectContent>
								{availableCustomers.map((c) => (
									<SelectItem key={c.id} value={c.id.toString()}>
										{c.name}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					)}
					<DateRangeField
						from={fromValue}
						to={toValue}
						onFromChange={(value) => {
							const parsed = parseAnalyticsDateInput(value)
							setDateRange((prev) => normalizeAnalyticsDateRange({ from: parsed ?? prev?.from, to: prev?.to }))
						}}
						onToChange={(value) => {
							const parsed = parseAnalyticsDateInput(value)
							setDateRange((prev) => normalizeAnalyticsDateRange({ from: prev?.from, to: parsed ?? prev?.to }))
						}}
					/>
					<Select value={selectedRegionId} onValueChange={setSelectedRegionId} disabled={loadingFilters}>
						<SelectTrigger className="h-9 w-full gap-2 sm:w-44" aria-label="Region">
							<SelectValue placeholder="All regions" />
						</SelectTrigger>
						<SelectContent>
							<SelectItem value="all">All regions</SelectItem>
							{regions.map((region) => (
								<SelectItem key={region.id} value={String(region.id)}>
									{region.name}
								</SelectItem>
							))}
						</SelectContent>
					</Select>
					<Select value={selectedStoreId} onValueChange={setSelectedStoreId} disabled={loadingFilters || filteredSites.length === 0}>
						<SelectTrigger className="h-9 w-full gap-2 sm:w-48" aria-label="Store">
							<MapPin className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
							<SelectValue placeholder="All stores" />
						</SelectTrigger>
						<SelectContent>
							<SelectItem value="all">All stores</SelectItem>
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
				</FilterBar>

				{loading && !data && (
					<div className="space-y-5" aria-busy="true" aria-label="Loading analytics">
						<StatLedger items={Array.from({ length: 6 }, (_, i) => ({ label: ' ', value: ' ', key: i }))} loading />
						<Skeleton className="h-[420px] w-full rounded-lg" />
					</div>
				)}

				{data && (
					<>
						<StatLedger items={ledgerItems} />

						{data.crimeTrends.totalIncidents === 0 ? (
							<div className="rounded-lg border border-border bg-card">
								<EmptyState
									icon={<CalendarIcon className="h-5 w-5" />}
									title="No incidents in this period"
									description="The hub opens on the last 30 days. If your records are older, widen the period to include them."
									action={
										<div className="flex flex-wrap justify-center gap-2">
											<Button variant="outline" size="sm" onClick={() => handleQuickDateRangeMonths(6)}>
												Last 6 months
											</Button>
											<Button variant="outline" size="sm" onClick={() => handleQuickDateRangeMonths(12)}>
												Last 12 months
											</Button>
										</div>
									}
								/>
							</div>
						) : (aiRiskSummary || recoveryInsights.length > 0) ? (
							<div className="grid gap-4 lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)]">
								{aiRiskSummary && (
									<section aria-labelledby="risk-heading" className="rounded-lg border border-border bg-card p-4 sm:p-5">
										<div className="flex items-start justify-between gap-3">
											<div>
												<h2 id="risk-heading" className="flex items-center gap-2 text-sm font-semibold text-foreground">
													<Shield className="h-4 w-4 text-muted-foreground" aria-hidden />
													Store risk
												</h2>
												<p className="mt-0.5 text-[13px] text-muted-foreground">From the deployment risk engine</p>
											</div>
											<span className={cn('font-display text-2xl font-semibold capitalize leading-none', riskTone[aiRiskSummary.overallLevel])}>
												{aiRiskSummary.overallLevel}
											</span>
										</div>
										<div className="mt-4 flex h-2.5 overflow-hidden rounded-full bg-muted" role="img" aria-label={`${aiRiskSummary.highCount} high, ${aiRiskSummary.mediumCount} medium and ${aiRiskSummary.lowCount} low risk stores`}>
											{riskTotal > 0 && (
												<>
													<span className="bg-loss" style={{ width: `${(aiRiskSummary.highCount / riskTotal) * 100}%` }} />
													<span className="bg-warning" style={{ width: `${(aiRiskSummary.mediumCount / riskTotal) * 100}%` }} />
													<span className="bg-recovered" style={{ width: `${(aiRiskSummary.lowCount / riskTotal) * 100}%` }} />
												</>
											)}
										</div>
										<dl className="figures mt-3 grid grid-cols-3 gap-2 text-sm">
											{[
												{ label: 'High', value: aiRiskSummary.highCount, dot: 'bg-loss' },
												{ label: 'Medium', value: aiRiskSummary.mediumCount, dot: 'bg-warning' },
												{ label: 'Low', value: aiRiskSummary.lowCount, dot: 'bg-recovered' },
											].map((row) => (
												<div key={row.label}>
													<dt className="flex items-center gap-1.5 text-xs text-muted-foreground">
														<span className={cn('h-2 w-2 rounded-full', row.dot)} aria-hidden />
														{row.label}
													</dt>
													<dd className="font-semibold text-foreground">{row.value} {row.value === 1 ? 'store' : 'stores'}</dd>
												</div>
											))}
										</dl>
										<p className="mt-4 border-t border-border pt-3 text-[13px] text-muted-foreground">
											Highest risk: <span className="font-semibold text-foreground">{aiRiskSummary.topStoreName}</span>{' '}
											<span className="text-muted-foreground">({aiRiskSummary.topStoreLevelLabel})</span>
										</p>
									</section>
								)}
								{recoveryInsights.length > 0 && (
									<section aria-labelledby="insights-heading" className="rounded-lg border border-border bg-card p-4 sm:p-5">
										<h2 id="insights-heading" className="text-sm font-semibold text-foreground">What stands out</h2>
										<ol className="mt-3 divide-y divide-border">
											{recoveryInsights.map((insight, index) => (
												<li key={insight} className="flex gap-3 py-2.5 first:pt-0 last:pb-0">
													<span className="figures mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-accent text-[11px] font-semibold text-accent-foreground">
														{index + 1}
													</span>
													<p className="text-sm leading-6 text-foreground">{insight}</p>
												</li>
											))}
										</ol>
									</section>
								)}
							</div>
						) : null}

						<Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
							<div className="sticky top-16 z-20 -mx-4 bg-background/95 px-4 backdrop-blur sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8">
								<div className="overflow-x-auto">
									<TabsList className="h-auto w-full min-w-max justify-start gap-1 rounded-none border-0 border-b border-border bg-transparent p-0 sm:min-w-0">
										{tabs.map((tab) => (
											<TabsTrigger
												key={tab.value}
												value={tab.value}
												className="-mb-px rounded-none border-0 border-b-2 border-transparent px-3 py-3 text-sm font-medium text-muted-foreground data-[state=active]:border-primary data-[state=active]:bg-transparent data-[state=active]:font-semibold data-[state=active]:text-foreground data-[state=active]:shadow-none"
											>
												{tab.label}
											</TabsTrigger>
										))}
									</TabsList>
								</div>
							</div>

							<TabsContent value="crime-trends" className="mt-5 overflow-x-hidden">
								<ErrorBoundary>
									<CrimeTrendExplorer data={data.crimeTrends} loading={loading} />
								</ErrorBoundary>
							</TabsContent>

							<TabsContent value="deployment" className="mt-5 overflow-x-hidden">
								<ErrorBoundary>
									<ResourceDeploymentEngine data={data.deploymentRecommendations} loading={loading} />
								</ErrorBoundary>
							</TabsContent>

							<TabsContent value="hot-products" className="mt-5 overflow-x-hidden">
								<ErrorBoundary>
									<HotProductsDashboard data={data.hotProducts} loading={loading} sites={filteredSites} />
								</ErrorBoundary>
							</TabsContent>

							<TabsContent value="recovery-performance" className="mt-5 overflow-x-hidden">
								<section className="rounded-lg border border-border bg-card">
									<div className="p-4 sm:p-5">
										<h2 className="text-base font-semibold text-foreground">Recovered and lost by store</h2>
										<p className="mt-0.5 text-[13px] text-muted-foreground">
											Stores ordered as the engine ranks them. The bar shows how much of the stolen value each store got back.
										</p>
									</div>
									{/* Phones: one row per store */}
									<ul className="divide-y divide-border border-t border-border sm:hidden">
										{data.storeRecoveryComparisons.slice(0, 12).map((store) => (
											<li key={`${store.storeId}-${store.storeName}`} className="px-4 py-3">
												<div className="flex items-baseline justify-between gap-3">
													<span className="truncate text-sm font-medium text-foreground">{store.storeName}</span>
													<span className="figures text-sm font-semibold text-foreground">{store.recoveryRate.toFixed(1)}%</span>
												</div>
												<RecoveryMeter rate={store.recoveryRate} className="mt-2" />
												<div className="figures mt-2 flex justify-between text-xs text-muted-foreground">
													<span>{store.incidentCount} incidents</span>
													<span>
														<span className="text-recovered">£{formatCurrencyExact(store.totalRecoveredValue)}</span> back ·{' '}
														<span className="text-loss">£{formatCurrencyExact(store.totalLostValue)}</span> lost
													</span>
												</div>
											</li>
										))}
									</ul>
									<div className="hidden border-t border-border sm:block">
										<Table>
											<TableHeader>
												<TableRow>
													<TableHead>Store</TableHead>
													<TableHead className="text-right">Incidents</TableHead>
													<TableHead className="text-right">Stolen</TableHead>
													<TableHead className="text-right">Recovered</TableHead>
													<TableHead className="text-right">Lost</TableHead>
													<TableHead className="w-[28%]">Recovery rate</TableHead>
												</TableRow>
											</TableHeader>
											<TableBody>
												{data.storeRecoveryComparisons.slice(0, 12).map((store) => (
													<TableRow key={`${store.storeId}-${store.storeName}`}>
														<TableCell className="font-medium">{store.storeName}</TableCell>
														<TableCell className="figures text-right">{store.incidentCount}</TableCell>
														<TableCell className="figures text-right">£{formatCurrencyExact(store.totalStolenValue)}</TableCell>
														<TableCell className="figures text-right text-recovered">£{formatCurrencyExact(store.totalRecoveredValue)}</TableCell>
														<TableCell className="figures text-right text-loss">£{formatCurrencyExact(store.totalLostValue)}</TableCell>
														<TableCell>
															<div className="flex items-center gap-3">
																<RecoveryMeter rate={store.recoveryRate} className="flex-1" />
																<span className="figures w-12 text-right text-sm font-semibold">{store.recoveryRate.toFixed(1)}%</span>
															</div>
														</TableCell>
													</TableRow>
												))}
											</TableBody>
										</Table>
									</div>
								</section>
							</TabsContent>

							<TabsContent value="repeat-offenders" className="mt-5 overflow-x-hidden">
								<ErrorBoundary>
									<RepeatOffenderAnalysis data={data.repeatOffenders} loading={loading} />
								</ErrorBoundary>
							</TabsContent>

							<TabsContent value="crime-linking" className="mt-5 overflow-x-hidden">
								<ErrorBoundary>
									<CrimeLinkingPanel data={data.crimeLinking} loading={loading} />
								</ErrorBoundary>
							</TabsContent>
						</Tabs>
					</>
				)}

				{error && data && (
					<div role="status" className="flex flex-col gap-2 rounded-lg border border-warning/30 bg-warning-soft px-4 py-3 text-sm text-warning sm:flex-row sm:items-center">
						<AlertCircle className="h-4 w-4 shrink-0" aria-hidden />
						<span className="flex-1">These figures may be out of date: {error}</span>
						<Button variant="ghost" size="sm" onClick={handleRefresh}>
							<RefreshCw className="h-4 w-4" />
							Retry
						</Button>
					</div>
				)}
			</div>
		</ErrorBoundary>
	)
}

function RecoveryMeter({ rate, className }: { rate: number; className?: string }) {
	const width = Math.max(0, Math.min(100, rate))
	return (
		<div className={cn('h-1.5 overflow-hidden rounded-full bg-loss/20', className)} aria-hidden>
			<div className="h-full rounded-full bg-recovered" style={{ width: `${width}%` }} />
		</div>
	)
}

export default DataAnalyticsHub
