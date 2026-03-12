import {
	IncidentGraphData,
	IncidentGraphResponse,
	IncidentTypeData,
	IncidentTypesResponse,
	IncidentGraphFilters,
} from '@/services/incidentService'
import { api } from '@/config/api'
import { regionService } from '@/services/regionService'
import { getCurrentCustomerId } from '@/lib/utils'

export interface RegionOption {
	id: string
	name: string
}

// ── Helpers ──────────────────────────────────────────────────────────────────

const extractRegionId = (incident: any): string | undefined =>
	incident.regionId?.toString() ||
	incident.RegionId?.toString() ||
	incident.regionID?.toString() ||
	incident.RegionID?.toString()

const getIncidentValue = (inc: any): number => {
	const raw =
		inc.totalValueRecovered ??
		inc.TotalValueRecovered ??
		inc.value ??
		inc.Value ??
		inc.valueRecovered ??
		inc.ValueRecovered ??
		0
	return typeof raw === 'number' ? raw : parseFloat(raw) || 0
}

const buildAuthHeaders = (): Record<string, string> => {
	const customerId = getCurrentCustomerId()
	return customerId ? { 'X-Customer-Id': customerId.toString() } : {}
}

const buildIncidentsUrl = (filters: Omit<IncidentGraphFilters, 'graphType'>): string => {
	const p = new URLSearchParams({
		page: '1',
		pageSize: '1000',
		customerId: filters.customerId.toString(),
	})
	if (filters.startDate) p.append('fromDate', filters.startDate)
	if (filters.endDate) p.append('toDate', filters.endDate)
	return `/incidents?${p.toString()}`
}

const passesOfficerFilter = (incident: any, officerType: string | undefined): boolean => {
	if (!officerType || officerType === 'all') return true
	const role: string = incident.officerRole || incident.OfficerRole || ''
	if (!role) return false
	if (officerType === 'uniform') return role === 'Uniform Officer'
	if (officerType === 'detective') return role === 'Store Detective'
	return true
}

// ── Service ───────────────────────────────────────────────────────────────────

