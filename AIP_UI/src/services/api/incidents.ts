import { Incident } from '@/types/incidents'
import { GetIncidentsParams, IncidentResponse, IncidentsResponse, UpsertIncidentRequest } from '@/types/api'
import { getCurrentCustomerId } from '@/lib/utils'
import { api } from '@/config/api'

// Helper function to get additional headers (auth is handled by axios interceptor)
const getAdditionalHeaders = (): Record<string, string> => {
  const customerId = getCurrentCustomerId()
  const headers: Record<string, string> = {}
  
  // Add customer ID header if available
  if (customerId) {
    headers['X-Customer-Id'] = customerId.toString()
  }
  
  return headers
}

export const incidentsApi = {
  // Get paginated incidents
  getIncidents: async (params?: GetIncidentsParams): Promise<IncidentsResponse> => {
    try {
      const response = await api.get('/incidents', {
        params,
        headers: getAdditionalHeaders()
      })
      return response.data
    } catch (error) {
      console.error('❌ [Incidents API] Failed to fetch incidents:', error)
      throw error
    }
  },

  // Get single incident
  getIncident: async (id: string): Promise<IncidentResponse> => {
    try {
      const response = await api.get(`/incidents/${id}`, {
        headers: getAdditionalHeaders()
      })
      return response.data
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error(`Incident with ID ${id} not found`)
      }
      console.error('❌ [Incidents API] Failed to fetch incident:', error)
      throw error
    }
  },

  // Create new incident
  createIncident: async (incident: Omit<Incident, 'id' | 'dateInputted'>): Promise<IncidentResponse> => {
    console.log('🚀 [Incidents API] Creating incident - POST request starting...')
    console.log('📦 Payload:', { incident })
    
    try {
      const response = await api.post('/incidents', 
        { incident } as UpsertIncidentRequest,
        { headers: getAdditionalHeaders() }
      )
      
      console.log('✅ [Incidents API] Incident created successfully:', response.data)
      return response.data
    } catch (error) {
      console.error('❌ [Incidents API] Failed to create incident:', error)
      throw error
    }
  },

  // Update incident
  updateIncident: async (id: string, incident: Omit<Incident, 'id' | 'dateInputted'>): Promise<IncidentResponse> => {
    console.log('🔄 [Incidents API] Updating incident - PUT request starting...')
    console.log('📦 Payload:', { id, incident })
    
    try {
      const response = await api.put(`/incidents/${id}`, 
        { incident } as UpsertIncidentRequest,
        { headers: getAdditionalHeaders() }
      )
      
      console.log('✅ [Incidents API] Incident updated successfully:', response.data)
      return response.data
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error(`Incident with ID ${id} not found`)
      }
      console.error('❌ [Incidents API] Failed to update incident:', error)
      throw error
    }
  },

  // Delete incident
  deleteIncident: async (id: string): Promise<void> => {
    try {
      await api.delete(`/incidents/${id}`, {
        headers: getAdditionalHeaders()
      })
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error(`Incident with ID ${id} not found`)
      }
      console.error('❌ [Incidents API] Failed to delete incident:', error)
      throw error
    }
  },
} 