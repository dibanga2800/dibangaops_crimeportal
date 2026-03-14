import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
	ArrowLeft,
	RefreshCcw,
	Target,
	TrendingUp,
	AlertTriangle,
	Activity,
	BarChart3,
	Clock,
	MapPin,
	Package,
	Shield,
	ChevronLeft,
	ChevronRight,
	SlidersHorizontal,
} from 'lucide-react'
import { format } from 'date-fns'
import {
	BarChart,
	Bar,
	XAxis,
	YAxis,
	CartesianGrid,
	Tooltip,
	ResponsiveContainer,
	PieChart,
	Pie,
	Cell,
	Legend,
} from 'recharts'

import { useAuth } from '@/contexts/AuthContext'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { useAvailableCustomers, findCustomerById } from '@/hooks/useAvailableCustomers'
import { siteService } from '@/services/siteService'
import { incidentGraphService, type RegionOption } from '@/services/incidentGraphService'
import { incidentsApi } from '@/services/api/incidents'
import type { Incident } from '@/types/incidents'
import type { Site } from '@/types/customer'
import type {
	CrimeInsightListItem,
	CrimeInsightTimeBucket,
	CrimeIntelligenceResponse,
	CrimeInsightMetric,
} from '@/types/crimeIntelligence'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { DatePicker } from '@/components/ui/date-picker'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/components/ui/use-toast'

// ============================================================================
// Constants
// ============================================================================

const CHART_PALETTE = ['#6366f1', '#f97316', '#22c55e', '#0ea5e9', '#ec4899', '#facc15']

const CHART_GRADIENTS = [
	{ id: 'grad0', start: '#6366f1', end: '#8b5cf6' },
	{ id: 'grad1', start: '#f97316', end: '#fb923c' },
	{ id: 'grad2', start: '#22c55e', end: '#4ade80' },
	{ id: 'grad3', start: '#0ea5e9', end: '#38bdf8' },
	{ id: 'grad4', start: '#ec4899', end: '#f472b6' },
	{ id: 'grad5', start: '#facc15', end: '#fde047' },
]

const HERO_ICONS = [Activity, BarChart3, Shield, Target]

const DEFAULT_RANGE_DAYS = 90

// ============================================================================
// Utility helpers
// ============================================================================

const toIsoDate = (date?: Date | null): string | undefined =>
	date ? date.toISOString().split('T')[0] : undefined

const formatCurrency = (value: number): string => {
	if (value === 0) return '£0'
	if (value < 1000) return `£${value.toLocaleString()}`
	if (value < 1_000_000) return `£${(value / 1000).toFixed(1)}K`
	return `£${(value / 1_000_000).toFixed(2)}M`
}

const getIncidentValue = (inc: Incident): number => {
	const raw =
		inc.totalValueRecovered ??
		(inc as any).TotalValueRecovered ??
		inc.value ??
		(inc as any).Value ??
		inc.valueRecovered ??
		(inc as any).ValueRecovered ??
		0
	return typeof raw === 'number' ? raw : parseFloat(raw) || 0
}

// ============================================================================
// Data processing
// ============================================================================

