import { useEffect, useState } from 'react'

/**
 * Store coordinates for the analytics map.
 *
 * Sites only carry a postal address, so postcodes are looked up with
 * postcodes.io (free UK postcode data from the ONS, no account or key) and
 * cached in the browser. When the backend starts returning latitude/longitude
 * on a site, those values are used directly and no lookup is made.
 */

export interface StoreLocationInput {
	id: string
	name: string
	street?: string
	postcode?: string
	town?: string
	latitude?: number | null
	longitude?: number | null
}

export type LngLat = [number, number]

const CACHE_PREFIX = 'aip.postcode.v1:'
const LOOKUP_URL = 'https://api.postcodes.io/postcodes'

const normalisePostcode = (value: string) => value.toUpperCase().replace(/\s+/g, '')

const readCache = (key: string): LngLat | null | undefined => {
	try {
		const raw = window.localStorage.getItem(CACHE_PREFIX + key)
		if (raw === null) return undefined
		return JSON.parse(raw) as LngLat | null
	} catch {
		return undefined
	}
}

const writeCache = (key: string, value: LngLat | null) => {
	try {
		window.localStorage.setItem(CACHE_PREFIX + key, JSON.stringify(value))
	} catch {
		// Storage can be full or blocked; the lookup simply repeats next time.
	}
}

/** Look up many postcodes at once (postcodes.io accepts up to 100 per request). */
async function lookupPostcodes(postcodes: string[]): Promise<Record<string, LngLat | null>> {
	const result: Record<string, LngLat | null> = {}
	const missing: string[] = []
	for (const pc of postcodes) {
		const cached = readCache(pc)
		if (cached !== undefined) result[pc] = cached
		else missing.push(pc)
	}
	for (let i = 0; i < missing.length; i += 100) {
		const batch = missing.slice(i, i + 100)
		try {
			const response = await fetch(LOOKUP_URL, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ postcodes: batch }),
			})
			if (!response.ok) continue
			const body = (await response.json()) as {
				result?: Array<{ query: string; result: { longitude: number; latitude: number } | null }>
			}
			for (const row of body.result ?? []) {
				const key = normalisePostcode(row.query)
				const value: LngLat | null = row.result ? [row.result.longitude, row.result.latitude] : null
				result[key] = value
				writeCache(key, value)
			}
		} catch {
			// Network failure: leave these stores unplaced for now and try again next time.
		}
	}
	return result
}

export interface StoreLocationsState {
	/** Coordinates by store id; stores that could not be placed are absent. */
	locations: Record<string, LngLat>
	loading: boolean
	/** Stores with no usable address or no geocoding result. */
	unplaced: string[]
}

export function useStoreLocations(stores: StoreLocationInput[]): StoreLocationsState {
	const [state, setState] = useState<StoreLocationsState>({ locations: {}, loading: true, unplaced: [] })
	const key = stores.map((s) => `${s.id}|${s.postcode ?? ''}|${s.latitude ?? ''}|${s.longitude ?? ''}`).join(';')

	useEffect(() => {
		let cancelled = false
		const run = async () => {
			const locations: Record<string, LngLat> = {}
			const unplaced: string[] = []
			const needLookup = stores.filter(
				(s) => !(typeof s.latitude === 'number' && typeof s.longitude === 'number') && s.postcode?.trim(),
			)
			const found = await lookupPostcodes([...new Set(needLookup.map((s) => normalisePostcode(s.postcode as string)))])
			for (const store of stores) {
				if (typeof store.latitude === 'number' && typeof store.longitude === 'number') {
					locations[store.id] = [store.longitude, store.latitude]
					continue
				}
				const coords = store.postcode?.trim() ? found[normalisePostcode(store.postcode)] : null
				if (coords) locations[store.id] = coords
				else unplaced.push(store.name)
			}
			if (!cancelled) setState({ locations, loading: false, unplaced })
		}
		setState((prev) => ({ ...prev, loading: true }))
		run()
		return () => {
			cancelled = true
		}
	}, [key])

	return state
}
