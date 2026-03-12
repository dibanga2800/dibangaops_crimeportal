import { api } from '@/config/api'
import type {
	IncidentClassificationRequest,
	IncidentClassificationResult,
	IncidentAnalyticsSummary
} from '@/types/classification'

export const classificationApi = {
	async classifyIncident(request: IncidentClassificationRequest): Promise<IncidentClassificationResult> {
		const { data } = await api.post<IncidentClassificationResult>('/Classification/classify', request)
		return data
	},

	async classifyExistingIncident(incidentId: number): Promise<IncidentClassificationResult> {
		const { data } = await api.post<IncidentClassificationResult>(`/Classification/classify/${incidentId}`)
		return data
	},

	async getAnalyticsSummary(params?: {
		customerId?: number
		siteId?: string
		regionId?: string
		from?: string
		to?: string
	}): Promise<IncidentAnalyticsSummary> {
		const { data } = await api.get<IncidentAnalyticsSummary>('/Analytics/summary', { params })
		return data
	}
}
