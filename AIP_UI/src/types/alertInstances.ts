export interface AlertInstance {
	alertInstanceId: number
	alertRuleId: number
	alertRuleName?: string
	incidentId?: number
	severity: 'low' | 'medium' | 'high'
	status: AlertStatus
	message?: string
	matchDetails?: string
	createdAt: string
	acknowledgedAt?: string
	acknowledgedBy?: string
	escalatedAt?: string
	escalatedTo?: string
	resolvedAt?: string
	resolvedBy?: string
	resolutionNotes?: string
	escalationLevel: number
}

export type AlertStatus = 'new' | 'acknowledged' | 'escalated' | 'resolved'

export interface AlertInstanceListResponse {
	success: boolean
	data: AlertInstance[]
	pagination: {
		currentPage: number
		totalPages: number
		pageSize: number
		totalCount: number
		hasPrevious: boolean
		hasNext: boolean
	}
}

export interface AlertSummary {
	totalActive: number
	newCount: number
	acknowledgedCount: number
	escalatedCount: number
	resolvedTodayCount: number
	recentAlerts: AlertInstance[]
}

export interface AcknowledgeAlertPayload {
	notes?: string
}

export interface EscalateAlertPayload {
	escalateTo: string
	notes?: string
}

export interface ResolveAlertPayload {
	resolutionNotes?: string
}
