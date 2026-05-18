import { format, parse, startOfDay, subDays } from 'date-fns'
import type { DateRange } from 'react-day-picker'

export const ANALYTICS_DEFAULT_LOOKBACK_DAYS = 30

export const getDefaultAnalyticsDateRange = (): DateRange => {
	const to = startOfDay(new Date())
	const from = startOfDay(subDays(to, ANALYTICS_DEFAULT_LOOKBACK_DAYS))
	return { from, to }
}

export const parseAnalyticsDateInput = (value: string): Date | undefined => {
	if (!value.trim()) {
		return undefined
	}

	const parsed = parse(value, 'yyyy-MM-dd', new Date())
	if (Number.isNaN(parsed.getTime())) {
		return undefined
	}

	return startOfDay(parsed)
}

export const normalizeAnalyticsDateRange = (range: DateRange | undefined): DateRange => {
	const fallback = getDefaultAnalyticsDateRange()
	const from = range?.from ? startOfDay(range.from) : fallback.from!
	const to = range?.to ? startOfDay(range.to) : fallback.to!

	if (from.getTime() > to.getTime()) {
		return { from: to, to: from }
	}

	return { from, to }
}

export const formatAnalyticsDateParam = (date: Date): string => format(date, 'yyyy-MM-dd')