const processIncidents = (incidents: Incident[]): CrimeIntelligenceResponse => {
	const total = incidents.length
	const totalValue = incidents.reduce((sum, inc) => sum + getIncidentValue(inc), 0)
	const distinctStores = new Set(incidents.map(i => i.siteName).filter(Boolean)).size

	const countBy = <T extends string | undefined>(
		key: (inc: Incident) => T,
		fallback: string
	): Record<string, { count: number; value: number }> =>
		incidents.reduce(
			(acc, inc) => {
				const k = key(inc) || fallback
				if (!acc[k]) acc[k] = { count: 0, value: 0 }
				acc[k].count++
				acc[k].value += getIncidentValue(inc)
				return acc
			},
			{} as Record<string, { count: number; value: number }>
		)

	const toListItems = (
		map: Record<string, { count: number; value: number }>,
		limit: number,
		denominator = total
	): CrimeInsightListItem[] =>
		Object.entries(map)
			.map(([name, d]) => ({
				name,
				count: d.count,
				value: d.value,
				percentage: denominator > 0 ? Math.round((d.count / denominator) * 1000) / 10 : 0,
			}))
			.sort((a, b) => b.count - a.count)
			.slice(0, limit)

	const topIncidentTypes = toListItems(countBy(i => i.incidentType as any, 'Unspecified'), 6)
	const topStores = toListItems(countBy(i => i.siteName as any, 'Unassigned Site'), 20)
	const topRegions = toListItems(countBy(i => i.regionName as any, 'Unassigned Region'), 20)

	// Products — grouped by product name across stolen items
	const productMap = new Map<string, { count: number; value: number }>()
	incidents.forEach(inc => {
		(inc.stolenItems ?? []).forEach(item => {
			const name = item.productName || item.category || 'Unspecified Product'
			const itemValue =
				(item as any).totalAmount ?? (item as any).TotalAmount ?? (item as any).value ?? 0
			const prev = productMap.get(name) ?? { count: 0, value: 0 }
			productMap.set(name, {
				count: prev.count + (item.quantity || 1),
				value: prev.value + (typeof itemValue === 'number' ? itemValue : parseFloat(itemValue) || 0),
			})
		})
	})
	const totalItems = Array.from(productMap.values()).reduce((s, p) => s + p.count, 0)
	const topProducts = Array.from(productMap.entries())
		.map(([name, d]) => ({
			name,
			count: d.count,
			value: d.value,
			percentage: totalItems > 0 ? Math.round((d.count / totalItems) * 1000) / 10 : 0,
		}))
		.sort((a, b) => b.count - a.count)
		.slice(0, 10)

	// Time buckets
	const timeBuckets: CrimeInsightTimeBucket[] = [
		{ bucket: '00:00 – 05:59', count: 0, percentage: 0 },
		{ bucket: '06:00 – 11:59', count: 0, percentage: 0 },
		{ bucket: '12:00 – 17:59', count: 0, percentage: 0 },
		{ bucket: '18:00 – 23:59', count: 0, percentage: 0 },
	]
	incidents.forEach(inc => {
		if (!inc.timeOfIncident) return
		const h = parseInt(inc.timeOfIncident.split(':')[0], 10)
		if (h < 6) timeBuckets[0].count++
		else if (h < 12) timeBuckets[1].count++
		else if (h < 18) timeBuckets[2].count++
		else timeBuckets[3].count++
	})
	timeBuckets.forEach(b => {
		b.percentage = total > 0 ? Math.round((b.count / total) * 1000) / 10 : 0
	})

	// Hero metrics
	const heroMetrics: CrimeInsightMetric[] = [
		{
			title: 'Total Incidents',
			value: total.toLocaleString(),
			subtext: distinctStores > 0 ? `${(total / distinctStores).toFixed(1)} per store avg` : 'No store data',
			trendIsPositive: false,
		},
		{
			title: 'Value Impact',
			value: formatCurrency(totalValue),
			subtext: 'Recovered / estimated loss',
			trendIsPositive: totalValue <= 0,
		},
	]
	if (topIncidentTypes[0]) {
		const t = topIncidentTypes[0]
		heroMetrics.push({
			title: 'Top Incident Type',
			value: t.name,
			subtext: `${t.count.toLocaleString()} reports · ${t.percentage.toFixed(1)}%`,
			trendIsPositive: false,
		})
	}
	if (topStores[0]) {
		const s = topStores[0]
		heroMetrics.push({
			title: 'Hot Store',
			value: s.name,
			subtext: `${s.count.toLocaleString()} incidents · ${s.percentage.toFixed(1)}%`,
			trendIsPositive: false,
		})
	}

	return {
		success: true,
		heroMetrics,
		topIncidentTypes,
		topStores,
		topProducts,
		topRegions,
		timeBuckets,
		generatedAt: new Date().toISOString(),
	}
}

const buildAnalystNotes = (d: CrimeIntelligenceResponse): string[] => {
	const notes: string[] = []
	if (d.topStores?.[0]) {
		const s = d.topStores[0]
		notes.push(`${s.name} accounts for ${s.percentage.toFixed(1)}% of all incidents in this period.`)
	}
	if (d.topProducts?.[0]) {
		const p = d.topProducts[0]
		notes.push(`"${p.name}" is the most frequently stolen item — ${p.count} units recorded.`)
	}
	if (d.timeBuckets?.length) {
		const peak = [...d.timeBuckets].sort((a, b) => b.count - a.count)[0]
		if (peak?.count > 0)
			notes.push(`Peak activity during ${peak.bucket.toLowerCase()} (${peak.percentage.toFixed(1)}% of cases).`)
	}
	if (!notes.length) notes.push('No significant trends detected for the selected filters.')
	return notes
}

// ============================================================================
// Sub-components
// ============================================================================

interface ChartCardProps {
	title: string
	icon: React.ElementType
	iconColor: string
	children: React.ReactNode
}

const ChartCard = ({ title, icon: Icon, iconColor, children }: ChartCardProps) => (
	<Card className="border border-slate-200 shadow-sm bg-white overflow-hidden">
		<CardHeader className="px-5 py-4 border-b border-slate-100">
			<CardTitle className="text-sm font-semibold text-slate-700 flex items-center gap-2">
				<Icon className={`h-4 w-4 flex-shrink-0 ${iconColor}`} />
				{title}
			</CardTitle>
		</CardHeader>
		{children}
	</Card>
)

interface HeroMetricCardProps {
	metric: CrimeInsightMetric
	index: number
}

