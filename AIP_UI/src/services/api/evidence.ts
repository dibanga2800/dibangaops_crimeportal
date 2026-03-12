import { api } from '@/config/api'
import type {
	EvidenceItem,
	EvidenceListResponse,
	RegisterEvidencePayload,
	RecordCustodyEventPayload,
	BarcodeScanPayload,
	BarcodeScanResult,
	EvidenceCustodyEvent
} from '@/types/evidence'

export const evidenceApi = {
	async registerEvidence(incidentId: number, payload: RegisterEvidencePayload): Promise<EvidenceItem> {
		const { data } = await api.post<EvidenceItem>(`/Evidence/incidents/${incidentId}/evidence`, payload)
		return data
	},

	async getByIncident(incidentId: number): Promise<EvidenceListResponse> {
		const { data } = await api.get<EvidenceListResponse>(`/Evidence/incidents/${incidentId}`)
		return data
	},

	async getById(evidenceItemId: number): Promise<EvidenceItem> {
		const { data } = await api.get<EvidenceItem>(`/Evidence/${evidenceItemId}`)
		return data
	},

	async scanBarcode(payload: BarcodeScanPayload): Promise<BarcodeScanResult> {
		const { data } = await api.post<BarcodeScanResult>('/Evidence/scan', payload)
		return data
	},

	async recordCustodyEvent(
		evidenceItemId: number,
		payload: RecordCustodyEventPayload
	): Promise<EvidenceCustodyEvent> {
		const { data } = await api.post<EvidenceCustodyEvent>(
			`/Evidence/${evidenceItemId}/custody`,
			payload
		)
		return data
	}
}
