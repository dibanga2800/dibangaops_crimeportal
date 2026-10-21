/**
 * One way to show money across the portal.
 *
 * - headline: summary figures and cards, whole pounds (£6,888)
 * - exact:    tables, line items and receipts, with pence (£6,888.02)
 * - axis:     chart axes and labels where space is tight (£6.9k)
 */
export type MoneyStyle = 'headline' | 'exact' | 'axis'

const exactFormatter = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', minimumFractionDigits: 2, maximumFractionDigits: 2 })
const headlineFormatter = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 })

export function formatMoney(value: number | null | undefined, style: MoneyStyle = 'exact'): string {
	const v = Number.isFinite(Number(value)) ? Number(value) : 0
	if (style === 'headline') return headlineFormatter.format(Math.round(v))
	if (style === 'axis') {
		const abs = Math.abs(v)
		if (abs >= 1_000_000) return `£${(v / 1_000_000).toFixed(abs >= 10_000_000 ? 0 : 1)}m`
		if (abs >= 1_000) return `£${(v / 1_000).toFixed(abs >= 10_000 ? 0 : 1)}k`
		return `£${Math.round(v)}`
	}
	return exactFormatter.format(v)
}
