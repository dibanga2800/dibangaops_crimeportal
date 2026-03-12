import React, { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { DatePicker } from '@/components/ui/date-picker'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import {
	BarChart,
	Bar,
	XAxis,
	YAxis,
	CartesianGrid,
	Tooltip,
	Legend,
	ResponsiveContainer,
	LabelList,
	Cell,
} from 'recharts'
import { Button } from '@/components/ui/button'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Loader2 } from 'lucide-react'
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
import { useAuth } from '@/hooks/useAuth'
import {
	incidentGraphService,
	type IncidentGraphData,
	type IncidentTypeData,
	type IncidentGraphFilters,
	type RegionOption,
} from '@/services/incidentGraphService'

// ── Types ─────────────────────────────────────────────────────────────────────

interface FilteredData {
	location: string
	value: number
	quantity: number
}

type GraphType = 'value' | 'quantity' | 'type'
type ScreenSize = 'sm' | 'md' | 'lg'

interface IncidentGraphProps {
	customerId?: string
}

// ── Constants ─────────────────────────────────────────────────────────────────

const COLOR_PALETTE = [
	'#3b82f6',
	'#8b5cf6',
	'#ec4899',
	'#f97316',
	'#10b981',
	'#06b6d4',
	'#6366f1',
	'#f43f5e',
	'#facc15',
	'#14b8a6',
]

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

const formatCurrency = (value: number, compact = false): string => {
	if (value === 0) return '£0'
	if (compact && value >= 1000) {
		return value < 1_000_000 ? `£${(value / 1000).toFixed(1)}k` : `£${(value / 1_000_000).toFixed(2)}m`
	}
	if (value < 1000) return `£${value.toLocaleString()}`
	if (value < 1_000_000) return `£${(value / 1000).toFixed(1)}K`
	return `£${(value / 1_000_000).toFixed(2)}M`
}

// ── Component ─────────────────────────────────────────────────────────────────

