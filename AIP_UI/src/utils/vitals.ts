import { logger } from './logger'

/**
 * Web Vitals Monitoring
 * Tracks Core Web Vitals for performance monitoring
 */

interface Metric {
	name: string
	value: number
	rating: 'good' | 'needs-improvement' | 'poor'
	delta: number
	id: string
}

type ReportCallback = (metric: Metric) => void

// Thresholds for Core Web Vitals
const THRESHOLDS = {
	CLS: { good: 0.1, poor: 0.25 },
	FID: { good: 100, poor: 300 },
	LCP: { good: 2500, poor: 4000 },
	FCP: { good: 1800, poor: 3000 },
	TTFB: { good: 800, poor: 1800 },
}

/**
 * Get rating based on thresholds
 */
function getRating(value: number, thresholds: { good: number, poor: number }): 'good' | 'needs-improvement' | 'poor' {
	if (value <= thresholds.good) return 'good'
	if (value <= thresholds.poor) return 'needs-improvement'
	return 'poor'
}

/**
 * Report metric
 */
function reportMetric(metric: Metric, callback?: ReportCallback): void {
	// Log to console in development
	if (import.meta.env.DEV) {
		const emoji = metric.rating === 'good' ? '✅' : metric.rating === 'needs-improvement' ? '⚠️' : '❌'
		logger.info(`${emoji} ${metric.name}: ${metric.value.toFixed(2)}`, {
			rating: metric.rating,
			id: metric.id,
		})
	}

	// Send to analytics or monitoring service in production
	if (import.meta.env.PROD && callback) {
		callback(metric)
	}

	// Log poor metrics as warnings
	if (metric.rating === 'poor') {
		logger.warn(`Poor ${metric.name} detected`, {
			value: metric.value,
			threshold: THRESHOLDS[metric.name as keyof typeof THRESHOLDS]?.poor || 0,
		})
	}
}

/**
 * Measure Cumulative Layout Shift (CLS)
 */
export function measureCLS(callback?: ReportCallback): void {
	if (typeof window === 'undefined' || !('PerformanceObserver' in window)) return

	let clsValue = 0
	let clsEntries: PerformanceEntry[] = []

	const observer = new PerformanceObserver((list) => {
		for (const entry of list.getEntries()) {
			if (!(entry as any).hadRecentInput) {
				clsValue += (entry as any).value
				clsEntries.push(entry)
			}
		}
	})

	observer.observe({ type: 'layout-shift', buffered: true })

	// Report on visibility change (page hide)
	document.addEventListener('visibilitychange', () => {
		if (document.visibilityState === 'hidden') {
			observer.disconnect()
			
			const metric: Metric = {
				name: 'CLS',
				value: clsValue,
				rating: getRating(clsValue, THRESHOLDS.CLS),
				delta: clsValue,
				id: `cls-${Date.now()}`,
			}

			reportMetric(metric, callback)
		}
	})
}

/**
 * Measure Largest Contentful Paint (LCP)
 */
export function measureLCP(callback?: ReportCallback): void {
	if (typeof window === 'undefined' || !('PerformanceObserver' in window)) return

	let lcpValue = 0

	const observer = new PerformanceObserver((list) => {
		const entries = list.getEntries()
		const lastEntry = entries[entries.length - 1] as any
		lcpValue = lastEntry.renderTime || lastEntry.loadTime
	})

	observer.observe({ type: 'largest-contentful-paint', buffered: true })

	// Report on visibility change (page hide)
	document.addEventListener('visibilitychange', () => {
		if (document.visibilityState === 'hidden') {
			observer.disconnect()
			
			const metric: Metric = {
				name: 'LCP',
				value: lcpValue,
				rating: getRating(lcpValue, THRESHOLDS.LCP),
				delta: lcpValue,
				id: `lcp-${Date.now()}`,
			}

			reportMetric(metric, callback)
		}
	}, { once: true })
}

/**
 * Measure First Input Delay (FID)
 */
export function measureFID(callback?: ReportCallback): void {
	if (typeof window === 'undefined' || !('PerformanceObserver' in window)) return

	const observer = new PerformanceObserver((list) => {
		for (const entry of list.getEntries()) {
			const fidValue = (entry as any).processingStart - entry.startTime
			
			const metric: Metric = {
				name: 'FID',
				value: fidValue,
				rating: getRating(fidValue, THRESHOLDS.FID),
				delta: fidValue,
				id: `fid-${Date.now()}`,
			}

			reportMetric(metric, callback)
			observer.disconnect()
		}
	})

	observer.observe({ type: 'first-input', buffered: true })
}

/**
 * Measure First Contentful Paint (FCP)
 */
export function measureFCP(callback?: ReportCallback): void {
	if (typeof window === 'undefined' || !('PerformanceObserver' in window)) return

	const observer = new PerformanceObserver((list) => {
		for (const entry of list.getEntries()) {
			if (entry.name === 'first-contentful-paint') {
				const fcpValue = entry.startTime
				
				const metric: Metric = {
					name: 'FCP',
					value: fcpValue,
					rating: getRating(fcpValue, THRESHOLDS.FCP),
					delta: fcpValue,
					id: `fcp-${Date.now()}`,
				}

				reportMetric(metric, callback)
				observer.disconnect()
			}
		}
	})

	observer.observe({ type: 'paint', buffered: true })
}

/**
 * Measure Time to First Byte (TTFB)
 */
export function measureTTFB(callback?: ReportCallback): void {
	if (typeof window === 'undefined') return

	window.addEventListener('load', () => {
		const navEntry = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming
		
		if (navEntry) {
			const ttfbValue = navEntry.responseStart - navEntry.requestStart
			
			const metric: Metric = {
				name: 'TTFB',
				value: ttfbValue,
				rating: getRating(ttfbValue, THRESHOLDS.TTFB),
				delta: ttfbValue,
				id: `ttfb-${Date.now()}`,
			}

			reportMetric(metric, callback)
		}
	}, { once: true })
}

/**
 * Initialize all Web Vitals measurements
 */
export function initWebVitals(callback?: ReportCallback): void {
	if (typeof window === 'undefined') return

	measureCLS(callback)
	measureLCP(callback)
	measureFID(callback)
	measureFCP(callback)
	measureTTFB(callback)
}

/**
 * Report all metrics to a callback
 * Useful for sending to analytics services
 */
export function reportWebVitals(callback: ReportCallback): void {
	initWebVitals(callback)
}