const HERO_ACCENT = [
	{ bg: 'bg-indigo-50', icon: 'bg-indigo-100 text-indigo-600', text: 'text-indigo-700' },
	{ bg: 'bg-emerald-50', icon: 'bg-emerald-100 text-emerald-600', text: 'text-emerald-700' },
	{ bg: 'bg-orange-50', icon: 'bg-orange-100 text-orange-600', text: 'text-orange-700' },
	{ bg: 'bg-sky-50', icon: 'bg-sky-100 text-sky-600', text: 'text-sky-700' },
]

const HeroMetricCard = ({ metric, index }: HeroMetricCardProps) => {
	const Icon = HERO_ICONS[index % HERO_ICONS.length]
	const accent = HERO_ACCENT[index % HERO_ACCENT.length]
	return (
		<Card className="border border-slate-200 shadow-sm bg-white">
			<CardContent className="p-5">
				<div className={`inline-flex p-2 rounded-lg ${accent.icon} mb-3`}>
					<Icon className="h-4 w-4" />
				</div>
				<p className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-1">{metric.title}</p>
				<p className={`text-2xl font-bold ${accent.text} leading-tight truncate mb-1`}>{metric.value}</p>
				{metric.subtext && <p className="text-xs text-slate-400">{metric.subtext}</p>}
				{metric.trend && (
					<div className="flex items-center gap-1 mt-2">
						<TrendingUp
							className={`h-3 w-3 ${metric.trendIsPositive ? 'text-emerald-500' : 'text-rose-500 rotate-180'}`}
						/>
						<span className={`text-xs font-medium ${metric.trendIsPositive ? 'text-emerald-600' : 'text-rose-600'}`}>
							{metric.trend}
						</span>
					</div>
				)}
			</CardContent>
		</Card>
	)
}

interface PaginatedBarChartProps {
	title: string
	icon: React.ElementType
	iconColor: string
	data: CrimeInsightListItem[]
	emptyLabel: string
	gradientIndex?: number
	loading: boolean
}

const PaginatedBarChart = ({
	title,
	icon,
	iconColor,
	data,
	emptyLabel,
	gradientIndex = 0,
	loading,
}: PaginatedBarChartProps) => {
	const [page, setPage] = useState(1)
	const itemsPerPage = 6
	const totalPages = Math.ceil(data.length / itemsPerPage)
	const start = (page - 1) * itemsPerPage
	const pageData = data.slice(start, start + itemsPerPage)
	const grad = CHART_GRADIENTS[gradientIndex % CHART_GRADIENTS.length]
	const gradId = `pgBarGrad-${gradientIndex}`

	return (
		<ChartCard title={title} icon={icon} iconColor={iconColor}>
			<CardContent className="p-5">
				{loading ? (
					<Skeleton className="h-64 w-full rounded-lg" />
				) : pageData.length ? (
					<>
						<div className="h-64">
							<ResponsiveContainer width="100%" height="100%">
								<BarChart data={pageData} margin={{ top: 8, right: 8, left: -8, bottom: 56 }}>
									<defs>
										<linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
											<stop offset="0%" stopColor={grad.start} stopOpacity={0.85} />
											<stop offset="100%" stopColor={grad.end} stopOpacity={0.55} />
										</linearGradient>
									</defs>
									<CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
									<XAxis
										dataKey="name"
										tick={{ fontSize: 10, fill: '#94a3b8', fontWeight: 500 }}
										axisLine={false}
										tickLine={false}
										angle={-40}
										textAnchor="end"
										height={56}
										interval={0}
									/>
									<YAxis
										tick={{ fontSize: 10, fill: '#94a3b8' }}
										axisLine={false}
										tickLine={false}
										width={32}
									/>
									<Tooltip
										contentStyle={{
											backgroundColor: '#fff',
											border: '1px solid #e2e8f0',
											borderRadius: '8px',
											boxShadow: '0 4px 12px rgba(0,0,0,.08)',
											fontSize: '12px',
											padding: '8px 12px',
										}}
										cursor={{ fill: 'rgba(99,102,241,.06)' }}
									/>
									<Bar dataKey="count" fill={`url(#${gradId})`} radius={[4, 4, 0, 0]} />
								</BarChart>
							</ResponsiveContainer>
						</div>
						{data.length > itemsPerPage && (
							<div className="flex items-center justify-between pt-4 mt-2 border-t border-slate-100">
								<Button
									variant="outline"
									size="sm"
									onClick={() => setPage(p => Math.max(1, p - 1))}
									disabled={page === 1}
									className="h-8 px-3 text-xs gap-1"
								>
									<ChevronLeft className="h-3 w-3" />
									Prev
								</Button>
								<span className="text-xs text-slate-400">
									{start + 1}–{Math.min(start + itemsPerPage, data.length)} of {data.length}
								</span>
								<Button
									variant="outline"
									size="sm"
									onClick={() => setPage(p => Math.min(totalPages, p + 1))}
									disabled={page >= totalPages}
									className="h-8 px-3 text-xs gap-1"
								>
									Next
									<ChevronRight className="h-3 w-3" />
								</Button>
							</div>
						)}
					</>
				) : (
					<div className="h-64 flex items-center justify-center">
						<p className="text-sm text-slate-400">{emptyLabel}</p>
					</div>
				)}
			</CardContent>
		</ChartCard>
	)
}

