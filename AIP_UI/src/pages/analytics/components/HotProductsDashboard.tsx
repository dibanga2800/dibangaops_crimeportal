/**
 * Hot Products Dashboard Module
 * 
 * Displays barcode frequency charts, value lost per product,
 * and store-level heatmaps for stolen items.
 */

import { useMemo, useState, useEffect, Fragment, Suspense, lazy } from 'react'
import {
	BarChart,
	Bar,
	XAxis,
	YAxis,
	CartesianGrid,
	Tooltip,
	ResponsiveContainer,
	Cell,
} from 'recharts'
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import type {
	HotProductsData,
	ProductFrequencyData,
	ProductStoreBreakdown,
	StoreProductHeatmapData,
} from '@/types/analytics'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useChartTheme, chartTooltipStyle, axisTick } from '@/lib/chart-theme'
import { RISK_CHIP, SUBTABS_LIST, SUBTABS_TRIGGER } from './moduleStyles'
import type { Site } from '@/types/dashboard'
import { useStoreLocations, type StoreLocationInput } from '@/lib/store-locations'

// The map library is large; load it only when the map is shown.
const StoreRiskMap = lazy(() => import('./StoreRiskMap'))
import { Package, TrendingUp, AlertTriangle, PoundSterling, Search, Filter, ChevronDown, ChevronUp, MapPin, Eye, EyeOff, ChevronLeft, ChevronRight } from 'lucide-react'

interface HotProductsDashboardProps {
	/** Sites for the selected company; their postcodes place stores on the map. */
	sites?: Site[]
	data: HotProductsData
	loading?: boolean
}

const RISK_COLORS = {
	low: '#0E7C66', // green
	medium: '#C4680F', // amber
	high: '#B42318', // red
	critical: '#8F1D14', // dark red
}

const CHART_COLORS = [
	'#1F6FA8',
	'#0E7C66',
	'#C4680F',
	'#B42318',
	'#5B4A8B',
	'#A3365E',
	'#1C7C78',
	'#B54708',
]

const HEAT_BG: Record<string, string> = { low: 'bg-recovered', medium: 'bg-warning', high: 'bg-loss', critical: 'bg-loss' }
const getRiskColor = (riskLevel: string) => {
	return RISK_COLORS[riskLevel as keyof typeof RISK_COLORS] || RISK_COLORS.low
}

const formatCurrencyExact = (value: number) =>
	value.toLocaleString('en-GB', {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	})

const getProductKey = (
	product: { barcode: string; productName: string },
	index: number,
	suffix = ''
) => {
	const base =
		product.barcode?.trim() ||
		`${product.productName?.trim() || 'unknown'}-${index}`
	return suffix ? `${base}-${suffix}` : base
}

const getStoreCardKey = (store: StoreProductHeatmapData) =>
	`${store.storeId}-${store.storeName}`

const getProductMatchKey = (barcode: string, productName: string): string => {
	const trimmedBarcode = barcode?.trim()
	if (
		trimmedBarcode &&
		trimmedBarcode.length >= 4 &&
		trimmedBarcode.toLowerCase() !== 'unknown'
	) {
		return `barcode:${trimmedBarcode.toLowerCase()}`
	}

	const name = productName?.trim()
	if (name) {
		return `name:${name.toLowerCase()}`
	}

	return 'unknown'
}

const resolveProductStores = (
	product: ProductFrequencyData,
	storeHeatmap: StoreProductHeatmapData[],
): ProductStoreBreakdown[] => {
	if (product.stores && product.stores.length > 0) {
		return product.stores
	}

	const productKey = getProductMatchKey(product.barcode, product.productName)
	const fromHeatmap: ProductStoreBreakdown[] = []

	storeHeatmap.forEach((store) => {
		store.products.forEach((line) => {
			if (getProductMatchKey(line.barcode, line.productName) !== productKey) {
				return
			}

			const existing = fromHeatmap.find(
				(s) => s.storeName.toLowerCase() === store.storeName.toLowerCase(),
			)
			if (existing) {
				existing.frequency += line.frequency
				existing.stolenValue += line.stolenValue
				existing.recoveredValue += line.recoveredValue
				existing.lostValue += line.lostValue
				existing.recoveryRate =
					existing.stolenValue > 0
						? (existing.recoveredValue / existing.stolenValue) * 100
						: 0
				return
			}

			fromHeatmap.push({
				storeId: store.storeId,
				storeName: store.storeName,
				frequency: line.frequency,
				stolenValue: line.stolenValue,
				recoveredValue: line.recoveredValue,
				lostValue: line.lostValue,
				recoveryRate: line.recoveryRate,
			})
		})
	})

	return fromHeatmap.sort(
		(a, b) => b.lostValue - a.lostValue || b.frequency - a.frequency,
	)
}

interface ProductStoreBreakdownPanelProps {
	stores: ProductStoreBreakdown[]
}