export const incidentGraphService = {
	/**
	 * Fetch incident graph data with client-side aggregation by location.
	 */
	async fetchGraphData(filters: IncidentGraphFilters): Promise<IncidentGraphResponse> {
		const url = buildIncidentsUrl(filters)

		try {
			const response = await api.get(url, { headers: buildAuthHeaders() })
			const incidents: any[] = response.data.data || []

			const grouped = new Map<string, { value: number; count: number; quantity: number }>()

			for (const incident of incidents) {
				const location: string =
					incident.siteName || incident.location || incident.siteId || 'Unknown Location'
				if (!location) continue

				if (filters.regionId) {
					const rid = extractRegionId(incident)
					if (!rid || rid !== filters.regionId) continue
				}

				if (!passesOfficerFilter(incident, filters.officerType)) continue

				const existing = grouped.get(location) ?? { value: 0, count: 0, quantity: 0 }
				existing.count += 1

				if (filters.graphType === 'value') {
					existing.value += getIncidentValue(incident)
				} else if (filters.graphType === 'quantity') {
					const qty =
						incident.quantityRecovered ??
						incident.QuantityRecovered ??
						incident.quantity ??
						1
					existing.quantity += qty
					existing.value += qty
				} else {
					existing.value += 1
				}

				grouped.set(location, existing)
			}

			let totalValue = 0
			let totalQuantity = 0
			let totalIncidents = 0
			const graphData: IncidentGraphData[] = []
			const now = new Date().toISOString().split('T')[0]

			grouped.forEach((data, location) => {
				const sample = incidents.find(
					(i: any) => (i.siteName || i.location || i.siteId) === location
				)

				graphData.push({
					id: `location-${location}`,
					customerId: filters.customerId,
					customerName: sample?.customerName || '',
					siteName: location,
					siteId: sample?.siteId || location,
					regionId: sample?.regionId || '',
					regionName: sample?.regionName || '',
					location,
					officerName: '',
					officerRole: '',
					officerType: '',
					dutyManagerName: '',
					dateOfIncident: now,
					timeOfIncident: '',
					date: now,
					incidentType: '',
					type: '',
					actionCode: '',
					description: '',
					incidentInvolved: [],
					stolenItems: [],
					totalValueRecovered: data.value,
					value: data.value,
					valueRecovered: data.value,
					quantityRecovered: data.quantity,
					quantity: data.quantity,
					amount: data.value,
					total: data.value,
					policeInvolvement: false,
					urnNumber: '',
					crimeRefNumber: '',
					policeID: '',
					status: 'resolved' as const,
					priority: 'medium' as const,
					actionTaken: '',
					evidenceAttached: false,
					witnessStatements: [],
					involvedParties: [],
					reportNumber: '',
					offenderName: '',
					offenderSex: '',
					gender: 'N/A or N/K' as const,
					offenderDOB: '',
					offenderPlaceOfBirth: '',
					offenderAddress: {},
					arrestSaveComment: '',
					dateInputted: now,
					assignedTo: '',
					count: data.count,
				})

				totalValue += data.value
				totalQuantity += data.quantity || data.count
				totalIncidents += data.count
			})

			graphData.sort((a, b) => b.value - a.value)

			return {
				success: true,
				data: {
					incidents: graphData,
					totals: { totalValue, totalQuantity, totalIncidents },
					filters: {
						customerId: filters.customerId,
						regionId: filters.regionId,
						officerType: filters.officerType || 'all',
						graphType: filters.graphType || 'value',
						startDate: filters.startDate,
						endDate: filters.endDate,
					},
				},
			}
		} catch (error: any) {
			console.error('[IncidentGraphService] fetchGraphData error:', error?.message)
			return {
				success: false,
				data: {
					incidents: [],
					totals: { totalValue: 0, totalQuantity: 0, totalIncidents: 0 },
					filters: {
						customerId: filters.customerId,
						regionId: filters.regionId,
						officerType: filters.officerType || 'all',
						graphType: filters.graphType || 'value',
						startDate: filters.startDate,
						endDate: filters.endDate,
					},
				},
			}
		}
	},

	/**
	 * Fetch incident types summary with client-side aggregation.
	 */
	async fetchTypesData(
		filters: Omit<IncidentGraphFilters, 'graphType'>
	): Promise<IncidentTypesResponse> {
		try {
			const response = await api.get(buildIncidentsUrl(filters), { headers: buildAuthHeaders() })
			const incidents: any[] = response.data.data || []
			const typeMap = new Map<string, number>()

			for (const incident of incidents) {
				if (filters.regionId) {
					const rid = extractRegionId(incident)
					if (!rid || rid !== filters.regionId) continue
				}
				if (!passesOfficerFilter(incident, filters.officerType)) continue

				const type: string = incident.incidentType || 'Unknown'
				typeMap.set(type, (typeMap.get(type) ?? 0) + 1)
			}

			const typeData: IncidentTypeData[] = Array.from(typeMap.entries())
				.map(([type, count]) => ({ type, count }))
				.sort((a, b) => b.count - a.count)

			return { success: true, data: typeData }
		} catch (error: any) {
			console.error('[IncidentGraphService] fetchTypesData error:', error?.message)
			return {
				success: false,
				data: [],
				message: error instanceof Error ? error.message : 'Failed to fetch types data',
			}
		}
	},

	/**
	 * Fetch available regions for a customer.
	 */
	async fetchRegions(customerId: number): Promise<{ success: boolean; data: RegionOption[] }> {
		try {
			const result = await regionService.getRegionsByCustomer(customerId)
			if (!result.success) return { success: false, data: [] }

			const data = result.data
				.filter(r => (r.regionID ?? r.RegionID ?? (r as any).id) !== undefined)
				.map(r => ({
					id: (r.regionID ?? r.RegionID ?? (r as any).id).toString(),
					name: r.regionName || r.RegionName || 'Unnamed Region',
				}))

			return { success: true, data }
		} catch (error: any) {
			console.error('[IncidentGraphService] fetchRegions error:', error?.message)
			return { success: false, data: [] }
		}
	},
}

export type {
	IncidentGraphData,
	IncidentGraphResponse,
	IncidentTypeData,
	IncidentTypesResponse,
	IncidentGraphFilters,
	RegionOption,
}
