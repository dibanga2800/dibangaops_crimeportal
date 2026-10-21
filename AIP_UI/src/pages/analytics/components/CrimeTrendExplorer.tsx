/**
 * Crime Trend Explorer Module
 * 
 * Displays day of week, time of day, and incident type analytics
 * with interactive drill-downs to store level.
 */

import { useState, useMemo } from 'react'
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts'
import { Button } from '@/components/ui/button'
import { Segmented } from '@/components/common/Segmented'
import { useChartTheme, chartTooltipStyle, axisTick } from '@/lib/chart-theme'
import type { CrimeTrendData, DayOfWeekData, StoreDrilldownData, TimeOfDayData } from '@/types/analytics'
import { Clock, MapPin, ChevronRight, ArrowLeft } from 'lucide-react'

interface CrimeTrendExplorerProps {
	data: CrimeTrendData
	loading?: boolean
}

type ChartTooltipProps = {
	active?: boolean
	payload?: Array<{ payload?: DayOfWeekData | TimeOfDayData }>
}

const getStoreDayCount = (store: StoreDrilldownData, day: string): number => {
	if (store.incidentsByDay && Object.keys(store.incidentsByDay).length > 0) {
		return Number(store.incidentsByDay[day] ?? 0)
	}
	return store.peakDay === day ? store.incidents : 0
}

const getStoreHourCount = (store: StoreDrilldownData, hour: number): number => {
	const hourKey = String(hour)
	if (store.incidentsByHour && Object.keys(store.incidentsByHour).length > 0) {
		return Number(store.incidentsByHour[hourKey] ?? store.incidentsByHour[hour] ?? 0)
	}
	return store.peakHour === hour ? store.incidents : 0
}

type DayOfWeekChartRow = DayOfWeekData & {
	storesPeaking: number
	storesWithIncidents: number
}

type TimeOfDayChartRow = TimeOfDayData & {
	storesPeaking: number
	storesWithIncidents: number
}

const DayOfWeekTooltip = ({ active, payload }: ChartTooltipProps) => {
	if (!active || !payload?.length) {
		return null
	}

	const point = payload[0]?.payload as DayOfWeekChartRow | undefined
	if (!point) {
		return null
	}

	return (
		<div className="rounded-md border border-border bg-background px-3 py-2 text-sm shadow-md">
			<p className="font-semibold">{point.day}</p>
			<p>
				{point.incidents} {point.incidents === 1 ? 'incident' : 'incidents'} ({point.percentage.toFixed(1)}% of period)
			</p>
			<p className="text-muted-foreground">
				across {point.storesWithIncidents} {point.storesWithIncidents === 1 ? 'store' : 'stores'} on this day
			</p>
		</div>
	)
}

const TimeOfDayTooltip = ({ active, payload }: ChartTooltipProps) => {
	if (!active || !payload?.length) {
		return null
	}

	const point = payload[0]?.payload as TimeOfDayChartRow | undefined
	if (!point) {
		return null
	}

	return (
		<div className="rounded-md border border-border bg-background px-3 py-2 text-sm shadow-md">
			<p className="font-semibold">{point.label}</p>
			<p>
				{point.incidents} {point.incidents === 1 ? 'incident' : 'incidents'} ({point.percentage.toFixed(1)}% of period)
			</p>
			<p className="text-muted-foreground">
				across {point.storesWithIncidents} {point.storesWithIncidents === 1 ? 'store' : 'stores'} at this hour
			</p>
		</div>
	)
}

const CHART_COLORS = [
	'#1F6FA8', // blue
	'#0E7C66', // green
	'#C4680F', // amber
	'#B42318', // red
	'#5B4A8B', // purple
	'#A3365E', // pink
	'#1C7C78', // teal
]

