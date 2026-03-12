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
	matches: OffenderMatchCandidate[]
	totalCount?: number
}

export const offenderRecognitionApi = {
	async indexAndMatch(payload: OffenderImageReferencePayload): Promise<OffenderMatchResult> {
		const response = await api.post<OffenderMatchResult>('/OffenderRecognition/index-and-match', payload)
		return response.data
	},
}

