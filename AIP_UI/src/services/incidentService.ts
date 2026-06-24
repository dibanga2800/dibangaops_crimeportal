import { Incident, IncidentStats, RepeatOffenderSearchPayload, RepeatOffenderSearchResponse } from '@/types/incidents'
import { getCurrentCustomerId } from '@/lib/utils'
import { api } from '@/config/api'

export interface IncidentGraphData {
	id: string
	customerId: number
	customerName: string
	siteName: string
	siteId: string
	regionId: string
	regionName: string
	location: string
	officerName: string
	officerRole: string
	officerType: string
	dutyManagerName: string
	dateOfIncident: string
	timeOfIncident: string
	incidentType: string
	type: string
	actionCode: string
	description: string
	incidentDetails?: string
	storeComments?: string
	incidentInvolved: string[]
	stolenItems: Array<{
		id: string
		category: string
		description: string
		productName: string
		cost: number
		quantity: number
		totalAmount: number
		wasRecovered?: boolean
		recoveredQuantity?: number
		recoveredAmount?: number
		lostAmount?: number
	}>
	totalStolenValue: number
	totalRecoveredValue: number
	totalLostValue: number
	totalRecoveredQuantity: number
	totalValueRecovered: number
	value: number
	valueRecovered: number
	quantityRecovered: number
	quantity: number
	amount: number
	total: number
	policeInvolvement: boolean
	urnNumber: string
	crimeRefNumber: string
	policeID: string
	status: 'pending' | 'resolved' | 'in-progress'
	priority: 'low' | 'medium' | 'high'
	actionTaken: string
	evidenceAttached: boolean
	witnessStatements: string[]
	involvedParties: string[]
	reportNumber: string
	offenderName: string
	offenderSex: string
	gender: 'Male' | 'Female' | 'N/A or N/K'
	offenderDOB: string
	offenderPlaceOfBirth: string
	offenderAddress: {
		houseName?: string
		numberAndStreet?: string
		villageOrSuburb?: string
		town?: string
		county?: string
		postCode?: string
	}
	arrestSaveComment: string
	dateInputted: string
	assignedTo: string
	count?: number
	store?: string
}

export interface IncidentGraphResponse {
	success: boolean
	data: {
		incidents: IncidentGraphData[]
		totals: {
			totalValue: number
			totalQuantity: number
			totalIncidents: number
		}
		filters: {
			customerId: number
			regionId?: string
			officerType: string
			graphType: string
			startDate?: string
			endDate?: string
		}
	}
}

export interface IncidentTypeData {
	code: string
	type: string
	count: number
	description: string
	fullName: string
}

export interface IncidentTypesResponse {
	success: boolean
	data: IncidentTypeData[]
}

export interface IncidentGraphFilters {
	customerId: number
	startDate?: string
	endDate?: string
	regionId?: string
	officerType?: string
	graphType?: string
}

interface ApiEnvelope<T> {
	success: boolean
	data: T
	message?: string
}

const customerHeaders = (customerId?: number | string | null): Record<string, string> =>
	customerId != null && customerId !== ''
		? { 'X-Customer-Id': String(customerId) }
		: {}

const unwrap = <T>(result: ApiEnvelope<T>, fallbackMessage: string): T => {
	if (!result.success) {
		throw new Error(result.message || fallbackMessage)
	}
	return result.data
}

export const fetchIncidentGraphData = async (
	filters: IncidentGraphFilters
): Promise<IncidentGraphResponse> => {
	const searchParams = new URLSearchParams()
	searchParams.append('customerId', filters.customerId.toString())
	if (filters.startDate) searchParams.append('startDate', filters.startDate)
	if (filters.endDate) searchParams.append('endDate', filters.endDate)
	if (filters.regionId) searchParams.append('regionId', filters.regionId)
	if (filters.officerType) searchParams.append('officerType', filters.officerType)
	if (filters.graphType) searchParams.append('graphType', filters.graphType)

	const { data } = await api.get<IncidentGraphResponse>(`/incidents/graph-data?${searchParams}`, {
		headers: customerHeaders(filters.customerId),
	})
	return data
}

