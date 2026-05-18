import { describe, it, expect } from 'vitest'
import {
	ANALYTICS_DEFAULT_LOOKBACK_DAYS,
	getDefaultAnalyticsDateRange,
	normalizeAnalyticsDateRange,
	parseAnalyticsDateInput,
	formatAnalyticsDateParam,
} from '@/pages/analytics/analyticsDateRange'
import { differenceInCalendarDays } from 'date-fns'

describe('analyticsDateRange', () => {
	it('defaults to a 30-day inclusive window ending today', () => {
		const range = getDefaultAnalyticsDateRange()
		expect(range.from).toBeDefined()
		expect(range.to).toBeDefined()
		const days = differenceInCalendarDays(range.to!, range.from!)
		expect(days).toBe(ANALYTICS_DEFAULT_LOOKBACK_DAYS)
	})

	it('parses yyyy-MM-dd without UTC shift', () => {
		const parsed = parseAnalyticsDateInput('2025-03-15')
		expect(parsed).toBeDefined()
		expect(formatAnalyticsDateParam(parsed!)).toBe('2025-03-15')
	})

	it('swaps inverted ranges', () => {
		const normalized = normalizeAnalyticsDateRange({
			from: parseAnalyticsDateInput('2025-06-01'),
			to: parseAnalyticsDateInput('2025-05-01'),
		})
		expect(normalized.from!.getTime()).toBeLessThanOrEqual(normalized.to!.getTime())
	})
})
