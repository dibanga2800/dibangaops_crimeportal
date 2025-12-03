import type { Incident, StolenItem } from '@/types/incidents'
import type { Site } from '@/types/customer'
import { IncidentType } from '@/types/incidents'
import { DUMMY_REGIONS } from './mockRegions'

const getRandomDate = (daysAgo: number): string => {
	const date = new Date()
	date.setDate(date.getDate() - daysAgo)
	date.setHours(Math.floor(Math.random() * 24), Math.floor(Math.random() * 60), 0, 0)
	return date.toISOString()
}

const getRandomItem = <T>(arr: T[]): T => arr[Math.floor(Math.random() * arr.length)]

const priorities: Array<'low' | 'medium' | 'high'> = ['low', 'medium', 'high']
const statuses: Array<'pending' | 'resolved' | 'in-progress'> = ['pending', 'resolved', 'in-progress']

const officerNames = [
	'David Brown',
	'Karen Walsh',
	'Lee Richards',
	'Emma Thompson',
	'James Wilson',
	'Sarah Johnson',
	'Michael Davis',
	'Lisa Anderson',
]

const incidentTypes = Object.values(IncidentType)

// Map customer IDs to customer names
const getCustomerNameById = (customerId: number): string => {
	const customerMap: Record<number, string> = {
		21: 'Central England COOP',
		22: 'Heart of England COOP',
		23: 'Midcounties COOP',
	}
	return customerMap[customerId] || 'Central England COOP' // Default fallback
}

const offenderNames = [
	'John Smith',
	'Jane Doe',
	'Robert Jones',
	'Mary Williams',
	'Thomas Brown',
	'Unknown',
]

const generateStolenItems = (): StolenItem[] => {
	const itemCategories = [
		{ category: 'Electronics', items: ['Laptop', 'Tablet', 'Mobile Phone', 'Headphones'] },
		{ category: 'Clothing', items: ['Jacket', 'Shoes', 'Jeans', 'Shirt'] },
		{ category: 'Food & Drink', items: ['Alcohol', 'Meat', 'Chocolate', 'Soft Drinks'] },
		{ category: 'Health & Beauty', items: ['Perfume', 'Skincare', 'Makeup', 'Vitamins'] },
		{ category: 'Household', items: ['Cleaning Products', 'Batteries', 'Light Bulbs'] },
	]

	const numItems = Math.floor(Math.random() * 3) + 1
	const items: StolenItem[] = []

	for (let i = 0; i < numItems; i++) {
		const category = getRandomItem(itemCategories)
		const productName = getRandomItem(category.items)
		const quantity = Math.floor(Math.random() * 3) + 1
		const cost = Math.floor(Math.random() * 100) + 10
		const totalAmount = cost * quantity

		items.push({
			id: `item-${i + 1}`,
			category: category.category,
			description: `${productName} - ${category.category}`,
			productName,
			cost,
			quantity,
			totalAmount,
		})
	}

	return items
}

const descriptions = [
	'Customer attempted to leave store without paying for items',
	'Offender was observed concealing items in their bag',
	'Suspicious behavior detected near high-value items section',
	'Customer became aggressive when approached by staff',
	'Repeat offender identified entering the store',
	'Underage customer attempted to purchase alcohol',
	'Customer damaged store property during altercation',
	'Credit card fraud detected at self-checkout',
	'Customer reported abusive behavior from another shopper',
	'Items recovered after customer attempted to leave store',
]

const storeComments = [
	'Staff handled situation professionally',
	'Police were called to assist',
	'Customer was cooperative after initial confrontation',
	'CCTV footage available for review',
	'Incident resolved without further action needed',
	'Witness statements collected',
	'Management notified of incident',
]

// Create an array with exactly 60% store detectives and 40% uniform officers
const createOfficerTypes = (totalCount: number): Array<'store detective' | 'uniform'> => {
	const types: Array<'store detective' | 'uniform'> = []
	const storeDetectiveCount = Math.floor(totalCount * 0.6) // 60%
	const uniformCount = totalCount - storeDetectiveCount // 40%
	
	// Add store detectives (60%)
	for (let i = 0; i < storeDetectiveCount; i++) {
		types.push('store detective')
	}
	// Add uniform officers (40%)
	for (let i = 0; i < uniformCount; i++) {
		types.push('uniform')
	}
	// Shuffle the array to mix them randomly
	for (let i = types.length - 1; i > 0; i--) {
		const j = Math.floor(Math.random() * (i + 1));
		[types[i], types[j]] = [types[j], types[i]]
	}
	return types
}

const TOTAL_INCIDENTS = 175
const officerTypes = createOfficerTypes(TOTAL_INCIDENTS)

// Generate mock incidents - this will be populated with sites from backend API
let availableSites: Site[] = []

