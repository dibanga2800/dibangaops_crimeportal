import type { BarcodeCsvImportResult } from '@/services/productBarcodeCsvImportService'

export type ImportOutcomeStatus = 'full' | 'partial' | 'failed' | null

export interface ImportOutcomeSummary {
	status: ImportOutcomeStatus
	label: string
	description: string
	reconciled: boolean
	processed: number
}

export const getImportOutcome = (result: BarcodeCsvImportResult | null): ImportOutcomeSummary | null => {
	if (!result) return null

	const processed = result.createdCount + result.updatedCount
	const reconciled = processed === result.validRows
	const hasRowIssues = result.invalidRows > 0 || result.rowErrors.length > 0
	const stoppedEarly = result.importCompleted === false

	if (stoppedEarly) {
		return {
			status: 'partial',
			label: 'Import stopped partway',
			description:
				result.errorMessage ??
				`Saved ${processed} of ${result.validRows} unique barcodes before a database error. Earlier chunks were committed.`,
			reconciled: false,
			processed,
		}
	}

	if (processed === 0 && hasRowIssues) {
		return {
			status: 'failed',
			label: 'Import failed',
			description: 'No rows were imported. Review row issues below.',
			reconciled: true,
			processed,
		}
	}

	if (hasRowIssues || !reconciled) {
		return {
			status: 'partial',
			label: 'Partial import',
			description: `${processed} of ${result.validRows} unique barcodes were saved. Some rows were invalid or could not be reconciled.`,
			reconciled,
			processed,
		}
	}

	return {
		status: 'full',
		label: 'Import successful',
		description: `All ${result.validRows} unique barcodes were imported (${result.createdCount} created, ${result.updatedCount} updated).`,
		reconciled: true,
		processed,
	}
}

export const getRowErrorsSummary = (result: BarcodeCsvImportResult): string | null => {
	if (result.invalidRows <= 0) return null
	const shown = result.rowErrorsReturned > 0 ? result.rowErrorsReturned : result.rowErrors.length
	if (result.invalidRows > shown) {
		return `Showing ${shown} of ${result.invalidRows} row issues. Download CSV for the returned set.`
	}
	return null
}

export const downloadRowErrorsCsv = (rowErrors: { lineNumber: number; message: string }[], fileName = 'import-row-errors.csv') => {
	if (rowErrors.length === 0) return

	const escape = (value: string) => `"${value.replace(/"/g, '""')}"`
	const lines = ['lineNumber,message', ...rowErrors.map((r) => `${r.lineNumber},${escape(r.message)}`)]
	const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8' })
	const url = URL.createObjectURL(blob)
	const anchor = document.createElement('a')
	anchor.href = url
	anchor.download = fileName
	anchor.click()
	URL.revokeObjectURL(url)
}
