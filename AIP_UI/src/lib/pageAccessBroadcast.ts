/**
 * Cross-tab notification for page access settings updates.
 * When an admin saves settings, other tabs refresh their page access context.
 */

const CHANNEL_NAME = 'aip-page-access-settings'

export const broadcastPageAccessUpdated = (): void => {
	try {
		if (typeof BroadcastChannel !== 'undefined') {
			const channel = new BroadcastChannel(CHANNEL_NAME)
			channel.postMessage({ type: 'page-access-updated', timestamp: Date.now() })
			channel.close()
		} else {
			// Fallback: storage event (only fires in other tabs; writing triggers it)
			localStorage.setItem(`${CHANNEL_NAME}-updated`, Date.now().toString())
		}
	} catch {
		// Ignore in environments where BroadcastChannel/storage unavailable
	}
}

export const subscribeToPageAccessUpdates = (onUpdate: () => void): (() => void) => {
	const handleMessage = () => onUpdate()

	try {
		if (typeof BroadcastChannel !== 'undefined') {
			const channel = new BroadcastChannel(CHANNEL_NAME)
			channel.onmessage = handleMessage
			return () => channel.close()
		}

		// Fallback: storage event
		const handleStorage = (e: StorageEvent) => {
			if (e.key === `${CHANNEL_NAME}-updated`) handleMessage()
		}
		window.addEventListener('storage', handleStorage)
		return () => window.removeEventListener('storage', handleStorage)
	} catch {
		return () => {}
	}
}
