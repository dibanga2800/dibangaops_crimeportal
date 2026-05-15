import { useEffect, useState, useCallback } from 'react'
import { alertCountStore, type AlertCountState } from '@/lib/alert-count/alert-count-store'

/**
 * Live active-alert count for the header badge.
 * Uses a shared store so mobile + desktop NotificationBell instances do not duplicate API calls.
 */
export const useAlertCount = () => {
	const [snapshot, setSnapshot] = useState<AlertCountState>({
		alertCount: 0,
		isLoading: true,
		error: null,
	})

	useEffect(() => alertCountStore.subscribe(setSnapshot), [])

	const refresh = useCallback(() => {
		void alertCountStore.refresh()
	}, [])

	return {
		alertCount: snapshot.alertCount,
		isLoading: snapshot.isLoading,
		error: snapshot.error,
		refresh,
	}
}
