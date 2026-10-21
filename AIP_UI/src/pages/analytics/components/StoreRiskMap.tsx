import { useEffect, useMemo, useRef, useState } from 'react'
import maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { StoreProductHeatmapData } from '@/types/analytics'
import type { LngLat } from '@/lib/store-locations'
import { useChartTheme } from '@/lib/chart-theme'

export interface StoreRiskMapProps {
	stores: StoreProductHeatmapData[]
	/** Coordinates keyed by String(store.storeId). */
	locations: Record<string, LngLat>
	/** One-line address keyed by store id, shown under the store name on the map. */
	addresses?: Record<string, string>
	selectedId: string | null
	onSelect: (id: string | null) => void
	className?: string
}

const SOURCE = 'stores'

type PointFeature = { geometry: { type: string; coordinates: number[] }; properties: Record<string, unknown> }
const UK_CENTRE: LngLat = [-1.6, 52.6]

const isDark = () => document.documentElement.classList.contains('dark')
// Free OpenStreetMap basemap from OpenFreeMap (no account or key): a quiet grey map with
// town, road and street names, so the heat reads clearly on top. Override with VITE_MAP_STYLE_URL.
const LIGHT_STYLE: string = import.meta.env.VITE_MAP_STYLE_URL || 'https://tiles.openfreemap.org/styles/positron'
const DARK_STYLE: string = import.meta.env.VITE_MAP_STYLE_URL_DARK || 'https://tiles.openfreemap.org/styles/dark'
const styleUrl = (dark: boolean) => (dark ? DARK_STYLE : LIGHT_STYLE)
// All stores are in the UK; keep the map around it (with neighbouring coasts for context).
const UK_BOUNDS: [LngLat, LngLat] = [
	[-17, 46.5],
	[11, 63],
]

export type MapViewMode = 'heatmap' | 'points'

// Classic density spectrum: cool where theft is light, red where it concentrates.
const HEAT_RAMP = [
	'interpolate',
	['linear'],
	['heatmap-density'],
	0,
	'rgba(103,169,207,0)',
	0.1,
	'rgba(103,169,207,0.55)',
	0.3,
	'rgb(94,201,182)',
	0.5,
	'rgb(166,217,106)',
	0.7,
	'rgb(253,219,99)',
	0.85,
	'rgb(246,140,96)',
	1,
	'rgb(239,83,80)',
] as unknown as maplibregl.ExpressionSpecification

/** Map style colours need the comma form of hsl(). */
const gl = (colour: string) => colour.replace(/^hsl\(\s*([\d.]+)\s+([\d.]+%)\s+([\d.]+%)\s*\)$/, 'hsl($1, $2, $3)')

/** In heatmap mode the store bubbles stay hidden (but clickable) until you zoom in close. */
const pointOpacity = (mode: MapViewMode) =>
	(mode === 'points' ? 0.9 : ['interpolate', ['linear'], ['zoom'], 9, 0, 11, 0.9]) as unknown as number

const money = (v: number) => `£${Math.round(v).toLocaleString('en-GB')}`

/**
 * Where stock is being lost. A density layer shows the hot areas when zoomed
 * out; as you zoom in it gives way to one bubble per store, sized by value lost
 * and coloured by the store's risk level. Selecting a bubble selects the store.
 */
