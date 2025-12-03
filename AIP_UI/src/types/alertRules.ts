export type AlertChannel = 'in-app' | 'email'
export type TriggerCondition = 'any' | 'all' | 'exact-match'

export interface StoreAlertRule {
	id?: string
	name: string
	keywords: string[]
	incidentTypes: string[]
	lpmRegion: string // Region name for stores in region
	storeRadius?: number // Deprecated - no longer used
	triggerCondition: TriggerCondition
	channels: AlertChannel[]
	emailRecipients?: string[] // Email recipients for store alerts
	isActive: boolean
	createdAt?: Date
	updatedAt?: Date
	createdBy?: string
}

export interface LPMAlertRule {
	id?: string
	name: string
	keywords: string[]
	incidentTypes: string[]
	lpmRegion: string
	triggerCondition: TriggerCondition
	channels: AlertChannel[]
	emailRecipients: string[] // Direct email to LPM
	isActive: boolean
	createdAt?: Date
	updatedAt?: Date
	createdBy?: string
}

