import { AxiosError } from 'axios'
import { getDismissedAlertInstanceIds, NOTIFICATIONS_DISMISSED_EVENT, extractAlertsFromListResponse, extractRecentAlertsFromSummary } from '@/lib/notifications/dismissed-notifications'
import { alertInstancesApi } from '@/services/api/alertInstances'

export interface AlertCountState {
	alertCount: number
	isLoading: boolean
	error: string | null
}

type Listener = (state: AlertCountState) => void

const POLL_INTERVAL_MS = 60_000
const FAILURE_BACKOFF_MS = 60_000

/** Scoped the same way as dashboard `getSummary(customerId)` / `getAlerts({ customerId })`. */
let scopeCustomerId: number | null = null

export const setAlertCountCustomerScope = (customerId: number | null): void => {
	if (scopeCustomerId === customerId) return
	scopeCustomerId = customerId
	void fetchAlertCount({ background: true })
}

let state: AlertCountState = { alertCount: 0, isLoading: true, error: null }
const listeners = new Set<Listener>()
let subscriberCount = 0
let pollTimer: ReturnType<typeof setInterval> | null = null
let inflight: Promise<void> | null = null
let backoffUntil = 0

const notify = () => {
	for (const listener of listeners) {
		listener(state)
	}
}

const isQuietFetchError = (err: unknown): boolean => {
	if (err instanceof AxiosError) {
		if (err.code === 'ECONNABORTED' || !err.response) return true
	}
	if (!(err instanceof Error)) return false
	if (err.name === 'TypeError') return true
	const message = err.message.toLowerCase()
	return (
		message.includes('failed to fetch') ||
		message.includes('network error') ||
		message.includes('timeout') ||
		message.includes('econnaborted')
	)
}

const fetchAlertCount = async (options?: { background?: boolean }) => {
	const background = options?.background ?? false
	if (Date.now() < backoffUntil) {
		return
	}
	if (inflight) {
		return inflight
	}

	if (!background) {
		state = { ...state, isLoading: true, error: null }
		notify()
	}

	inflight = (async () => {
		try {
			const customerId = scopeCustomerId ?? undefined
			const summary = await alertInstancesApi.getSummary(customerId)
			const dismissed = getDismissedAlertInstanceIds()
			let active: number
			if (dismissed.size === 0) {
				active =
					typeof summary.totalActive === 'number'
						? summary.totalActive
						: summary.newCount + summary.acknowledgedCount + summary.escalatedCount
			} else {
				try {
					const list = await alertInstancesApi.getAlerts({
						page: 1,
						pageSize: 500,
						customerId,
					})
					const rows = extractAlertsFromListResponse(list)
					const unresolved = rows.filter((a) => a.status !== 'resolved')
					active = unresolved.filter((a) => {
						const id = Number(a.alertInstanceId)
						return Number.isFinite(id) && !dismissed.has(id)
					}).length
				} catch {
					const recentAll = extractRecentAlertsFromSummary(summary)
					const overlap = recentAll.filter((a) => {
						const id = Number(a.alertInstanceId)
						return Number.isFinite(id) && dismissed.has(id)
					}).length
					const base =
						typeof summary.totalActive === 'number'
							? summary.totalActive
							: summary.newCount + summary.acknowledgedCount + summary.escalatedCount
					active = Math.max(0, base - overlap)
				}
			}

			state = { alertCount: active, isLoading: false, error: null }
			backoffUntil = 0
		} catch (err) {
			if (!isQuietFetchError(err)) {
				console.warn('[alertCountStore] Failed to load alert summary:', err)
			}
			state = { alertCount: 0, isLoading: false, error: null }
			backoffUntil = Date.now() + FAILURE_BACKOFF_MS
		} finally {
			inflight = null
			notify()
		}
	})()

	return inflight
}

const startPolling = () => {
	if (pollTimer) return
	pollTimer = setInterval(() => {
		void fetchAlertCount({ background: true })
	}, POLL_INTERVAL_MS)
}

const stopPolling = () => {
	if (pollTimer) {
		clearInterval(pollTimer)
		pollTimer = null
	}
}

const onAlertEvent = () => {
	void fetchAlertCount({ background: true })
}

export const alertCountStore = {
	subscribe(listener: Listener): () => void {
		const first = subscriberCount === 0
		subscriberCount++
		listeners.add(listener)
		listener(state)

		if (first) {
			void fetchAlertCount()
			startPolling()
			window.addEventListener('alert-created', onAlertEvent)
			window.addEventListener('alert-updated', onAlertEvent)
			window.addEventListener('alert-resolved', onAlertEvent)
			window.addEventListener(NOTIFICATIONS_DISMISSED_EVENT, onAlertEvent)
		}

		return () => {
			listeners.delete(listener)
			subscriberCount = Math.max(0, subscriberCount - 1)
			if (subscriberCount === 0) {
				stopPolling()
				window.removeEventListener('alert-created', onAlertEvent)
				window.removeEventListener('alert-updated', onAlertEvent)
				window.removeEventListener('alert-resolved', onAlertEvent)
				window.removeEventListener(NOTIFICATIONS_DISMISSED_EVENT, onAlertEvent)
			}
		}
	},

	refresh: () => fetchAlertCount({ background: true }),
}