interface IncidentPieChartProps {
	data: CrimeInsightListItem[]
	loading: boolean
}

const IncidentPieChart = ({ data, loading }: IncidentPieChartProps) => (
	<ChartCard title="Incident Mix" icon={Target} iconColor="text-indigo-500">
		<CardContent className="p-5">
			{loading ? (
				<Skeleton className="h-64 w-full rounded-lg" />
			) : data.length ? (
				<div className="grid gap-4 sm:grid-cols-2 items-center">
					<div className="h-56">
						<ResponsiveContainer width="100%" height="100%">
							<PieChart>
								<defs>
									{CHART_GRADIENTS.map(g => (
										<linearGradient key={g.id} id={`pie-${g.id}`} x1="0" y1="0" x2="1" y2="1">
											<stop offset="0%" stopColor={g.start} stopOpacity={0.9} />
											<stop offset="100%" stopColor={g.end} stopOpacity={0.7} />
										</linearGradient>
									))}
								</defs>
								<Pie
									data={data}
									dataKey="count"
									nameKey="name"
									innerRadius={46}
									outerRadius={78}
									paddingAngle={4}
									stroke="white"
									strokeWidth={2}
								>
									{data.map((_, i) => (
										<Cell
											key={`cell-${i}`}
											fill={`url(#pie-${CHART_GRADIENTS[i % CHART_GRADIENTS.length].id})`}
										/>
									))}
								</Pie>
								<Tooltip
									contentStyle={{
										backgroundColor: '#fff',
										border: '1px solid #e2e8f0',
										borderRadius: '8px',
										boxShadow: '0 4px 12px rgba(0,0,0,.08)',
										fontSize: '12px',
										padding: '8px 12px',
									}}
								/>
								<Legend
									wrapperStyle={{ fontSize: '10px' }}
									iconType="circle"
									layout="horizontal"
									verticalAlign="bottom"
								/>
							</PieChart>
						</ResponsiveContainer>
					</div>
					<ul className="space-y-1.5">
						{data.map((item, i) => (
							<li
								key={item.name}
								className="flex items-center justify-between py-1.5 px-2 rounded-md hover:bg-slate-50 transition-colors"
							>
								<span className="flex items-center gap-2 min-w-0">
									<span
										className="h-2.5 w-2.5 rounded-full flex-shrink-0"
										style={{ backgroundColor: CHART_PALETTE[i % CHART_PALETTE.length] }}
									/>
									<span className="text-xs text-slate-700 truncate">{item.name}</span>
								</span>
								<span className="text-xs font-semibold text-slate-600 ml-3 flex-shrink-0">
									{item.count.toLocaleString()}
									<span className="text-slate-400 font-normal ml-1">({item.percentage.toFixed(0)}%)</span>
								</span>
							</li>
						))}
					</ul>
				</div>
			) : (
				<div className="h-64 flex items-center justify-center">
					<p className="text-sm text-slate-400">No incident type distribution available.</p>
				</div>
			)}
		</CardContent>
	</ChartCard>
)

interface TimeChartProps {
	data: CrimeInsightTimeBucket[]
	loading: boolean
}

const TimeChart = ({ data, loading }: TimeChartProps) => (
	<ChartCard title="Time-of-Day Activity" icon={Clock} iconColor="text-emerald-500">
		<CardContent className="p-5">
			{loading ? (
				<Skeleton className="h-64 w-full rounded-lg" />
			) : data.length ? (
				<div className="h-64">
					<ResponsiveContainer width="100%" height="100%">
						<BarChart data={data} margin={{ top: 8, right: 8, left: -8, bottom: 40 }}>
							<defs>
								<linearGradient id="timeGrad" x1="0" y1="0" x2="0" y2="1">
									<stop offset="0%" stopColor="#22c55e" stopOpacity={0.85} />
									<stop offset="100%" stopColor="#4ade80" stopOpacity={0.55} />
								</linearGradient>
							</defs>
							<CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
							<XAxis
								dataKey="bucket"
								tick={{ fontSize: 10, fill: '#94a3b8', fontWeight: 500 }}
								axisLine={false}
								tickLine={false}
								angle={-25}
								textAnchor="end"
								height={48}
							/>
							<YAxis tick={{ fontSize: 10, fill: '#94a3b8' }} axisLine={false} tickLine={false} width={32} />
							<Tooltip
								contentStyle={{
									backgroundColor: '#fff',
									border: '1px solid #e2e8f0',
									borderRadius: '8px',
									boxShadow: '0 4px 12px rgba(0,0,0,.08)',
									fontSize: '12px',
									padding: '8px 12px',
								}}
								cursor={{ fill: 'rgba(34,197,94,.06)' }}
							/>
							<Bar dataKey="count" fill="url(#timeGrad)" radius={[4, 4, 0, 0]} />
						</BarChart>
					</ResponsiveContainer>
				</div>
			) : (
				<div className="h-64 flex items-center justify-center">
					<p className="text-sm text-slate-400">No time-of-day data available.</p>
				</div>
			)}
		</CardContent>
	</ChartCard>
)

