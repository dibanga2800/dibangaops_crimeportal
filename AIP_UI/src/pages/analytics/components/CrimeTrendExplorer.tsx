/**
 * Crime Trend Explorer Module
 * 
 * Displays day of week, time of day, and incident type analytics
 * with interactive drill-downs to store level.
 */

import { useState, useMemo } from 'react'
import {
	BarChart,
	Bar,
	LineChart,
	Line,
	XAxis,
	YAxis,
	CartesianGrid,
	Tooltip,
	Legend,
	ResponsiveContainer,
	PieChart,
	Pie,
	Cell,
} from 'recharts'
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
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
import type { CrimeTrendData, DayOfWeekData, StoreDrilldownData, TimeOfDayData } from '@/types/analytics'
import {
	Calendar,
	Clock,
	TrendingUp,
	MapPin,
	ChevronRight,
	ArrowLeft,
} from 'lucide-react'

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

type DayOfWeekChartRow = DayOfWeekData & { storesPeaking: number }
type TimeOfDayChartRow = TimeOfDayData & { storesPeaking: number }

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
				{point.storesPeaking} {point.storesPeaking === 1 ? 'store peaks' : 'stores peak'} on this day
			</p>
			<p className="text-muted-foreground">
				{point.incidents} incidents across all stores ({point.percentage.toFixed(1)}% of period)
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
				{point.storesPeaking} {point.storesPeaking === 1 ? 'store peaks' : 'stores peak'} at this hour
			</p>
			<p className="text-muted-foreground">
				{point.incidents} incidents across all stores ({point.percentage.toFixed(1)}% of period)
			</p>
		</div>
	)
}

const CHART_COLORS = [
	'#3b82f6', // blue
	'#10b981', // green
	'#f59e0b', // amber
	'#ef4444', // red
	'#8b5cf6', // purple
	'#ec4899', // pink
	'#14b8a6', // teal
]

