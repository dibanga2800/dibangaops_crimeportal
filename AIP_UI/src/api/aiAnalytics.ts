import { api, ANALYTICS_ENDPOINTS, ApiResponse } from '@/config/api'

export interface StoreRiskScore {
	storeRiskScoreId: number
	customerId: number
	storeId: number | null
	siteId: string | null
	storeName: string
	forDate: string
	score: number
	level: string
	expectedIncidentsMin: number | null
	expectedIncidentsMax: number | null
	peakRiskWindows: string | null
	modelVersion: string
	generatedAt: string
}

export interface IncidentPatternSummary {
	hotLocations: string[] | null
	incidentTrend: Record<string, number> | null
	categoryBreakdown: Record<string, number> | null
	generatedAt: string
}

export const fetchStoreRiskScores = async (customerId: number, date?: string) => {
	const params: Record<string, string | number> = { customerId }
	if (date) {
		params.date = date
	}

	const response = await api.get<ApiResponse<StoreRiskScore[]>>(
		ANALYTICS_ENDPOINTS.AI_RISK_SCORES,
		{ params }
	)

	return response.data.data
}

export const fetchIncidentPatterns = async (options: {
	customerId?: number
	siteId?: string
	regionId?: string
	from?: string
	to?: string
}) => {
	const response = await api.get<ApiResponse<IncidentPatternSummary>>(
		ANALYTICS_ENDPOINTS.AI_PATTERNS,
		{ params: options }
	)

	return response.data.data
}

