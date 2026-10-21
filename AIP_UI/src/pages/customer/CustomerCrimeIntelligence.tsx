import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
	ArrowLeft,
	RefreshCcw,
	Target,
	TrendingUp,
	AlertTriangle,
	BarChart3,
	Clock,
	MapPin,
	Package,
	ChevronLeft,
	ChevronRight,
	Building2,
} from 'lucide-react'
import { format, startOfYear } from 'date-fns'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts'

import { useAuth } from '@/contexts/AuthContext'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { useAvailableCustomers, findCustomerById } from '@/hooks/useAvailableCustomers'
import { siteService } from '@/services/siteService'
import { incidentGraphService, type RegionOption } from '@/services/incidentGraphService'
import { crimeIntelligenceService } from '@/services/crimeIntelligenceService'
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { PageHeader } from '@/components/layout/PageHeader'
import { FilterBar, DateRangeField, FilterChip } from '@/components/common/FilterBar'
import { StatLedger, type StatLedgerItem } from '@/components/common/StatLedger'
import { EmptyState } from '@/components/common/EmptyState'
import { useChartTheme, chartTooltipStyle, axisTick } from '@/lib/chart-theme'
import { useToast } from '@/components/ui/use-toast'
import { filterByAssignedSiteIds } from '@/utils/siteAccess'

// ============================================================================
// Utility helpers
// ============================================================================

const toIsoDate = (date?: Date | null): string | undefined =>
	date ? date.toISOString().split('T')[0] : undefined

const fromDateInputValue = (value: string): Date | undefined => {
	if (!value) return undefined
	const parsed = new Date(`${value}T00:00:00`)
	return Number.isNaN(parsed.getTime()) ? undefined : parsed
}

const emptyInsights = (): CrimeIntelligenceResponse => ({
	success: true,
	heroMetrics: [],
	topIncidentTypes: [],
	topStores: [],
	topProducts: [],
	topRegions: [],
	timeBuckets: [],
	generatedAt: new Date().toISOString(),
})

const buildAnalystNotes = (d: CrimeIntelligenceResponse): string[] => {
	const notes: string[] = []
	if (d.topStores?.[0]) {
		const s = d.topStores[0]
		notes.push(`${s.name} accounts for ${s.percentage.toFixed(1)}% of all incidents in this period.`)
	}
	if (d.topProducts?.[0]) {
		const p = d.topProducts[0]
		notes.push(`${p.name} is the most frequently stolen item, with ${p.count} units recorded.`)
	}
	if (d.timeBuckets?.length) {
		const peak = [...d.timeBuckets].sort((a, b) => b.count - a.count)[0]
		if (peak?.count > 0)
			notes.push(`Activity peaks in the ${peak.bucket.toLowerCase()} (${peak.percentage.toFixed(1)}% of cases).`)
	}
	if (!notes.length) notes.push('No significant trends for the selected filters.')
	return notes
}

// ============================================================================
// Sub-components
// ============================================================================

interface PanelProps {
	title: string
	description?: string
	icon: React.ElementType
	actions?: React.ReactNode
	children: React.ReactNode
	className?: string
}

const Panel = ({ title, description, icon: Icon, actions, children, className }: PanelProps) => (
	<section className={`rounded-lg border border-border bg-card ${className ?? ''}`}>
		<div className="flex flex-wrap items-start justify-between gap-3 p-4 sm:p-5">
			<div className="min-w-0">
				<h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
					<Icon className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
					{title}
				</h2>
				{description ? <p className="mt-0.5 text-[13px] text-muted-foreground">{description}</p> : null}
			</div>
			{actions}
		</div>
		{children}
	</section>
)

const PanelEmpty = ({ label }: { label: string }) => (
	<p className="px-4 pb-8 pt-4 text-center text-sm text-muted-foreground sm:px-5">{label}</p>
)

const PanelLoading = ({ rows = 5 }: { rows?: number }) => (
	<div className="space-y-3 px-4 pb-5 sm:px-5" aria-busy="true">
		{Array.from({ length: rows }).map((_, i) => (
			<Skeleton key={i} className="h-7 w-full" />
		))}
	</div>
)

