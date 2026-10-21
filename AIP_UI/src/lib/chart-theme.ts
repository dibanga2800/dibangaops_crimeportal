import { useEffect, useState } from 'react'

/**
 * Resolved colours for Recharts and other SVG/canvas drawing.
 *
 * SVG presentation attributes (stroke, fill) cannot read CSS variables, so the
 * design tokens are resolved once from the document and re-resolved whenever
 * the theme class on <html> changes.
 */
export interface ChartTheme {
	loss: string
	recovered: string
	warning: string
	info: string
	signal: string
	primary: string
	foreground: string
	muted: string
	grid: string
	axis: string
	card: string
	/** Ordered categorical palette for series that have no semantic meaning. */
	series: string[]
}

const TOKENS = {
	loss: '--loss',
	recovered: '--recovered',
	warning: '--warning',
	info: '--info',
	signal: '--signal',
	primary: '--primary',
	foreground: '--foreground',
	muted: '--muted-foreground',
	grid: '--border',
	card: '--card',
} as const

const FALLBACK: ChartTheme = {
	loss: 'hsl(4 76% 40%)',
	recovered: 'hsl(168 80% 27%)',
	warning: 'hsl(25 90% 37%)',
	info: 'hsl(210 65% 38%)',
	signal: 'hsl(43 70% 51%)',
	primary: 'hsl(225 55% 31%)',
	foreground: 'hsl(222 35% 14%)',
	muted: 'hsl(210 9% 40%)',
	grid: 'hsl(100 9% 86%)',
	axis: 'hsl(210 9% 40%)',
	card: 'hsl(0 0% 100%)',
	series: [],
}

const read = (): ChartTheme => {
	if (typeof window === 'undefined') return FALLBACK
	const style = getComputedStyle(document.documentElement)
	const get = (name: string, fallback: string) => {
		const raw = style.getPropertyValue(name).trim()
		return raw ? `hsl(${raw})` : fallback
	}
	const theme = Object.fromEntries(
		Object.entries(TOKENS).map(([key, cssVar]) => [key, get(cssVar, FALLBACK[key as keyof typeof TOKENS] as string)]),
	) as Omit<ChartTheme, 'axis' | 'series'>
	const isDark = document.documentElement.classList.contains('dark')
	return {
		...theme,
		axis: theme.muted,
		// Logo navy and gold lead the categorical palette; loss/recovered are reserved for money.
		series: isDark
			? ['hsl(220 75% 74%)', 'hsl(43 80% 60%)', 'hsl(190 55% 58%)', 'hsl(265 55% 72%)', 'hsl(16 80% 64%)', 'hsl(330 55% 70%)']
			: ['hsl(225 55% 34%)', 'hsl(43 75% 42%)', 'hsl(190 60% 32%)', 'hsl(265 40% 48%)', 'hsl(16 70% 46%)', 'hsl(330 45% 45%)'],
	}
}

export function useChartTheme(): ChartTheme {
	const [theme, setTheme] = useState<ChartTheme>(read)

	useEffect(() => {
		setTheme(read())
		const observer = new MutationObserver(() => setTheme(read()))
		observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'data-ui'] })
		return () => observer.disconnect()
	}, [])

	return theme
}

/** Shared Recharts tooltip styling that follows the theme. */
export const chartTooltipStyle = (theme: ChartTheme) => ({
	contentStyle: {
		backgroundColor: theme.card,
		border: `1px solid ${theme.grid}`,
		borderRadius: 8,
		boxShadow: '0 8px 24px -6px rgb(0 0 0 / 0.18)',
		fontSize: 12,
		padding: '8px 10px',
		color: theme.foreground,
	},
	labelStyle: { color: theme.foreground, fontWeight: 600, marginBottom: 4 },
	itemStyle: { padding: '1px 0', color: theme.foreground },
	cursor: { stroke: theme.grid, strokeWidth: 1, fill: theme.grid, fillOpacity: 0.35 },
})

export const axisTick = (theme: ChartTheme) => ({ fontSize: 11, fill: theme.axis })
