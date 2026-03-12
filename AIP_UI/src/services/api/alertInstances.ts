import { api } from '@/config/api'
import type {
	AlertInstance,
	AlertInstanceListResponse,
	AlertSummary,
	AcknowledgeAlertPayload,
	EscalateAlertPayload,
	ResolveAlertPayload
} from '@/types/alertInstances'

export const alertInstancesApi = {
	async getAlerts(params?: {
		status?: string
		severity?: string
		customerId?: number
		page?: number
		pageSize?: number
	}): Promise<AlertInstanceListResponse> {
		const { data } = await api.get<AlertInstanceListResponse>('/alerts', { params })
		return data
	},

	async getById(id: number): Promise<AlertInstance> {
		const { data } = await api.get<AlertInstance>(`/alerts/${id}`)
		return data
	},

	async getSummary(customerId?: number): Promise<AlertSummary> {
		const { data } = await api.get<AlertSummary>('/alerts/summary', {
			params: customerId ? { customerId } : undefined
		})
		return data
	},

	async acknowledge(id: number, payload?: AcknowledgeAlertPayload): Promise<AlertInstance> {
		const { data } = await api.patch<AlertInstance>(`/alerts/${id}/acknowledge`, payload ?? {})
		return data
	},

	async escalate(id: number, payload: EscalateAlertPayload): Promise<AlertInstance> {
		const { data } = await api.patch<AlertInstance>(`/alerts/${id}/escalate`, payload)
		return data
	},

	async resolve(id: number, payload: ResolveAlertPayload): Promise<AlertInstance> {
		const { data } = await api.patch<AlertInstance>(`/alerts/${id}/resolve`, payload)
		return data
	}
}
