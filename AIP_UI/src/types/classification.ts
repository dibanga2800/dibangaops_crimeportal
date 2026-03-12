export interface IncidentClassificationRequest {
	incidentId: number
	incidentType: string
	description?: string
	incidentDetails?: string
	totalValueRecovered?: number
	policeInvolvement: boolean
	offenderName?: string
	incidentInvolved?: string[]
	stolenItemCount: number
}

export interface IncidentClassificationResult {
	suggestedCategory: string
	riskLevel: 'low' | 'medium' | 'high'
	riskScore: number
	confidence: number
	suggestedActions: string[]
	tags: string[]
	classifierVersion: string
}

export interface IncidentAnalyticsSummary {
	totalIncidents: number
	totalValueAtRisk: number
	repeatOffenderCount: number
	hotLocations: HotLocation[]
	incidentTrend: TrendDataPoint[]
	categoryBreakdown: CategoryBreakdown[]
	riskIndicators: RiskIndicator[]
	generatedAt: string
}

export interface HotLocation {
	siteName: string
	regionName?: string
	incidentCount: number
	totalValue: number
	riskScore: number
}

export interface TrendDataPoint {
	period: string
	count: number
	value: number
}

export interface CategoryBreakdown {
	category: string
	count: number
	percentage: number
	totalValue: number
}

export interface RiskIndicator {
	indicator: string
	level: 'low' | 'medium' | 'high'
	score: number
	description?: string
}