export const CrimeTrendExplorer = ({ data, loading = false }: CrimeTrendExplorerProps) => {
	const [selectedDay, setSelectedDay] = useState<string | null>(null)
	const [selectedHour, setSelectedHour] = useState<number | null>(null)
	const [selectedStore, setSelectedStore] = useState<string | null>(null)
	const [view, setView] = useState<'day' | 'hour'>('day')
	const theme = useChartTheme()

	const storeList = useMemo(() => Object.values(data.storeDrilldown), [data.storeDrilldown])

	const dayOfWeekData = useMemo<DayOfWeekChartRow[]>(() => {
		return data.dayOfWeek.map((item) => {
			const peakingStores = storeList.filter((store) => store.peakDay === item.day)
			const storesWithIncidents = storeList.filter(
				(store) => getStoreDayCount(store, item.day) > 0,
			).length
			return {
				...item,
				storesPeaking: peakingStores.length,
				storesWithIncidents,
				fill: CHART_COLORS[0],
			}
		})
	}, [data.dayOfWeek, storeList])

	const timeOfDayData = useMemo<TimeOfDayChartRow[]>(() => {
		// Show every hour the backend reports so no incidents are hidden;
		// order ascending by hour for a readable 24-hour distribution.
		return [...data.timeOfDay]
			.sort((a, b) => a.hour - b.hour)
			.map((item) => {
				const peakingStores = storeList.filter((store) => store.peakHour === item.hour)
				const storesWithIncidents = storeList.filter(
					(store) => getStoreHourCount(store, item.hour) > 0,
				).length
				return {
					...item,
					storesPeaking: peakingStores.length,
					storesWithIncidents,
					fill: CHART_COLORS[1],
				}
			})
	}, [data.timeOfDay, storeList])

	const incidentTypeData = useMemo(() => {
		return data.incidentTypes.map((item, index) => ({
			...item,
			fill: CHART_COLORS[index % CHART_COLORS.length],
		}))
	}, [data.incidentTypes])

	const recoveryTrendData = useMemo(() => {
		return data.recoveryTrend.map((item) => ({
			...item,
			stolenValueLabel: Number(item.stolenValue.toFixed(2)),
			recoveredValueLabel: Number(item.recoveredValue.toFixed(2)),
			lostValueLabel: Number(item.lostValue.toFixed(2)),
		}))
	}, [data.recoveryTrend])

	const handleDayClick = (day: string) => {
		if (selectedDay === day) {
			setSelectedDay(null)
			setSelectedStore(null)
		} else {
			setSelectedDay(day)
			setSelectedStore(null)
		}
	}

	const handleHourClick = (hour: number) => {
		if (selectedHour === hour) {
			setSelectedHour(null)
			setSelectedStore(null)
		} else {
			setSelectedHour(hour)
			setSelectedStore(null)
		}
	}

	const handleStoreClick = (storeName: string) => {
		setSelectedStore(storeName)
	}

	type StoreSliceRow = {
		store: StoreDrilldownData
		sliceIncidents: number
	}

	const filteredStores = useMemo((): StoreSliceRow[] => {
		if (!selectedDay) {
			return []
		}

		// Show every store that recorded at least one incident on the selected day so the
		// per-store rows reconcile with the bar total (incidents across all stores that day).
		return Object.values(data.storeDrilldown)
			.map((store) => ({
				store,
				sliceIncidents: getStoreDayCount(store, selectedDay),
			}))
			.filter((row) => row.sliceIncidents > 0)
			.sort((a, b) => b.sliceIncidents - a.sliceIncidents)
	}, [selectedDay, data.storeDrilldown])

	const filteredDayIncidentTotal = useMemo(
		() => filteredStores.reduce((sum, row) => sum + row.sliceIncidents, 0),
		[filteredStores],
	)

	const filteredStoresByHour = useMemo((): StoreSliceRow[] => {
		if (selectedHour === null) {
			return []
		}

		// Show every store that recorded at least one incident at the selected hour so the
		// per-store rows reconcile with the bar total (incidents across all stores that hour).
		return Object.values(data.storeDrilldown)
			.map((store) => ({
				store,
				sliceIncidents: getStoreHourCount(store, selectedHour),
			}))
			.filter((row) => row.sliceIncidents > 0)
			.sort((a, b) => b.sliceIncidents - a.sliceIncidents)
	}, [selectedHour, data.storeDrilldown])

	const filteredHourIncidentTotal = useMemo(
		() => filteredStoresByHour.reduce((sum, row) => sum + row.sliceIncidents, 0),
		[filteredStoresByHour],
	)

	const selectedHourLabel = useMemo(() => {
		if (selectedHour === null) {
			return null
		}
		return data.timeOfDay.find((slot) => slot.hour === selectedHour)?.label ?? `${selectedHour}:00`
	}, [selectedHour, data.timeOfDay])

	const selectedStoreData = useMemo(() => {
		if (!selectedStore) {
			return null
		}

		const store = data.storeDrilldown[selectedStore]
		if (!store) {
			return null
		}

		if (selectedDay) {
			const sliceIncidents = getStoreDayCount(store, selectedDay)
			return {
				...store,
				incidents: sliceIncidents,
				incidentTypes: store.incidentTypesByDay?.[selectedDay] ?? [],
				filterContext: `on ${selectedDay}`,
			}
		}

		if (selectedHour !== null) {
			const sliceIncidents = getStoreHourCount(store, selectedHour)
			const hourKey = String(selectedHour)
			return {
				...store,
				incidents: sliceIncidents,
				incidentTypes:
					store.incidentTypesByHour?.[hourKey] ??
					store.incidentTypesByHour?.[selectedHour] ??
					[],
				filterContext: `at ${selectedHourLabel ?? `${selectedHour}:00`}`,
			}
		}

		return store
	}, [selectedStore, selectedDay, selectedHour, selectedHourLabel, data.storeDrilldown])

	const peakDay = useMemo(() => [...dayOfWeekData].sort((a, b) => b.incidents - a.incidents)[0], [dayOfWeekData])
	const peakHour = useMemo(() => [...timeOfDayData].sort((a, b) => b.incidents - a.incidents)[0], [timeOfDayData])
	const sortedTypes = useMemo(() => [...incidentTypeData].sort((a, b) => b.count - a.count), [incidentTypeData])
	const maxTypeCount = sortedTypes[0]?.count || 1

	if (loading) {
		return <div className="h-[420px] animate-pulse rounded-lg border border-border bg-card" aria-busy="true" aria-label="Loading crime trends" />
	}

	const moneyTick = (v: number) => (Math.abs(v) >= 1000 ? `£${(v / 1000).toFixed(v >= 10000 ? 0 : 1)}k` : `£${v}`)
	const legend = [
		{ key: 'stolenValue', label: 'Stolen', color: theme.foreground },
		{ key: 'recoveredValue', label: 'Recovered', color: theme.recovered },
		{ key: 'lostValue', label: 'Lost', color: theme.loss },
	]

	const storeList_ = (rows: StoreSliceRow[], slice: string, peakLabel: string, peakValue: (s: StoreDrilldownData) => string) => (
		<div className="border-t border-border">
			<div className="flex flex-wrap items-baseline justify-between gap-2 px-4 pt-4 sm:px-5">
				<h4 className="flex items-center gap-2 text-sm font-semibold text-foreground">
					<MapPin className="h-4 w-4 text-muted-foreground" aria-hidden />
					Stores {slice}
				</h4>
				<span className="figures text-xs text-muted-foreground">
					{rows.length} {rows.length === 1 ? 'store' : 'stores'} · {rows.reduce((s, r) => s + r.sliceIncidents, 0)} incidents
				</span>
			</div>
			{rows.length === 0 ? (
				<p className="px-4 py-6 text-center text-sm text-muted-foreground sm:px-5">No stores recorded incidents {slice}.</p>
			) : (
				<ul className="mt-2 divide-y divide-border">
					{rows.map(({ store, sliceIncidents }) => (
						<li key={store.storeId}>
							<button
								type="button"
								onClick={() => handleStoreClick(store.storeName)}
								className="!flex w-full !items-center !justify-between gap-3 !rounded-none !border-0 !px-4 !py-2.5 text-left hover:bg-muted/50 sm:!px-5"
							>
								<span className="min-w-0">
									<span className="block truncate text-sm font-medium text-foreground">{store.storeName}</span>
									<span className="block text-xs text-muted-foreground">
										{peakLabel} {peakValue(store)}
									</span>
								</span>
								<span className="flex shrink-0 items-center gap-2">
									<span className="figures text-sm font-semibold text-foreground">{sliceIncidents}</span>
									<ChevronRight className="h-4 w-4 text-muted-foreground" aria-hidden />
								</span>
							</button>
						</li>
					))}
				</ul>
			)}
		</div>
	)

	if (selectedStore && selectedStoreData) {
		const inSelection = 'filterContext' in selectedStoreData && selectedStoreData.filterContext
		const cells = [
			{ label: inSelection ? 'Incidents in selection' : 'Incidents', value: String(selectedStoreData.incidents), tone: 'text-foreground' },
			{ label: 'Peak day', value: selectedStoreData.peakDay, tone: 'text-foreground' },
			{ label: 'Peak hour', value: `${selectedStoreData.peakHour}:00`, tone: 'text-foreground' },
			{ label: 'Stolen', value: `£${selectedStoreData.totalStolenValue.toFixed(0)}`, tone: 'text-foreground' },
			{ label: 'Recovered', value: `£${selectedStoreData.totalRecoveredValue.toFixed(0)}`, tone: 'text-recovered' },
			{ label: 'Lost', value: `£${selectedStoreData.totalLostValue.toFixed(0)}`, tone: 'text-loss' },
		]
		return (
			<section className="rounded-lg border border-border bg-card">
				<div className="flex flex-wrap items-start justify-between gap-3 p-4 sm:p-5">
					<div>
						<Button variant="ghost" size="sm" onClick={() => setSelectedStore(null)} className="-ml-2 mb-1 h-8 px-2 text-muted-foreground">
							<ArrowLeft className="h-4 w-4" />
							Back to trends
						</Button>
						<h3 className="font-display text-2xl font-semibold text-foreground">{selectedStoreData.storeName}</h3>
						<p className="text-[13px] text-muted-foreground">
							{inSelection
								? `Incidents ${selectedStoreData.filterContext}`
								: 'Every incident at this store in the period'}{' '}
							· recovery rate {selectedStoreData.recoveryRate.toFixed(1)}%
						</p>
					</div>
				</div>
				<dl className="figures grid grid-cols-2 border-y border-border sm:grid-cols-3 xl:grid-cols-6">
					{cells.map((cell) => (
						<div key={cell.label} className="px-4 py-3 shadow-[1px_1px_0_0_hsl(var(--border))] sm:px-5">
							<dt className="text-xs font-medium text-muted-foreground">{cell.label}</dt>
							<dd className={`font-display text-xl font-semibold ${cell.tone}`}>{cell.value}</dd>
						</div>
					))}
				</dl>
				<div className="p-4 sm:p-5">
					<h4 className="mb-3 text-sm font-semibold text-foreground">Incident types</h4>
					{selectedStoreData.incidentTypes.length === 0 ? (
						<p className="text-sm text-muted-foreground">No incident types recorded for this selection.</p>
					) : (
						<ul className="space-y-3">
							{selectedStoreData.incidentTypes.map((type) => (
								<li key={type.type}>
									<div className="flex items-baseline justify-between gap-3 text-sm">
										<span className="truncate font-medium text-foreground">{type.type}</span>
										<span className="figures shrink-0 text-muted-foreground">
											<span className="font-semibold text-foreground">{type.count}</span> · {type.percentage.toFixed(1)}% · £
											{type.totalValue.toFixed(2)}
										</span>
									</div>
									<div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-muted">
										<div className="h-full rounded-full bg-primary" style={{ width: `${Math.min(100, type.percentage)}%` }} />
									</div>
								</li>
							))}
						</ul>
					)}
				</div>
			</section>
		)
	}

	return (
		<div className="space-y-5">
			{/* Value over time */}
			<section className="rounded-lg border border-border bg-card">
				<div className="flex flex-wrap items-start justify-between gap-3 p-4 sm:p-5">
					<div>
						<h3 className="text-base font-semibold text-foreground">Value stolen, recovered and lost</h3>
						<p className="mt-0.5 text-[13px] text-muted-foreground">Per period across the selected stores.</p>
					</div>
					<ul className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground" aria-label="Legend">
						{legend.map((item) => (
							<li key={item.key} className="flex items-center gap-1.5">
								<span className="h-0.5 w-4 rounded-full" style={{ backgroundColor: item.color }} aria-hidden />
								{item.label}
							</li>
						))}
					</ul>
				</div>
				<div className="h-[260px] w-full px-2 pb-3 sm:h-[300px] sm:px-4">
					<ResponsiveContainer width="100%" height="100%">
						<AreaChart data={recoveryTrendData} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
							<defs>
								<linearGradient id="cte-lost" x1="0" y1="0" x2="0" y2="1">
									<stop offset="0%" stopColor={theme.loss} stopOpacity={0.18} />
									<stop offset="100%" stopColor={theme.loss} stopOpacity={0} />
								</linearGradient>
							</defs>
							<CartesianGrid vertical={false} stroke={theme.grid} />
							<XAxis dataKey="period" tick={axisTick(theme)} tickLine={false} axisLine={{ stroke: theme.grid }} minTickGap={16} />
							<YAxis tick={axisTick(theme)} tickLine={false} axisLine={false} width={48} tickFormatter={moneyTick} />
							<Tooltip {...chartTooltipStyle(theme)} formatter={(v: number) => `£${Number(v).toLocaleString('en-GB', { maximumFractionDigits: 0 })}`} />
							<Area type="monotone" dataKey="stolenValue" name="Stolen" stroke={theme.foreground} strokeWidth={1.75} fill="none" dot={false} />
							<Area type="monotone" dataKey="lostValue" name="Lost" stroke={theme.loss} strokeWidth={2} fill="url(#cte-lost)" dot={false} />
							<Area type="monotone" dataKey="recoveredValue" name="Recovered" stroke={theme.recovered} strokeWidth={2} fill="none" dot={false} />
						</AreaChart>
					</ResponsiveContainer>
				</div>
			</section>

			<div className="grid gap-5 xl:grid-cols-[minmax(0,7fr)_minmax(0,5fr)]">
				{/* When it happens */}
				<section className="rounded-lg border border-border bg-card">
					<div className="flex flex-wrap items-start justify-between gap-3 p-4 sm:p-5">
						<div>
							<h3 className="text-base font-semibold text-foreground">When incidents happen</h3>
							<p className="mt-0.5 text-[13px] text-muted-foreground">
								{view === 'day'
									? peakDay
										? <>Busiest day: <span className="font-medium text-foreground">{peakDay.day}</span>. Select a bar to see the stores.</>
										: 'Select a bar to see the stores.'
									: peakHour
										? <>Busiest hour: <span className="font-medium text-foreground">{peakHour.label}</span>. Select a bar to see the stores.</>
										: 'Select a bar to see the stores.'}
							</p>
						</div>
						<Segmented<'day' | 'hour'>
							ariaLabel="Group by"
							size="sm"
							value={view}
							onChange={(v) => {
								setView(v)
								setSelectedDay(null)
								setSelectedHour(null)
							}}
							options={[
								{ value: 'day', label: 'Day of week' },
								{ value: 'hour', label: 'Hour of day' },
							]}
						/>
					</div>
					<div className="h-[260px] w-full px-2 pb-3 sm:h-[300px] sm:px-4">
						<ResponsiveContainer width="100%" height="100%">
							{view === 'day' ? (
								<BarChart key="day" data={dayOfWeekData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
									<CartesianGrid vertical={false} stroke={theme.grid} />
									<XAxis dataKey="day" tick={axisTick(theme)} tickLine={false} axisLine={{ stroke: theme.grid }} tickFormatter={(d: string) => d.slice(0, 3)} />
									<YAxis tick={axisTick(theme)} tickLine={false} axisLine={false} width={36} allowDecimals={false} />
									<Tooltip content={<DayOfWeekTooltip />} cursor={{ fill: theme.grid, fillOpacity: 0.35 }} />
									<Bar
										dataKey="incidents"
										name="Incidents"
										radius={[3, 3, 0, 0]}
										animationDuration={450}
										maxBarSize={56}
										onClick={(barData) => {
											const payload = barData?.payload as DayOfWeekData | undefined
											if (payload?.day) handleDayClick(payload.day)
										}}
										style={{ cursor: 'pointer' }}
									>
										{dayOfWeekData.map((row) => (
											<Cell
												key={row.day}
												fill={theme.primary}
												fillOpacity={selectedDay ? (row.day === selectedDay ? 1 : 0.2) : row.day === peakDay?.day ? 1 : 0.5}
											/>
										))}
									</Bar>
								</BarChart>
							) : (
								<BarChart key="hour" data={timeOfDayData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
									<CartesianGrid vertical={false} stroke={theme.grid} />
									<XAxis dataKey="label" tick={axisTick(theme)} tickLine={false} axisLine={{ stroke: theme.grid }} interval="preserveStartEnd" minTickGap={12} />
									<YAxis tick={axisTick(theme)} tickLine={false} axisLine={false} width={36} allowDecimals={false} />
									<Tooltip content={<TimeOfDayTooltip />} cursor={{ fill: theme.grid, fillOpacity: 0.35 }} />
									<Bar
										dataKey="incidents"
										name="Incidents"
										radius={[3, 3, 0, 0]}
										animationDuration={450}
										maxBarSize={32}
										onClick={(barData) => {
											const payload = barData?.payload as TimeOfDayData | undefined
											if (payload?.hour !== undefined) handleHourClick(payload.hour)
										}}
										style={{ cursor: 'pointer' }}
									>
										{timeOfDayData.map((row) => (
											<Cell
												key={row.hour}
												fill={theme.primary}
												fillOpacity={selectedHour !== null ? (row.hour === selectedHour ? 1 : 0.2) : row.hour === peakHour?.hour ? 1 : 0.5}
											/>
										))}
									</Bar>
								</BarChart>
							)}
						</ResponsiveContainer>
					</div>
					{view === 'day' && selectedDay && storeList_(filteredStores, `on ${selectedDay}`, 'Peak hour', (s) => `${s.peakHour}:00`)}
					{view === 'hour' && selectedHour !== null &&
						storeList_(filteredStoresByHour, `at ${selectedHourLabel ?? `${selectedHour}:00`}`, 'Peak day', (s) => s.peakDay)}
					{view === 'hour' && selectedHour === null && (() => {
						const peaks = [...timeOfDayData]
							.filter((item) => item.storesPeaking > 0)
							.sort((a, b) => b.storesPeaking - a.storesPeaking)
							.slice(0, 3)
						if (!peaks.length) return null
						return (
							<div className="border-t border-border px-4 py-3 sm:px-5">
								<p className="mb-2 text-xs font-medium text-muted-foreground">Most common store peak hours</p>
								<div className="flex flex-wrap gap-2">
									{peaks.map((item) => (
										<button
											key={item.hour}
											type="button"
											onClick={() => handleHourClick(item.hour)}
											className="!flex !items-center gap-2 !rounded-full !border !border-border !px-3 !py-1 text-sm hover:bg-muted"
										>
											<Clock className="h-3.5 w-3.5 text-muted-foreground" aria-hidden />
											<span className="font-medium text-foreground">{item.label}</span>
											<span className="figures text-xs text-muted-foreground">
												{item.storesPeaking} {item.storesPeaking === 1 ? 'store' : 'stores'}
											</span>
										</button>
									))}
								</div>
							</div>
						)
					})()}
				</section>

				{/* What happens */}
				<section className="rounded-lg border border-border bg-card">
					<div className="p-4 sm:p-5">
						<h3 className="text-base font-semibold text-foreground">Incident types</h3>
						<p className="mt-0.5 text-[13px] text-muted-foreground">Share of all incidents in the period.</p>
					</div>
					{sortedTypes.length === 0 ? (
						<p className="px-4 pb-6 text-sm text-muted-foreground sm:px-5">No incidents recorded.</p>
					) : (
						<ul className="space-y-3.5 px-4 pb-5 sm:px-5">
							{sortedTypes.map((item, index) => (
								<li key={item.type}>
									<div className="flex items-baseline justify-between gap-3 text-sm">
										<span className="truncate font-medium text-foreground">{item.type}</span>
										<span className="figures shrink-0 text-muted-foreground">
											<span className="font-semibold text-foreground">{item.count}</span> · {item.percentage.toFixed(1)}%
										</span>
									</div>
									<div className="mt-1.5 h-2 overflow-hidden rounded-full bg-muted">
										<div
											className="h-full rounded-full"
											style={{
												width: `${(item.count / maxTypeCount) * 100}%`,
												backgroundColor: index === 0 ? theme.primary : theme.series[0] ?? theme.primary,
												opacity: index === 0 ? 1 : 0.55,
											}}
										/>
									</div>
								</li>
							))}
						</ul>
					)}
				</section>
			</div>
		</div>
	)
}
