import { logger } from './logger'

/**
 * Performance Monitoring Utility
 * Tracks and reports performance metrics
 */

interface PerformanceMetric {
	name: string
	duration: number
	timestamp: number
	type: 'navigation' | 'resource' | 'measure' | 'custom'
	details?: Record<string, any>
}

class PerformanceMonitor {
	private static instance: PerformanceMonitor
	private metrics: PerformanceMetric[] = []
	private observers: PerformanceObserver[] = []
	private isEnabled: boolean
	private readonly maxMetrics = 100

	private constructor() {
		this.isEnabled = import.meta.env.DEV || import.meta.env.VITE_ENABLE_ANALYTICS === 'true'
		
		if (this.isEnabled && typeof window !== 'undefined' && 'performance' in window) {
			this.setupObservers()
			this.trackPageLoad()
		}
	}

	static getInstance(): PerformanceMonitor {
		if (!PerformanceMonitor.instance) {
			PerformanceMonitor.instance = new PerformanceMonitor()
		}
		return PerformanceMonitor.instance
	}

	/**
	 * Setup performance observers
	 */
	private setupObservers(): void {
		try {
			// Observe resource timing (images, scripts, stylesheets, etc.)
			if ('PerformanceObserver' in window) {
				const resourceObserver = new PerformanceObserver((list) => {
					for (const entry of list.getEntries()) {
						if (entry.duration > 1000) { // Only log slow resources (>1s)
							this.recordMetric({
								name: entry.name,
								duration: entry.duration,
								timestamp: entry.startTime,
								type: 'resource',
								details: {
									initiatorType: (entry as PerformanceResourceTiming).initiatorType,
									transferSize: (entry as PerformanceResourceTiming).transferSize,
								}
							})
						}
					}
				})

				resourceObserver.observe({ entryTypes: ['resource'] })
				this.observers.push(resourceObserver)

				// Observe navigation timing
				const navigationObserver = new PerformanceObserver((list) => {
					for (const entry of list.getEntries()) {
						this.recordMetric({
							name: 'Page Navigation',
							duration: entry.duration,
							timestamp: entry.startTime,
							type: 'navigation',
							details: {
								domContentLoaded: (entry as PerformanceNavigationTiming).domContentLoadedEventEnd,
								loadComplete: (entry as PerformanceNavigationTiming).loadEventEnd,
							}
						})
					}
				})

				navigationObserver.observe({ entryTypes: ['navigation'] })
				this.observers.push(navigationObserver)
			}
		} catch (error) {
			logger.warn('Failed to setup performance observers', error)
		}
	}

	/**
	 * Track page load performance
	 */
	private trackPageLoad(): void {
		if (typeof window === 'undefined') return

		window.addEventListener('load', () => {
			setTimeout(() => {
				const perfData = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming

				if (perfData) {
					const metrics = {
						dns: perfData.domainLookupEnd - perfData.domainLookupStart,
						tcp: perfData.connectEnd - perfData.connectStart,
						ttfb: perfData.responseStart - perfData.requestStart,
						download: perfData.responseEnd - perfData.responseStart,
						domInteractive: perfData.domInteractive - perfData.fetchStart,
						domComplete: perfData.domComplete - perfData.fetchStart,
						loadComplete: perfData.loadEventEnd - perfData.fetchStart,
					}

					logger.info('Page Load Performance', metrics)

					// Record slow page loads
					if (metrics.loadComplete > 5000) { // >5 seconds
						logger.warn('Slow page load detected', { loadTime: metrics.loadComplete })
					}
				}
			}, 0)
		})
	}

	/**
	 * Record a performance metric
	 */
	private recordMetric(metric: PerformanceMetric): void {
		this.metrics.push(metric)

		// Keep metrics array size manageable
		if (this.metrics.length > this.maxMetrics) {
			this.metrics = this.metrics.slice(-this.maxMetrics)
		}

		// Log in development
		if (import.meta.env.DEV) {
			logger.debug(`Performance: ${metric.name}`, {
				duration: `${metric.duration.toFixed(2)}ms`,
				type: metric.type,
				...metric.details
			})
		}
	}