export default function StoreRiskMap({ stores, locations, addresses, selectedId, onSelect, className }: StoreRiskMapProps) {
	const theme = useChartTheme()
	const container = useRef<HTMLDivElement>(null)
	const mapRef = useRef<maplibregl.Map | null>(null)
	const popupRef = useRef<maplibregl.Popup | null>(null)
	const onSelectRef = useRef(onSelect)
	onSelectRef.current = onSelect
	const [mode, setMode] = useState<MapViewMode>('heatmap')
	const modeRef = useRef<MapViewMode>(mode)
	modeRef.current = mode

	const geojson = useMemo(() => {
		const placed = stores.filter((s) => locations[String(s.storeId)])
		const maxLost = Math.max(1, ...placed.map((s) => s.totalValueLost))
		return {
			type: 'FeatureCollection' as const,
			features: placed.map((s) => ({
				type: 'Feature' as const,
				id: s.storeId,
				geometry: { type: 'Point' as const, coordinates: locations[String(s.storeId)] },
				properties: {
					id: String(s.storeId),
					name: s.storeName,
					lost: s.totalValueLost,
					incidents: s.incidentsWithStolenItems ?? s.totalIncidents ?? 0,
					recovery: s.recoveryRate,
					address: addresses?.[String(s.storeId)] ?? '',
					risk: s.riskLevel,
					weight: s.totalValueLost / maxLost,
				},
			})),
		}
	}, [stores, locations, addresses])

	const colours = useMemo(
		() => ({
			critical: gl(theme.loss),
			high: gl(theme.loss),
			medium: gl(theme.warning),
			low: gl(theme.recovered),
			ring: gl(theme.foreground),
			card: gl(theme.card),
		}),
		[theme],
	)

	// Layers are re-added whenever the style changes (light/dark), so keep them in one place.
	const addLayers = (map: maplibregl.Map) => {
		if (!map.getSource(SOURCE)) map.addSource(SOURCE, { type: 'geojson', data: geojson })
		// Draw the density layer under the basemap's labels so place and street names stay readable.
		const firstLabel = map.getStyle()?.layers?.find((l) => l.type === 'symbol')?.id
		const maxLost = Math.max(1, ...geojson.features.map((f) => f.properties.lost))
		if (!map.getLayer('store-heat')) {
			map.addLayer({
				id: 'store-heat',
				type: 'heatmap',
				source: SOURCE,
				layout: { visibility: modeRef.current === 'heatmap' ? 'visible' : 'none' },
				paint: {
					// Every store shows a hot core, scaled by its share of the largest loss.
					'heatmap-weight': ['interpolate', ['linear'], ['get', 'weight'], 0, 0.45, 1, 1],
					'heatmap-intensity': ['interpolate', ['linear'], ['zoom'], 5, 3.4, 9, 3.8, 13, 4.2],
					'heatmap-radius': ['interpolate', ['linear'], ['zoom'], 5, 12, 8, 20, 11, 36, 14, 70],
					'heatmap-color': HEAT_RAMP,
					'heatmap-opacity': ['interpolate', ['linear'], ['zoom'], 12, 0.85, 15, 0.35],
				},
			}, firstLabel)
		}
		if (!map.getLayer('store-points')) {
			map.addLayer({
				id: 'store-points',
				type: 'circle',
				source: SOURCE,
				paint: {
					'circle-radius': ['interpolate', ['linear'], ['get', 'lost'], 0, 6, maxLost, 20],
					'circle-color': [
						'match',
						['get', 'risk'],
						'critical',
						colours.critical,
						'high',
						colours.high,
						'medium',
						colours.medium,
						colours.low,
					],
					'circle-opacity': pointOpacity(modeRef.current),
					'circle-stroke-opacity': pointOpacity(modeRef.current),
					'circle-stroke-width': ['case', ['boolean', ['feature-state', 'selected'], false], 3, 1.5],
					'circle-stroke-color': ['case', ['boolean', ['feature-state', 'selected'], false], colours.ring, colours.card],
				},
			})
		}
		if (!map.getLayer('store-labels')) {
			map.addLayer({
				id: 'store-labels',
				type: 'symbol',
				source: SOURCE,
				layout: {
					visibility: modeRef.current === 'points' ? 'visible' : 'none',
					'text-field': ['format', ['get', 'name'], { 'font-scale': 1 }, '\n', {}, ['get', 'address'], { 'font-scale': 0.85 }],
					'text-size': ['interpolate', ['linear'], ['zoom'], 6, 11, 12, 13],
					'text-font': ['Noto Sans Regular'],
					'text-max-width': 14,
					'text-offset': [0, 1.6],
					'text-anchor': 'top',
					'text-optional': true,
				},
				paint: {
					'text-color': colours.ring,
					'text-halo-color': colours.card,
					'text-halo-width': 1.5,
				},
			})
		}
	}

	// Create the map once.
	useEffect(() => {
		if (!container.current) return
		const map = new maplibregl.Map({
			container: container.current,
			style: styleUrl(isDark()),
			center: UK_CENTRE,
			zoom: 5.4,
			maxBounds: UK_BOUNDS,
			attributionControl: { compact: true },
			cooperativeGestures: true,
		})
		// If the dark basemap cannot be loaded, fall back to the light one instead of a blank map.
		let fellBack = false
		map.on('error', () => {
			if (!fellBack && isDark() && DARK_STYLE !== LIGHT_STYLE && !map.isStyleLoaded()) {
				fellBack = true
				map.setStyle(LIGHT_STYLE)
			}
		})
		mapRef.current = map
		map.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right')
		map.addControl(new maplibregl.FullscreenControl(), 'top-right')
		map.addControl(new maplibregl.ScaleControl({ unit: 'imperial', maxWidth: 100 }), 'bottom-right')

		popupRef.current = new maplibregl.Popup({ closeButton: false, closeOnClick: false, offset: 14, className: 'store-map-popup' })

		map.on('mouseenter', 'store-points', (e) => {
			map.getCanvas().style.cursor = 'pointer'
			const f = e.features?.[0] as unknown as PointFeature | undefined
			if (!f || f.geometry.type !== 'Point') return
			const p = f.properties as { name: string; address: string; lost: number; incidents: number; recovery: number }
			const esc = (v: unknown) => String(v ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;')
			popupRef.current
				?.setLngLat(f.geometry.coordinates as LngLat)
				.setHTML(
					`<strong>${esc(p.name)}</strong>${p.address ? `<br/><span style="opacity:.75">${esc(p.address)}</span>` : ''}<br/>${money(Number(p.lost))} lost · ${p.incidents} theft incidents<br/>${Number(p.recovery).toFixed(1)}% recovered`,
				)
				.addTo(map)
		})
		map.on('mouseleave', 'store-points', () => {
			map.getCanvas().style.cursor = ''
			popupRef.current?.remove()
		})
		map.on('click', 'store-points', (e) => {
			const id = (e.features?.[0] as unknown as PointFeature | undefined)?.properties?.id
			if (id) onSelectRef.current(String(id))
		})

		// Follow the app's light/dark theme.
		let dark = isDark()
		const observer = new MutationObserver(() => {
			if (isDark() === dark) return
			dark = isDark()
			map.setStyle(styleUrl(dark))
		})
		observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })

		return () => {
			observer.disconnect()
			popupRef.current?.remove()
			map.remove()
			mapRef.current = null
		}
	}, [])

	// Keep data and layers in sync (also after a style swap).
	useEffect(() => {
		const map = mapRef.current
		if (!map) return
		const sync = () => {
			const source = map.getSource(SOURCE) as maplibregl.GeoJSONSource | undefined
			if (source) source.setData(geojson)
			else addLayers(map)
			if (geojson.features.length) {
				const bounds = new maplibregl.LngLatBounds()
				geojson.features.forEach((f) => bounds.extend(f.geometry.coordinates))
				map.fitBounds(bounds, { padding: 72, maxZoom: 8, duration: 0 })
			}
		}
		const onStyle = () => addLayers(map)
		if (map.isStyleLoaded()) sync()
		else map.once('load', sync)
		map.on('style.load', onStyle)
		return () => {
			map.off('style.load', onStyle)
		}
	}, [geojson, colours])

	// Highlight and fly to the selected store.
	useEffect(() => {
		const map = mapRef.current
		if (!map || !map.getSource(SOURCE)) return
		geojson.features.forEach((f) => map.setFeatureState({ source: SOURCE, id: f.id }, { selected: f.properties.id === selectedId }))
		const target = geojson.features.find((f) => f.properties.id === selectedId)
		if (target) map.flyTo({ center: target.geometry.coordinates, zoom: Math.max(map.getZoom(), 11), duration: 700 })
	}, [selectedId, geojson])

	// Switch between the density view and one bubble per store.
	useEffect(() => {
		const map = mapRef.current
		if (!map || !map.getLayer('store-points')) return
		map.setLayoutProperty('store-heat', 'visibility', mode === 'heatmap' ? 'visible' : 'none')
		map.setLayoutProperty('store-labels', 'visibility', mode === 'points' ? 'visible' : 'none')
		map.setPaintProperty('store-points', 'circle-opacity', pointOpacity(mode))
		map.setPaintProperty('store-points', 'circle-stroke-opacity', pointOpacity(mode))
	}, [mode])

	return (
		<div className={`relative ${className ?? ''}`}>
			<div ref={container} className="h-full w-full" role="region" aria-label="Map of stores by value lost" />
			<div className="absolute left-2 top-2 z-10 flex items-center gap-2.5 rounded-md border border-border bg-card/95 px-3 py-1.5 text-xs shadow-sm backdrop-blur">
				<span className="font-medium text-muted-foreground">View</span>
				<button
					type="button"
					role="switch"
					aria-checked={mode === 'heatmap'}
					aria-label="Show heatmap"
					onClick={() => setMode((m) => (m === 'heatmap' ? 'points' : 'heatmap'))}
					className="!relative !inline-flex !h-5 !w-9 shrink-0 !items-center !rounded-full !border-0 !p-0 transition-colors"
					style={{ background: mode === 'heatmap' ? 'hsl(var(--primary))' : 'hsl(var(--input))' }}
				>
					<span
						className="block h-4 w-4 rounded-full bg-white shadow transition-transform"
						style={{ transform: mode === 'heatmap' ? 'translateX(18px)' : 'translateX(2px)' }}
					/>
				</button>
				<span className="w-14 font-semibold text-foreground">{mode === 'heatmap' ? 'Heatmap' : 'Stores'}</span>
			</div>
			<div className="pointer-events-none absolute bottom-8 left-2 z-10 rounded-md border border-border bg-card/95 px-3 py-2 text-[11px] text-muted-foreground shadow-sm backdrop-blur">
				{mode === 'heatmap' ? (
					<>
						<p className="mb-1 font-medium text-foreground">Value lost</p>
						<div
							className="h-2 w-40 rounded-full"
							style={{ background: 'linear-gradient(90deg, rgba(103,169,207,0.55), rgb(94,201,182), rgb(166,217,106), rgb(253,219,99), rgb(246,140,96), rgb(239,83,80))' }}
						/>
						<div className="mt-0.5 flex justify-between">
							<span>Less</span>
							<span>More</span>
						</div>
					</>
				) : (
					<>
						<div className="flex items-center gap-3">
							{[
								{ label: 'Critical / high', dot: 'bg-loss' },
								{ label: 'Medium', dot: 'bg-warning' },
								{ label: 'Low', dot: 'bg-recovered' },
							].map((item) => (
								<span key={item.label} className="flex items-center gap-1.5">
									<span className={`h-2.5 w-2.5 rounded-full ${item.dot}`} aria-hidden />
									{item.label}
								</span>
							))}
						</div>
						<p className="mt-1">Bubble size shows value lost</p>
					</>
				)}
			</div>
		</div>
	)
}