interface ProductsTableProps {
	data: CrimeInsightListItem[]
	loading: boolean
}

const ProductsTable = ({ data, loading }: ProductsTableProps) => (
	<ChartCard title="Most Stolen Products" icon={Package} iconColor="text-orange-500">
		<CardContent className="p-5">
			{loading ? (
				<Skeleton className="h-52 w-full rounded-lg" />
			) : data.length ? (
				<div className="overflow-x-auto">
					<table className="w-full text-sm">
						<thead>
							<tr className="border-b border-slate-200">
								<th className="text-left py-2 px-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
									Product
								</th>
								<th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
									Qty
								</th>
								<th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
									Value
								</th>
								<th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
									Share
								</th>
							</tr>
						</thead>
						<tbody className="divide-y divide-slate-100">
							{data.map((item, i) => (
								<tr key={item.name} className="hover:bg-slate-50 transition-colors">
									<td className="py-3 px-3">
										<div className="flex items-center gap-2.5">
											<span className="flex h-6 w-6 items-center justify-center rounded bg-orange-100 text-orange-700 text-xs font-bold flex-shrink-0">
												{i + 1}
											</span>
											<span className="text-slate-700 font-medium text-xs truncate max-w-[160px]">
												{item.name}
											</span>
										</div>
									</td>
									<td className="py-3 px-3 text-right text-slate-600 font-semibold text-xs">
										{item.count.toLocaleString()}
									</td>
									<td className="py-3 px-3 text-right text-emerald-600 font-semibold text-xs">
										£{(item.value ?? 0).toLocaleString()}
									</td>
									<td className="py-3 px-3 text-right">
										<Badge variant="secondary" className="text-xs font-semibold">
											{item.percentage.toFixed(1)}%
										</Badge>
									</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>
			) : (
				<div className="h-52 flex items-center justify-center">
					<p className="text-sm text-slate-400">No stolen product data available.</p>
				</div>
			)}
		</CardContent>
	</ChartCard>
)

interface HotProductsCardProps {
	data: CrimeInsightListItem[]
	loading: boolean
}

const HotProductsCard = ({ data, loading }: HotProductsCardProps) => {
	const top5 = data.slice(0, 5)
	return (
		<ChartCard title="Top Hot Products" icon={Target} iconColor="text-rose-500">
			<CardContent className="p-5">
				{loading ? (
					<div className="space-y-3">
						{Array.from({ length: 5 }).map((_, i) => (
							<Skeleton key={i} className="h-14 w-full rounded-lg" />
						))}
					</div>
				) : top5.length ? (
					<div className="space-y-2">
						{top5.map((product, i) => (
							<div
								key={product.name}
								className="flex items-center gap-3 p-3 rounded-lg border border-slate-100 hover:border-slate-200 hover:bg-slate-50 transition-all"
							>
								<span className="flex h-7 w-7 items-center justify-center rounded-full bg-rose-100 text-rose-700 text-xs font-bold flex-shrink-0">
									{i + 1}
								</span>
								<div className="flex-1 min-w-0">
									<p className="text-sm font-semibold text-slate-800 truncate">{product.name}</p>
									<div className="flex items-center gap-2 mt-0.5 flex-wrap">
										<span className="text-xs text-slate-500">{product.count.toLocaleString()} units</span>
										<span className="text-xs text-emerald-600 font-medium">
											£{(product.value ?? 0).toLocaleString()}
										</span>
									</div>
								</div>
								<Badge variant="secondary" className="text-xs flex-shrink-0">
									{product.percentage.toFixed(1)}%
								</Badge>
							</div>
						))}
					</div>
				) : (
					<p className="text-sm text-slate-400 text-center py-8">Insufficient product data.</p>
				)}
			</CardContent>
		</ChartCard>
	)
}

interface AnalystNotesCardProps {
	notes: string[]
	loading: boolean
}

const AnalystNotesCard = ({ notes, loading }: AnalystNotesCardProps) => (
	<ChartCard title="Analyst Notes" icon={TrendingUp} iconColor="text-indigo-500">
		<CardContent className="p-5">
			{loading ? (
				<div className="space-y-3">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-5 w-full rounded" />
					))}
				</div>
			) : (
				<ul className="space-y-2">
					{notes.map((note, i) => (
						<li
							key={i}
							className="flex items-start gap-3 p-3 rounded-lg border-l-4 border-indigo-400 bg-indigo-50/40"
						>
							<span className="flex h-5 w-5 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 text-xs font-bold flex-shrink-0 mt-0.5">
								{i + 1}
							</span>
							<p className="text-xs text-slate-600 leading-relaxed">{note}</p>
						</li>
					))}
				</ul>
			)}
		</CardContent>
	</ChartCard>
)

