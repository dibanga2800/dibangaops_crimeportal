import { alertInstancesApi } from '@/services/api/alertInstances'
import type { AlertInstance, AlertInstanceListResponse, AlertSummary } from '@/types/alertInstances'
import { sessionStore } from '@/state/sessionStore'

/** Dispatched when local dismissal state changes (badge + dashboard should refresh). */
export const NOTIFICATIONS_DISMISSED_EVENT = 'notifications-dismissed'

const STORAGE_INSTANCE = 'aip:dismissed-alert-instance-ids'
const STORAGE_UI_KEYS = 'aip:dismissed-alert-ui-keys'
const MAX_INSTANCE_IDS = 2500
const MAX_UI_KEYS = 200

/** Placeholder row ids used when the dashboard falls back to demo alerts (no API data). */
export const DASHBOARD_PLACEHOLDER_ALERT_IDS = ['1', '2', '3', '4'] as const

export interface DismissNotificationsOptions {
	/** Match dashboard / summary scope (managers, store, etc.). */
	customerId?: number | null
	/** IDs currently shown (e.g. dashboard `recentAlerts`) so clear works even if list/summary differ. */
	explicitInstanceIds?: number[]
}

export const getNotificationDismissalScope = (): string => {
	const user = sessionStore.getUser()
	return user?.id ? `u:${user.id}` : 'anon'
}

const readJson = <T>(baseKey: string, scope: string, fallback: T): T => {
	try {
		const raw = localStorage.getItem(`${baseKey}:${scope}`)
		if (!raw) return fallback
		return JSON.parse(raw) as T
	} catch {
		return fallback
	}
}

const writeJson = (baseKey: string, scope: string, value: unknown): void => {
	try {
		localStorage.setItem(`${baseKey}:${scope}`, JSON.stringify(value))
	} catch {
		// private mode / quota
	}
}

export const getDismissedAlertInstanceIds = (): Set<number> => {
	const scope = getNotificationDismissalScope()
	const arr = readJson<number[]>(STORAGE_INSTANCE, scope, [])
	const nums = arr.map((x) => Number(x)).filter((n) => Number.isFinite(n))
	return new Set(nums)
}

export const getDismissedAlertUiKeys = (): Set<string> => {
	const scope = getNotificationDismissalScope()
	const arr = readJson<string[]>(STORAGE_UI_KEYS, scope, [])
	return new Set(arr)
}

const mergeInstanceIds = (ids: number[]): void => {
	if (ids.length === 0) return
	const scope = getNotificationDismissalScope()
	const existing = readJson<number[]>(STORAGE_INSTANCE, scope, [])
	const merged = [...new Set([...existing, ...ids])].slice(-MAX_INSTANCE_IDS)
	writeJson(STORAGE_INSTANCE, scope, merged)
}

const mergeUiKeys = (keys: string[]): void => {
	if (keys.length === 0) return
	const scope = getNotificationDismissalScope()
	const existing = readJson<string[]>(STORAGE_UI_KEYS, scope, [])
	const merged = [...new Set([...existing, ...keys])].slice(-MAX_UI_KEYS)
	writeJson(STORAGE_UI_KEYS, scope, merged)
}

const broadcastDismissed = (): void => {
	window.dispatchEvent(new CustomEvent(NOTIFICATIONS_DISMISSED_EVENT))
}

const asRecord = (v: unknown): Record<string, unknown> =>
	v !== null && typeof v === 'object' ? (v as Record<string, unknown>) : {}

function alertInstanceIdFromRow(row: unknown): number | null {
	const r = asRecord(row)
	const raw = r.alertInstanceId ?? r.AlertInstanceId
	const n = typeof raw === 'number' ? raw : Number(raw)
	return Number.isFinite(n) ? n : null
}

/** Supports camelCase and PascalCase list payloads from the API. */
export function extractAlertsFromListResponse(payload: unknown): AlertInstance[] {
	const root = asRecord(payload)
	const data = root.data ?? root.Data
	if (!Array.isArray(data)) return []
	const out: AlertInstance[] = []
	for (const item of data) {
		const id = alertInstanceIdFromRow(item)
		if (id === null) continue
		out.push(item as AlertInstance)
	}
	return out
}

/** Supports camelCase / PascalCase summary payloads. */
export function extractRecentAlertsFromSummary(payload: unknown): AlertInstance[] {
	const root = asRecord(payload)
	const recent = root.recentAlerts ?? root.RecentAlerts
	if (!Array.isArray(recent)) return []
	const out: AlertInstance[] = []
	for (const item of recent) {
		if (alertInstanceIdFromRow(item) !== null) {
			out.push(item as AlertInstance)
		}
	}
	return out
}

/**
 * Mark unresolved alert instances as dismissed locally (per user in localStorage).
 * Pass the same customerId the dashboard uses for `getSummary`, plus any IDs currently visible.
 */
export const dismissAllNotificationsFromServer = async (
	options?: DismissNotificationsOptions
): Promise<void> => {
	const customerId =
		options?.customerId !== null && options?.customerId !== undefined
			? options.customerId
			: undefined

	const mergedNumeric: number[] = []

	for (const raw of options?.explicitInstanceIds ?? []) {
		const n = Number(raw)
		if (Number.isFinite(n)) mergedNumeric.push(n)
	}

	try {
		const list: AlertInstanceListResponse = await alertInstancesApi.getAlerts({
			page: 1,
			pageSize: 500,
			customerId,
		})
		const rows = extractAlertsFromListResponse(list)
		for (const a of rows) {
			const id = alertInstanceIdFromRow(a)
			if (id !== null) mergedNumeric.push(id)
		}
	} catch {
		// summary may still help
	}

	try {
		const summary: AlertSummary = await alertInstancesApi.getSummary(customerId)
		for (const a of extractRecentAlertsFromSummary(summary)) {
			const id = alertInstanceIdFromRow(a)
			if (id !== null) mergedNumeric.push(id)
		}
	} catch {
		// ignore
	}

	if (mergedNumeric.length > 0) {
		mergeInstanceIds(mergedNumeric)
	}
	mergeUiKeys([...DASHBOARD_PLACEHOLDER_ALERT_IDS])
	broadcastDismissed()
}