interface RankedBarsProps {
	title: string
	description: string
	icon: React.ElementType
	data: CrimeInsightListItem[]
	emptyLabel: string
	loading: boolean
	unit?: string
}

/** A ranked list with proportional bars: easier to read than rotated chart labels. */
const RankedBars = ({ title, description, icon, data, emptyLabel, loading, unit = 'incidents' }: RankedBarsProps) => {
	const [page, setPage] = useState(1)
	const perPage = 8
	const totalPages = Math.max(1, Math.ceil(data.length / perPage))
	const start = (page - 1) * perPage
	const pageData = data.slice(start, start + perPage)
	const max = Math.max(1, ...data.map((d) => d.count))

	return (
		<Panel title={title} description={description} icon={icon}>
			{loading ? (
				<PanelLoading />
			) : !pageData.length ? (
				<PanelEmpty label={emptyLabel} />
			) : (
				<>
					<ol className="space-y-3.5 px-4 pb-5 sm:px-5" start={start + 1}>
						{pageData.map((item, i) => {
							const rank = start + i + 1
							return (
								<li key={item.name}>
									<div className="flex items-baseline justify-between gap-3 text-sm">
										<span className="flex min-w-0 items-baseline gap-2">
											<span className="figures w-4 shrink-0 text-xs text-muted-foreground">{rank}</span>
											<span className="truncate font-medium text-foreground">{item.name}</span>
										</span>
										<span className="figures shrink-0 text-muted-foreground">
											<span className="font-semibold text-foreground">{item.count.toLocaleString('en-GB')}</span> {unit} ·{' '}
											{item.percentage.toFixed(1)}%
										</span>
									</div>
									<div className="ml-6 mt-1.5 h-2 overflow-hidden rounded-full bg-muted">
										<div
											className={`h-full rounded-full ${rank === 1 ? 'bg-primary' : 'bg-primary/55'}`}
											style={{ width: `${(item.count / max) * 100}%` }}
										/>
									</div>
								</li>
							)
						})}
					</ol>
					{data.length > perPage && (
						<div className="flex items-center justify-between border-t border-border px-4 py-2.5 sm:px-5">
							<Button variant="ghost" size="sm" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}>
								<ChevronLeft className="h-4 w-4" />
								Previous
							</Button>
							<span className="figures text-xs text-muted-foreground">
								{start + 1}–{Math.min(start + perPage, data.length)} of {data.length}
							</span>
							<Button variant="ghost" size="sm" onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages}>
								Next
								<ChevronRight className="h-4 w-4" />
							</Button>
						</div>
					)}
				</>
			)}
		</Panel>
	)
}

const IncidentMix = ({ data, loading }: { data: CrimeInsightListItem[]; loading: boolean }) => {
	const theme = useChartTheme()
	const palette = theme.series.length ? theme.series : [theme.primary]
	const total = data.reduce((sum, d) => sum + d.count, 0)
	return (
		<Panel title="Incident mix" description="What kind of incidents are being reported." icon={Target}>
			{loading ? (
				<PanelLoading />
			) : !data.length ? (
				<PanelEmpty label="No incident types recorded for these filters." />
			) : (
				<div className="grid items-center gap-4 px-4 pb-5 sm:grid-cols-[11rem_minmax(0,1fr)] sm:px-5">
					<div className="relative mx-auto h-44 w-44">
						<ResponsiveContainer width="100%" height="100%">
							<PieChart>
								<Pie data={data} dataKey="count" nameKey="name" innerRadius={54} outerRadius={84} paddingAngle={2} stroke={theme.card} strokeWidth={2}>
									{data.map((_, i) => (
										<Cell key={`cell-${i}`} fill={palette[i % palette.length]} />
									))}
								</Pie>
								<Tooltip {...chartTooltipStyle(theme)} />
							</PieChart>
						</ResponsiveContainer>
						<div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
							<span className="figures font-display text-2xl font-semibold leading-none text-foreground">{total.toLocaleString('en-GB')}</span>
							<span className="mt-1 text-[11px] text-muted-foreground">incidents</span>
						</div>
					</div>
					<ul className="divide-y divide-border">
						{data.map((item, i) => (
							<li key={item.name} className="flex items-center justify-between gap-3 py-2">
								<span className="flex min-w-0 items-center gap-2">
									<span className="h-2.5 w-2.5 shrink-0 rounded-sm" style={{ backgroundColor: palette[i % palette.length] }} aria-hidden />
									<span className="truncate text-sm text-foreground">{item.name}</span>
								</span>
								<span className="figures shrink-0 text-sm text-muted-foreground">
									<span className="font-semibold text-foreground">{item.count.toLocaleString('en-GB')}</span> · {item.percentage.toFixed(0)}%
								</span>
							</li>
						))}
					</ul>
				</div>
			)}
		</Panel>
	)
}

