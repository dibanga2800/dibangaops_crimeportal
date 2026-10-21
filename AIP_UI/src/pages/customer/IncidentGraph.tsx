import React, { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import {
	BarChart,
	Bar,
	XAxis,
	YAxis,
	CartesianGrid,
	Tooltip,
	ResponsiveContainer,
	LabelList,
	Cell,
} from 'recharts'
import { Button } from '@/components/ui/button'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Loader2, Building2, Users, BarChart3 } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { FilterBar, DateRangeField } from '@/components/common/FilterBar'
import { Segmented } from '@/components/common/Segmented'
import { StatLedger, type StatLedgerItem } from '@/components/common/StatLedger'
import { EmptyState } from '@/components/common/EmptyState'
import {
	startOfWeek,
	endOfWeek,
	startOfMonth,
	endOfMonth,
	startOfYear,
	format,
	subDays,
} from 'date-fns'
import { cn } from '@/lib/utils'
import { useChartTheme, chartTooltipStyle } from '@/lib/chart-theme'
import { useAuth } from '@/hooks/useAuth'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { useAvailableCustomers } from '@/hooks/useAvailableCustomers'
import {
	incidentGraphService,
	type IncidentGraphData,
	type IncidentTypeData,
	type IncidentGraphFilters,
	type RegionOption,
} from '@/services/incidentGraphService'
import { getCustomerNameById } from '@/services/customerMappingService'

/** Bar colours for the incident graph; one per store so neighbouring bars stay distinct. */
const BAR_PALETTE = ['#3b82f6', '#8b5cf6', '#ec4899', '#f97316', '#10b981', '#06b6d4', '#6366f1', '#f43f5e', '#eab308', '#14b8a6']

const MEASURES: ReadonlyArray<{ value: GraphType; label: string; total: string; chartTitle: string }> = [
	{ value: 'value', label: 'Value recovered', total: 'Value recovered', chartTitle: 'Value recovered by store' },
	{ value: 'lost', label: 'Value lost', total: 'Value lost', chartTitle: 'Value lost by store' },
	{ value: 'quantity', label: 'Items recovered', total: 'Items recovered', chartTitle: 'Items recovered by store' },
	{ value: 'count', label: 'Incidents', total: 'Incidents reported', chartTitle: 'Incidents reported by store' },
	{ value: 'type', label: 'Incident types', total: 'Incidents', chartTitle: 'Incidents by type' },
]

const PERIODS: ReadonlyArray<{ value: string; label: string }> = [
	{ value: 'ytd', label: 'Year to date' },
	{ value: 'month', label: 'This month' },
	{ value: 'week', label: 'This week' },
	{ value: 'today', label: 'Today' },
	{ value: 'last30', label: '30 days' },
]

const OFFICERS = [
	['all', 'All officers'],
	['uniform', 'Uniform officers'],
	['detective', 'Store detectives'],
	['store-user', 'Store users'],
] as const

// ── Types ─────────────────────────────────────────────────────────────────────

interface FilteredData {
	location: string
	value: number
	quantity: number
	count: number
}

type GraphType = 'value' | 'lost' | 'quantity' | 'type' | 'count'
type ScreenSize = 'sm' | 'md' | 'lg'

interface IncidentGraphProps {
	customerId?: string
}

// ── Constants ─────────────────────────────────────────────────────────────────

const toDateInputValue = (date: Date) => format(date, 'yyyy-MM-dd')

const fromDateInputValue = (value: string): Date | null => {
	if (!value) return null
	const parsed = new Date(`${value}T00:00:00`)
	return Number.isNaN(parsed.getTime()) ? null : parsed
}


// ── Responsive helpers ────────────────────────────────────────────────────────

const getScreenSize = (): ScreenSize => {
	if (window.innerWidth < 640) return 'sm'
	if (window.innerWidth < 1024) return 'md'
	return 'lg'
}