const ProductStoreBreakdownPanel = ({ stores }: ProductStoreBreakdownPanelProps) => {
	if (stores.length === 0) {
		return (
			<p className="text-xs text-muted-foreground py-2">
				No store breakdown available for this product in the selected period.
			</p>
		)
	}

	return (
		<div className="rounded-md border bg-muted/20 overflow-hidden">
			<Table>
				<TableHeader>
					<TableRow>
						<TableHead>Store</TableHead>
						<TableHead>Line items</TableHead>
						<TableHead className="text-right">Recovered</TableHead>
						<TableHead className="text-right">Lost</TableHead>
						<TableHead className="text-right">Recovery</TableHead>
					</TableRow>
				</TableHeader>
				<TableBody>
					{stores.map((store) => (
						<TableRow key={`${store.storeId}-${store.storeName}`}>
							<TableCell className="font-medium">{store.storeName}</TableCell>
							<TableCell>{store.frequency}</TableCell>
							<TableCell className="text-right text-recovered">
								£{formatCurrencyExact(store.recoveredValue)}
							</TableCell>
							<TableCell className="text-right text-loss">
								£{formatCurrencyExact(store.lostValue)}
							</TableCell>
							<TableCell className="text-right">{store.recoveryRate.toFixed(1)}%</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>
		</div>
	)
}

const riskPct = (score?: number) => {
	const v = score ?? 0
	return Math.round(v > 1 ? v : v * 100)
}

const RISK_LEVEL_HELP =
	'Store risk uses incident volume, £ lost, police involvement, and last-7-day activity (score 0–100%). Critical ≥70%, High ≥40%, Medium ≥20%.'

interface StoreHeatmapDetailProps {
	store: StoreProductHeatmapData
}

const StoreHeatmapDetail = ({ store }: StoreHeatmapDetailProps) => {
	const sortedProducts = [...store.products].sort(
		(a, b) => b.lostValue - a.lostValue || b.frequency - a.frequency,
	)

	return (
		<div className="space-y-4">
			<div className="space-y-2 rounded-md border border-border bg-muted/30 p-3 text-xs">
				<p className="flex items-center gap-2 font-semibold text-foreground">
					<span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold first-letter:uppercase ${RISK_CHIP[store.riskLevel] ?? RISK_CHIP.low}`}>{store.riskLevel} risk</span>
					<span className="figures text-muted-foreground">score {riskPct(store.riskScore)}/100</span>
				</p>
				{store.riskSummary ? (
					<p className="text-muted-foreground leading-relaxed">{store.riskSummary}</p>
				) : null}
				{store.riskFactors && store.riskFactors.length > 0 && (
					<ul className="list-disc pl-4 text-muted-foreground space-y-0.5">
						{store.riskFactors.map((factor) => (
							<li key={factor.factor}>{factor.description}</li>
						))}
					</ul>
				)}
			</div>
			<dl className="figures grid grid-cols-2 overflow-hidden rounded-md border border-border text-xs">
				{[
					{ label: 'All incidents', value: store.totalIncidents },
					{ label: 'With stolen items', value: store.incidentsWithStolenItems ?? 0 },
					{ label: 'Product lines', value: store.productLineCount ?? 0 },
					{ label: 'Product groups', value: store.productGroupCount ?? sortedProducts.length },
				].map((cell) => (
					<div key={cell.label} className="px-3 py-2 shadow-[1px_1px_0_0_hsl(var(--border))]">
						<dt className="text-muted-foreground">{cell.label}</dt>
						<dd className="text-sm font-semibold text-foreground">{cell.value}</dd>
					</div>
				))}
			</dl>
			<div>
				<p className="text-xs font-semibold text-muted-foreground mb-2">
					Stolen products ({sortedProducts.length})
				</p>
				{sortedProducts.length === 0 ? (
					<p className="text-xs text-muted-foreground">No stolen product lines in this period.</p>
				) : (
					<div className="max-h-56 overflow-y-auto space-y-2 pr-1">
						{sortedProducts.map((product, productIndex) => (
							<div
								key={getProductKey(product, productIndex, `detail-${getStoreCardKey(store)}`)}
								className="flex items-center justify-between rounded border border-border p-2 text-xs"
							>
								<div className="flex-1 min-w-0">
									<div className="font-medium truncate">{product.productName}</div>
									{product.barcode ? (
										<div className="text-muted-foreground truncate font-mono">{product.barcode}</div>
									) : null}
								</div>
								<div className="text-right ml-2 shrink-0">
									<div className="font-semibold">{product.frequency} lines</div>
									<div className="text-muted-foreground">£{formatCurrencyExact(product.lostValue)} lost</div>
								</div>
							</div>
						))}
					</div>
				)}
			</div>
		</div>
	)
}

export const HotProductsDashboard = ({
	data,
	loading = false,
	sites = [],
}: HotProductsDashboardProps) => {
	const theme = useChartTheme()
	const [selectedMapStoreId, setSelectedMapStoreId] = useState<string | null>(null)
	const locationInputs = useMemo<StoreLocationInput[]>(
		() =>
			data.storeHeatmap.map((store) => {
				const site = sites.find((s) => {
					const anySite = s as Site & { siteID?: string | number }
					return String(anySite.siteID ?? anySite.id) === String(store.storeId)
				}) ?? sites.find((s) => s.locationName === store.storeName)
				const geo = site as (Site & { latitude?: number; longitude?: number }) | undefined
				return {
					id: String(store.storeId),
					name: store.storeName,
					street: site?.street,
					postcode: site?.postcode,
					town: site?.town,
					latitude: geo?.latitude,
					longitude: geo?.longitude,
				}
			}),
		[data.storeHeatmap, sites],
	)
	const storeLocations = useStoreLocations(locationInputs)
	const storeAddresses = useMemo(
		() =>
			Object.fromEntries(
				locationInputs.map((l) => [l.id, [l.street, l.town, l.postcode].filter((part) => part && String(part).trim()).join(', ')]),
			) as Record<string, string>,
		[locationInputs],
	)
	const [searchTerm, setSearchTerm] = useState('')
	const [riskFilter, setRiskFilter] = useState<string>('all')
	const [expandedStores, setExpandedStores] = useState<Set<string>>(new Set())
	const [expandedProducts, setExpandedProducts] = useState<Set<string>>(new Set())
	const [viewMode, setViewMode] = useState<'map' | 'grid' | 'table' | 'heatmap'>('map')
	const [currentPage, setCurrentPage] = useState(1)
	const STORES_PER_PAGE = 12

	const topProductsChartData = useMemo(() => {
		return data.topProducts.slice(0, 10).map((product, index) => ({
			...product,
			fill: CHART_COLORS[index % CHART_COLORS.length],
		}))
	}, [data.topProducts])

	const sortedTopProducts = useMemo(
		() => [...data.topProducts].sort((a, b) => b.lostValue - a.lostValue),
		[data.topProducts],
	)

	const toggleProductExpansion = (productKey: string) => {
		setExpandedProducts((prev) => {
			const next = new Set(prev)
			if (next.has(productKey)) {
				next.delete(productKey)
			} else {
				next.add(productKey)
			}
			return next
		})
	}

	const sortedStores = useMemo(() => {
		return [...data.storeHeatmap].sort(
			(a, b) =>
				b.totalValueLost - a.totalValueLost ||
				(b.incidentsWithStolenItems ?? 0) - (a.incidentsWithStolenItems ?? 0),
		)
	}, [data.storeHeatmap])

	const filteredStores = useMemo(() => {
		let filtered = sortedStores

		// Filter by search term
		if (searchTerm) {
			filtered = filtered.filter((store) =>
				store.storeName.toLowerCase().includes(searchTerm.toLowerCase()) ||
				store.products.some((p) =>
					p.productName.toLowerCase().includes(searchTerm.toLowerCase())
				)
			)
		}

		// Filter by risk level
		if (riskFilter !== 'all') {
			filtered = filtered.filter((store) => store.riskLevel === riskFilter)
		}

		return filtered
	}, [sortedStores, searchTerm, riskFilter])

	// Pagination calculations
	const totalPages = Math.ceil(filteredStores.length / STORES_PER_PAGE)
	const startIndex = (currentPage - 1) * STORES_PER_PAGE
	const endIndex = startIndex + STORES_PER_PAGE
	const paginatedStores = filteredStores.slice(startIndex, endIndex)
	const selectedMapStore = selectedMapStoreId ? data.storeHeatmap.find((s) => String(s.storeId) === selectedMapStoreId) ?? null : null

	// Reset to page 1 when filters change
	useEffect(() => {
		setCurrentPage(1)
	}, [searchTerm, riskFilter])

	const toggleStoreExpansion = (storeKey: string) => {
		const newExpanded = new Set(expandedStores)
		if (newExpanded.has(storeKey)) {
			newExpanded.delete(storeKey)
		} else {
			newExpanded.add(storeKey)
		}
		setExpandedStores(newExpanded)
	}

	const getHeatmapIntensity = (store: StoreProductHeatmapData) => {
		const maxLost = Math.max(...sortedStores.map((s) => s.totalValueLost), 0)
		return maxLost > 0 ? (store.totalValueLost / maxLost) * 100 : 0
	}

	const getHeatmapColor = (intensity: number, riskLevel: string) => {
		if (riskLevel === 'critical') return `rgba(220, 38, 38, ${Math.max(0.8, intensity / 100)})`
		if (riskLevel === 'high') return `rgba(239, 68, 68, ${Math.max(0.7, intensity / 120)})`
		if (riskLevel === 'medium') return `rgba(245, 158, 11, ${Math.max(0.6, intensity / 140)})`
		return `rgba(16, 185, 129, ${Math.max(0.4, intensity / 160)})`
	}

	if (loading) {
		return <div className="h-[420px] animate-pulse rounded-lg border border-border bg-card" aria-busy="true" aria-label="Loading product analytics" />
	}


	return (
		<div className="space-y-5">
			<div className="grid gap-5 xl:grid-cols-[minmax(0,7fr)_minmax(0,5fr)]">
				<section className="rounded-lg border border-border bg-card">
					<div className="p-4 sm:p-5">
						<h3 className="flex items-center gap-2 text-base font-semibold text-foreground">
							<Package className="h-4 w-4 text-muted-foreground" aria-hidden />
							Most stolen products
						</h3>
						<p className="mt-0.5 text-[13px] text-muted-foreground">Top 10 by number of incidents they appear in.</p>
					</div>
					<div className="w-full px-2 pb-4 sm:px-4" style={{ height: Math.max(220, topProductsChartData.length * 36 + 40) }}>
						<ResponsiveContainer width="100%" height="100%">
							<BarChart data={topProductsChartData} layout="vertical" margin={{ top: 0, right: 28, left: 0, bottom: 0 }} barCategoryGap={8}>
								<CartesianGrid horizontal={false} stroke={theme.grid} />
								<XAxis type="number" tick={axisTick(theme)} tickLine={false} axisLine={false} allowDecimals={false} />
								<YAxis type="category" dataKey="productName" tick={{ ...axisTick(theme), fill: theme.foreground }} tickLine={false} axisLine={false} width={150} />
								<Tooltip {...chartTooltipStyle(theme)} />
								<Bar dataKey="frequency" name="Incidents" radius={[0, 3, 3, 0]} maxBarSize={22} label={{ position: 'right', fontSize: 11, fill: theme.muted }}>
									{topProductsChartData.map((entry, index) => (
										<Cell key={`cell-${index}`} fill={index === 0 ? theme.primary : theme.series[0] ?? theme.primary} fillOpacity={index === 0 ? 1 : 0.55} />
									))}
								</Bar>
							</BarChart>
						</ResponsiveContainer>
					</div>
				</section>

				<section className="rounded-lg border border-border bg-card">
					<div className="grid divide-y divide-border">
						{[
							{ title: 'Best recovered', hint: 'Most value got back', items: data.topRecoveredProducts.slice(0, 5), value: (p: ProductFrequencyData) => p.recoveredValue, tone: 'text-recovered', key: 'recovered' },
							{ title: 'Worst recovered', hint: 'Most value still lost', items: data.worstRecoveryProducts.slice(0, 5), value: (p: ProductFrequencyData) => p.lostValue, tone: 'text-loss', key: 'worst' },
						].map((group) => (
							<div key={group.key} className="p-4 sm:p-5">
								<div className="mb-2 flex items-baseline justify-between">
									<h3 className="text-sm font-semibold text-foreground">{group.title}</h3>
									<span className="text-xs text-muted-foreground">{group.hint}</span>
								</div>
								<ol className="divide-y divide-border">
									{group.items.map((product, index) => (
										<li key={getProductKey(product, index, group.key)} className="flex items-center gap-3 py-2">
											<span className="figures w-4 text-xs text-muted-foreground">{index + 1}</span>
											<span className="min-w-0 flex-1">
												<span className="block truncate text-sm font-medium text-foreground">{product.productName}</span>
												<span className="figures block text-xs text-muted-foreground">
													{product.frequency} incidents · {product.recoveryRate.toFixed(1)}% recovered
												</span>
											</span>
											<span className={`figures text-sm font-semibold ${group.tone}`}>£{formatCurrencyExact(group.value(product))}</span>
										</li>
									))}
								</ol>
							</div>
						))}
					</div>
				</section>
			</div>

				{/* Product Recovery Table */}
				<section className="rounded-lg border border-border bg-card">
					<div className="p-4 sm:p-5">
						<h3 className="text-base font-semibold text-foreground">Recovered and lost by product</h3>
						<p className="mt-0.5 text-[13px] text-muted-foreground">Ordered by value lost. Open a product to see which stores it was taken from.</p>
					</div>
					<div className="overflow-x-auto border-t border-border">
						<Table>
							<TableHeader>
								<TableRow>
									<TableHead>Product</TableHead>
									<TableHead>Barcode</TableHead>
									<TableHead className="text-right">Incidents</TableHead>
									<TableHead className="text-right">Stores</TableHead>
									<TableHead className="text-right">Saved</TableHead>
									<TableHead className="text-right">Lost</TableHead>
									<TableHead className="text-right">Rate</TableHead>
									<TableHead className="w-[90px]"><span className="sr-only">Stores</span></TableHead>
								</TableRow>
							</TableHeader>
							<TableBody>
								{sortedTopProducts.map((product, index) => {
									const productRowKey = getProductKey(product, index, 'table')
									const isProductExpanded = expandedProducts.has(productRowKey)
									const productStores = resolveProductStores(product, data.storeHeatmap)

									return (
										<Fragment key={productRowKey}>
											<TableRow>
												<TableCell className="font-medium">
													{product.productName}
												</TableCell>
												<TableCell className="figures text-xs text-muted-foreground">
													{product.barcode || '—'}
												</TableCell>
												<TableCell className="figures text-right">{product.frequency}</TableCell>
												<TableCell className="figures text-right">{productStores.length || product.storesAffected}</TableCell>
												<TableCell className="figures text-right font-semibold text-recovered">
													£{product.recoveredValue.toLocaleString('en-GB', {
														minimumFractionDigits: 2,
														maximumFractionDigits: 2,
													})}
												</TableCell>
												<TableCell className="figures text-right font-semibold text-loss">
													£{product.lostValue.toLocaleString('en-GB', {
														minimumFractionDigits: 2,
														maximumFractionDigits: 2,
													})}
												</TableCell>
												<TableCell className="figures text-right">
													{product.recoveryRate.toFixed(1)}%
												</TableCell>
												<TableCell className="text-right">
													<Button
														type="button"
														variant="ghost"
														size="sm"
														className="text-xs"
														onClick={() => toggleProductExpansion(productRowKey)}
														aria-expanded={isProductExpanded}
													>
														{isProductExpanded ? (
															<>
																<ChevronUp className="h-4 w-4 mr-1" />
																Hide
															</>
														) : (
															<>
																<ChevronDown className="h-4 w-4 mr-1" />
																Stores
															</>
														)}
													</Button>
												</TableCell>
											</TableRow>
											{isProductExpanded && (
												<TableRow>
													<TableCell colSpan={8} className="bg-muted/30 p-4">
														<p className="text-xs font-semibold text-muted-foreground mb-2">
															Stores where this product was stolen ({productStores.length})
														</p>
														<ProductStoreBreakdownPanel stores={productStores} />
													</TableCell>
												</TableRow>
											)}
										</Fragment>
									)
								})}
							</TableBody>
						</Table>
					</div>
				</section>

				{/* Store Heatmap */}
				<section className="rounded-lg border border-border bg-card p-4 sm:p-5">
					<div className="mb-3 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
						<div>
							<h3 className="flex items-center gap-2 text-base font-semibold text-foreground">
								<MapPin className="h-4 w-4 text-muted-foreground" aria-hidden />
								Stores by product theft
							</h3>
							<p className="mt-0.5 max-w-xl text-[13px] text-muted-foreground">{RISK_LEVEL_HELP}</p>
						</div>
						<div className="flex flex-col sm:flex-row gap-2">
							<div className="relative">
								<Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden />
								<Input
									placeholder="Search stores or products"
									value={searchTerm}
									onChange={(e) => setSearchTerm(e.target.value)}
									className="h-9 w-full pl-9 sm:w-60"
								/>
							</div>
							<Select value={riskFilter} onValueChange={setRiskFilter}>
								<SelectTrigger className="h-9 w-full sm:w-40" aria-label="Risk level">
									<SelectValue placeholder="All risk levels" />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value="all">All risk levels</SelectItem>
									<SelectItem value="critical">Critical</SelectItem>
									<SelectItem value="high">High</SelectItem>
									<SelectItem value="medium">Medium</SelectItem>
									<SelectItem value="low">Low</SelectItem>
								</SelectContent>
							</Select>
						</div>
					</div>
					

					<Tabs value={viewMode} onValueChange={(v) => setViewMode(v as typeof viewMode)} className="w-full">
						<TabsList className={SUBTABS_LIST}>
							<TabsTrigger className={SUBTABS_TRIGGER} value="map">Map</TabsTrigger>
							<TabsTrigger className={SUBTABS_TRIGGER} value="heatmap">Cards</TabsTrigger>
							<TabsTrigger className={SUBTABS_TRIGGER} value="grid">List</TabsTrigger>
							<TabsTrigger className={SUBTABS_TRIGGER} value="table">Table</TabsTrigger>
						</TabsList>

						<TabsContent value="map" className="mt-0">
							<div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_20rem]">
								<div className="relative h-[360px] overflow-hidden rounded-md border border-border bg-muted/40 sm:h-[460px] lg:h-[540px]">
									<Suspense fallback={<div className="h-full w-full animate-pulse bg-muted" />}>
											<StoreRiskMap
												stores={filteredStores}
												locations={storeLocations.locations}
												addresses={storeAddresses}
												selectedId={selectedMapStoreId}
												onSelect={setSelectedMapStoreId}
												className="h-full w-full"
											/>
										</Suspense>
									{!storeLocations.loading && storeLocations.unplaced.length > 0 && (
										<p className="absolute left-2 top-12 z-10 max-w-[70%] rounded-md border border-border bg-card/95 px-2.5 py-1.5 text-[11px] text-muted-foreground shadow-sm">
											{storeLocations.unplaced.length} {storeLocations.unplaced.length === 1 ? 'store has' : 'stores have'} no postcode we could find, so {storeLocations.unplaced.length === 1 ? 'it is' : 'they are'} not on the map.
										</p>
									)}
								</div>

								<div className="flex min-h-0 flex-col rounded-md border border-border lg:h-[540px]">
									{selectedMapStore ? (
										<div className="flex min-h-0 flex-1 flex-col">
											<div className="flex items-start justify-between gap-2 border-b border-border px-4 py-3">
												<div className="min-w-0">
													<p className="truncate text-sm font-semibold text-foreground">{selectedMapStore.storeName}</p>
													{storeAddresses[String(selectedMapStore.storeId)] ? (
														<p className="text-xs text-muted-foreground">{storeAddresses[String(selectedMapStore.storeId)]}</p>
													) : null}
													<p className="figures text-xs text-muted-foreground">
														£{formatCurrencyExact(selectedMapStore.totalValueLost)} lost · {selectedMapStore.recoveryRate.toFixed(1)}% recovered
													</p>
												</div>
												<Button variant="ghost" size="sm" className="h-8 shrink-0 px-2" onClick={() => setSelectedMapStoreId(null)}>
													<ChevronLeft className="h-4 w-4" />
													All stores
												</Button>
											</div>
											<div className="min-h-0 flex-1 overflow-y-auto p-4">
												<StoreHeatmapDetail store={selectedMapStore} />
											</div>
										</div>
									) : (
										<>
											<p className="border-b border-border px-4 py-3 text-xs font-medium text-muted-foreground">
												{filteredStores.length} {filteredStores.length === 1 ? 'store' : 'stores'} by value lost
											</p>
											<ol className="min-h-0 flex-1 divide-y divide-border overflow-y-auto">
												{filteredStores.map((store, index) => (
													<li key={getStoreCardKey(store)}>
														<button
															type="button"
															onClick={() => setSelectedMapStoreId(String(store.storeId))}
															className="!flex w-full !items-center gap-3 !rounded-none !border-0 !px-4 !py-2.5 text-left hover:bg-muted/50"
														>
															<span className="figures w-4 text-xs text-muted-foreground">{index + 1}</span>
															<span className={`h-2.5 w-2.5 shrink-0 rounded-full ${HEAT_BG[store.riskLevel] ?? 'bg-recovered'}`} aria-hidden />
															<span className="min-w-0 flex-1">
																<span className="block truncate text-sm font-medium text-foreground">{store.storeName}</span>
																<span className="sr-only">{store.riskLevel} risk. </span>
																<span className="block truncate text-xs text-muted-foreground">
																	{storeAddresses[String(store.storeId)] || <span className="capitalize">{store.riskLevel} risk</span>}
																</span>
															</span>
															<span className="figures text-sm font-semibold text-loss">£{Math.round(store.totalValueLost).toLocaleString('en-GB')}</span>
														</button>
													</li>
												))}
											</ol>
										</>
									)}
								</div>
							</div>
						</TabsContent>

						<TabsContent value="heatmap" className="space-y-4">
							{/* Visual Heatmap Grid */}
							<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
								{paginatedStores.map((store) => {
									const storeKey = getStoreCardKey(store)
									const intensity = getHeatmapIntensity(store)
									const heatmapColor = getHeatmapColor(intensity, store.riskLevel)
									const isExpanded = expandedStores.has(storeKey)

									return (
										<Card
											key={storeKey}
											className="cursor-pointer overflow-hidden transition-colors hover:border-foreground/25"
											onClick={() => toggleStoreExpansion(storeKey)}
										>
											<div className="h-1 bg-muted" aria-hidden>
												<div className={`h-full ${HEAT_BG[store.riskLevel] ?? 'bg-recovered'}`} style={{ width: `${Math.max(6, intensity)}%` }} />
											</div>
											<div className="p-4">
												<div className="mb-3 flex items-start justify-between gap-2">
													<p className="min-w-0 text-sm font-semibold leading-5 text-foreground">{store.storeName}</p>
													<span
														className={`figures shrink-0 rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${RISK_CHIP[store.riskLevel] ?? RISK_CHIP.low}`}
														title={store.riskSummary || RISK_LEVEL_HELP}
													>
														{store.riskLevel}
														{(store.riskScore ?? 0) > 0 ? ` · ${riskPct(store.riskScore)}` : ''}
													</span>
												</div>
												<dl className="figures grid grid-cols-2 gap-x-3 gap-y-2.5 text-sm">
													<div>
														<dt className="text-xs text-muted-foreground">Lost</dt>
														<dd className="font-display text-lg font-semibold leading-tight text-loss">£{formatCurrencyExact(store.totalValueLost)}</dd>
													</div>
													<div>
														<dt className="text-xs text-muted-foreground">Recovery</dt>
														<dd className="font-display text-lg font-semibold leading-tight text-foreground">{store.recoveryRate.toFixed(1)}%</dd>
													</div>
													<div>
														<dt className="text-xs text-muted-foreground">Theft incidents</dt>
														<dd className="font-semibold text-foreground">{store.incidentsWithStolenItems ?? 0}</dd>
													</div>
													<div>
														<dt className="text-xs text-muted-foreground">Product lines</dt>
														<dd className="font-semibold text-foreground">{store.productLineCount ?? 0}</dd>
													</div>
												</dl>
											</div>
											{isExpanded && (
												<CardContent
													className="p-4 border-t"
													onClick={(e) => e.stopPropagation()}
												>
													<StoreHeatmapDetail store={store} />
													<Button
														variant="ghost"
														size="sm"
														className="w-full mt-3"
														onClick={(e) => {
															e.stopPropagation()
															toggleStoreExpansion(storeKey)
														}}
													>
														<EyeOff className="h-4 w-4 mr-2" />
														Collapse
													</Button>
												</CardContent>
											)}
											{!isExpanded && (
												<div className="flex items-center justify-center gap-1.5 border-t border-border py-2 text-xs font-medium text-muted-foreground">
													<Eye className="h-3.5 w-3.5" aria-hidden />
													Details
												</div>
											)}
										</Card>
									)
								})}
							</div>
							{filteredStores.length === 0 && (
								<div className="text-center py-12 text-muted-foreground">
									<MapPin className="h-12 w-12 mx-auto mb-3 text-gray-300" />
									<p>No stores found matching your filters</p>
								</div>
							)}
							{/* Pagination */}
							{totalPages > 1 && (
								<div className="flex flex-col sm:flex-row items-center justify-between gap-4 pt-4 border-t">
									<div className="text-sm text-muted-foreground">
										Showing {startIndex + 1} to {Math.min(endIndex, filteredStores.length)} of {filteredStores.length} stores
									</div>
									<div className="flex items-center gap-2">
										<Button
											variant="outline"
											size="sm"
											onClick={() => setCurrentPage(currentPage - 1)}
											disabled={currentPage === 1}
										>
											<ChevronLeft className="h-4 w-4" />
											Previous
										</Button>
										<div className="flex items-center gap-1">
											{Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
												let pageNumber: number
												if (totalPages <= 5) {
													pageNumber = i + 1
												} else if (currentPage <= 3) {
													pageNumber = i + 1
												} else if (currentPage >= totalPages - 2) {
													pageNumber = totalPages - 4 + i
												} else {
													pageNumber = currentPage - 2 + i
												}
												return (
													<Button
														key={pageNumber}
														variant={currentPage === pageNumber ? "default" : "outline"}
														size="sm"
														onClick={() => setCurrentPage(pageNumber)}
														className="h-8 w-8 p-0"
													>
														{pageNumber}
													</Button>
												)
											})}
										</div>
										<Button
											variant="outline"
											size="sm"
											onClick={() => setCurrentPage(currentPage + 1)}
											disabled={currentPage === totalPages}
										>
											Next
											<ChevronRight className="h-4 w-4" />
										</Button>
									</div>
								</div>
							)}
						</TabsContent>

						<TabsContent value="grid" className="space-y-4">
							<div className="space-y-4">
								{paginatedStores.map((store) => {
									const isExpanded = expandedStores.has(getStoreCardKey(store))
									const totalValue = store.totalValueLost

									return (
										<Card key={getStoreCardKey(store)}>
											<CardHeader className="pb-3">
												<div className="flex items-center justify-between">
													<CardTitle className="text-base">{store.storeName}</CardTitle>
													<div className="flex items-center gap-3">
														<div className="text-right">
															<div className="text-sm font-semibold">{store.incidentsWithStolenItems ?? 0} theft incidents</div>
															<div className="text-xs text-muted-foreground">
																£{totalValue.toFixed(2)} lost, {store.recoveryRate.toFixed(1)}% recovered
															</div>
														</div>
														<span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${RISK_CHIP[store.riskLevel] ?? RISK_CHIP.low}`}>{store.riskLevel}</span>
														<Button
															variant="ghost"
															size="sm"
															onClick={() => toggleStoreExpansion(getStoreCardKey(store))}
														>
															{isExpanded ? (
																<ChevronUp className="h-4 w-4" />
															) : (
																<ChevronDown className="h-4 w-4" />
															)}
														</Button>
													</div>
												</div>
											</CardHeader>
											{isExpanded && (
												<CardContent>
													<StoreHeatmapDetail store={store} />
												</CardContent>
											)}
										</Card>
									)
								})}
							</div>
							{filteredStores.length === 0 && (
								<div className="text-center py-12 text-muted-foreground">
									<MapPin className="h-12 w-12 mx-auto mb-3 text-gray-300" />
									<p>No stores found matching your filters</p>
								</div>
							)}
							{/* Pagination */}
							{totalPages > 1 && (
								<div className="flex flex-col sm:flex-row items-center justify-between gap-4 pt-4 border-t">
									<div className="text-sm text-muted-foreground">
										Showing {startIndex + 1} to {Math.min(endIndex, filteredStores.length)} of {filteredStores.length} stores
									</div>
									<div className="flex items-center gap-2">
										<Button
											variant="outline"
											size="sm"
											onClick={() => setCurrentPage(currentPage - 1)}
											disabled={currentPage === 1}
										>
											<ChevronLeft className="h-4 w-4" />
											Previous
										</Button>
										<div className="flex items-center gap-1">
											{Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
												let pageNumber: number
												if (totalPages <= 5) {
													pageNumber = i + 1
												} else if (currentPage <= 3) {
													pageNumber = i + 1
												} else if (currentPage >= totalPages - 2) {
													pageNumber = totalPages - 4 + i
												} else {
													pageNumber = currentPage - 2 + i
												}
												return (
													<Button
														key={pageNumber}
														variant={currentPage === pageNumber ? "default" : "outline"}
														size="sm"
														onClick={() => setCurrentPage(pageNumber)}
														className="h-8 w-8 p-0"
													>
														{pageNumber}
													</Button>
												)
											})}
										</div>
										<Button
											variant="outline"
											size="sm"
											onClick={() => setCurrentPage(currentPage + 1)}
											disabled={currentPage === totalPages}
										>
											Next
											<ChevronRight className="h-4 w-4" />
										</Button>
									</div>
								</div>
							)}
						</TabsContent>

						<TabsContent value="table" className="space-y-4">
							<div className="border rounded-lg">
								<Table>
									<TableHeader>
										<TableRow>
											<TableHead>Store</TableHead>
											<TableHead>Risk Level</TableHead>
											<TableHead>Theft incidents</TableHead>
											<TableHead>Product groups</TableHead>
											<TableHead className="text-right">Total Value Lost</TableHead>
											<TableHead className="text-right">Recovery Rate</TableHead>
											<TableHead className="text-right">Actions</TableHead>
										</TableRow>
									</TableHeader>
									<TableBody>
										{paginatedStores.map((store) => {
											const totalValue = store.totalValueLost
											const isExpanded = expandedStores.has(getStoreCardKey(store))

											return (
												<Fragment key={getStoreCardKey(store)}>
													<TableRow>
														<TableCell className="font-medium">{store.storeName}</TableCell>
														<TableCell>
															<span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${RISK_CHIP[store.riskLevel] ?? RISK_CHIP.low}`}>{store.riskLevel}</span>
														</TableCell>
														<TableCell>{store.incidentsWithStolenItems ?? 0}</TableCell>
														<TableCell>{store.productGroupCount ?? store.products.length}</TableCell>
														<TableCell className="text-right font-semibold">
															£{totalValue.toLocaleString('en-GB', {
																minimumFractionDigits: 2,
																maximumFractionDigits: 2,
															})}
														</TableCell>
														<TableCell className="text-right">{store.recoveryRate.toFixed(1)}%</TableCell>
														<TableCell className="text-right">
															<Button
																variant="ghost"
																size="sm"
																onClick={() => toggleStoreExpansion(getStoreCardKey(store))}
															>
																{isExpanded ? (
																	<>
																		<EyeOff className="h-4 w-4 mr-2" />
																		Hide
																	</>
																) : (
																	<>
																		<Eye className="h-4 w-4 mr-2" />
																		View Products
																	</>
																)}
															</Button>
														</TableCell>
													</TableRow>
													{isExpanded && (
														<TableRow>
															<TableCell colSpan={7} className="bg-muted/50">
																<div className="p-4">
																	<StoreHeatmapDetail store={store} />
																</div>
															</TableCell>
														</TableRow>
													)}
												</Fragment>
											)
										})}
									</TableBody>
								</Table>
							</div>
							{filteredStores.length === 0 && (
								<div className="text-center py-12 text-muted-foreground">
									<MapPin className="h-12 w-12 mx-auto mb-3 text-gray-300" />
									<p>No stores found matching your filters</p>
								</div>
							)}
							{/* Pagination */}
							{totalPages > 1 && (
								<div className="flex flex-col sm:flex-row items-center justify-between gap-4 pt-4 border-t">
									<div className="text-sm text-muted-foreground">
										Showing {startIndex + 1} to {Math.min(endIndex, filteredStores.length)} of {filteredStores.length} stores
									</div>
									<div className="flex items-center gap-2">
										<Button
											variant="outline"
											size="sm"
											onClick={() => setCurrentPage(currentPage - 1)}
											disabled={currentPage === 1}
										>
											<ChevronLeft className="h-4 w-4" />
											Previous
										</Button>
										<div className="flex items-center gap-1">
											{Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
												let pageNumber: number
												if (totalPages <= 5) {
													pageNumber = i + 1
												} else if (currentPage <= 3) {
													pageNumber = i + 1
												} else if (currentPage >= totalPages - 2) {
													pageNumber = totalPages - 4 + i
												} else {
													pageNumber = currentPage - 2 + i
												}
												return (
													<Button
														key={pageNumber}
														variant={currentPage === pageNumber ? "default" : "outline"}
														size="sm"
														onClick={() => setCurrentPage(pageNumber)}
														className="h-8 w-8 p-0"
													>
														{pageNumber}
													</Button>
												)
											})}
										</div>
										<Button
											variant="outline"
											size="sm"
											onClick={() => setCurrentPage(currentPage + 1)}
											disabled={currentPage === totalPages}
										>
											Next
											<ChevronRight className="h-4 w-4" />
										</Button>
									</div>
								</div>
							)}
						</TabsContent>
					</Tabs>
				</section>
		</div>
	)
}