const TimeChart = ({ data, loading }: { data: CrimeInsightTimeBucket[]; loading: boolean }) => {
	const theme = useChartTheme()
	const peak = [...data].sort((a, b) => b.count - a.count)[0]
	return (
		<Panel
			title="Time of day"
			description={peak && peak.count > 0 ? `Busiest: ${peak.bucket.toLowerCase()}, ${peak.percentage.toFixed(0)}% of incidents.` : 'When incidents are reported.'}
			icon={Clock}
		>
			{loading ? (
				<PanelLoading />
			) : !data.length ? (
				<PanelEmpty label="No time-of-day data for these filters." />
			) : (
				<div className="h-64 px-2 pb-4 sm:px-4">
					<ResponsiveContainer width="100%" height="100%">
						<BarChart data={data} margin={{ top: 16, right: 8, left: 0, bottom: 0 }}>
							<CartesianGrid vertical={false} stroke={theme.grid} />
							<XAxis
								dataKey="bucket"
								tick={axisTick(theme)}
								axisLine={{ stroke: theme.grid }}
								tickLine={false}
								interval={0}
								tickFormatter={(v: string) => String(v).replace(/:00/g, '')}
							/>
							<YAxis tick={axisTick(theme)} axisLine={false} tickLine={false} width={32} allowDecimals={false} />
							<Tooltip {...chartTooltipStyle(theme)} />
							<Bar dataKey="count" name="Incidents" radius={[3, 3, 0, 0]} maxBarSize={56}>
								{data.map((row) => (
									<Cell key={row.bucket} fill={theme.primary} fillOpacity={row.bucket === peak?.bucket ? 1 : 0.5} />
								))}
							</Bar>
						</BarChart>
					</ResponsiveContainer>
				</div>
			)}
		</Panel>
	)
}

const ProductsTable = ({ data, loading }: { data: CrimeInsightListItem[]; loading: boolean }) => (
	<Panel title="Most stolen products" description="By units taken in the period." icon={Package} className="lg:col-span-2">
		{loading ? (
			<PanelLoading rows={6} />
		) : !data.length ? (
			<PanelEmpty label="No stolen products recorded for these filters." />
		) : (
			<div className="overflow-x-auto border-t border-border">
				<Table>
					<TableHeader>
						<TableRow>
							<TableHead className="w-10">#</TableHead>
							<TableHead>Product</TableHead>
							<TableHead className="text-right">Units</TableHead>
							<TableHead className="hidden text-right sm:table-cell">Value</TableHead>
							<TableHead className="w-[28%]">Share</TableHead>
						</TableRow>
					</TableHeader>
					<TableBody>
						{data.map((item, i) => (
							<TableRow key={item.name}>
								<TableCell className="figures text-muted-foreground">{i + 1}</TableCell>
								<TableCell className="font-medium">{item.name}</TableCell>
								<TableCell className="figures text-right">{item.count.toLocaleString('en-GB')}</TableCell>
								<TableCell className="figures hidden text-right sm:table-cell">£{(item.value ?? 0).toLocaleString('en-GB')}</TableCell>
								<TableCell>
									<div className="flex items-center gap-2">
										<div className="hidden h-1.5 flex-1 overflow-hidden rounded-full bg-muted sm:block">
											<div className="h-full rounded-full bg-primary/70" style={{ width: `${Math.min(100, item.percentage)}%` }} />
										</div>
										<span className="figures w-12 text-right text-xs text-muted-foreground">{item.percentage.toFixed(1)}%</span>
									</div>
								</TableCell>
							</TableRow>
						))}
					</TableBody>
				</Table>
			</div>
		)}
	</Panel>
)