const CHART_CONFIG: Record<ScreenSize, {
	height: number
	barSize: number
	barCategoryGap: number
	xAngle: number
	xHeight: number
	fontSize: number
	labelOffset: number
	marginLeft: number
	marginRight: number
	marginBottom: number
	marginTop: number
	yLabelOffset: number
}> = {
	sm: {
		height: 300,
		barSize: 25,
		barCategoryGap: 15,
		xAngle: -45,
		xHeight: 50,
		fontSize: 8,
		labelOffset: 5,
		marginLeft: 35,
		marginRight: 10,
		marginBottom: 60,
		marginTop: 15,
		yLabelOffset: -25,
	},
	md: {
		height: 400,
		barSize: 35,
		barCategoryGap: 25,
		xAngle: -30,
		xHeight: 60,
		fontSize: 10,
		labelOffset: 8,
		marginLeft: 50,
		marginRight: 20,
		marginBottom: 80,
		marginTop: 20,
		yLabelOffset: -35,
	},
	lg: {
		height: 500,
		barSize: 40,
		barCategoryGap: 30,
		xAngle: -20,
		xHeight: 80,
		fontSize: 12,
		labelOffset: 12,
		marginLeft: 60,
		marginRight: 30,
		marginBottom: 100,
		marginTop: 20,
		yLabelOffset: -45,
	},
}

// ── Utility helpers ───────────────────────────────────────────────────────────

const getIncidentValue = (inc: any): number => {
	const raw =
		inc.totalValueRecovered ??
		inc.TotalValueRecovered ??
		inc.value ??
		inc.Value ??
		inc.valueRecovered ??
		inc.ValueRecovered ??
		0
	return typeof raw === 'number' ? raw : parseFloat(raw) || 0
}

const getIncidentLostValue = (inc: any): number => {
	const raw =
		inc.totalLostValue ??
		inc.TotalLostValue ??
		inc.lostValue ??
		inc.LostValue ??
		inc.valueLost ??
		inc.ValueLost ??
		0
	return typeof raw === 'number' ? raw : parseFloat(raw) || 0
}

/** Whole pounds with thousands separators, for headline figures. */
const formatPounds = (value: number): string => `£${Math.round(value).toLocaleString('en-GB')}`

const formatCurrency = (value: number, compact = false): string => {
	if (value === 0) return '£0'
	if (compact && value >= 1000) {
		return value < 1_000_000 ? `£${(value / 1000).toFixed(1)}k` : `£${(value / 1_000_000).toFixed(2)}m`
	}
	if (value < 1000) return `£${value.toLocaleString('en-GB', { maximumFractionDigits: 2 })}`
	if (value < 1_000_000) return `£${(value / 1000).toFixed(1)}K`
	return `£${(value / 1_000_000).toFixed(2)}M`
}

// ── Component ─────────────────────────────────────────────────────────────────

