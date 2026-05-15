import { describe, it, expect } from 'vitest'
import { getImportOutcome, getRowErrorsSummary } from '@/lib/barcode-csv/import-result-status'
import type { BarcodeCsvImportResult } from '@/services/productBarcodeCsvImportService'

const baseResult = (): BarcodeCsvImportResult => ({
	fileName: 'test.csv',
	totalDataRows: 10,
	validRows: 5,
	invalidRows: 0,
	createdCount: 5,
	updatedCount: 0,
	deduplicatedInFileCount: 0,
	retailPriceColumnPresent: true,
	ignoredCostPriceColumnDetected: false,
	ignoredPriceColumnsDetected: false,
	completedAtUtc: new Date().toISOString(),
	rowErrorsReturned: 0,
	importCompleted: true,
	rowErrors: [],
})

describe('import-result-status', () => {
	it('returns full success when all valid rows saved', () => {
		const outcome = getImportOutcome(baseResult())
		expect(outcome?.status).toBe('full')
	})

	it('returns partial when import stopped early', () => {
		const outcome = getImportOutcome({
			...baseResult(),
			validRows: 10,
			createdCount: 3,
			updatedCount: 2,
			importCompleted: false,
			errorMessage: 'Stopped at chunk 2',
		})
		expect(outcome?.status).toBe('partial')
		expect(outcome?.label).toBe('Import stopped partway')
	})

	it('returns failed when nothing saved and row errors exist', () => {
		const outcome = getImportOutcome({
			...baseResult(),
			validRows: 0,
			createdCount: 0,
			updatedCount: 0,
			invalidRows: 2,
			rowErrors: [
				{ lineNumber: 2, message: 'bad' },
				{ lineNumber: 3, message: 'bad' },
			],
			rowErrorsReturned: 2,
		})
		expect(outcome?.status).toBe('failed')
	})

	it('summarizes truncated row errors', () => {
		const summary = getRowErrorsSummary({
			...baseResult(),
			invalidRows: 250,
			rowErrorsReturned: 200,
			rowErrors: Array.from({ length: 200 }, (_, i) => ({
				lineNumber: i + 2,
				message: 'err',
			})),
		})
		expect(summary).toContain('200 of 250')
	})
})