const AnalystNotes = ({ notes, loading, generatedAt }: { notes: string[]; loading: boolean; generatedAt?: string }) => (
	<Panel title="What stands out" icon={TrendingUp} description={generatedAt ? `Generated ${format(new Date(generatedAt), 'd MMM yyyy, HH:mm')}` : undefined}>
		{loading ? (
			<PanelLoading rows={3} />
		) : (
			<ol className="divide-y divide-border px-4 pb-4 sm:px-5">
				{notes.map((note, i) => (
					<li key={i} className="flex gap-3 py-2.5 first:pt-0">
						<span className="figures mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-accent text-[11px] font-semibold text-accent-foreground">
							{i + 1}
						</span>
						<p className="text-sm leading-6 text-foreground">{note}</p>
					</li>
				))}
			</ol>
		)}
	</Panel>
)

const toLedgerItem = (metric: CrimeInsightMetric): StatLedgerItem => {
	if (metric.valueImpact) {
		return {
			label: metric.title,
			value: metric.valueImpact.recovered,
			tone: 'recovered',
			hint: (
				<>
					recovered · <span className="text-loss">{metric.valueImpact.lossLabel ?? 'est. loss'} {metric.valueImpact.estimatedLoss}</span>
				</>
			),
		}
	}
	const text = String(metric.value ?? '')
	const long = text.length > 9 && !/^[£$€]?[\d.,]+[%kKmM]?$/.test(text)
	return {
		label: metric.title,
		value: long ? <span className="block whitespace-normal text-[1.375rem] leading-tight sm:text-[1.625rem]">{text}</span> : metric.value,
		hint: metric.trend ? (
			<span className={metric.trendIsPositive ? 'text-recovered' : 'text-loss'}>
				{metric.trendIsPositive ? '▲' : '▼'} {metric.trend}
			</span>
		) : (
			metric.subtext
		),
	}
}

// ============================================================================
// Page Component
// ============================================================================