const IncidentGraph: React.FC<IncidentGraphProps> = ({ customerId }) => {
	const { user } = useAuth()
	const chartTheme = useChartTheme()
	const categoryColors = chartTheme.series
	const { isAdmin, isManager, selectedCustomerId: contextCustomerId, setSelectedCustomerId, assignedCustomers } = useCustomerSelection()
	const { availableCustomers, isLoading: loadingCustomers } = useAvailableCustomers()
	const [searchParams, setSearchParams] = useSearchParams()

	const urlCustomerId = searchParams.get('customerId')
	const userCustomerId = user && 'customerId' in user ? (user as any).customerId : undefined

	const currentCustomerId = useMemo(() => {
		if (customerId) return parseInt(customerId)
		if (urlCustomerId) return parseInt(urlCustomerId)
		if (isAdmin && contextCustomerId) return contextCustomerId
		if (isManager && contextCustomerId) return contextCustomerId
		if (isAdmin && availableCustomers.length > 0) return availableCustomers[0].id
		if (isManager && assignedCustomers.length > 0) return assignedCustomers[0].id
		if (userCustomerId) return typeof userCustomerId === 'string' ? parseInt(userCustomerId, 10) : userCustomerId
		return null
	}, [customerId, urlCustomerId, isAdmin, isManager, contextCustomerId, userCustomerId, availableCustomers, assignedCustomers])

	// ── Responsive state ──────────────────────────────────────────────────────

	const [screenSize, setScreenSize] = useState<ScreenSize>(getScreenSize)
	const [storesPerPage, setStoresPerPage] = useState<number>(() => {
		const s = getScreenSize()
		return s === 'sm' ? 5 : s === 'md' ? 10 : 20
	})

	// ── Filter state ──────────────────────────────────────────────────────────

	const [startDate, setStartDate] = useState<Date>(() => startOfYear(new Date()))
	const [endDate, setEndDate] = useState<Date>(() => new Date())
	const [selectedRegionId, setSelectedRegionId] = useState('all')
	const [graphType, setGraphType] = useState<GraphType>('value')
	const [officerType, setOfficerType] = useState('all')
	const [timeFilter, setTimeFilter] = useState('ytd')

	// ── Data state ────────────────────────────────────────────────────────────

	const [graphData, setGraphData] = useState<IncidentGraphData[]>([])
	const [incidentTypeData, setIncidentTypeData] = useState<IncidentTypeData[]>([])
	const [availableRegions, setAvailableRegions] = useState<RegionOption[]>([])
	const [customerName, setCustomerName] = useState<string>('')
	const [filteredTotal, setFilteredTotal] = useState(0)
	const [selectedCustomerForAdmin, setSelectedCustomerForAdmin] = useState<number | null>(null)

	// ── Loading / error state ─────────────────────────────────────────────────

	const [loading, setLoading] = useState(false)
	const [error, setError] = useState<string | null>(null)
	const [currentPage, setCurrentPage] = useState(1)
	const [filtersVersion, setFiltersVersion] = useState(0)

	// ── Derived values ────────────────────────────────────────────────────────

	const cfg = CHART_CONFIG[screenSize]

	const filters = useMemo(
		(): IncidentGraphFilters => ({
			customerId: currentCustomerId,
			startDate: startDate ? format(startDate, 'yyyy-MM-dd') : undefined,
			endDate: endDate ? format(endDate, 'yyyy-MM-dd') : undefined,
			regionId: selectedRegionId === 'all' ? undefined : selectedRegionId,
			officerType,
			graphType,
		}),
		[currentCustomerId, startDate, endDate, selectedRegionId, officerType, graphType]
	)

	const selectedRegionLabel = useMemo(
		() =>
			selectedRegionId === 'all'
				? 'All Regions'
				: (availableRegions.find(r => r.id === selectedRegionId)?.name ?? 'Selected Region'),
		[selectedRegionId, availableRegions]
	)

	const regionOptions = useMemo(
		() => [
			{ value: 'all', label: 'All Regions' },
			...availableRegions.map(r => ({ value: r.id, label: r.name })),
		],
		[availableRegions]
	)

	// ── Resize listener ───────────────────────────────────────────────────────

	useEffect(() => {
		const handleResize = () => {
			const s = getScreenSize()
			setScreenSize(s)
			setStoresPerPage(s === 'sm' ? 5 : s === 'md' ? 10 : 20)
		}
		window.addEventListener('resize', handleResize)
		return () => window.removeEventListener('resize', handleResize)
	}, [])

	// ── Initial customer sync for admin/manager ─────────────────────────────────

	useEffect(() => {
		if (!isAdmin && !isManager) return
		if (!currentCustomerId) return
		setSelectedCustomerForAdmin(currentCustomerId)
		if (!contextCustomerId) {
			setSelectedCustomerId(currentCustomerId)
		}
	}, [isAdmin, isManager, currentCustomerId])

	// ── Data fetching ─────────────────────────────────────────────────────────

	const fetchRegions = useCallback(async () => {
		if (!currentCustomerId) return
		try {
			const res = await incidentGraphService.fetchRegions(currentCustomerId)
			if (res.success) setAvailableRegions(res.data)
		} catch {
			// silent — region filter is non-critical
		}
	}, [currentCustomerId])

	const fetchCustomerName = useCallback(async () => {
		if (!currentCustomerId) {
			setCustomerName('')
			return
		}

		const availableCustomer = availableCustomers.find(c => c.id === currentCustomerId)
		if (availableCustomer?.name) {
			setCustomerName(availableCustomer.name)
			return
		}

		const assignedCustomer = assignedCustomers.find(c => c.id === currentCustomerId)
		if (assignedCustomer?.name) {
			setCustomerName(assignedCustomer.name)
			return
		}

		const resolvedCustomerName = await getCustomerNameById(currentCustomerId)
		setCustomerName(resolvedCustomerName ?? '')
	}, [currentCustomerId, availableCustomers, assignedCustomers])

	useEffect(() => {
		fetchRegions()
		fetchCustomerName()
	}, [fetchRegions, fetchCustomerName])

	const fetchData = useCallback(async () => {
		if (!currentCustomerId) return

		setLoading(true)
		setError(null)

		try {
			const [graphResponse, typesResponse] = await Promise.all([
				incidentGraphService.fetchGraphData(filters),
				graphType === 'type'
					? incidentGraphService.fetchTypesData({
						customerId: filters.customerId,
						startDate: filters.startDate,
						endDate: filters.endDate,
						regionId: filters.regionId,
						officerType: filters.officerType,
					})
					: Promise.resolve(null),
			])

			if (graphResponse.success) {
				setGraphData(graphResponse.data.incidents)
				setFilteredTotal(
					graphType === 'quantity'
						? graphResponse.data.totals.totalQuantity
						: graphType === 'count'
							? graphResponse.data.totals.totalIncidents
							: graphResponse.data.totals.totalValue
				)
			} else if (!graphResponse.data?.incidents?.length) {
				setError('Unable to load incident data. Please ensure the backend server is running.')
			}

			if (typesResponse?.success) {
				setIncidentTypeData(typesResponse.data)
				setFilteredTotal(graphResponse.data?.totals.totalIncidents ?? typesResponse.data.reduce((s, i) => s + i.count, 0))
			}
		} catch (err: any) {
			const isNetworkError = err?.code === 'ERR_NETWORK' || err?.message === 'Network Error'
			setError(
				isNetworkError
					? 'Unable to connect to the backend API. Please ensure the backend server is running.'
					: err instanceof Error
						? err.message
						: 'Failed to fetch incident data'
			)
		} finally {
			setLoading(false)
		}
	}, [filters, graphType, currentCustomerId, user])

	useEffect(() => {
		if (!currentCustomerId) return
		fetchData()
	}, [fetchData, filtersVersion, currentCustomerId])

	// ── Paginated / sorted data ───────────────────────────────────────────────

	const { filteredData, paginatedData } = useMemo(() => {
		const result: FilteredData[] =
			graphType === 'type'
				? incidentTypeData.map(item => ({ location: item.type, value: item.count, quantity: item.count, count: item.count }))
				: graphData.map(item => ({
					location: item.location || item.siteName || 'Unknown Location',
					value:
						graphType === 'value'
							? getIncidentValue(item)
							: graphType === 'lost'
								? getIncidentLostValue(item)
								: item.value || 0,
					quantity: item.quantity || item.quantityRecovered || 0,
					count: item.count || 0,
				}))

		const sorted = [...result].sort((a, b) =>
			graphType === 'quantity'
				? b.quantity - a.quantity
				: graphType === 'count'
					? b.count - a.count
					: b.value - a.value
		)

		const start = (currentPage - 1) * storesPerPage
		return { filteredData: sorted, paginatedData: sorted.slice(start, start + storesPerPage) }
	}, [graphData, incidentTypeData, graphType, currentPage, storesPerPage])

	const totalPages = Math.ceil(filteredData.length / storesPerPage)

	// ── Time filter handler ───────────────────────────────────────────────────

	const handleTimeFilterChange = (filter: string) => {
		setTimeFilter(filter)
		const now = new Date()
		let nextStart = startDate
		let nextEnd = endDate

		switch (filter) {
			case 'today':
				nextStart = now
				nextEnd = now
				break
			case 'week':
				nextStart = startOfWeek(now)
				nextEnd = endOfWeek(now)
				break
			case 'month':
				nextStart = startOfMonth(now)
				nextEnd = endOfMonth(now)
				break
			case 'ytd':
				nextStart = startOfYear(now)
				nextEnd = now
				break
			case 'last30':
				nextStart = subDays(now, 30)
				nextEnd = now
				break
		}

		setStartDate(nextStart)
		setEndDate(nextEnd)
		setCurrentPage(1)
		setFiltersVersion(v => v + 1)
	}

	// ── Summary title ─────────────────────────────────────────────────────────

	const summaryTitle = useMemo(() => {
		const ofText =
			officerType === 'uniform' ? 'Uniform Officers' :
			officerType === 'detective' ? 'Store Detectives' :
			'All Officers'
		const periodText =
			timeFilter === 'ytd' ? 'Year to Date' :
			timeFilter === 'month' ? 'Current Month' :
			timeFilter === 'week' ? 'Current Week' :
			timeFilter === 'last30' ? 'Last 30 Days' :
			'Selected Period'
		if (graphType === 'type') return `Total Incidents by ${ofText} (${selectedRegionLabel}) — ${periodText}`
		if (graphType === 'count') return `Total Incidents Reported per Store by ${ofText} (${selectedRegionLabel}) — ${periodText}`
		if (graphType === 'quantity') return `Total Items Recovered by ${ofText} (${selectedRegionLabel}) — ${periodText}`
		if (graphType === 'lost') return `Total Value Lost by ${ofText} (${selectedRegionLabel}) — ${periodText}`
		return `Total Value Recovered by ${ofText} (${selectedRegionLabel}) — ${periodText}`
	}, [officerType, timeFilter, graphType, selectedRegionLabel])

	// ── Chart rendering ───────────────────────────────────────────────────────

	const formatValue = (value: number) =>
		graphType === 'value' || graphType === 'lost' ? formatCurrency(value, screenSize === 'sm') : value.toString()

	const formatTooltipValue = (val: any, _name: string, props: any) => {
		if (graphType === 'type') {
			return [`${val} incidents`, props.payload.fullName ?? props.payload.name]
		}
		return [formatValue(val), props.payload.fullLocation ?? props.payload.location]
	}

	const chartData = useMemo(() => {
		if (graphType === 'type') {
			return paginatedData.map(item => ({
				name: item.location.length > 12 ? `${item.location.substring(0, 10)}…` : item.location,
				count: item.value,
				fullName: item.location,
			}))
		}
		const maxLen = screenSize === 'sm' ? 12 : 20
		return paginatedData.map(item => ({
			location:
				item.location.length > maxLen ? `${item.location.substring(0, maxLen - 2)}…` : item.location,
			value: item.value,
			quantity: item.quantity,
			count: item.count,
			fullLocation: item.location,
		}))
	}, [paginatedData, graphType, screenSize])

	const barColor =
		graphType === 'lost' ? chartTheme.loss : graphType === 'value' ? chartTheme.recovered : chartTheme.primary

	const barName =
		graphType === 'type' ? 'Incident Count' :
		graphType === 'count' ? 'Incidents Reported' :
		graphType === 'quantity' ? 'Items Recovered' :
		graphType === 'lost' ? 'Value Lost' :
		officerType === 'uniform' ? 'Uniform Officer' :
		officerType === 'detective' ? 'Store Detective' :
		'Total Value'

	// ── Main render ───────────────────────────────────────────────────────────

	const measureKey: 'count' | 'quantity' | 'value' =
		graphType === 'type' || graphType === 'count' ? 'count' : graphType === 'quantity' ? 'quantity' : 'value'
	const isMoney = graphType === 'value' || graphType === 'lost'
	const measureTone = graphType === 'lost' ? 'loss' : graphType === 'value' ? 'recovered' : 'neutral'
	const fmtFull = (v: number) => (isMoney ? formatPounds(v) : v.toLocaleString('en-GB'))
	const unitLabel =
		graphType === 'type' || graphType === 'count' ? 'incidents' : graphType === 'quantity' ? 'items' : ''
	const leader = filteredData[0]
	const leaderValue = leader ? (graphType === 'type' ? leader.value : (leader as any)[measureKey] ?? leader.value) : 0
	const subjectLabel = graphType === 'type' ? 'incident types' : 'stores'
	const officerLabel =
		officerType === 'uniform' ? 'Uniform officers' : officerType === 'detective' ? 'Store detectives' : officerType === 'store-user' ? 'Store users' : 'All officers'

	const ledger: StatLedgerItem[] = [
		{
			label: MEASURES.find((m) => m.value === graphType)?.total ?? 'Total',
			value: isMoney ? formatPounds(filteredTotal) : filteredTotal.toLocaleString('en-GB'),
			hint: `${format(startDate, 'd MMM')} to ${format(endDate, 'd MMM yyyy')}`,
			tone: measureTone,
		},
		{
			label: graphType === 'type' ? 'Most common type' : 'Top store',
			value: leader ? <span className="block truncate text-[1.375rem] sm:text-[1.5rem]">{leader.location}</span> : '—',
			hint: leader ? `${fmtFull(leaderValue)}${unitLabel ? ` ${unitLabel}` : ''} · ${filteredTotal > 0 ? Math.round((leaderValue / filteredTotal) * 100) : 0}% of total` : undefined,
		},
		{
			label: graphType === 'type' ? 'Incident types' : 'Stores with activity',
			value: filteredData.length.toLocaleString('en-GB'),
			hint: selectedRegionLabel,
		},
		{
			label: graphType === 'type' ? 'Average per type' : 'Average per store',
			value: filteredData.length ? (isMoney ? formatPounds(filteredTotal / filteredData.length) : (filteredTotal / filteredData.length).toFixed(1)) : '—',
			hint: officerLabel,
		},
	]

	const rows = paginatedData.map((item: any) => ({
		name: item.location,
		value: graphType === 'type' ? item.value : item[measureKey] ?? item.value,
	}))

	const companyControl = isAdmin || (isManager && assignedCustomers.length > 1) ? (
		<Select
			disabled={isAdmin && (loadingCustomers || availableCustomers.length === 0)}
			value={selectedCustomerForAdmin?.toString() ?? ''}
			onValueChange={(value) => {
				const id = parseInt(value, 10)
				setSelectedCustomerForAdmin(id)
				setSelectedCustomerId(id)
				const params = new URLSearchParams(searchParams)
				params.set('customerId', value)
				setSearchParams(params, { replace: true })
				setFiltersVersion((v) => v + 1)
			}}
		>
			<SelectTrigger className="h-9 w-full gap-2 sm:w-60" aria-label="Company">
				<Building2 className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
				<SelectValue placeholder={loadingCustomers ? 'Loading companies…' : 'Select company'} />
			</SelectTrigger>
			<SelectContent className="max-h-72">
				{(isAdmin ? availableCustomers : assignedCustomers).map((c) => (
					<SelectItem key={c.id} value={c.id.toString()}>
						{c.name}
					</SelectItem>
				))}
			</SelectContent>
		</Select>
	) : (
		<span className="inline-flex h-9 items-center gap-2 rounded-md border border-border bg-card px-3 text-sm font-medium text-foreground">
			<Building2 className="h-4 w-4 text-muted-foreground" aria-hidden />
			{customerName || 'Loading company…'}
		</span>
	)

	return (
		<div className="w-full space-y-5">
			<PageHeader
				title="Incident graph"
				description="Compare recoveries, losses and incidents across stores and incident types."
				className="pb-0"
				actions={companyControl}
			/>

			<div className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
				<Segmented<GraphType>
					ariaLabel="What to compare"
					value={graphType}
					onChange={(v) => {
						setGraphType(v)
						setCurrentPage(1)
						setFiltersVersion((f) => f + 1)
					}}
					options={MEASURES.map((m) => ({ value: m.value, label: m.label }))}
					className="min-w-max"
				/>
			</div>

			<FilterBar
				label="Graph filters"
				activeCount={(selectedRegionId !== 'all' ? 1 : 0) + (officerType !== 'all' ? 1 : 0)}
				primary={
					<>
						<Select value={timeFilter} onValueChange={handleTimeFilterChange}>
							<SelectTrigger className="h-9 w-full sm:hidden" aria-label="Period">
								<SelectValue placeholder="Custom dates" />
							</SelectTrigger>
							<SelectContent>
								{PERIODS.map((p) => (
									<SelectItem key={p.value} value={p.value}>
										{p.label}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
						<Segmented<string>
							ariaLabel="Period"
							size="sm"
							value={timeFilter}
							onChange={handleTimeFilterChange}
							options={PERIODS}
							className="hidden sm:inline-flex"
						/>
					</>
				}
			>
				<DateRangeField
					from={toDateInputValue(startDate)}
					to={toDateInputValue(endDate)}
					onFromChange={(value) => {
						const parsed = fromDateInputValue(value)
						if (parsed) {
							setStartDate(parsed)
							setTimeFilter('custom')
							setCurrentPage(1)
						}
					}}
					onToChange={(value) => {
						const parsed = fromDateInputValue(value)
						if (parsed) {
							setEndDate(parsed)
							setTimeFilter('custom')
							setCurrentPage(1)
						}
					}}
				/>
				<Select
					value={selectedRegionId}
					onValueChange={(v) => {
						setSelectedRegionId(v)
						setCurrentPage(1)
						setFiltersVersion((f) => f + 1)
					}}
				>
					<SelectTrigger className="h-9 w-full sm:w-44" aria-label="Region">
						<SelectValue placeholder="All regions" />
					</SelectTrigger>
					<SelectContent>
						{regionOptions.map((o) => (
							<SelectItem key={o.value} value={o.value}>
								{o.label}
							</SelectItem>
						))}
					</SelectContent>
				</Select>
				<Select
					value={officerType}
					onValueChange={(value) => {
						setOfficerType(value)
						setCurrentPage(1)
						setFiltersVersion((f) => f + 1)
					}}
				>
					<SelectTrigger className="h-9 w-full gap-2 sm:w-48" aria-label="Officer role">
						<Users className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
						<SelectValue />
					</SelectTrigger>
					<SelectContent>
						{OFFICERS.map(([val, label]) => (
							<SelectItem key={val} value={val}>
								{label}
							</SelectItem>
						))}
					</SelectContent>
				</Select>
			</FilterBar>

			<StatLedger items={ledger} loading={loading} />

			<section className="rounded-lg border border-border bg-card">
				<div className="flex flex-wrap items-start justify-between gap-3 p-4 sm:p-5">
					<div className="min-w-0">
						<h2 className="text-base font-semibold text-foreground">
							{MEASURES.find((m) => m.value === graphType)?.chartTitle} · {selectedRegionLabel}
						</h2>
						<p className="mt-0.5 text-[13px] text-muted-foreground">
							{officerLabel} · {format(startDate, 'd MMM yyyy')} to {format(endDate, 'd MMM yyyy')} · ranked highest first
						</p>
					</div>
					{graphType !== 'type' ? (
						<Select
							value={storesPerPage.toString()}
							onValueChange={(v) => {
								setStoresPerPage(parseInt(v))
								setCurrentPage(1)
							}}
						>
							<SelectTrigger className="h-8 w-32 text-[13px]" aria-label="Stores per page">
								<SelectValue />
							</SelectTrigger>
							<SelectContent>
								{['5', '10', '20', '50'].map((n) => (
									<SelectItem key={n} value={n}>
										{n} per page
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					) : null}
				</div>
				<div className="border-t border-border px-2 py-4 sm:px-4">
					{loading ? (
						<div className="flex items-center justify-center text-sm text-muted-foreground" style={{ height: 280 }}>
							<Loader2 className="mr-2 h-5 w-5 animate-spin" aria-hidden />
							Loading incident data…
						</div>
					) : error ? (
						<Alert variant="destructive">
							<AlertDescription>{error}</AlertDescription>
						</Alert>
					) : rows.length === 0 ? (
						<EmptyState
							icon={<BarChart3 className="h-5 w-5" />}
							title="Nothing to show for these filters"
							description="Try a longer period, another region or all officers."
						/>
					) : (
						<ResponsiveContainer width="100%" height={cfg.height}>
							<BarChart
								data={chartData}
								margin={{ top: cfg.marginTop + 8, right: cfg.marginRight, left: cfg.marginLeft, bottom: cfg.marginBottom }}
								barSize={cfg.barSize}
								barGap={0}
								barCategoryGap={cfg.barCategoryGap}
							>
								<defs>
									<filter id="barShadow" filterUnits="userSpaceOnUse">
										<feDropShadow dx="3" dy="5" stdDeviation="4" floodOpacity="0.18" floodColor="#000" />
									</filter>
								</defs>
								<CartesianGrid stroke={chartTheme.grid} vertical={false} strokeDasharray="3 3" />
								<XAxis
									dataKey={graphType === 'type' ? 'name' : 'location'}
									angle={cfg.xAngle}
									textAnchor="end"
									height={cfg.xHeight}
									interval={0}
									tick={{ fontSize: cfg.fontSize, fill: chartTheme.foreground }}
									axisLine={{ stroke: chartTheme.grid }}
									tickLine={false}
								/>
								<YAxis
									tickFormatter={formatValue}
									tick={{ fontSize: cfg.fontSize, fill: chartTheme.axis }}
									axisLine={false}
									tickLine={false}
									width={56}
								/>
								<Tooltip formatter={formatTooltipValue} {...chartTooltipStyle(chartTheme)} />
								<Bar
									dataKey={measureKey}
									name={barName}
									minPointSize={0}
									animationDuration={500}
									style={{ filter: 'url(#barShadow)' }}
									shape={(props: { x: number; y: number; width: number; height: number; index: number }) => {
										const { x, y, width, height, index } = props
										if (!height || height <= 0) return <g />
										const colour = BAR_PALETTE[index % BAR_PALETTE.length]
										const depth = Math.max(4, width * (screenSize === 'sm' ? 0.16 : 0.2))
										const rise = depth * 0.55
										const side = `M ${x + width} ${y} l ${depth} ${-rise} l 0 ${height} l ${-depth} ${rise} Z`
										const top = `M ${x} ${y} l ${width} 0 l ${depth} ${-rise} l ${-width} 0 Z`
										return (
											<g>
												<path d={side} fill={colour} />
												<path d={side} fill="#000" fillOpacity={0.28} />
												<path d={top} fill={colour} />
												<path d={top} fill="#fff" fillOpacity={0.22} />
												<rect x={x} y={y} width={width} height={height} fill={colour} />
												<rect x={x} y={y} width={width} height={Math.min(height, 24)} fill="#fff" fillOpacity={0.06} />
											</g>
										)
									}}
								>
									<LabelList
										dataKey={measureKey}
										position="top"
										offset={cfg.labelOffset + 6}
										formatter={formatValue}
										style={{ fontSize: cfg.fontSize, fill: chartTheme.foreground, fontWeight: 600 }}
									/>
								</Bar>
							</BarChart>
						</ResponsiveContainer>
					)}
				</div>

				{graphType !== 'type' && filteredData.length > storesPerPage && (
					<div className="flex flex-col items-center justify-between gap-3 border-t border-border px-4 py-3 sm:flex-row sm:px-5">
						<p className="figures text-xs text-muted-foreground">
							{(currentPage - 1) * storesPerPage + 1}–{Math.min(currentPage * storesPerPage, filteredData.length)} of {filteredData.length} {subjectLabel}
						</p>
						<div className="flex items-center gap-1">
							<Button variant="outline" size="sm" onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))} disabled={currentPage === 1}>
								Previous
							</Button>
							{Array.from({ length: totalPages }, (_, i) => i + 1)
								.slice(Math.max(0, Math.min(currentPage - 3, totalPages - 5)), Math.max(5, Math.min(currentPage + 2, totalPages)))
								.map((page) => (
									<Button
										key={page}
										variant={currentPage === page ? 'default' : 'ghost'}
										size="sm"
										onClick={() => setCurrentPage(page)}
										className="figures h-8 w-8 p-0"
										aria-current={currentPage === page ? 'page' : undefined}
									>
										{page}
									</Button>
								))}
							<Button variant="outline" size="sm" onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))} disabled={currentPage === totalPages}>
								Next
							</Button>
						</div>
					</div>
				)}
			</section>
		</div>
	)
}

export default IncidentGraph
