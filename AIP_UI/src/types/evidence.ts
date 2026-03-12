export interface EvidenceItem {
	evidenceItemId: number
	incidentId: number
	barcode: string
	evidenceType: string
	description?: string
	storageLocation?: string
	status: string
	registeredAt: string
	registeredBy?: string
	custodyEvents: EvidenceCustodyEvent[]
}

export interface EvidenceCustodyEvent {
	custodyEventId: number
	eventType: string
	notes?: string
	location?: string
	eventTimestamp: string
	performedBy: string
	performedByName?: string
}

export interface RegisterEvidencePayload {
	barcode: string
	evidenceType: string
	description?: string
	storageLocation?: string
}

export interface RecordCustodyEventPayload {
	eventType: string
	notes?: string
	location?: string
}

export interface BarcodeScanPayload {
	barcode: string
	scanLocation?: string
}

export interface BarcodeScanResult {
	found: boolean
	evidenceItem?: EvidenceItem
	incidentId?: number
	incidentType?: string
	siteName?: string
	message: string
}

export interface EvidenceListResponse {
	success: boolean
	data: EvidenceItem[]
	totalCount: number
}

export type EvidenceEventType =
	| 'registered'
	| 'transferred'
	| 'received'
	| 'scanned'
	| 'released'
	| 'disposed'
	| 'returned'

export type EvidenceStatus =
	| 'registered'
	| 'in-transit'
	| 'in-storage'
	| 'released'
	| 'disposed'
	| 'returned'