// Function to generate incidents using sites from backend
export const generateMockIncidents = (sites: Site[]): Incident[] => {
	if (sites.length === 0) {
		// Return empty array if no sites available from backend
		return []
	}
	
	return Array.from({ length: TOTAL_INCIDENTS }, (_, index) => {
		const site = getRandomItem(sites)
		const region = DUMMY_REGIONS.find(r => r.regionID === site.fkRegionID)
	const incidentType = getRandomItem(incidentTypes)
	const daysAgo = Math.floor(Math.random() * 1825) // Last 5 years (1825 days)
	const dateOfIncident = getRandomDate(daysAgo)
	const priority = getRandomItem(priorities)
	const status = getRandomItem(statuses)
	const stolenItems = Math.random() > 0.3 ? generateStolenItems() : []
	const totalValue = stolenItems.reduce((sum, item) => sum + item.totalAmount, 0)
	const hasOffender = Math.random() > 0.4
	const hasPolice = Math.random() > 0.6

	// Assign officer type from pre-shuffled array (60% store detectives, 40% uniform officers)
	const officerType = officerTypes[index]
	const isStoreDetective = officerType === 'store detective'
	const officerRole = isStoreDetective ? 'Store Detective' : 'Uniform Officer'

	const siteCustomerId = site.fkCustomerID || 21
	
	return {
		id: `INC-${String(index + 1).padStart(6, '0')}`,
		customerId: siteCustomerId,
		customerName: getCustomerNameById(siteCustomerId),
		siteName: site.locationName,
		siteId: site.siteID?.toString(),
		regionId: region?.regionID?.toString(),
		regionName: region?.regionName,
		location: site.locationName,
		store: site.locationName,
		officerName: getRandomItem(officerNames),
		officerRole,
		officerType,
		dateOfIncident,
		date: dateOfIncident,
		timeOfIncident: new Date(dateOfIncident).toLocaleTimeString('en-GB', {
			hour: '2-digit',
			minute: '2-digit',
		}),
		incidentType,
		type: incidentType,
		description: getRandomItem(descriptions),
		incidentDetails: `Detailed information about the ${incidentType.toLowerCase()} incident.`,
		storeComments: Math.random() > 0.5 ? getRandomItem(storeComments) : undefined,
		totalValueRecovered: totalValue > 0 ? Math.floor(totalValue * (Math.random() * 0.5 + 0.5)) : 0,
		value: totalValue,
		valueRecovered: totalValue > 0 ? Math.floor(totalValue * (Math.random() * 0.5 + 0.5)) : 0,
		amount: totalValue,
		total: totalValue,
		stolenItems: stolenItems.length > 0 ? stolenItems : undefined,
		policeInvolvement: hasPolice,
		urnNumber: hasPolice ? `URN-${String(Math.floor(Math.random() * 999999)).padStart(6, '0')}` : undefined,
		crimeRefNumber: hasPolice
			? `CR-${String(Math.floor(Math.random() * 999999)).padStart(6, '0')}`
			: undefined,
		status,
		priority,
		actionTaken: status === 'resolved' ? 'Incident resolved and closed' : 'Investigation ongoing',
		evidenceAttached: Math.random() > 0.6,
		witnessStatements: Math.random() > 0.7 ? ['Witness statement available'] : undefined,
		reportNumber: `RPT-${String(index + 1).padStart(6, '0')}`,
		offenderName: hasOffender ? getRandomItem(offenderNames) : undefined,
		offenderSex: hasOffender ? getRandomItem(['Male', 'Female', 'N/A or N/K']) : undefined,
		gender: hasOffender ? (getRandomItem(['Male', 'Female', 'N/A or N/K']) as 'Male' | 'Female' | 'N/A or N/K') : undefined,
		offenderDOB: hasOffender && Math.random() > 0.5 ? getRandomDate(Math.floor(Math.random() * 10000)) : undefined,
		offenderMarks: hasOffender && Math.random() > 0.7 ? 'Visible tattoos on left arm' : undefined,
		offenderAddress:
			hasOffender && Math.random() > 0.6
				? {
						numberAndStreet: `${Math.floor(Math.random() * 100)} High Street`,
						town: site.town,
						county: site.county,
						postCode: site.postcode,
					}
				: undefined,
		dateInputted: getRandomDate(daysAgo - Math.floor(Math.random() * 2)),
	}
	})
}

// Store generated incidents
let cachedIncidents: Incident[] = []

// Function to get mock incidents, generating them if needed
export const getMockIncidents = (sites?: Site[]): Incident[] => {
	if (sites && sites.length > 0) {
		availableSites = sites
		cachedIncidents = generateMockIncidents(sites)
		// Sort by date, most recent first
		cachedIncidents.sort((a, b) => new Date(b.dateOfIncident).getTime() - new Date(a.dateOfIncident).getTime())
		return cachedIncidents
	}
	return cachedIncidents
}

// Export for backward compatibility - will be populated by MSW handler
export const MOCK_INCIDENTS: Incident[] = []

