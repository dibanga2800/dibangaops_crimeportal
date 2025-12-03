import { api, BASE_API_URL } from '@/config/api'
import type { StoreAlertRule, LPMAlertRule } from '@/types/alertRules'

export interface AlertRuleResponse {
	success: boolean
	data: any
	message: string
}

export interface AlertRuleListResponse {
	success: boolean
	data: {
		data: any[]
		total: number
		page: number
		pageSize: number
		totalPages: number
	}
	message: string
}

class AlertRuleService {
	private baseUrl = `${BASE_API_URL}/alert-rules`

	/**
	 * Get paginated list of alert rules
	 */
	async getAlertRules(params?: {
		page?: number
		pageSize?: number
		search?: string
		ruleType?: 'Store' | 'LPM'
		isActive?: boolean
		customerId?: number
	}): Promise<AlertRuleListResponse> {
		try {
			const response = await api.get(this.baseUrl, { params })
			
			// Map backend field names to frontend (AlertRuleId -> id)
			if (response.data.success && response.data.data?.data) {
				response.data.data.data = response.data.data.data.map((rule: any) => ({
					...rule,
					id: rule.alertRuleId || rule.AlertRuleId || rule.id,
				}))
			}
			
			console.log('✅ Fetched alert rules:', response.data.data?.data?.length || 0)
			return response.data
		} catch (error: any) {
			console.error('Failed to fetch alert rules:', error)
			throw new Error(error.response?.data?.message || 'Failed to fetch alert rules')
		}
	}

	/**
	 * Get alert rule by ID
	 */
	async getAlertRuleById(id: number): Promise<AlertRuleResponse> {
		try {
			const response = await api.get(`${this.baseUrl}/${id}`)
			return response.data
		} catch (error: any) {
			console.error(`Failed to fetch alert rule ${id}:`, error)
			throw new Error(error.response?.data?.message || 'Failed to fetch alert rule')
		}
	}

	/**
	 * Create a new store alert rule
	 */
	async createStoreAlertRule(rule: StoreAlertRule): Promise<AlertRuleResponse> {
		try {
			const payload = {
				name: rule.name,
				ruleType: 'Store',
				keywords: rule.keywords,
				incidentTypes: rule.incidentTypes,
				storeRadius: rule.storeRadius,
				triggerCondition: rule.triggerCondition,
				channels: rule.channels,
				emailRecipients: [], // Store rules typically notify nearby stores
				isActive: rule.isActive,
				customerId: null,
				siteId: null
			}

			const response = await api.post(this.baseUrl, payload)
			console.log('✅ Store alert rule created:', response.data)
			return response.data
		} catch (error: any) {
			console.error('Failed to create store alert rule:', error)
			throw new Error(error.response?.data?.message || 'Failed to create store alert rule')
		}
	}

	/**
	 * Create a new LPM alert rule
	 */
	async createLPMAlertRule(rule: LPMAlertRule): Promise<AlertRuleResponse> {
		try {
			const payload = {
				name: rule.name,
				ruleType: 'LPM',
				keywords: rule.keywords,
				incidentTypes: rule.incidentTypes,
				lpmRegion: rule.lpmRegion,
				triggerCondition: rule.triggerCondition,
				channels: rule.channels,
				emailRecipients: rule.emailRecipients,
				isActive: rule.isActive,
				customerId: null,
				siteId: null
			}

			const response = await api.post(this.baseUrl, payload)
			console.log('✅ LPM alert rule created:', response.data)
			return response.data
		} catch (error: any) {
			console.error('Failed to create LPM alert rule:', error)
			throw new Error(error.response?.data?.message || 'Failed to create LPM alert rule')
		}
	}

	/**
	 * Update an existing alert rule
	 */
	async updateAlertRule(id: number, rule: StoreAlertRule | LPMAlertRule): Promise<AlertRuleResponse> {
		try {
			// Determine if it's a Store or LPM rule based on the presence of fields
			const isStoreRule = 'storeRadius' in rule || !('lpmRegion' in rule)
			
			const payload = {
				name: rule.name,
				ruleType: isStoreRule ? 'Store' : 'LPM',
				keywords: rule.keywords,
				incidentTypes: rule.incidentTypes,
				...(isStoreRule ? { storeRadius: (rule as StoreAlertRule).storeRadius } : {}),
				...(!isStoreRule ? { lpmRegion: (rule as LPMAlertRule).lpmRegion } : { lpmRegion: (rule as any).lpmRegion }),
				triggerCondition: rule.triggerCondition,
				channels: rule.channels,
				emailRecipients: rule.emailRecipients || [],
				isActive: rule.isActive,
			}

			console.log('🔄 Updating alert rule:', { id, ruleType: payload.ruleType, payload })
			const response = await api.put(`${this.baseUrl}/${id}`, payload)
			console.log('✅ Alert rule updated:', response.data)
			return response.data
		} catch (error: any) {
			console.error(`Failed to update alert rule ${id}:`, error)
			throw new Error(error.response?.data?.message || 'Failed to update alert rule')
		}
	}

	/**
	 * Delete an alert rule
	 */
	async deleteAlertRule(id: number): Promise<AlertRuleResponse> {
		try {
			console.log('🗑️ Deleting alert rule:', id)
			const response = await api.delete(`${this.baseUrl}/${id}`)
			console.log('✅ Alert rule deleted:', response.data)
			return response.data
		} catch (error: any) {
			console.error(`❌ Failed to delete alert rule ${id}:`, error.response?.data || error.message)
			throw new Error(error.response?.data?.message || 'Failed to delete alert rule')
		}
	}

	/**
	 * Toggle alert rule active status
	 */
	async toggleAlertRule(id: number, isActive: boolean): Promise<AlertRuleResponse> {
		try {
			console.log('🔄 Toggling alert rule:', { id, isActive })
			// Backend expects just the boolean value, not an object
			const response = await api.patch(`${this.baseUrl}/${id}/toggle`, isActive, {
				headers: {
					'Content-Type': 'application/json'
				}
			})
			console.log('✅ Alert rule toggled:', response.data)
			return response.data
		} catch (error: any) {
			console.error(`Failed to toggle alert rule ${id}:`, error.response?.data || error.message)
			throw new Error(error.response?.data?.message || 'Failed to toggle alert rule')
		}
	}

	/**
	 * Manually trigger alert check for a specific incident
	 */
	async checkIncidentForAlerts(incidentId: number): Promise<AlertRuleResponse> {
		try {
			const response = await api.post(`${this.baseUrl}/check-incident/${incidentId}`)
			console.log('✅ Alert check triggered for incident:', incidentId)
			return response.data
		} catch (error: any) {
			console.error(`Failed to check incident ${incidentId} for alerts:`, error)
			throw new Error(error.response?.data?.message || 'Failed to check incident for alerts')
		}
	}
}

export const alertRuleService = new AlertRuleService()