export default function CustomerCrimeIntelligence() {
	const navigate = useNavigate()
	const [searchParams] = useSearchParams()
	const { toast } = useToast()
	const { user, isLoading: authLoading } = useAuth()
	const { isAdmin, isManager, selectedCustomerId: contextCustomerId, setSelectedCustomerId, assignedCustomers, selectedSiteId: contextSiteId } = useCustomerSelection()
	const { availableCustomers, isLoading: loadingCustomers } = useAvailableCustomers()

	const [customer, setCustomer] = useState<{ id: number; name: string } | null>(null)
	const [sites, setSites] = useState<Site[]>([])
	const [regions, setRegions] = useState<RegionOption[]>([])
	const [selectedSiteId, setSelectedSiteId] = useState<string>('all')
	const [selectedRegionId, setSelectedRegionId] = useState<string>('all')
	const [filtersVersion, setFiltersVersion] = useState(0)
	const [startDate, setStartDate] = useState<Date | undefined>(() => startOfYear(new Date()))
	const [endDate, setEndDate] = useState<Date | undefined>(new Date())

	const [insights, setInsights] = useState<CrimeIntelligenceResponse | null>(null)
	const [loadingInsights, setLoadingInsights] = useState(false)
	const [pageError, setPageError] = useState<string | null>(null)
	const [isResolvingCustomer, setIsResolvingCustomer] = useState(true)
	const [selectedCustomerForAdmin, setSelectedCustomerForAdmin] = useState<number | null>(null)

	const urlCustomerId = searchParams.get('customerId')
	const urlSiteId = searchParams.get('siteId')

	// Resolve customer ID: URL > admin/manager context > user.customerId/CustomerId > selection context
	const resolvedCustomerId = useMemo(() => {
		if (urlCustomerId) return parseInt(urlCustomerId, 10)
		if (isAdmin && contextCustomerId) return contextCustomerId
		if (isManager && contextCustomerId) return contextCustomerId
		const userCustomerId = (user as any)?.customerId ?? (user as any)?.CustomerId ?? (user as any)?.companyId ?? (user as any)?.CompanyId
		if (userCustomerId != null) return typeof userCustomerId === 'string' ? parseInt(userCustomerId, 10) : userCustomerId
		return contextCustomerId ?? null
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

	const loadRegions = useCallback(async () => {
		const id = resolvedCustomerId
		if (id == null) return
		try {
			const res = await incidentGraphService.fetchRegions(id)
			const regionsList = res.success && res.data?.length ? res.data : []
			setRegions(regionsList)
		} catch {
			setRegions([])
		}
	}, [resolvedCustomerId])

	// Sites depend on region selection: fetch by region when selected, or by customer when "All regions"
	const loadSites = useCallback(async () => {
		const id = resolvedCustomerId
		if (id == null) return

		try {
			const sitesRes =
				selectedRegionId !== 'all'
					? await siteService.getSitesByRegion(parseInt(selectedRegionId, 10))
					: await siteService.getSitesByCustomer(id)
			let sitesList: Site[] = sitesRes.success && sitesRes.data?.length ? sitesRes.data : []
			sitesList = filterByAssignedSiteIds(sitesList, user, (site) =>
				site.siteID ?? (site as any).SiteID ?? (site as any).id ?? null
			)

			setSites(sitesList)
		} catch {
			setSites([])
		}
	}, [resolvedCustomerId, selectedRegionId, user])

	useEffect(() => {
		if (resolvedCustomerId) loadRegions()
	}, [resolvedCustomerId, loadRegions])

	useEffect(() => {
		if (resolvedCustomerId) loadSites()
	}, [resolvedCustomerId, selectedRegionId, loadSites])

	const fetchInsights = useCallback(async () => {
		const customerId = resolvedCustomerId
		if (customerId == null) return
		setLoadingInsights(true)
		setPageError(null)
		try {
			const data = await crimeIntelligenceService.getInsights({
				customerId,
				startDate: toIsoDate(startDate),
				endDate: toIsoDate(endDate),
				siteId: selectedSiteId !== 'all' ? selectedSiteId : undefined,
				regionId: selectedRegionId !== 'all' ? selectedRegionId : undefined,
			})

			if (data.success === false) {
				throw new Error(data.message || 'Failed to fetch crime intelligence')
			}

			setInsights(data)
		} catch (err) {
			const msg = err instanceof Error ? err.message : 'Unable to load insights'
			setPageError(msg)
			setInsights(emptyInsights())
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
		if (!resolvedCustomerId || authLoading) return
		void fetchInsights()
	}, [resolvedCustomerId, filtersVersion, authLoading, fetchInsights])

	const analystNotes = useMemo(() => (insights ? buildAnalystNotes(insights) : []), [insights])

	// Sites filtered by selected region (cascading: region selection drives site options)
	const sitesForSelectedRegion = useMemo(() => {
		if (selectedRegionId === 'all') return sites
		return sites.filter(
			(s) =>
				String(s.fkRegionID ?? (s as any).fkRegionId ?? (s as any).regionId ?? (s as any).FkRegionID ?? '') ===
				selectedRegionId
		)
	}, [sites, selectedRegionId])

	// Reset site selection when region changes and current site is not in the new region
	useEffect(() => {
		if (selectedRegionId === 'all' || selectedSiteId === 'all') return
		const siteInRegion = sitesForSelectedRegion.some(
			(s) => String(s.siteID ?? (s as any).SiteID ?? (s as any).id ?? '') === selectedSiteId
		)
		if (!siteInRegion) setSelectedSiteId('all')
	}, [selectedRegionId, selectedSiteId, sitesForSelectedRegion])

	// ──────────────────────────────────────────────────────────────────────────
	// Loading / Error states
	// ──────────────────────────────────────────────────────────────────────────

	if (isResolvingCustomer || authLoading) {
		return (
			<div className="flex min-h-[50vh] items-center justify-center" aria-busy="true">
				<div className="space-y-3 text-center">
					<div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
					<p className="text-sm text-muted-foreground">Loading company…</p>
				</div>
			</div>
		)
	}

	if (pageError && !isAdmin && !insights) {
		return (
			<div className="max-w-2xl space-y-4">
				<Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
					<ArrowLeft className="h-4 w-4 mr-1.5" />
					Back
				</Button>
				<Card className="border-loss/30 bg-loss-soft">
					<CardContent className="flex items-center gap-3 py-5">
						<AlertTriangle className="h-5 w-5 text-loss flex-shrink-0" />
						<p className="text-loss text-sm font-medium">{pageError}</p>
					</CardContent>
				</Card>
			</div>
		)
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Main render
	// ──────────────────────────────────────────────────────────────────────────

	const canPickCompany = isAdmin || (isManager && assignedCustomers.length > 1)
	const regionName = regions.find((r) => r.id === selectedRegionId)?.name
	const siteName = sites.find((s) => String(s.siteID) === selectedSiteId)?.locationName
	const ledgerItems = (insights?.heroMetrics ?? []).slice(0, 4).map(toLedgerItem)

	return (
		<div className="w-full space-y-5">
			<PageHeader
				leading={
					<Button variant="ghost" size="sm" onClick={() => navigate(-1)} className="-ml-2 h-8 px-2 text-muted-foreground hover:text-foreground">
						<ArrowLeft className="h-4 w-4" />
						Back
					</Button>
				}
				title="Crime intelligence"
				description={
					<>
						{customer?.name ? <span className="font-medium text-foreground">{customer.name}: </span> : null}
						where incidents happen, what is taken and when.
					</>
				}
				className="pb-0"
				actions={
					<>
						{canPickCompany && (
							<Select
								disabled={isAdmin && (loadingCustomers || availableCustomers.length === 0)}
								value={selectedCustomerForAdmin?.toString() ?? ''}
								onValueChange={(value) => {
									const id = parseInt(value, 10)
									setSelectedCustomerForAdmin(id)
									setSelectedCustomerId(id)
									const params = new URLSearchParams(searchParams)
									params.set('customerId', value)
									navigate({ search: params.toString() }, { replace: true })
									setFiltersVersion((v) => v + 1)
								}}
							>
								<SelectTrigger className="h-9 w-full gap-2 sm:w-60" aria-label="Company">
									<Building2 className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
									<SelectValue placeholder={isAdmin && loadingCustomers ? 'Loading companies…' : 'Select company'} />
								</SelectTrigger>
								<SelectContent>
									{(isAdmin ? availableCustomers : assignedCustomers).map((c) => (
										<SelectItem key={c.id} value={c.id.toString()}>
											{c.name}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						)}
						<Button variant="outline" onClick={fetchInsights} disabled={loadingInsights || !resolvedCustomerId}>
							<RefreshCcw className={`h-4 w-4 ${loadingInsights ? 'animate-spin' : ''}`} />
							Refresh
						</Button>
					</>
				}
			/>

			{!resolvedCustomerId ? (
				<div className="rounded-lg border border-border bg-card">
					<EmptyState
						icon={<Building2 className="h-5 w-5" />}
						title="Choose a company"
						description="Crime intelligence is worked out per company. Pick one above to see where incidents happen, what is taken and when."
					/>
				</div>
			) : (
				<>
					<FilterBar
						label="Crime intelligence filters"
						summary={
							<>
								<span>Showing</span>
								<FilterChip>
									{startDate && endDate ? `${format(startDate, 'd MMM yyyy')} to ${format(endDate, 'd MMM yyyy')}` : 'All dates'}
								</FilterChip>
								{regionName ? (
									<FilterChip
										onRemove={() => {
											setSelectedRegionId('all')
											setSelectedSiteId('all')
											setFiltersVersion((v) => v + 1)
										}}
										removeLabel="Remove region filter"
									>
										{regionName}
									</FilterChip>
								) : (
									<FilterChip>All regions</FilterChip>
								)}
								{siteName ? (
									<FilterChip
										onRemove={() => {
											setSelectedSiteId('all')
											setFiltersVersion((v) => v + 1)
										}}
										removeLabel="Remove store filter"
									>
										{siteName}
									</FilterChip>
								) : null}
							</>
						}
					>
						<DateRangeField
							from={toIsoDate(startDate) ?? ''}
							to={toIsoDate(endDate) ?? ''}
							onFromChange={(value) => {
								setStartDate(fromDateInputValue(value))
								setFiltersVersion((v) => v + 1)
							}}
							onToChange={(value) => {
								setEndDate(fromDateInputValue(value))
								setFiltersVersion((v) => v + 1)
							}}
						/>
						<Select
							value={selectedRegionId}
							onValueChange={(value) => {
								setSelectedRegionId(value)
								setSelectedSiteId('all')
								setFiltersVersion((v) => v + 1)
							}}
						>
							<SelectTrigger className="h-9 w-full sm:w-44" aria-label="Region">
								<SelectValue placeholder="All regions" />
							</SelectTrigger>
							<SelectContent>
								<SelectItem value="all">All regions</SelectItem>
								{regions.map((r) => (
									<SelectItem key={r.id} value={r.id}>
										{r.name}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
						<Select
							value={selectedSiteId}
							onValueChange={(value) => {
								setSelectedSiteId(value)
								setFiltersVersion((v) => v + 1)
							}}
						>
							<SelectTrigger className="h-9 w-full gap-2 sm:w-52" aria-label="Store">
								<MapPin className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
								<SelectValue placeholder="All stores" />
							</SelectTrigger>
							<SelectContent>
								<SelectItem value="all">All stores</SelectItem>
								{sites.map((s) => (
									<SelectItem key={s.siteID} value={s.siteID?.toString() ?? ''}>
										{s.locationName}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</FilterBar>

					{pageError && (
						<div role="alert" className="flex items-center gap-2 rounded-lg border border-loss/30 bg-loss-soft px-4 py-3 text-sm text-loss">
							<AlertTriangle className="h-4 w-4 shrink-0" aria-hidden />
							{pageError}
						</div>
					)}

					{loadingInsights && !insights ? (
						<StatLedger items={Array.from({ length: 4 }, () => ({ label: ' ', value: ' ' }))} loading />
					) : ledgerItems.length ? (
						<StatLedger items={ledgerItems} />
					) : null}

					<div className="grid gap-5 xl:grid-cols-2">
						<RankedBars
							title="Hot stores"
							description="Stores ranked by number of incidents."
							icon={MapPin}
							data={insights?.topStores ?? []}
							emptyLabel="No store-level incidents for these filters."
							loading={loadingInsights}
						/>
						<IncidentMix data={insights?.topIncidentTypes ?? []} loading={loadingInsights} />
					</div>

					<div className="grid gap-5 xl:grid-cols-2">
						<TimeChart data={insights?.timeBuckets ?? []} loading={loadingInsights} />
						<RankedBars
							title="Regional exposure"
							description="Regions ranked by number of incidents."
							icon={BarChart3}
							data={insights?.topRegions ?? []}
							emptyLabel="No regional breakdown for these filters."
							loading={loadingInsights}
						/>
					</div>

					<div className="grid gap-5 lg:grid-cols-3">
						<ProductsTable data={insights?.topProducts ?? []} loading={loadingInsights} />
						<AnalystNotes notes={analystNotes} loading={loadingInsights} generatedAt={insights?.generatedAt} />
					</div>
				</>
			)}
		</div>
	)
}
