import { useState, useEffect, useCallback } from 'react'
import { alertInstancesApi } from '@/services/api/alertInstances'

/**
 * Hook to expose a live count of active alerts for the current user/context.
 * Uses the Alert Summary endpoint and refreshes periodically and on alert events.
 */
export const useAlertCount = () => {
  const [alertCount, setAlertCount] = useState<number>(0)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [error, setError] = useState<string | null>(null)

  const fetchAlertCount = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)

      const summary = await alertInstancesApi.getSummary()

      // Prefer backend totalActive, fallback to sum of non-resolved statuses
      const active = typeof summary.totalActive === 'number'
        ? summary.totalActive
        : (summary.newCount + summary.acknowledgedCount + summary.escalatedCount)

      setAlertCount(active)
    } catch (err) {
      // Log only non-network errors to avoid noisy console when backend is down
      const isNetworkError =
        err instanceof Error &&
        (err.message.includes('Failed to fetch') ||
          err.message.includes('Network Error') ||
          err.name === 'TypeError')

      if (!isNetworkError) {
        console.error('[useAlertCount] Failed to load alert summary:', err)
      }

      setAlertCount(0)
      setError(null)
    } finally {
      setIsLoading(false)
    }
  }, [])

  // Initial load
  useEffect(() => {
    fetchAlertCount()
  }, [fetchAlertCount])

  // Periodic refresh (every 30 seconds)
  useEffect(() => {
    const interval = setInterval(() => {
      fetchAlertCount()
    }, 30000)

    return () => clearInterval(interval)
  }, [fetchAlertCount])

  // React to alert-related events fired from alert UI components
  useEffect(() => {
    const handleAlertEvent = () => {
      fetchAlertCount()
    }

    window.addEventListener('alert-created', handleAlertEvent)
    window.addEventListener('alert-updated', handleAlertEvent)
    window.addEventListener('alert-resolved', handleAlertEvent)

    return () => {
      window.removeEventListener('alert-created', handleAlertEvent)
      window.removeEventListener('alert-updated', handleAlertEvent)
      window.removeEventListener('alert-resolved', handleAlertEvent)
    }
  }, [fetchAlertCount])

  return {
    alertCount,
    isLoading,
    error,
    refresh: fetchAlertCount,
  }
}