export const CrimeTrendExplorer = ({ data, loading = false }: CrimeTrendExplorerProps) => {
	const [selectedDay, setSelectedDay] = useState<string | null>(null)
	const [selectedHour, setSelectedHour] = useState<number | null>(null)
	const [selectedStore, setSelectedStore] = useState<string | null>(null)

	const storeList = useMemo(() => Object.values(data.storeDrilldown), [data.storeDrilldown])

	const dayOfWeekData = useMemo<DayOfWeekChartRow[]>(() => {
		return data.dayOfWeek.map((item) => ({
			...item,
			storesPeaking: storeList.filter((store) => store.peakDay === item.day).length,
			fill: CHART_COLORS[0],
		}))
	}, [data.dayOfWeek, storeList])

	const timeOfDayData = useMemo<TimeOfDayChartRow[]>(() => {
		// Filter to only show store operating hours (7 AM - 10 PM)
		return data.timeOfDay
			.filter((item) => item.hour >= 7 && item.hour <= 22)
			.map((item) => ({
				...item,
				storesPeaking: storeList.filter((store) => store.peakHour === item.hour).length,
				fill: CHART_COLORS[1],
			}))
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

		// Strict mode: only show stores whose *own* peak day matches the selected slice.
		// Stores that have some incidents on this day but peak on a different day are
		// excluded to keep the drill-down focused on stores that PATTERN around this day.
		return Object.values(data.storeDrilldown)
			.filter((store) => store.peakDay === selectedDay)
			.map((store) => ({
				store,
				sliceIncidents: getStoreDayCount(store, selectedDay),
			}))
			.filter((row) => row.sliceIncidents > 0)
			.sort((a, b) => b.sliceIncidents - a.sliceIncidents)
	}, [selectedDay, data.storeDrilldown])

	const filteredStoresByHour = useMemo((): StoreSliceRow[] => {
		if (selectedHour === null) {
			return []
		}

		// Strict mode: only show stores whose *own* peak hour matches the selected slice.
		// Stores that recorded incidents at this hour but peak at a different hour are
		// excluded.
		return Object.values(data.storeDrilldown)
			.filter((store) => store.peakHour === selectedHour)
			.map((store) => ({
				store,
				sliceIncidents: getStoreHourCount(store, selectedHour),
			}))
			.filter((row) => row.sliceIncidents > 0)
			.sort((a, b) => b.sliceIncidents - a.sliceIncidents)
	}, [selectedHour, data.storeDrilldown])

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

	if (loading) {
		return (
			<Card>
				<CardHeader>
					<CardTitle>Crime Trend Explorer</CardTitle>
					<CardDescription>Loading analytics data...</CardDescription>
				</CardHeader>
			</Card>
		)
	}

	return (
		<Card className="w-full shadow-sm border-2">
			<CardHeader className="pb-4">
				<div className="flex items-center justify-between">
					<div>
						<CardTitle className="flex items-center gap-2">
							<TrendingUp className="h-5 w-5" />
							Crime Trend Explorer
						</CardTitle>
						<CardDescription>
							Analyze crime patterns by day, time, and incident type
						</CardDescription>
						<div className="mt-1 flex items-center gap-2 flex-wrap">
							{selectedDay && (
								<div className="flex items-center gap-2">
									<span className="text-sm text-muted-foreground">Day:</span>
									<Badge variant="secondary">{selectedDay}</Badge>
								</div>
							)}
							{selectedHour !== null && (
								<div className="flex items-center gap-2">
									<span className="text-sm text-muted-foreground">Hour:</span>
									<Badge variant="secondary">{selectedHour}:00</Badge>
								</div>
							)}
						</div>
					</div>
					{(selectedDay || selectedHour || selectedStore) && (
						<Button
							variant="outline"
							size="sm"
							onClick={() => {
								setSelectedDay(null)
								setSelectedHour(null)
								setSelectedStore(null)
							}}
						>
							<ArrowLeft className="h-4 w-4 mr-2" />
							Reset View
						</Button>
					)}
				</div>
			</CardHeader>
			<CardContent className="pt-6">
				<div className="mb-8 space-y-4">
					<div>
						<h3 className="font-semibold">Recovered vs Lost Trend</h3>
						<p className="text-sm text-muted-foreground">
							Track stolen value against saved and unrecovered loss over time.
						</p>
					</div>
					<div className="h-[320px] w-full">
						<ResponsiveContainer width="100%" height="100%">
							<LineChart data={recoveryTrendData}>
								<CartesianGrid strokeDasharray="3 3" className="stroke-gray-200" />
								<XAxis dataKey="period" className="text-xs" tick={{ fill: '#6b7280' }} />
								<YAxis className="text-xs" tick={{ fill: '#6b7280' }} />
								<Tooltip
									contentStyle={{
										backgroundColor: '#fff',
										border: '1px solid #e5e7eb',
										borderRadius: '0.5rem',
									}}
								/>
								<Legend />
								<Line type="monotone" dataKey="stolenValue" name="Stolen" stroke="#6366f1" strokeWidth={2} dot={false} />
								<Line type="monotone" dataKey="recoveredValue" name="Saved" stroke="#10b981" strokeWidth={2} dot={false} />
								<Line type="monotone" dataKey="lostValue" name="Lost" stroke="#ef4444" strokeWidth={2} dot={false} />
							</LineChart>
						</ResponsiveContainer>
					</div>
				</div>

				{!selectedStore ? (
					<Tabs defaultValue="day-of-week" className="w-full">
						<TabsList className="grid w-full grid-cols-3 mb-6">
							<TabsTrigger value="day-of-week">Day of Week</TabsTrigger>
							<TabsTrigger value="time-of-day">Time of Day</TabsTrigger>
							<TabsTrigger value="incident-types">Incident Types</TabsTrigger>
						</TabsList>

						<TabsContent value="day-of-week" className="space-y-6 mt-6">
							<div className="h-[400px] w-full">
								<ResponsiveContainer width="100%" height="100%">
									<BarChart data={dayOfWeekData}>
										<CartesianGrid strokeDasharray="3 3" className="stroke-gray-200" />
										<XAxis
											dataKey="day"
											className="text-xs"
											tick={{ fill: '#6b7280' }}
										/>
										<YAxis className="text-xs" tick={{ fill: '#6b7280' }} />
										<Tooltip content={<DayOfWeekTooltip />} />
										<Bar
											dataKey="storesPeaking"
											fill={CHART_COLORS[0]}
											name="Stores peaking"
											radius={[8, 8, 0, 0]}
											onClick={(barData) => {
												const payload = barData?.payload as DayOfWeekData | undefined
												if (payload?.day) {
													handleDayClick(payload.day)
												}
											}}
											style={{ cursor: 'pointer' }}
										/>
									</BarChart>
								</ResponsiveContainer>
							</div>

							{selectedDay && filteredStores.length > 0 && (
								<div className="space-y-4">
									<div className="flex items-center gap-2">
										<MapPin className="h-4 w-4 text-gray-600" />
										<h3 className="font-semibold">
											Stores peaking on {selectedDay} ({filteredStores.length}{' '}
											{filteredStores.length === 1 ? 'store' : 'stores'})
										</h3>
									</div>
									<div className="border rounded-lg">
										<Table>
											<TableHeader>
												<TableRow>
													<TableHead>Store</TableHead>
													<TableHead>{`Incidents on ${selectedDay}`}</TableHead>
													<TableHead>Store Peak Hour</TableHead>
													<TableHead className="text-right">Actions</TableHead>
												</TableRow>
											</TableHeader>
											<TableBody>
												{filteredStores.map(({ store, sliceIncidents }) => (
													<TableRow key={store.storeId}>
														<TableCell className="font-medium">
															{store.storeName}
														</TableCell>
														<TableCell>{sliceIncidents}</TableCell>
														<TableCell>
															<Badge variant="outline">{store.peakHour}:00</Badge>
														</TableCell>
														<TableCell className="text-right">
															<Button
																variant="ghost"
																size="sm"
																onClick={() => handleStoreClick(store.storeName)}
															>
																View Details
																<ChevronRight className="h-4 w-4 ml-1" />
															</Button>
														</TableCell>
													</TableRow>
												))}
											</TableBody>
										</Table>
									</div>
								</div>
							)}

							{selectedDay && filteredStores.length === 0 && (
								<div className="text-center py-8 text-gray-500">
									No stores peak on {selectedDay}
								</div>
							)}
						</TabsContent>

						<TabsContent value="time-of-day" className="space-y-6 mt-6">
							<div className="h-[400px] w-full">
								<ResponsiveContainer width="100%" height="100%">
									<BarChart data={timeOfDayData}>
										<CartesianGrid strokeDasharray="3 3" className="stroke-gray-200" />
										<XAxis
											dataKey="label"
											className="text-xs"
											tick={{ fill: '#6b7280' }}
											angle={-45}
											textAnchor="end"
											height={80}
										/>
										<YAxis className="text-xs" tick={{ fill: '#6b7280' }} />
										<Tooltip content={<TimeOfDayTooltip />} />
										<Bar
											dataKey="storesPeaking"
											fill={CHART_COLORS[1]}
											name="Stores peaking"
											radius={[8, 8, 0, 0]}
											onClick={(barData) => {
												const payload = barData?.payload as TimeOfDayData | undefined
												if (payload?.hour !== undefined) {
													handleHourClick(payload.hour)
												}
											}}
											style={{ cursor: 'pointer' }}
										/>
									</BarChart>
								</ResponsiveContainer>
							</div>

							{selectedHour !== null && filteredStoresByHour.length > 0 && (
								<div className="space-y-4">
									<div className="flex items-center gap-2">
										<MapPin className="h-4 w-4 text-gray-600" />
										<h3 className="font-semibold">
											Stores peaking at {selectedHourLabel ?? `${selectedHour}:00`} (
											{filteredStoresByHour.length}{' '}
											{filteredStoresByHour.length === 1 ? 'store' : 'stores'})
										</h3>
									</div>
									<div className="border rounded-lg">
										<Table>
											<TableHeader>
												<TableRow>
													<TableHead>Store</TableHead>
													<TableHead>{`Incidents at ${selectedHourLabel ?? `${selectedHour}:00`}`}</TableHead>
													<TableHead>Store Peak Day</TableHead>
													<TableHead className="text-right">Actions</TableHead>
												</TableRow>
											</TableHeader>
											<TableBody>
												{filteredStoresByHour.map(({ store, sliceIncidents }) => (
													<TableRow key={store.storeId}>
														<TableCell className="font-medium">
															{store.storeName}
														</TableCell>
														<TableCell>{sliceIncidents}</TableCell>
														<TableCell>
															<Badge variant="outline">{store.peakDay}</Badge>
														</TableCell>
														<TableCell className="text-right">
															<Button
																variant="ghost"
																size="sm"
																onClick={() => handleStoreClick(store.storeName)}
															>
																View Details
																<ChevronRight className="h-4 w-4 ml-1" />
															</Button>
														</TableCell>
													</TableRow>
												))}
											</TableBody>
										</Table>
									</div>
								</div>
							)}

							{selectedHour !== null && filteredStoresByHour.length === 0 && (
								<div className="text-center py-8 text-gray-500">
									No stores peak at {selectedHourLabel ?? `${selectedHour}:00`}
								</div>
							)}

							<div className="space-y-4">
								<div className="flex items-center gap-2">
									<Clock className="h-4 w-4 text-gray-600" />
									<h3 className="font-semibold">Top 3 Most Common Peak Hours</h3>
								</div>
								<div className="grid grid-cols-1 md:grid-cols-3 gap-4">
									{[...timeOfDayData]
										.filter((item) => item.storesPeaking > 0)
										.sort((a, b) => b.storesPeaking - a.storesPeaking)
										.slice(0, 3)
										.map((item) => {
											const totalStores = storeList.length
											const sharePct = totalStores > 0 ? (item.storesPeaking / totalStores) * 100 : 0
											return (
												<Card
													key={item.hour}
													className={`cursor-pointer transition-all hover:shadow-md ${
														selectedHour === item.hour ? 'ring-2 ring-blue-500' : ''
													}`}
													onClick={() => handleHourClick(item.hour)}
												>
													<CardHeader className="pb-2">
														<CardTitle className="text-sm font-medium flex items-center gap-2">
															<Clock className="h-4 w-4" />
															{item.label}
														</CardTitle>
													</CardHeader>
													<CardContent>
														<div className="text-2xl font-bold">
															{item.storesPeaking} {item.storesPeaking === 1 ? 'store' : 'stores'}
														</div>
														<p className="text-xs text-gray-500">
															{sharePct.toFixed(1)}% of stores peak here
														</p>
													</CardContent>
												</Card>
											)
										})}
								</div>
							</div>
						</TabsContent>

						<TabsContent value="incident-types" className="space-y-6 mt-6">
							<div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
								<div className="h-[400px] w-full">
									<ResponsiveContainer width="100%" height="100%">
										<PieChart>
											<Pie
												data={incidentTypeData}
												cx="50%"
												cy="50%"
												labelLine={false}
												label={({ name, percentage }) =>
													percentage > 5 ? `${name}: ${percentage.toFixed(1)}%` : ''
												}
												outerRadius={120}
												fill="#8884d8"
												dataKey="count"
											>
												{incidentTypeData.map((entry, index) => (
													<Cell key={`cell-${index}`} fill={entry.fill} />
												))}
											</Pie>
											<Tooltip
												contentStyle={{
													backgroundColor: '#fff',
													border: '1px solid #e5e7eb',
													borderRadius: '0.5rem',
												}}
											/>
										</PieChart>
									</ResponsiveContainer>
								</div>
								<div className="space-y-2">
									<h3 className="font-semibold mb-4">Incident Type Breakdown</h3>
									{[...incidentTypeData]
										.sort((a, b) => b.count - a.count)
										.map((item, index) => (
											<Card key={item.type}>
												<CardContent className="p-4">
													<div className="flex items-center justify-between">
														<div className="flex items-center gap-3">
															<div
																className="w-4 h-4 rounded"
																style={{ backgroundColor: item.fill }}
															/>
															<span className="font-medium">{item.type}</span>
														</div>
														<div className="text-right">
															<div className="font-semibold">{item.count}</div>
															<div className="text-xs text-gray-500">
																{item.percentage.toFixed(1)}%
															</div>
														</div>
													</div>
												</CardContent>
											</Card>
										))}
								</div>
							</div>
						</TabsContent>
					</Tabs>
				) : (
					<div className="space-y-4">
						<div className="flex items-center gap-2">
							<Button
								variant="ghost"
								size="sm"
								onClick={() => setSelectedStore(null)}
							>
								<ArrowLeft className="h-4 w-4 mr-2" />
								Back to Overview
							</Button>
						</div>
						{selectedStoreData && (
							<>
								<Card>
									<CardHeader>
										<CardTitle>{selectedStoreData.storeName}</CardTitle>
										<CardDescription>
											{'filterContext' in selectedStoreData && selectedStoreData.filterContext
												? `Incidents ${selectedStoreData.filterContext} (${selectedStoreData.incidents} in selection)`
												: 'Detailed incident breakdown for this store'}
										</CardDescription>
									</CardHeader>
									<CardContent>
										<div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
											<Card>
												<CardContent className="p-4">
													<div className="text-sm text-gray-500">
														{'filterContext' in selectedStoreData && selectedStoreData.filterContext
															? 'Incidents in selection'
															: 'Total Incidents'}
													</div>
													<div className="text-2xl font-bold">
														{selectedStoreData.incidents}
													</div>
												</CardContent>
											</Card>
											<Card>
												<CardContent className="p-4">
													<div className="text-sm text-gray-500">Peak Day</div>
													<div className="text-2xl font-bold">
														{selectedStoreData.peakDay}
													</div>
												</CardContent>
											</Card>
											<Card>
												<CardContent className="p-4">
													<div className="text-sm text-gray-500">Peak Hour</div>
													<div className="text-2xl font-bold">
														{selectedStoreData.peakHour}:00
													</div>
												</CardContent>
											</Card>
										</div>
										<div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
											<Card>
												<CardContent className="p-4">
													<div className="text-sm text-gray-500">Value Stolen</div>
													<div className="text-2xl font-bold">
														£{selectedStoreData.totalStolenValue.toFixed(0)}
													</div>
												</CardContent>
											</Card>
											<Card>
												<CardContent className="p-4">
													<div className="text-sm text-gray-500">Value Saved</div>
													<div className="text-2xl font-bold text-emerald-600 dark:text-emerald-300">
														£{selectedStoreData.totalRecoveredValue.toFixed(0)}
													</div>
												</CardContent>
											</Card>
											<Card>
												<CardContent className="p-4">
													<div className="text-sm text-gray-500">Value Lost</div>
													<div className="text-2xl font-bold text-rose-600 dark:text-rose-300">
														£{selectedStoreData.totalLostValue.toFixed(0)}
													</div>
													<div className="text-xs text-muted-foreground mt-1">
														Recovery rate {selectedStoreData.recoveryRate.toFixed(1)}%
													</div>
												</CardContent>
											</Card>
										</div>
										<div>
											<h3 className="font-semibold mb-4">Incident Types</h3>
											<div className="border rounded-lg">
												<Table>
													<TableHeader>
														<TableRow>
															<TableHead>Type</TableHead>
															<TableHead>Count</TableHead>
															<TableHead>Percentage</TableHead>
															<TableHead className="text-right">Total Value</TableHead>
														</TableRow>
													</TableHeader>
													<TableBody>
														{selectedStoreData.incidentTypes.map((type) => (
															<TableRow key={type.type}>
																<TableCell className="font-medium">{type.type}</TableCell>
																<TableCell>{type.count}</TableCell>
																<TableCell>
																	{type.percentage.toFixed(1)}%
																</TableCell>
																<TableCell className="text-right">
																	£{type.totalValue.toFixed(2)}
																</TableCell>
															</TableRow>
														))}
													</TableBody>
												</Table>
											</div>
										</div>
									</CardContent>
								</Card>
							</>
						)}
					</div>
				)}
			</CardContent>
		</Card>
	)
}

