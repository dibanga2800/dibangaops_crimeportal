import React, { useEffect, useRef } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/ui/use-toast'

const IDLE_TIMEOUT_MINUTES = Number(import.meta.env.VITE_IDLE_TIMEOUT_MINUTES ?? 10)
const IDLE_TIMEOUT_MS = IDLE_TIMEOUT_MINUTES * 60 * 1000

const activityEvents: Array<keyof DocumentEventMap> = [
  'mousemove',
  'mousedown',
  'keydown',
  'scroll',
  'touchstart',
]

export const SessionTimeoutManager: React.FC = () => {
  const { user, logout } = useAuth()
  const { toast } = useToast()
  const navigate = useNavigate()
  const location = useLocation()
  const timerRef = useRef<number | null>(null)
  const lastActivityRef = useRef<number>(Date.now())

  const clearTimer = () => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current)
      timerRef.current = null
    }
  }

  const scheduleTimeout = () => {
    clearTimer()
    const now = Date.now()
    lastActivityRef.current = now

    timerRef.current = window.setTimeout(async () => {
      // Only enforce timeout for authenticated users
      if (!user) {
        return
      }

      try {
        logout()
      } finally {
        toast({
          title: 'Session expired',
          description: `You were signed out after ${IDLE_TIMEOUT_MINUTES} minutes of inactivity for security.`,
          duration: 5000,
        })
        if (!location.pathname.startsWith('/login')) {
          navigate('/login', { replace: true })
        }
      }
    }, IDLE_TIMEOUT_MS)
  }

  useEffect(() => {
    // If no authenticated user, do not track idle state
    if (!user) {
      clearTimer()
      return
    }

    const handleActivity = () => {
      // Ignore activity on the login page
      if (location.pathname.startsWith('/login')) {
        clearTimer()
        return
      }

      scheduleTimeout()
    }

    activityEvents.forEach((event) => {
      window.addEventListener(event, handleActivity, { passive: true })
    })

    // Schedule initial timeout when component mounts or user/route changes
    scheduleTimeout()

    return () => {
      activityEvents.forEach((event) => {
        window.removeEventListener(event, handleActivity)
      })
      clearTimer()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user, location.pathname])

  return null
}

