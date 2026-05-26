import { PageHeaderData } from '@/types/header'
import { api } from '@/config/api'

export const headerService = {
	getHeaderData: async (pageId: string): Promise<PageHeaderData> => {
		try {
			const { data } = await api.get<PageHeaderData>(`/headers/${pageId}`)
			return data
		} catch (error) {
			console.error('Error fetching header data:', error)
			throw error
		}
	},

	updateHeaderData: async (pageId: string, data: PageHeaderData): Promise<PageHeaderData> => {
		try {
			const { data: response } = await api.put<PageHeaderData>(`/headers/${pageId}`, data)
			return response
		} catch (error) {
			console.error('Error updating header data:', error)
			throw error
		}
	},

	deleteHeaderData: async (pageId: string): Promise<void> => {
		try {
			await api.delete(`/headers/${pageId}`)
		} catch (error) {
			console.error('Error deleting header data:', error)
			throw error
		}
	},
}
