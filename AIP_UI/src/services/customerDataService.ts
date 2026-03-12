import { incidentService } from './incidentService'
import { customerService } from './customerService'
import { Incident } from '@/types/incidents'

export interface CustomerData {
	customerId: string
	customerName: string
	totalIncidents: number
	totalValue: number
	topIncidentTypes: Array<{ type: string; count: number }>
	monthlyTrend: Array<{ month: string; incidents: number; value: number }>
}

export const getCustomerData = async (customerId: string): Promise<CustomerData> => {
	const [incidents, customer] = await Promise.all([
		incidentService.getIncidentsByCustomer(customerId),
		customerService.getCustomer(customerId),
	])

	const customerName = customer?.companyName || 'Unknown Customer'
	const totalIncidents = incidents.length
	const totalValue = incidents.reduce((sum, inc) => sum + inc.value, 0)

	const incidentTypeCount: Record<string, number> = {}
	incidents.forEach(inc => {
		incidentTypeCount[inc.incidentType] = (incidentTypeCount[inc.incidentType] || 0) + 1
	})

	const topIncidentTypes = Object.entries(incidentTypeCount)
		.map(([type, count]) => ({ type, count }))
		.sort((a, b) => b.count - a.count)
		.slice(0, 5)

	const monthlyData: Record<string, { incidents: number; value: number }> = {}
	incidents.forEach(inc => {
		const month = inc.date.substring(0, 7)
		if (!monthlyData[month]) monthlyData[month] = { incidents: 0, value: 0 }
		monthlyData[month].incidents++
		monthlyData[month].value += inc.value
	})

	const monthlyTrend = Object.entries(monthlyData)
		.map(([month, data]) => ({ month, ...data }))
		.sort((a, b) => a.month.localeCompare(b.month))

	return { customerId, customerName, totalIncidents, totalValue, topIncidentTypes, monthlyTrend }
}

export const getAllCustomersData = async (): Promise<CustomerData[]> => {
	const customers = await customerService.getAllCustomers()
	return Promise.all(customers.map(c => getCustomerData(String(c.id))))
}

export class CustomerDataService {

	static async getCustomerInfo(customerId: string) {
		return customerService.getCustomer(customerId)
	}

	static async getCustomerIncidents(customerId: string): Promise<Incident[]> {
		return incidentService.getIncidentsByCustomer(customerId)
	}

	static async getCustomerIncidentStats(customerId: string) {
		const incidents = await incidentService.getIncidentsByCustomer(customerId)
		return {
			totalIncidents: incidents.length,
			totalValueRecovered: incidents.reduce((sum, inc) => sum + inc.value, 0),
			openIncidents: 0,
			uniqueStores: [...new Set(incidents.map(inc => inc.siteId))].length,
			incidentTypes: incidents.reduce((acc: Record<string, number>, inc) => {
				acc[inc.incidentType] = (acc[inc.incidentType] || 0) + 1
				return acc
			}, {})
		}
	}

	static async getCustomerIncidentTrends(customerId: string) {
		const incidents = await incidentService.getIncidentsByCustomer(customerId)
		const monthlyData = incidents.reduce((acc: Record<string, any>, incident) => {
			const month = incident.date.substring(0, 7)
			if (!acc[month]) acc[month] = { month, incidents: 0, valueRecovered: 0 }
			acc[month].incidents++
			acc[month].valueRecovered += incident.value || 0
			return acc
		}, {})
		return Object.values(monthlyData).sort((a: any, b: any) => a.month.localeCompare(b.month))
	}

	// Satisfaction and daily activity have no backend API yet — return empty defaults
	static getCustomerSatisfactionSurveys(_customerId: string) {
		return []
	}

	static getCustomerSatisfactionStats(_customerId: string) {
		return { averageOverallRating: 0, npsScore: 0, totalSurveys: 0, recommendationRate: 0 }
	}

	static getCustomerSatisfactionTrends(_customerId: string) {
		return []
	}

	static getCustomerDailyActivity(_customerId: string) {
		return []
	}

	static getCustomerDailyActivityStats(_customerId: string) {
		return { totalReports: 0, totalHours: 0, averageReportQuality: 0, supervisorApprovalRate: 0 }
	}

	static getCustomerBeSafeBeSecureData(_customerId: string) {
		return { checksBySite: [], complianceBreakdown: [], averageReportQuality: 0 }
	}

	static async getCustomerDashboardData(customerId: string) {
		const customer = await this.getCustomerInfo(customerId)
		const incidentStats = await this.getCustomerIncidentStats(customerId)
		const satisfactionStats = this.getCustomerSatisfactionStats(customerId)
		const dailyActivityStats = this.getCustomerDailyActivityStats(customerId)
		const beSafeData = this.getCustomerBeSafeBeSecureData(customerId)

		return {
			customer,
			incidents: {
				total: incidentStats.totalIncidents,
				valueRecovered: incidentStats.totalValueRecovered,
				openIncidents: incidentStats.openIncidents,
				uniqueStores: incidentStats.uniqueStores,
				incidentTypes: incidentStats.incidentTypes,
			},
			satisfaction: {
				averageRating: satisfactionStats.averageOverallRating,
				npsScore: satisfactionStats.npsScore,
				totalSurveys: satisfactionStats.totalSurveys,
				recommendationRate: satisfactionStats.recommendationRate,
			},
			dailyActivity: {
				totalReports: dailyActivityStats.totalReports,
				totalHours: dailyActivityStats.totalHours,
				averageQuality: dailyActivityStats.averageReportQuality,
				approvalRate: dailyActivityStats.supervisorApprovalRate,
			},
			beSafe: {
				totalChecks: beSafeData.checksBySite.reduce((sum: number, site: any) =>
					sum + site.insecureAreas + site.compliance + site.systems, 0),
				complianceIssues: beSafeData.complianceBreakdown.reduce((sum: number, item: any) =>
					sum + item.value, 0),
				averageQuality: beSafeData.averageReportQuality,
			},
		}
	}

	static async getCustomerDataByRegion(customerId: string, regionId: string) {
		const incidents = (await this.getCustomerIncidents(customerId)).filter(i => i.regionId === regionId)
		return {
			incidents,
			surveys: [],
			dailyReports: [],
			stats: { incidentCount: incidents.length, averageSatisfaction: 0, reportCount: 0 },
		}
	}

	static async getCustomerDataBySite(customerId: string, siteId: string) {
		const incidents = (await this.getCustomerIncidents(customerId)).filter(i => i.siteId === siteId)
		return {
			incidents,
			surveys: [],
			dailyReports: [],
			stats: { incidentCount: incidents.length, averageSatisfaction: 0, reportCount: 0 },
		}
	}

	static filterDataByDateRange<T extends { dateReported?: string; surveyDate?: string; reportDate?: string }>(
		data: T[],
		startDate?: Date,
		endDate?: Date
	): T[] {
		if (!startDate && !endDate) return data
		return data.filter(item => {
			const itemDate = new Date(item.dateReported || item.surveyDate || item.reportDate || '')
			if (isNaN(itemDate.getTime())) return false
			if (startDate && itemDate < startDate) return false
			if (endDate && itemDate > endDate) return false
			return true
		})
	}
}

export default CustomerDataService
