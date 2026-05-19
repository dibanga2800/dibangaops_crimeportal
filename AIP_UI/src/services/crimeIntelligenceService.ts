import { CrimeIntelligenceQuery, CrimeIntelligenceResponse } from '@/types/crimeIntelligence'
import { api } from '@/config/api'
import { getCurrentCustomerId } from '@/lib/utils'

const buildQueryString = (params: CrimeIntelligenceQuery): string => {
	const search = new URLSearchParams()
	search.append('customerId', params.customerId.toString())
	if (params.siteId) search.append('siteId', params.siteId)
	if (params.regionId) search.append('regionId', params.regionId)
	if (params.startDate) search.append('startDate', params.startDate)
	if (params.endDate) search.append('endDate', params.endDate)
	return search.toString()
}

const buildAuthHeaders = (): Record<string, string> => {
	const customerId = getCurrentCustomerId()
	return customerId ? { 'X-Customer-Id': customerId.toString() } : {}
}

const mapInsightsResponse = (data: Record<string, unknown>): CrimeIntelligenceResponse => ({
	success: data.success !== false,
	message: typeof data.message === 'string' ? data.message : undefined,
	heroMetrics: Array.isArray(data.heroMetrics) ? data.heroMetrics : [],
	topIncidentTypes: Array.isArray(data.topIncidentTypes) ? data.topIncidentTypes : [],
	topStores: Array.isArray(data.topStores) ? data.topStores : [],
	topProducts: Array.isArray(data.topProducts) ? data.topProducts : [],
	topRegions: Array.isArray(data.topRegions) ? data.topRegions : [],
	timeBuckets: Array.isArray(data.timeBuckets) ? data.timeBuckets : [],
	hotProduct: data.hotProduct as CrimeIntelligenceResponse['hotProduct'],
	generatedAt: typeof data.generatedAt === 'string' ? data.generatedAt : new Date().toISOString(),
})

export const crimeIntelligenceService = {
	async getInsights(query: CrimeIntelligenceQuery): Promise<CrimeIntelligenceResponse> {
		const qs = buildQueryString(query)
		const response = await api.get(`/incidents/insights?${qs}`, {
			headers: buildAuthHeaders(),
		})
		return mapInsightsResponse(response.data ?? {})
	},
}

export type CrimeIntelligenceService = typeof crimeIntelligenceService
