import { Incident } from './incidents'

export interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
  errors?: string[]
}

export interface PaginationInfo {
  currentPage: number
  totalPages: number
  pageSize: number
  totalCount: number
  hasPrevious: boolean
  hasNext: boolean
}

export interface PaginatedResponse<T> extends ApiResponse<T> {
  pagination: PaginationInfo
}

export interface IncidentListSummary {
	totalIncidents: number
	totalAmountRecovered: number
	totalAmountLost: number
	uniqueSites: number
	/**
	 * Server-side counts computed across the full filtered set (not just
	 * the paginated page), so dashboards can render headline cards that
	 * reconcile with `totalIncidents` regardless of the backend's page-size
	 * clamp. All optional for backwards compatibility with older servers.
	 */
	shopliftingIncidents?: number
	todayIncidents?: number
	highPriorityIncidents?: number
	pendingIncidents?: number
	resolvedIncidents?: number
}

export type IncidentResponse = ApiResponse<Incident>
export type IncidentsResponse = PaginatedResponse<Incident[]> & {
	summary?: IncidentListSummary
}

// Query parameters for incidents
export interface GetIncidentsParams {
  page?: number
  pageSize?: number
  search?: string
  fromDate?: string
  toDate?: string
  incidentType?: string
  siteName?: string
  siteId?: string
  regionId?: string
  status?: string
  customerId?: string
}

// Create/Update incident request
export interface UpsertIncidentRequest {
  incident: Omit<Incident, 'id' | 'dateInputted'>
} 