export const fetchIncidentTypesData = async (
	filters: Omit<IncidentGraphFilters, 'graphType'>
): Promise<IncidentTypesResponse> => {
	const searchParams = new URLSearchParams()
	searchParams.append('customerId', filters.customerId.toString())
	if (filters.startDate) searchParams.append('startDate', filters.startDate)
	if (filters.endDate) searchParams.append('endDate', filters.endDate)
	if (filters.regionId) searchParams.append('regionId', filters.regionId)
	if (filters.officerType) searchParams.append('officerType', filters.officerType)

	const { data } = await api.get<IncidentTypesResponse>(`/incidents/types-summary?${searchParams}`, {
		headers: customerHeaders(filters.customerId),
	})
	return data
}

export const fetchCustomerRegions = async (
	customerId: number
): Promise<{ success: boolean; data: string[] }> => {
	const { data } = await api.get<{ success: boolean; data: string[] }>(
		`/incidents/regions?customerId=${customerId}`,
		{ headers: customerHeaders(customerId) }
	)
	return data
}

export const incidentService = {
	async getIncidents(): Promise<Incident[]> {
		try {
			const customerId = getCurrentCustomerId()
			const { data: result } = await api.get<ApiEnvelope<Incident[]>>('/incidents', {
				headers: customerHeaders(customerId),
			})
			return unwrap(result, 'Failed to fetch incidents')
		} catch (error) {
			console.error('Error fetching incidents:', error)
			throw error
		}
	},

	async getIncidentById(id: string): Promise<Incident> {
		try {
			const customerId = getCurrentCustomerId()
			const { data: result } = await api.get<ApiEnvelope<Incident>>(`/incidents/${id}`, {
				headers: customerHeaders(customerId),
			})
			return unwrap(result, 'Failed to fetch incident')
		} catch (error) {
			console.error('Error fetching incident:', error)
			throw error
		}
	},

	async getIncidentsByCustomer(customerId: string): Promise<Incident[]> {
		try {
			const { data: result } = await api.get<ApiEnvelope<Incident[]>>('/incidents', {
				headers: customerHeaders(customerId),
			})
			return unwrap(result, 'Failed to fetch incidents')
		} catch (error) {
			console.error('Error fetching incidents by customer:', error)
			throw error
		}
	},

	async getIncidentStats(): Promise<IncidentStats> {
		try {
			const customerId = getCurrentCustomerId()
			const { data: result } = await api.get<ApiEnvelope<IncidentStats>>('/incidents/stats', {
				headers: customerHeaders(customerId),
			})
			return unwrap(result, 'Failed to fetch incident stats')
		} catch (error) {
			console.error('Error fetching incident stats:', error)
			throw error
		}
	},

	async createIncident(incident: Partial<Incident>): Promise<void> {
		try {
			const customerId = getCurrentCustomerId()
			const { data: result } = await api.post<ApiEnvelope<unknown>>('/incidents', incident, {
				headers: customerHeaders(customerId),
			})
			unwrap(result, 'Failed to create incident')
		} catch (error) {
			console.error('Error creating incident:', error)
			throw error
		}
	},

	async updateIncident(id: string, incident: Partial<Incident>): Promise<void> {
		try {
			const customerId = getCurrentCustomerId()
			const { data: result } = await api.put<ApiEnvelope<unknown>>(`/incidents/${id}`, incident, {
				headers: customerHeaders(customerId),
			})
			unwrap(result, 'Failed to update incident')
		} catch (error) {
			console.error('Error updating incident:', error)
			throw error
		}
	},

	async deleteIncident(id: string): Promise<void> {
		try {
			const customerId = getCurrentCustomerId()
			const { data: result } = await api.delete<ApiEnvelope<unknown>>(`/incidents/${id}`, {
				headers: customerHeaders(customerId),
			})
			unwrap(result, 'Failed to delete incident')
		} catch (error) {
			console.error('Error deleting incident:', error)
			throw error
		}
	},

	async searchRepeatOffenders(
		payload: RepeatOffenderSearchPayload
	): Promise<RepeatOffenderSearchResponse> {
		const searchParams = new URLSearchParams()
		if (payload.name) searchParams.append('name', payload.name)
		if (payload.dateOfBirth) searchParams.append('dateOfBirth', payload.dateOfBirth)
		if (payload.marks) searchParams.append('marks', payload.marks)
		if (payload.page) searchParams.append('page', payload.page.toString())
		if (payload.pageSize) searchParams.append('pageSize', payload.pageSize.toString())

		const { data: result } = await api.get<RepeatOffenderSearchResponse>(
			`/incidents/repeat-offenders?${searchParams}`
		)
		if (!result.success) {
			throw new Error(result.message || 'Failed to search repeat offenders')
		}
		return result
	},
}
