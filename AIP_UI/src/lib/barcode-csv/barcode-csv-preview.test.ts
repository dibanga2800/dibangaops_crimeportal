import { describe, it, expect } from 'vitest'
import {
	buildBarcodeCsvPreview,
	parseCsvLine,
	sanitizeFormulaInjectionPrefix,
	normalizeField,
	tryParseRetailPrice,
} from '@/lib/barcode-csv/barcode-csv-preview'

const standardHeaders = 'barcode,Department,VMECode,ProductName,RetailPrice'

describe('barcode-csv-preview', () => {
	it('maps headers case-insensitively', () => {
		const r = buildBarcodeCsvPreview(
			'Barcode,Department,vmecode,PRODUCTNAME,RetailPrice\n5012345678900,PROVISIONS,V1,Name,2.80\n'
		)
		expect(r.ok).toBe(true)
		expect(r.rows).toHaveLength(1)
		expect(r.rows[0].validationError).toBeNull()
		expect(r.rows[0].department).toBe('PROVISIONS')
	})

	it('rejects missing VMECode column', () => {
		const r = buildBarcodeCsvPreview('barcode,Department,ProductName,RetailPrice\n1,D,A,1\n')
		expect(r.ok).toBe(false)
		expect(r.fatalError).toContain('VMECode')
	})

	it('rejects missing RetailPrice column', () => {
		const r = buildBarcodeCsvPreview('barcode,Department,VMECode,ProductName\n1,D,V,N\n')
		expect(r.ok).toBe(false)
		expect(r.fatalError).toContain('RetailPrice')
	})

	it('allows blank header cells from trailing commas or gaps', () => {
		const withTrailing = `${standardHeaders},\n5012345678900,PROVISIONS,V1,Name,2.50\n`
		const r1 = buildBarcodeCsvPreview(withTrailing)
		expect(r1.ok).toBe(true)
		expect(r1.rows).toHaveLength(1)
		expect(r1.rows[0].validationError).toBeNull()

		const withGap = 'barcode,Department,,VMECode,ProductName,RetailPrice\n5012345678900,PROVISIONS,,V1,Name,2.50\n'
		const r2 = buildBarcodeCsvPreview(withGap)
		expect(r2.ok).toBe(true)
		expect(r2.rows[0].validationError).toBeNull()
	})

	it('allows empty department cell for preserve on update', () => {
		const r = buildBarcodeCsvPreview(`${standardHeaders}\n5012345678900,,V1,Name,2.49\n`)
		expect(r.ok).toBe(true)
		expect(r.rows[0].validationError).toBeNull()
	})

	it('parses quoted commas', () => {
		expect(parseCsvLine('"a,b",c')).toEqual(['a,b', 'c'])
	})

	it('maps RetailPrice column', () => {
		const r = buildBarcodeCsvPreview(`${standardHeaders}\n1,BAKERY,V,N,2.50\n`)
		expect(r.ok).toBe(true)
		expect(r.retailPriceColumnPresent).toBe(true)
		expect(r.rows[0].retailPrice).toBe('2.50')
	})

	it('detects ignored CostPrice column', () => {
		const r = buildBarcodeCsvPreview(
			'barcode,Department,VMECode,ProductName,CostPrice,RetailPrice\n1,BAKERY,V,N,9,10\n'
		)
		expect(r.ok).toBe(true)
		expect(r.ignoredCostPriceColumnDetected).toBe(true)
	})

	it('prefixes formula-like values', () => {
		expect(sanitizeFormulaInjectionPrefix('=1+1')).toBe("'=1+1")
		expect(normalizeField('  =x  ')).toBe("'=x")
	})

	it('parses retail price with invariant decimals only', () => {
		expect(tryParseRetailPrice('3.99')).toEqual({ ok: true, value: 3.99 })
		expect(tryParseRetailPrice('£2.50')).toEqual({ ok: true, value: 2.5 })
		expect(tryParseRetailPrice('1,50').ok).toBe(false)
		expect(tryParseRetailPrice('-1').ok).toBe(false)
	})

	it('rejects invalid retail price in preview', () => {
		const r = buildBarcodeCsvPreview(`${standardHeaders}\n1,BAKERY,V,N,not-a-price\n`)
		expect(r.ok).toBe(true)
		expect(r.rows[0].validationError).toContain('RetailPrice')
	})
})