	/**
	 * Measure execution time of a function
	 */
	async measureAsync<T>(
		name: string,
		fn: () => Promise<T>
	): Promise<T> {
		if (!this.isEnabled) {
			return fn()
		}

		const startTime = performance.now()
		
		try {
			const result = await fn()
			const duration = performance.now() - startTime

			this.recordMetric({
				name,
				duration,
				timestamp: startTime,
				type: 'custom',
			})

			return result
		} catch (error) {
			const duration = performance.now() - startTime
			
			this.recordMetric({
				name: `${name} (failed)`,
				duration,
				timestamp: startTime,
				type: 'custom',
				details: { error: String(error) }
			})

			throw error
		}
	}

	/**
	 * Measure execution time of a synchronous function
	 */
	measure<T>(name: string, fn: () => T): T {
		if (!this.isEnabled) {
			return fn()
		}

		const startTime = performance.now()
		
		try {
			const result = fn()
			const duration = performance.now() - startTime

			this.recordMetric({
				name,
				duration,
				timestamp: startTime,
				type: 'custom',
			})

			return result
		} catch (error) {
			const duration = performance.now() - startTime
			
			this.recordMetric({
				name: `${name} (failed)`,
				duration,
				timestamp: startTime,
				type: 'custom',
				details: { error: String(error) }
			})

			throw error
		}
	}

	/**
	 * Mark a specific point in time
	 */
	mark(name: string): void {
		if (!this.isEnabled) return

		try {
			performance.mark(name)
		} catch (error) {
			logger.warn(`Failed to mark performance: ${name}`, error)
		}
	}

	/**
	 * Measure between two marks
	 */
	measureBetween(name: string, startMark: string, endMark: string): void {
		if (!this.isEnabled) return

		try {
			performance.measure(name, startMark, endMark)
			const measure = performance.getEntriesByName(name, 'measure')[0]
			
			if (measure) {
				this.recordMetric({
					name,
					duration: measure.duration,
					timestamp: measure.startTime,
					type: 'measure',
				})
			}
		} catch (error) {
			logger.warn(`Failed to measure between marks: ${name}`, error)
		}
	}

	/**
	 * Get all recorded metrics
	 */
	getMetrics(): PerformanceMetric[] {
		return [...this.metrics]
	}

	/**
	 * Get metrics by type
	 */
	getMetricsByType(type: PerformanceMetric['type']): PerformanceMetric[] {
		return this.metrics.filter(m => m.type === type)
	}

	/**
	 * Get slow operations (>1 second)
	 */
	getSlowOperations(): PerformanceMetric[] {
		return this.metrics.filter(m => m.duration > 1000)
	}

	/**
	 * Clear all metrics
	 */
	clearMetrics(): void {
		this.metrics = []
		
		try {
			performance.clearMarks()
			performance.clearMeasures()
		} catch (error) {
			logger.warn('Failed to clear performance marks', error)
		}
	}

	/**
	 * Get performance report
	 */
	getReport(): {
		totalMetrics: number
		slowOperations: number
		averageDuration: number
		byType: Record<string, number>
	} {
		const slowOps = this.getSlowOperations()
		const totalDuration = this.metrics.reduce((sum, m) => sum + m.duration, 0)
		
		const byType: Record<string, number> = {}
		this.metrics.forEach(m => {
			byType[m.type] = (byType[m.type] || 0) + 1
		})

		return {
			totalMetrics: this.metrics.length,
			slowOperations: slowOps.length,
			averageDuration: this.metrics.length > 0 ? totalDuration / this.metrics.length : 0,
			byType,
		}
	}

	/**
	 * Log performance report to console
	 */
	logReport(): void {
		const report = this.getReport()
		logger.info('Performance Report', report)
		
		const slowOps = this.getSlowOperations()
		if (slowOps.length > 0) {
			logger.warn('Slow Operations Detected', {
				count: slowOps.length,
				operations: slowOps.map(op => ({
					name: op.name,
					duration: `${op.duration.toFixed(2)}ms`
				}))
			})
		}
	}

	/**
	 * Cleanup observers
	 */
	cleanup(): void {
		this.observers.forEach(observer => observer.disconnect())
		this.observers = []
	}
}

// Export singleton instance
export const performanceMonitor = PerformanceMonitor.getInstance()

// Export helper functions for convenience
export const measureAsync = <T>(name: string, fn: () => Promise<T>) => 
	performanceMonitor.measureAsync(name, fn)

export const measure = <T>(name: string, fn: () => T) => 
	performanceMonitor.measure(name, fn)

export const mark = (name: string) => 
	performanceMonitor.mark(name)

export const measureBetween = (name: string, startMark: string, endMark: string) => 
	performanceMonitor.measureBetween(name, startMark, endMark)
