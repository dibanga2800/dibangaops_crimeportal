/**
 * Analytics Service
 *
 * Fetches comprehensive analytics data from the real backend API.
 * The API response shape is designed to match the frontend types directly,
 * so no transformation layer is needed — axios deserialises into the types as-is.
 */

import { api, ANALYTICS_ENDPOINTS } from '@/config/api'
import type {
	AnalyticsHubData,
	CrimeTrendData,
	HotProductsData,
	RepeatOffenderData,
	DeploymentRecommendation,
	CrimeLinkingData,
} from '@/types/analytics'

export interface StoreOption {
	id: number | string
	name: string
}

export interface RegionOption {
	id: number | string
	name: string
}

export interface AnalyticsQueryParams {
	customerId?: number
	startDate?: string
	endDate?: string
	storeIds?: number[]
	regionIds?: number[]
	/** Client-side only — not sent to the backend */
	stores?: StoreOption[]
	/** Client-side only — not sent to the backend */
	regions?: RegionOption[]
}

class AnalyticsService {
	/**
	 * Get complete analytics hub data from the real backend.
	 */
	async getAnalyticsHub(params?: AnalyticsQueryParams): Promise<AnalyticsHubData> {
		const query: Record<string, string | number | undefined> = {}

		if (params?.customerId !== undefined) query.customerId = params.customerId
		if (params?.startDate) query.from = params.startDate
		if (params?.endDate) query.to = params.endDate

		// The backend accepts a single siteId / regionId filter for now.
		// When multiple IDs are supplied we pass the first one; full multi-select
		// can be wired later once the repository supports it.
		if (params?.storeIds?.length) query.siteId = params.storeIds[0]
		if (params?.regionIds?.length) query.regionId = params.regionIds[0]

		const { data } = await api.get<AnalyticsHubData>(ANALYTICS_ENDPOINTS.HUB, {
			params: query,
		})

		return data
	}

	/**
	 * Get crime trend analytics (extracted slice from hub data).
	 */
	async getCrimeTrends(params?: AnalyticsQueryParams): Promise<CrimeTrendData> {
		const data = await this.getAnalyticsHub(params)
		return data.crimeTrends
	}

	/**
	 * Get hot products analytics (extracted slice from hub data).
	 */
	async getHotProducts(params?: AnalyticsQueryParams): Promise<HotProductsData> {
		const data = await this.getAnalyticsHub(params)
		return data.hotProducts
	}

	/**
	 * Get repeat offender analytics (extracted slice from hub data).
	 */
	async getRepeatOffenders(params?: AnalyticsQueryParams): Promise<RepeatOffenderData> {
		const data = await this.getAnalyticsHub(params)
		return data.repeatOffenders
	}

	/**
	 * Get deployment recommendations (extracted slice from hub data).
	 */
	async getDeploymentRecommendations(
		params?: AnalyticsQueryParams
	): Promise<DeploymentRecommendation> {
		const data = await this.getAnalyticsHub(params)
		return data.deploymentRecommendations
	}

	/**
	 * Get crime linking data (extracted slice from hub data).
	 */
	async getCrimeLinking(params?: AnalyticsQueryParams): Promise<CrimeLinkingData> {
		const data = await this.getAnalyticsHub(params)
		return data.crimeLinking
	}
}

export const analyticsService = new AnalyticsService()
