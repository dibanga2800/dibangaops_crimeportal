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

const buildAuthHeaders = (): Record<string, string> => {
	const customerId = getCurrentCustomerId()
	return customerId ? { 'X-Customer-Id': customerId.toString() } : {}
}

const buildGraphAnalyticsUrl = (filters: IncidentGraphFilters): string => {
	const p = new URLSearchParams({
		customerId: filters.customerId.toString(),
		officerType: filters.officerType || 'all',
		graphType: filters.graphType || 'value',
	})
	if (filters.startDate) p.append('fromDate', filters.startDate)
	if (filters.endDate) p.append('toDate', filters.endDate)
	if (filters.regionId) p.append('regionId', filters.regionId)
	return `/incidents/graph-analytics?${p.toString()}`
}

const mapLocationToGraphData = (
	location: Record<string, unknown>,
	filters: IncidentGraphFilters,
	graphType: string
): IncidentGraphData => {
	const now = new Date().toISOString().split('T')[0]
	const siteName = String(location.siteName ?? location.location ?? 'Unknown Location')
	const value = Number(location.value ?? 0)
	const lostValue = Number(location.lostValue ?? 0)
	const quantity = Number(location.quantity ?? 0)
	const count = Number(location.count ?? 0)

	const displayValue =
		graphType === 'lost' ? lostValue : graphType === 'quantity' ? quantity : value

	return {
		id: `location-${siteName}`,
		customerId: filters.customerId,
		customerName: String(location.customerName ?? ''),
		siteName,
		siteId: String(location.siteId ?? siteName),
		regionId: String(location.regionId ?? ''),
		regionName: String(location.regionName ?? ''),
		location: String(location.location ?? siteName),
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
		totalStolenValue: 0,
		totalRecoveredValue: value,
		totalLostValue: lostValue,
		totalRecoveredQuantity: quantity,
		totalValueRecovered: value,
		value: displayValue,
		valueRecovered: value,
		quantityRecovered: quantity,
		quantity,
		amount: displayValue,
		total: displayValue,
		policeInvolvement: false,
		urnNumber: '',
		crimeRefNumber: '',
		policeID: '',
		status: 'resolved',
		priority: 'medium',
		actionTaken: '',
		evidenceAttached: false,
		witnessStatements: [],
		involvedParties: [],
		reportNumber: '',
		offenderName: '',
		offenderSex: '',
		gender: 'N/A or N/K',
		offenderDOB: '',
		offenderPlaceOfBirth: '',
		offenderAddress: {},
		arrestSaveComment: '',
		dateInputted: now,
		assignedTo: '',
		count,
	}
}

const mapAnalyticsPayload = (
	payload: Record<string, unknown>,
	filters: IncidentGraphFilters
): { graphResponse: IncidentGraphResponse; types: IncidentTypeData[] } => {
	const graphType = filters.graphType || 'value'
	const locations = Array.isArray(payload.locations) ? payload.locations : []
	const totals = (payload.totals ?? {}) as Record<string, unknown>
	const typesRaw = Array.isArray(payload.types) ? payload.types : []

	const incidents = locations.map((loc) =>
		mapLocationToGraphData(loc as Record<string, unknown>, filters, graphType)
	)

	const typeData: IncidentTypeData[] = typesRaw.map((entry) => {
		const row = entry as Record<string, unknown>
		const type = String(row.type ?? 'Unknown')
		const count = Number(row.count ?? 0)
		return {
			code: type,
			type,
			count,
			description: type,
			fullName: type,
		}
	})

	const graphResponse: IncidentGraphResponse = {
		success: payload.success !== false,
		data: {
			incidents,
			totals: {
				totalValue: Number(totals.totalValue ?? 0),
				totalQuantity: Number(totals.totalQuantity ?? 0),
				totalIncidents: Number(totals.totalIncidents ?? 0),
			},
			filters: {
				customerId: filters.customerId,
				regionId: filters.regionId,
				officerType: filters.officerType || 'all',
				graphType,
				startDate: filters.startDate,
				endDate: filters.endDate,
			},
		},
	}

	return { graphResponse, types: typeData }
}

export const incidentGraphService = {
	async fetchGraphData(filters: IncidentGraphFilters): Promise<IncidentGraphResponse> {
		try {
			const response = await api.get(buildGraphAnalyticsUrl(filters), {
				headers: buildAuthHeaders(),
			})
			const { graphResponse } = mapAnalyticsPayload(response.data ?? {}, filters)
			return graphResponse
		} catch (error: unknown) {
			const message = error instanceof Error ? error.message : 'Failed to fetch graph data'
			console.error('[IncidentGraphService] fetchGraphData error:', message)
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

	async fetchTypesData(
		filters: Omit<IncidentGraphFilters, 'graphType'>
	): Promise<IncidentTypesResponse> {
		try {
			const response = await api.get(
				buildGraphAnalyticsUrl({ ...filters, graphType: 'type' }),
				{ headers: buildAuthHeaders() }
			)
			const { types } = mapAnalyticsPayload(response.data ?? {}, {
				...filters,
				graphType: 'type',
			})
			return { success: true, data: types }
		} catch (error: unknown) {
			const message = error instanceof Error ? error.message : 'Failed to fetch types data'
			console.error('[IncidentGraphService] fetchTypesData error:', message)
			return {
				success: false,
				data: [],
				message,
			}
		}
	},

	async fetchRegions(customerId: number): Promise<{ success: boolean; data: RegionOption[] }> {
		try {
			const result = await regionService.getRegionsByCustomer(customerId)
			if (!result.success) return { success: false, data: [] }

			const data = result.data
				.filter((r) => (r.regionID ?? r.RegionID ?? (r as { id?: number }).id) !== undefined)
				.map((r) => ({
					id: (r.regionID ?? r.RegionID ?? (r as { id?: number }).id)!.toString(),
					name: r.regionName || r.RegionName || 'Unnamed Region',
				}))

			return { success: true, data }
		} catch (error: unknown) {
			const message = error instanceof Error ? error.message : 'Failed to fetch regions'
			console.error('[IncidentGraphService] fetchRegions error:', message)
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
}