// ============================================================================
// Page Component
// ============================================================================

export default function CustomerCrimeIntelligence() {
	const navigate = useNavigate()
	const [searchParams] = useSearchParams()
	const { toast } = useToast()
	const { user, isLoading: authLoading } = useAuth()
	const { isAdmin, isManager, selectedCustomerId: contextCustomerId, setSelectedCustomerId, assignedCustomers } = useCustomerSelection()
	const { availableCustomers, isLoading: loadingCustomers } = useAvailableCustomers()

	const [customer, setCustomer] = useState<{ id: number; name: string } | null>(null)
	const [sites, setSites] = useState<Site[]>([])
	const [regions, setRegions] = useState<RegionOption[]>([])
	const [selectedSiteId, setSelectedSiteId] = useState<string>('all')
	const [selectedRegionId, setSelectedRegionId] = useState<string>('all')
	const [filtersVersion, setFiltersVersion] = useState(0)
	const [startDate, setStartDate] = useState<Date | undefined>(() => {
		const d = new Date()
		d.setDate(d.getDate() - DEFAULT_RANGE_DAYS)
		return d
	})
	const [endDate, setEndDate] = useState<Date | undefined>(new Date())

	const [insights, setInsights] = useState<CrimeIntelligenceResponse | null>(null)
	const [loadingInsights, setLoadingInsights] = useState(false)
	const [pageError, setPageError] = useState<string | null>(null)
	const [isResolvingCustomer, setIsResolvingCustomer] = useState(true)
	const [selectedCustomerForAdmin, setSelectedCustomerForAdmin] = useState<number | null>(null)

	const urlCustomerId = searchParams.get('customerId')
	const urlSiteId = searchParams.get('siteId')

	const resolvedCustomerId = useMemo(() => {
		if (urlCustomerId) return parseInt(urlCustomerId, 10)
		if (isAdmin && contextCustomerId) return contextCustomerId
		if (isManager && contextCustomerId) return contextCustomerId
		if (user && 'customerId' in user) return (user as any).customerId ?? null
		return null
	}, [urlCustomerId, isAdmin, isManager, contextCustomerId, user])

	useEffect(() => {
		if (!isAdmin && !isManager) return
		if (!resolvedCustomerId) return
		setSelectedCustomerForAdmin(resolvedCustomerId)
	}, [isAdmin, isManager, resolvedCustomerId])

	const loadCustomer = useCallback(async () => {
		if (authLoading) return
		const id = resolvedCustomerId ?? null
		setCustomer(null)
		setPageError(null)
		setIsResolvingCustomer(false)
		if (urlSiteId) setSelectedSiteId(urlSiteId)
		if (!id) return
		try {
			const data = await findCustomerById(id)
			if (data) setCustomer(data)
		} catch {
			setCustomer({ id, name: `Customer ${id}` })
		}
	}, [authLoading, resolvedCustomerId, urlSiteId])

	useEffect(() => {
		loadCustomer()
	}, [loadCustomer])

	const loadSites = useCallback(async () => {
		const id = resolvedCustomerId
		if (id == null) return
		try {
			const res = await siteService.getSitesByCustomer(id)
			if (res.success && res.data?.length) {
				setSites(res.data)
			} else {
				setSites([])
			}
		} catch {
			setSites([])
		}
	}, [resolvedCustomerId])

	const loadRegions = useCallback(async () => {
		const id = resolvedCustomerId
		if (id == null) return
		try {
			const res = await incidentGraphService.fetchRegions(id)
			if (res.success && res.data?.length) {
				setRegions(res.data)
			} else {
				setRegions([])
			}
		} catch {
			setRegions([])
		}
	}, [resolvedCustomerId])

	useEffect(() => {
		if (resolvedCustomerId) {
			loadSites()
			loadRegions()
		}
	}, [resolvedCustomerId, loadSites, loadRegions])

	const fetchInsights = useCallback(async () => {
		const customerId = resolvedCustomerId
		if (customerId == null) return
		setLoadingInsights(true)
		setPageError(null)
		try {
			const res = await incidentsApi.getIncidents({
				page: 1,
				pageSize: 250,
				customerId: customerId.toString(),
				fromDate: toIsoDate(startDate),
				toDate: toIsoDate(endDate),
				siteId: selectedSiteId !== 'all' ? selectedSiteId : undefined,
				regionId: selectedRegionId !== 'all' ? selectedRegionId : undefined,
			})

			if (res.success === false) throw new Error(res.message || 'Failed to fetch incidents')

			const raw = res as any
			let incidents: Incident[] = Array.isArray(raw.data)
				? raw.data
				: Array.isArray(raw)
					? raw
					: Array.isArray(raw.data?.data)
						? raw.data.data
						: []

			// Client-side date guard (belt-and-suspenders)
			if (startDate && endDate && incidents.length) {
				const s = startDate.getTime()
				const e = endDate.getTime()
				incidents = incidents.filter(i => {
					const t = new Date(i.dateOfIncident).getTime()
					return t >= s && t <= e
				})
			}

			setInsights(processIncidents(incidents))
		} catch (err) {
			const msg = err instanceof Error ? err.message : 'Unable to load insights'
			setPageError(msg)
			setInsights(processIncidents([]))
			toast({ variant: 'destructive', title: 'Unable to load data', description: msg })
		} finally {
			setLoadingInsights(false)
		}
	}, [resolvedCustomerId, selectedSiteId, selectedRegionId, startDate, endDate, toast])

	useEffect(() => {
		if (!customer && resolvedCustomerId) {
			setCustomer({ id: resolvedCustomerId, name: `Customer ${resolvedCustomerId}` })
			setIsResolvingCustomer(false)
		}
	}, [customer, resolvedCustomerId])

	useEffect(() => {
		if (resolvedCustomerId) fetchInsights()
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [resolvedCustomerId, filtersVersion])

	const analystNotes = useMemo(() => (insights ? buildAnalystNotes(insights) : []), [insights])

	// ──────────────────────────────────────────────────────────────────────────
	// Loading / Error states
	// ──────────────────────────────────────────────────────────────────────────

	if (isResolvingCustomer || authLoading) {
		return (
			<div className="flex items-center justify-center min-h-screen bg-slate-50">
				<div className="space-y-3 text-center">
					<div className="animate-spin rounded-full h-9 w-9 border-2 border-indigo-600 border-t-transparent mx-auto" />
					<p className="text-sm text-slate-500">Loading customer context…</p>
				</div>
			</div>
		)
	}

	if (pageError && !isAdmin && !insights) {
		return (
			<div className="container mx-auto p-6 space-y-4 max-w-2xl">
				<Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
					<ArrowLeft className="h-4 w-4 mr-1.5" />
					Back
				</Button>
				<Card className="border-rose-200 bg-rose-50">
					<CardContent className="flex items-center gap-3 py-5">
						<AlertTriangle className="h-5 w-5 text-rose-500 flex-shrink-0" />
						<p className="text-rose-700 text-sm font-medium">{pageError}</p>
					</CardContent>
				</Card>
			</div>
		)
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Main render
	// ──────────────────────────────────────────────────────────────────────────

	return (
		<div className="min-h-screen bg-slate-50 overflow-x-hidden">
			<div className="container mx-auto px-4 sm:px-6 py-6 space-y-6 max-w-screen-xl">

				{/* ── Page Header ── */}
				<div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
					<div className="space-y-1 min-w-0">
						<div className="flex items-center gap-2 flex-wrap">
							<Button
								variant="ghost"
								size="sm"
								onClick={() => navigate(-1)}
								className="h-8 px-2 hover:bg-slate-100 -ml-2"
							>
								<ArrowLeft className="h-4 w-4 mr-1" />
								Back
							</Button>
							<Badge className="bg-indigo-600 text-white border-0 text-xs">Customer Insight</Badge>
						</div>
						<h1 className="text-2xl sm:text-3xl font-bold text-slate-900 leading-tight">
							{customer?.name} — Crime Intelligence
						</h1>
						<p className="text-sm text-slate-500">
							Live incident telemetry across stores, products, and time-of-day patterns.
						</p>
						{(isAdmin || (isManager && assignedCustomers.length > 1)) && (
							<div className="mt-3 max-w-xs">
								<p className="text-xs font-medium text-slate-500 mb-1">Customer</p>
								<Select
									disabled={isAdmin && (loadingCustomers || availableCustomers.length === 0)}
									value={selectedCustomerForAdmin?.toString() ?? ''}
									onValueChange={value => {
										const id = parseInt(value, 10)
										setSelectedCustomerForAdmin(id)
										setSelectedCustomerId(id)
										const params = new URLSearchParams(searchParams)
										params.set('customerId', value)
										navigate({ search: params.toString() }, { replace: true })
										setFiltersVersion(v => v + 1)
									}}
								>
									<SelectTrigger className="h-9 text-sm" aria-label="Select customer">
										<SelectValue placeholder={isAdmin && loadingCustomers ? 'Loading customers…' : 'Select customer'} />
									</SelectTrigger>
									<SelectContent>
										{isAdmin
											? availableCustomers.map(c => (
												<SelectItem key={c.id} value={c.id.toString()}>
													{c.name}
												</SelectItem>
											  ))
											: assignedCustomers.map(c => (
												<SelectItem key={c.id} value={c.id.toString()}>
													{c.name}
												</SelectItem>
											  ))}
									</SelectContent>
								</Select>
							</div>
						)}
					</div>
					<Button
						variant="outline"
						size="sm"
						onClick={fetchInsights}
						disabled={loadingInsights}
						className="self-start sm:self-center flex-shrink-0 h-9"
					>
						<RefreshCcw className={`h-3.5 w-3.5 mr-2 ${loadingInsights ? 'animate-spin' : ''}`} />
						Refresh
					</Button>
				</div>

				{/* ── Filter Bar ── */}
				<Card className="border border-slate-200 shadow-sm bg-white">
					<CardHeader className="px-5 py-3.5 border-b border-slate-100">
						<CardTitle className="text-sm font-semibold text-slate-600 flex items-center gap-2">
							<SlidersHorizontal className="h-3.5 w-3.5 text-slate-400" />
							Filters
						</CardTitle>
					</CardHeader>
					<CardContent className="px-5 py-4 grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
						<div className="space-y-1.5">
							<p className="text-xs font-medium text-slate-500">Start Date</p>
							<DatePicker date={startDate} setDate={setStartDate} />
						</div>
						<div className="space-y-1.5">
							<p className="text-xs font-medium text-slate-500">End Date</p>
							<DatePicker date={endDate} setDate={setEndDate} />
						</div>
						<div className="space-y-1.5">
							<p className="text-xs font-medium text-slate-500">Region</p>
							<Select
								value={selectedRegionId}
								onValueChange={value => {
									setSelectedRegionId(value)
									setFiltersVersion(v => v + 1)
								}}
							>
								<SelectTrigger className="h-9 text-sm">
									<SelectValue placeholder="All regions" />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value="all">All Regions</SelectItem>
									{regions.map(r => (
										<SelectItem key={r.id} value={r.id}>
											{r.name}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
						<div className="space-y-1.5">
							<p className="text-xs font-medium text-slate-500">Site</p>
							<Select
								value={selectedSiteId}
								onValueChange={value => {
									setSelectedSiteId(value)
									setFiltersVersion(v => v + 1)
								}}
							>
								<SelectTrigger className="h-9 text-sm">
									<SelectValue placeholder="All sites" />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value="all">All Sites</SelectItem>
									{sites.map(s => (
										<SelectItem key={s.siteID} value={s.siteID?.toString() ?? ''}>
											{s.locationName}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
					</CardContent>
				</Card>

				{/* ── Hero Metrics ── */}
				{loadingInsights && !insights ? (
					<div className="grid gap-4 grid-cols-1 sm:grid-cols-2 xl:grid-cols-4">
						{Array.from({ length: 4 }).map((_, i) => (
							<Card key={i} className="border border-slate-200 shadow-sm bg-white p-5">
								<Skeleton className="h-8 w-8 rounded-lg mb-3" />
								<Skeleton className="h-3 w-20 mb-2" />
								<Skeleton className="h-7 w-28" />
							</Card>
						))}
					</div>
				) : (
					<div className="grid gap-4 grid-cols-1 sm:grid-cols-2 xl:grid-cols-4">
						{(insights?.heroMetrics ?? []).map((m, i) => (
							<HeroMetricCard key={m.title} metric={m} index={i} />
						))}
					</div>
				)}

				{/* ── Charts Row 1: Hot Stores + Incident Mix ── */}
				<div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
					<PaginatedBarChart
						title="Hot Stores"
						icon={MapPin}
						iconColor="text-indigo-500"
						data={insights?.topStores ?? []}
						emptyLabel="No store-level crime data available."
						gradientIndex={0}
						loading={loadingInsights}
					/>
					<IncidentPieChart data={insights?.topIncidentTypes ?? []} loading={loadingInsights} />
				</div>

				{/* ── Charts Row 2: Time of Day + Regional Exposure ── */}
				<div className="grid gap-6 grid-cols-1 lg:grid-cols-2">
					<TimeChart data={insights?.timeBuckets ?? []} loading={loadingInsights} />
					<PaginatedBarChart
						title="Regional Exposure"
						icon={BarChart3}
						iconColor="text-sky-500"
						data={insights?.topRegions ?? []}
						emptyLabel="No regional breakdown available."
						gradientIndex={3}
						loading={loadingInsights}
					/>
				</div>

				{/* ── Bottom Row: Products Table + Hot Products + Analyst Notes ── */}
				<div className="grid gap-6 grid-cols-1 lg:grid-cols-3">
					<div className="lg:col-span-2 space-y-6">
						<ProductsTable data={insights?.topProducts ?? []} loading={loadingInsights} />
						<AnalystNotesCard notes={analystNotes} loading={loadingInsights} />
					</div>
					<HotProductsCard data={insights?.topProducts ?? []} loading={loadingInsights} />
				</div>

				{/* ── Footer timestamp ── */}
				{insights?.generatedAt && (
					<p className="text-xs text-slate-400 text-right">
						Last generated {format(new Date(insights.generatedAt), 'dd MMM yyyy HH:mm')}
					</p>
				)}

			</div>
		</div>
	)
}
