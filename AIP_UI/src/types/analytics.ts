/**
 * Data Analytics Hub - Type Definitions
 * 
 * Types for all analytics modules including:
 * - Crime Trend Explorer
 * - Hot Products Dashboard
 * - Repeat Offender Analysis
 * - Resource Deployment Engine
 * - Crime Linking Panel
 */

// ============================================================================
// Crime Trend Explorer Types
// ============================================================================

export interface DayOfWeekData {
	day: string
	incidents: number
	stores: number
	percentage: number
}

export interface TimeOfDayData {
	hour: number
	label: string
	incidents: number
	percentage: number
}

export interface IncidentTypeData {
	type: string
	count: number
	percentage: number
	totalValue: number
}

export interface StoreDrilldownData {
	storeId: number
	storeName: string
	incidents: number
	incidentTypes: IncidentTypeData[]
	totalStolenValue: number
	totalRecoveredValue: number
	totalLostValue: number
	recoveryRate: number
	peakDay: string
	peakHour: number
}

export interface RecoveryTrendPoint {
	period: string
	incidentCount: number
	stolenValue: number
	recoveredValue: number
	lostValue: number
}

export interface CrimeTrendData {
	dayOfWeek: DayOfWeekData[]
	timeOfDay: TimeOfDayData[]
	incidentTypes: IncidentTypeData[]
	storeDrilldown: Record<string, StoreDrilldownData>
	recoveryTrend: RecoveryTrendPoint[]
	totalIncidents: number
	dateRange: {
		start: string
		end: string
	}
}

// ============================================================================
// Hot Products Dashboard Types
// ============================================================================

export interface ProductFrequencyData {
	barcode: string
	productName: string
	frequency: number
	totalValue: number
	stolenValue: number
	recoveredValue: number
	lostValue: number
	recoveryRate: number
	storesAffected: number
}

export interface StoreProductItemData {
	barcode: string
	productName: string
	frequency: number
	value: number
	stolenValue: number
	recoveredValue: number
	lostValue: number
	recoveryRate: number
}

export interface StoreProductHeatmapData {
	storeId: number
	storeName: string
	products: StoreProductItemData[]
	totalIncidents: number
	totalValueStolen: number
	totalValueRecovered: number
	totalValueLost: number
	recoveryRate: number
	riskLevel: 'low' | 'medium' | 'high' | 'critical'
}

export interface HotProductsData {
	topProducts: ProductFrequencyData[]
	topRecoveredProducts: ProductFrequencyData[]
	worstRecoveryProducts: ProductFrequencyData[]
	storeHeatmap: StoreProductHeatmapData[]
	totalValueStolen: number
	totalValueRecovered: number
	totalValueLost: number
	recoveryRate: number
	period: {
		start: string
		end: string
	}
}

export interface AnalyticsFinancialSummary {
	totalStolenValue: number
	totalRecoveredValue: number
	totalLostValue: number
	recoveryRate: number
	totalRecoveredQuantity: number
	totalLostQuantity: number
}

export interface StoreRecoveryComparison {
	storeId: number
	storeName: string
	incidentCount: number
	totalStolenValue: number
	totalRecoveredValue: number
	totalLostValue: number
	recoveryRate: number
	totalRecoveredQuantity: number
	totalLostQuantity: number
}

// ============================================================================
// Repeat Offender Analysis Types
// ============================================================================

export interface OffenderProfile {
	offenderId: string
	name: string
	incidentCount: number
	firstIncident: string
	lastIncident: string
	storesTargeted: string[]
	totalValue: number
	riskLevel: 'low' | 'medium' | 'high' | 'critical'
	modusOperandi: string[]
}

export interface CrossStoreMovement {
	offenderId: string
	offenderName: string
	movements: {
		fromStore: string
		toStore: string
		date: string
		incidentType: string
	}[]
	totalStores: number
}

export interface OffenderNetworkNode {
	id: string
	name: string
	type: 'offender' | 'store'
	x: number
	y: number
}

export interface OffenderNetworkLink {
	source: string
	target: string
	strength: number
	incidentCount: number
}

export interface OffenderNetworkData {
	nodes: OffenderNetworkNode[]
	links: OffenderNetworkLink[]
}

export interface RepeatOffenderData {
	mostActive: OffenderProfile[]
	crossStoreMovements: CrossStoreMovement[]
	networkMap: OffenderNetworkData
	totalOffenders: number
}

// ============================================================================
// Resource Deployment Engine Types
// ============================================================================

export interface TimeDeploymentRecommendation {
	day: string
	hour: number
	hourLabel: string
	recommendedOfficers: number
	officerType: 'uniform' | 'store detectives'
	recommendedLPM?: boolean
	priority: 'low' | 'medium' | 'high' | 'critical'
	reason: string
	expectedIncidents: number
}

export interface StoreRiskRanking {
	storeId: number
	storeName: string
	riskScore: number
	riskLevel: 'low' | 'medium' | 'high' | 'critical'
	incidentCount: number
	trend: 'increasing' | 'stable' | 'decreasing'
	recommendedOfficerType: 'uniform' | 'store detectives'
	recommendedLPM?: boolean
	recommendedHours: string[]
	priority: number
}

export interface DeploymentRecommendation {
	bestTimes: TimeDeploymentRecommendation[]
	storeRankings: StoreRiskRanking[]
	overallStrategy: string
	lastUpdated: string
}

// ============================================================================
// Crime Linking Panel Types
// ============================================================================

export interface LinkedIncident {
	incidentId: string
	date: string
	storeName: string
	incidentType: string
	offenderId?: string
	offenderName?: string
	value: number
	similarityScore: number
	matchingFeatures: string[]
}

export interface IncidentCluster {
	clusterId: string
	incidents: LinkedIncident[]
	commonFeatures: string[]
	suspectedOffender?: {
		id: string
		name: string
		confidence: number
	}
	totalValue: number
	dateRange: {
		start: string
		end: string
	}
}

export interface OffenderChain {
	chainId: string
	offenderId: string
	offenderName: string
	incidents: LinkedIncident[]
	timeline: {
		date: string
		store: string
		incidentType: string
	}[]
	totalValue: number
	pattern: string
}

export interface CrimeLinkingData {
	clusters: IncidentCluster[]
	offenderChains: OffenderChain[]
	totalLinkedIncidents: number
	period: {
		start: string
		end: string
	}
}

// ============================================================================
// Combined Analytics Response
// ============================================================================

export interface AnalyticsHubData {
	crimeTrends: CrimeTrendData
	hotProducts: HotProductsData
	financialSummary: AnalyticsFinancialSummary
	storeRecoveryComparisons: StoreRecoveryComparison[]
	repeatOffenders: RepeatOffenderData
	deploymentRecommendations: DeploymentRecommendation
	crimeLinking: CrimeLinkingData
	metadata: {
		generatedAt: string
		dateRange: {
			start: string
			end: string
		}
		customerId?: number
	}
}

