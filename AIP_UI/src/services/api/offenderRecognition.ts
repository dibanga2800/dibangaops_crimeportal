import { api } from '@/config/api'
import type { RepeatOffenderMatch } from '@/types/incidents'

export interface OffenderImageReferencePayload {
	fileName?: string
	url?: string
}

export interface OffenderMatchCandidate extends RepeatOffenderMatch {
	similarityScore?: number
	embeddingId?: string
}

export interface OffenderMatchResult {
	embeddingId?: string
	faceDetected?: boolean
	matches?: OffenderMatchCandidate[]
	candidates?: OffenderMatchCandidate[]
	totalCount?: number
	classifierVersion?: string
}

export const offenderRecognitionApi = {
	async indexAndMatch(payload: OffenderImageReferencePayload): Promise<OffenderMatchResult> {
		const response = await api.post<OffenderMatchResult>('/OffenderRecognition/index-and-match', payload)
		return response.data
	},

	/**
	 * Search for repeat offenders by captured/uploaded image.
	 * Pass base64 string (with or without data URL prefix) or a data URL.
	 */
	async searchByImage(imageBase64OrDataUrl: string): Promise<OffenderMatchResult> {
		const base64 = imageBase64OrDataUrl.includes(',')
			? imageBase64OrDataUrl.split(',')[1]
			: imageBase64OrDataUrl
		// Azure Face detect + identify can take several seconds; use 30s timeout
		const response = await api.post<OffenderMatchResult>('/OffenderRecognition/search-by-image', {
			imageBase64: base64,
		}, { timeout: 30000 })
		return response.data
	},

	/** Lightweight face detection for guided capture (red/green overlay). */
	async detectOnly(imageBase64OrDataUrl: string): Promise<OffenderMatchResult> {
		const base64 = imageBase64OrDataUrl.includes(',')
			? imageBase64OrDataUrl.split(',')[1]
			: imageBase64OrDataUrl
		// Azure Face API can take several seconds; use 30s timeout
		const response = await api.post<OffenderMatchResult>('/OffenderRecognition/detect-only', {
			imageBase64: base64,
		}, { timeout: 30000 })
		return response.data
	},
}