const IncidentGraph: React.FC<IncidentGraphProps> = ({ customerId }) => {
	const { user } = useAuth()
	const [searchParams] = useSearchParams()

	const urlCustomerId = searchParams.get('customerId')
	const userCustomerId = user && 'customerId' in user ? (user as any).customerId : undefined
	const currentCustomerId = customerId
		? parseInt(customerId)
		: urlCustomerId
			? parseInt(urlCustomerId)
			: userCustomerId || 1

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
	const [totalSaved, setTotalSaved] = useState(0)
	const [filteredTotal, setFilteredTotal] = useState(0)

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
		// Customer name is a nice-to-have; failures are swallowed silently
	}, [])

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
				setTotalSaved(graphResponse.data.totals.totalValue)
				setFilteredTotal(
					graphType === 'quantity'
						? graphResponse.data.totals.totalQuantity
						: graphResponse.data.totals.totalValue
				)
			} else if (!graphResponse.data?.incidents?.length) {
				setError('Unable to load incident data. Please ensure the backend server is running.')
			}

			if (typesResponse?.success) {
				setIncidentTypeData(typesResponse.data)
				setFilteredTotal(typesResponse.data.reduce((s, i) => s + i.count, 0))
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
	}, [filters, graphType, currentCustomerId])

	useEffect(() => {
		fetchData()
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [filtersVersion])

	// ── Paginated / sorted data ───────────────────────────────────────────────

	const { filteredData, paginatedData } = useMemo(() => {
		const result: FilteredData[] =
			graphType === 'type'
				? incidentTypeData.map(item => ({ location: item.type, value: item.count, quantity: item.count }))
				: graphData.map(item => ({
					location: item.location || item.siteName || 'Unknown Location',
					value: graphType === 'value' ? getIncidentValue(item) : item.value || 0,
					quantity: item.quantity || item.quantityRecovered || 0,
				}))

		const sorted = [...result].sort((a, b) =>
			graphType === 'quantity' ? b.quantity - a.quantity : b.value - a.value
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
		if (graphType === 'quantity') return `Total Items Recovered by ${ofText} (${selectedRegionLabel}) — ${periodText}`
		return `Total Value Recovered by ${ofText} (${selectedRegionLabel}) — ${periodText}`
	}, [officerType, timeFilter, graphType, selectedRegionLabel])

	// ── Chart rendering ───────────────────────────────────────────────────────

	const formatValue = (value: number) =>
		graphType === 'value' ? formatCurrency(value, screenSize === 'sm') : value.toString()

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
			fullLocation: item.location,
		}))
	}, [paginatedData, graphType, screenSize])

	const barName =
		graphType === 'type' ? 'Incident Count' :
		graphType === 'quantity' ? 'Items Recovered' :
		officerType === 'uniform' ? 'Uniform Officer' :
		officerType === 'detective' ? 'Store Detective' :
		'Total Value'

	// ── Main render ───────────────────────────────────────────────────────────

	return (
		<div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
			<div className="container mx-auto px-3 sm:px-4 md:px-6 lg:px-8 py-4 sm:py-6 md:py-8 space-y-4 sm:space-y-6 max-w-screen-2xl">

				{/* ── Page Header ── */}
				<div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-indigo-500/10 via-purple-500/10 to-pink-500/10 p-4 sm:p-6 md:p-8 border border-white/10">
					<div className="flex flex-col lg:flex-row justify-between items-start lg:items-center gap-4">
						<div>
							<h1 className="text-2xl sm:text-3xl md:text-4xl font-bold bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent">
								Incident Analytics Dashboard
							</h1>
							<p className="text-slate-400 mt-1 text-sm sm:text-base">
								Track and analyse security incidents across locations
							</p>
							{customerName && (
								<div className="flex flex-wrap items-center gap-2 mt-3">
									<span className="bg-indigo-500/20 border border-indigo-500/30 text-indigo-300 text-sm font-medium px-3 py-1 rounded-full">
										{customerName}
									</span>
									<span className="bg-emerald-500/20 border border-emerald-500/30 text-emerald-300 text-sm font-medium px-3 py-1 rounded-full">
										ID: {currentCustomerId}
									</span>
								</div>
							)}
						</div>
						<div className="bg-slate-800/80 p-4 sm:p-6 rounded-xl border border-white/10 w-full lg:w-auto">
							<h2 className="text-sm sm:text-base font-semibold text-slate-300 leading-tight">
								{summaryTitle}
							</h2>
							<p className="text-2xl sm:text-3xl md:text-4xl font-bold bg-gradient-to-r from-emerald-400 to-sky-400 bg-clip-text text-transparent mt-2">
								{graphType === 'type'
									? `${filteredTotal.toLocaleString()} Incidents`
									: graphType === 'quantity'
										? `${filteredTotal.toLocaleString()} Items`
										: formatCurrency(filteredTotal)}
							</p>
						</div>
					</div>
				</div>

				{/* ── Filters Card ── */}
				<Card className="bg-slate-900/90 border-slate-800">
					<CardHeader className="py-3 px-4 sm:px-6">
						<CardTitle className="text-base sm:text-lg font-semibold text-slate-200">
							Filters &amp; Controls
						</CardTitle>
					</CardHeader>
					<CardContent className="px-4 sm:px-6 pb-5">
						<div className="grid gap-4 sm:gap-6 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">

							{/* Region */}
							<div className="space-y-2">
								<Label className="text-xs sm:text-sm font-medium text-slate-300">Region</Label>
								<Select
									value={selectedRegionId}
									onValueChange={v => {
										setSelectedRegionId(v)
										setCurrentPage(1)
										setFiltersVersion(f => f + 1)
									}}
								>
									<SelectTrigger className="bg-white border-slate-300 text-slate-900 h-9 text-sm">
										<SelectValue placeholder="Select region" />
									</SelectTrigger>
									<SelectContent className="bg-white border-slate-200 text-sm">
										{regionOptions.map(o => (
											<SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>
										))}
									</SelectContent>
								</Select>
							</div>

							{/* Graph Type */}
							<div className="space-y-2">
								<Label className="text-xs sm:text-sm font-medium text-slate-300">Graph Type</Label>
									<RadioGroup
									value={graphType}
									onValueChange={(v: GraphType) => {
										setGraphType(v)
										setCurrentPage(1)
										setFiltersVersion(f => f + 1)
									}}
									className="flex flex-wrap gap-x-4 gap-y-2"
								>
									{([['value', 'Value Recovered'], ['quantity', 'Items Recovered'], ['type', 'Action Types']] as const).map(
										([val, label]) => (
											<div key={val} className="flex items-center gap-2">
												<RadioGroupItem value={val} id={`gt-${val}`} className="border-slate-600 text-indigo-500" />
												<Label htmlFor={`gt-${val}`} className="text-xs sm:text-sm text-slate-300 cursor-pointer">{label}</Label>
											</div>
										)
									)}
								</RadioGroup>
							</div>

							{/* Time Period */}
							<div className="space-y-2">
								<Label className="text-xs sm:text-sm font-medium text-slate-300">Time Period</Label>
									<RadioGroup
									value={timeFilter}
									onValueChange={handleTimeFilterChange}
									className="grid grid-cols-2 gap-x-4 gap-y-2"
								>
									{([
										['ytd', 'Year to Date'],
										['month', 'Current Month'],
										['week', 'Current Week'],
										['today', 'Today'],
										['last30', 'Last 30 Days'],
									] as const).map(([val, label]) => (
										<div key={val} className="flex items-center gap-2">
											<RadioGroupItem value={val} id={`tf-${val}`} className="border-slate-600 text-indigo-500" />
											<Label htmlFor={`tf-${val}`} className="text-xs sm:text-sm text-slate-300 cursor-pointer">{label}</Label>
										</div>
									))}
								</RadioGroup>
							</div>

							{/* Officer Role + Items per Page */}
							<div className="space-y-4">
								<div className="space-y-2">
									<Label className="text-xs sm:text-sm font-medium text-slate-300">Officer Role</Label>
									<RadioGroup
										value={officerType}
										onValueChange={value => {
											setOfficerType(value)
											setCurrentPage(1)
											setFiltersVersion(f => f + 1)
										}}
										className="flex flex-col gap-2"
									>
										{([['all', 'All Officers'], ['uniform', 'Uniform Officers'], ['detective', 'Store Detectives']] as const).map(
											([val, label]) => (
												<div key={val} className="flex items-center gap-2">
													<RadioGroupItem value={val} id={`ot-${val}`} className="border-slate-600 text-indigo-500" />
													<Label htmlFor={`ot-${val}`} className="text-xs sm:text-sm text-slate-300 cursor-pointer">{label}</Label>
												</div>
											)
										)}
									</RadioGroup>
								</div>

								<div className="space-y-2">
									<Label className="text-xs sm:text-sm font-medium text-slate-300">Items per Page</Label>
									<Select
										value={storesPerPage.toString()}
										onValueChange={v => { setStoresPerPage(parseInt(v)); setCurrentPage(1) }}
									>
										<SelectTrigger className="bg-white border-slate-300 text-slate-900 h-9 text-sm">
											<SelectValue />
										</SelectTrigger>
										<SelectContent className="bg-white border-slate-200 text-sm">
											{['5', '10', '20', '50'].map(n => (
												<SelectItem key={n} value={n}>{n} items</SelectItem>
											))}
										</SelectContent>
									</Select>
								</div>
							</div>

						</div>

						{/* Date pickers — always visible */}
						<div className="flex flex-col sm:flex-row gap-3 mt-4 pt-4 border-t border-slate-800">
							<DatePicker date={startDate} setDate={v => v && setStartDate(v)} />
							<DatePicker date={endDate} setDate={v => v && setEndDate(v)} />
						</div>
					</CardContent>
				</Card>

				{/* ── Chart Card ── */}
				<Card className="bg-slate-900/90 border-slate-800">
					<CardHeader className="py-3 px-4 sm:px-6">
						<CardTitle className="text-base sm:text-lg font-semibold text-slate-200">
							{graphType === 'type'
								? `${selectedRegionLabel} — Incident Types Distribution`
								: `${selectedRegionLabel} — ${
										officerType === 'all' ? 'Total Incidents by Location' :
										officerType === 'uniform' ? 'Uniform Officer Incidents' :
										'Store Detective Incidents'
									}`}
						</CardTitle>
						{startDate && endDate && (
							<p className="text-xs sm:text-sm text-slate-400 mt-1">
								Period: {format(startDate, 'PP')} – {format(endDate, 'PP')}
							</p>
						)}
					</CardHeader>
					<CardContent className="px-3 sm:px-4 pb-5">
						<div className="bg-slate-800/50 rounded-xl p-3 sm:p-4 md:p-6 border border-slate-700/50">
							{loading ? (
								<div className="flex items-center justify-center" style={{ height: cfg.height }}>
									<Loader2 className="h-7 w-7 animate-spin text-slate-400 mr-2" />
									<span className="text-slate-400 text-sm">Loading incident data…</span>
								</div>
							) : error ? (
								<Alert className="bg-red-900/20 border-red-700">
									<AlertDescription className="text-red-300">{error}</AlertDescription>
								</Alert>
							) : paginatedData.length === 0 ? (
								<div
									className="flex items-center justify-center text-slate-400 text-sm"
									style={{ height: cfg.height }}
								>
									No incident data available for the selected filters.
								</div>
							) : (
								<div className="relative">
									<div className="absolute inset-0 bg-gradient-to-r from-indigo-500/5 via-purple-500/5 to-pink-500/5 rounded-xl pointer-events-none" />
									<ResponsiveContainer width="100%" height={cfg.height} className="mt-4">
										<BarChart
											data={chartData}
											margin={{
												top: cfg.marginTop,
												right: cfg.marginRight,
												left: cfg.marginLeft,
												bottom: cfg.marginBottom,
											}}
											barSize={cfg.barSize}
											barGap={0}
											barCategoryGap={cfg.barCategoryGap}
										>
											<defs>
												{chartData.map((_, index) => {
													const color = COLOR_PALETTE[index % COLOR_PALETTE.length]
													return (
														<React.Fragment key={index}>
															<linearGradient id={`frontGrad${index}`} x1="0" y1="0" x2="0" y2="1">
																<stop offset="0%" stopColor={color} stopOpacity={1} />
																<stop offset="100%" stopColor={color} stopOpacity={0.85} />
															</linearGradient>
															<linearGradient id={`sideGrad${index}`} x1="0" y1="0" x2="1" y2="0">
																<stop offset="0%" stopColor={color} stopOpacity={0.7} />
																<stop offset="100%" stopColor={color} stopOpacity={0.3} />
															</linearGradient>
															<linearGradient id={`topGrad${index}`} x1="0" y1="1" x2="1" y2="0">
																<stop offset="0%" stopColor={color} stopOpacity={1} />
																<stop offset="100%" stopColor={color} stopOpacity={0.95} />
															</linearGradient>
															<linearGradient id={`reflGrad${index}`} x1="0" y1="0" x2="0" y2="1">
																<stop offset="0%" stopColor={color} stopOpacity={0.2} />
																<stop offset="100%" stopColor={color} stopOpacity={0} />
															</linearGradient>
														</React.Fragment>
													)
												})}
												<filter id="barShadow" filterUnits="userSpaceOnUse">
													<feDropShadow dx="4" dy="6" stdDeviation="5" floodOpacity="0.3" floodColor="#000" />
												</filter>
											</defs>

											<CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" vertical={false} />

											<XAxis
												dataKey={graphType === 'type' ? 'name' : 'location'}
												angle={cfg.xAngle}
												textAnchor="end"
												height={cfg.xHeight}
												interval={0}
												tick={{ fontSize: cfg.fontSize, fill: '#E2E8F0', fontWeight: 500 }}
												axisLine={{ stroke: '#475569' }}
												tickLine={{ stroke: '#475569' }}
											/>

											<YAxis
												label={{
													value:
														graphType === 'type' ? 'Number of Incidents' :
														graphType === 'value' ? 'Amount Recovered (£)' :
														'Number of Items',
													angle: -90,
													position: 'insideLeft',
													offset: cfg.yLabelOffset,
													fill: '#94A3B8',
													fontSize: cfg.fontSize,
													fontWeight: 500,
												}}
												tickFormatter={formatValue}
												tick={{ fontSize: cfg.fontSize, fill: '#94A3B8', fontWeight: 500 }}
												axisLine={{ stroke: '#475569' }}
												tickLine={{ stroke: '#475569' }}
											/>

											<Tooltip
												formatter={formatTooltipValue}
												contentStyle={{
													backgroundColor: 'rgba(15, 23, 42, 0.9)',
													backdropFilter: 'blur(8px)',
													border: '1px solid rgba(148,163,184,0.2)',
													borderRadius: '8px',
													padding: '10px 14px',
													color: '#E2E8F0',
													boxShadow: '0 4px 20px -1px rgba(0,0,0,0.4)',
												}}
												itemStyle={{ padding: '3px 0', color: '#E2E8F0' }}
												labelStyle={{ fontWeight: 600, marginBottom: '5px', color: '#F8FAFC' }}
											/>

											<Legend
												wrapperStyle={{ paddingTop: screenSize === 'sm' ? '10px' : '20px' }}
												formatter={value => (
													<span style={{ color: '#94A3B8', fontWeight: 500, fontSize: cfg.fontSize }}>
														{value}
													</span>
												)}
												iconSize={screenSize === 'sm' ? 8 : 10}
												iconType="circle"
											/>

											<Bar
												dataKey={graphType === 'type' ? 'count' : graphType === 'value' ? 'value' : 'quantity'}
												name={barName}
												radius={[0, 0, 0, 0]}
												style={{ filter: 'url(#barShadow)' }}
												minPointSize={0}
												shape={(props: any) => {
													const { x, y, width, height, index } = props
													const depth = width * (screenSize === 'sm' ? 0.15 : 0.2)
													const topH = depth * 0.5
													return (
														<g>
															<path
																d={`M ${x + width} ${y} l ${depth} ${-topH} l 0 ${height} l ${-depth} ${depth * 0.3} Z`}
																fill={`url(#sideGrad${index})`}
															/>
															<path
																d={`M ${x} ${y} l ${width} 0 l ${depth} ${-topH} l ${-width} 0 Z`}
																fill={`url(#topGrad${index})`}
															/>
															<path
																d={`M ${x} ${y} l ${width} 0 l 0 ${height} l ${-width} 0 Z`}
																fill={`url(#frontGrad${index})`}
															/>
															<path
																d={`M ${x} ${y} l ${width} 0 l 0 ${height * 0.3} l ${-width} 0 Z`}
																fill={`url(#reflGrad${index})`}
															/>
														</g>
													)
												}}
											>
												<LabelList
													dataKey={graphType === 'type' ? 'count' : graphType === 'value' ? 'value' : 'quantity'}
													position="top"
													offset={cfg.labelOffset}
													formatter={formatValue}
													style={{
														fontSize: cfg.fontSize,
														fill: '#FFFFFF',
														fontWeight: 600,
														textShadow: '0 1px 2px rgba(0,0,0,0.6)',
													}}
												/>
											</Bar>
										</BarChart>
									</ResponsiveContainer>
								</div>
							)}
						</div>

						{/* ── Incident Types Legend ── */}
						{graphType === 'type' && incidentTypeData.length > 0 && !loading && (
							<div className="mt-4 rounded-lg bg-slate-800/60 border border-slate-700/50 p-3 sm:p-4">
								<h3 className="text-sm font-medium text-slate-300 mb-3 text-center">
									Incident Types Reference
								</h3>
								<div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-2">
									{incidentTypeData.map((item, i) => (
										<div
											key={item.type}
											className="flex items-center gap-2 p-2 rounded-md bg-slate-900/50 border border-slate-700/30"
										>
											<span
												className="h-2.5 w-2.5 rounded-sm flex-shrink-0"
												style={{ backgroundColor: COLOR_PALETTE[i % COLOR_PALETTE.length] }}
											/>
											<div className="min-w-0">
												<span className="text-[10px] sm:text-xs text-slate-100 block truncate">
													{item.type}
												</span>
												<span className="text-[9px] sm:text-[10px] text-slate-400">
													{item.count.toLocaleString()}
												</span>
											</div>
										</div>
									))}
								</div>
							</div>
						)}
					</CardContent>
				</Card>

				{/* ── Pagination ── */}
				{graphType !== 'type' && filteredData.length > storesPerPage && (
					<div className="flex flex-col sm:flex-row justify-between items-center gap-3 px-1">
						<p className="text-xs sm:text-sm text-slate-400 order-2 sm:order-1">
							Showing {((currentPage - 1) * storesPerPage) + 1}–{Math.min(currentPage * storesPerPage, filteredData.length)} of {filteredData.length} stores
						</p>
						<div className="flex items-center gap-1.5 order-1 sm:order-2">
							<Button
								variant="outline"
								size="sm"
								onClick={() => setCurrentPage(p => Math.max(p - 1, 1))}
								disabled={currentPage === 1}
								className="border-slate-700 hover:bg-slate-800 text-slate-300 h-8 px-3 text-xs"
							>
								Previous
							</Button>
							{Array.from({ length: Math.min(5, totalPages) }, (_, i) => i + 1).map(page => (
								<Button
									key={page}
									variant={currentPage === page ? 'default' : 'outline'}
									size="sm"
									onClick={() => setCurrentPage(page)}
									className={cn(
										'h-8 w-8 p-0 text-xs',
										currentPage === page
											? 'bg-indigo-500 hover:bg-indigo-600 text-white border-transparent'
											: 'border-slate-700 hover:bg-slate-800 text-slate-300'
									)}
								>
									{page}
								</Button>
							))}
							{totalPages > 5 && (
								<>
									<span className="text-slate-500 text-xs">…</span>
									<Button
										variant={currentPage === totalPages ? 'default' : 'outline'}
										size="sm"
										onClick={() => setCurrentPage(totalPages)}
										className={cn(
											'h-8 w-8 p-0 text-xs',
											currentPage === totalPages
												? 'bg-indigo-500 hover:bg-indigo-600 text-white border-transparent'
												: 'border-slate-700 hover:bg-slate-800 text-slate-300'
										)}
									>
										{totalPages}
									</Button>
								</>
							)}
							<Button
								variant="outline"
								size="sm"
								onClick={() => setCurrentPage(p => Math.min(p + 1, totalPages))}
								disabled={currentPage === totalPages}
								className="border-slate-700 hover:bg-slate-800 text-slate-300 h-8 px-3 text-xs"
							>
								Next
							</Button>
						</div>
					</div>
				)}

			</div>
		</div>
	)
}

export default IncidentGraph
