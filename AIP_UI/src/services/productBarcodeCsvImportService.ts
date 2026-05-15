import { BASE_API_URL } from '@/config/api'
import { sessionStore } from '@/state/sessionStore'

export interface BarcodeCsvImportRowError {
	lineNumber: number
	message: string
}

export interface BarcodeCsvImportResult {
	fileName: string
	totalDataRows: number
	validRows: number
	invalidRows: number
	createdCount: number
	updatedCount: number
	deduplicatedInFileCount: number
	retailPriceColumnPresent: boolean
	ignoredCostPriceColumnDetected: boolean
	ignoredPriceColumnsDetected: boolean
	completedAtUtc: string
	rowErrorsReturned: number
	importCompleted: boolean
	failedAtChunk?: number | null
	errorMessage?: string | null
	ignoredExtraHeaders?: string[]
	rowErrors: BarcodeCsvImportRowError[]
}

interface ApiEnvelope<T> {
	success: boolean
	message: string
	data?: T
}

const IMPORT_TIMEOUT_MS = 120_000

const mapHttpError = (status: number, message: string): string => {
	if (status === 401) return 'You must sign in again to import the catalog.'
	if (status === 403) return 'You do not have permission to import the barcode catalog.'
	return message
}

export const productBarcodeCsvImportService = {
	importCsv: async (file: File): Promise<BarcodeCsvImportResult> => {
		const token = sessionStore.getToken()
		const form = new FormData()
		form.append('file', file)

		let response: Response
		try {
			response = await fetch(`${BASE_API_URL}/ProductImport/barcode-csv`, {
				method: 'POST',
				headers: {
					...(token ? { Authorization: `Bearer ${token}` } : {}),
				},
				body: form,
				signal: AbortSignal.timeout(IMPORT_TIMEOUT_MS),
			})
		} catch (error) {
			if (error instanceof DOMException && error.name === 'TimeoutError') {
				throw new Error('Import timed out after 2 minutes. Try a smaller file or retry later.')
			}
			throw error
		}

		const payload = (await response.json()) as ApiEnvelope<BarcodeCsvImportResult> & { message?: string }

		if (!response.ok) {
			const msg =
				typeof payload.message === 'string' && payload.message.length > 0
					? payload.message
					: `Import failed (${response.status})`
			throw new Error(mapHttpError(response.status, msg))
		}

		if (!payload.data) {
			const msg =
				typeof payload.message === 'string' && payload.message.length > 0
					? payload.message
					: 'Import failed: no result returned.'
			throw new Error(mapHttpError(response.status, msg))
		}

		if (!payload.success && !payload.data.importCompleted) {
			return payload.data
		}

		if (!payload.success) {
			const msg =
				typeof payload.message === 'string' && payload.message.length > 0
					? payload.message
					: 'Import failed.'
			throw new Error(msg)
		}

		return payload.data
	},
}
