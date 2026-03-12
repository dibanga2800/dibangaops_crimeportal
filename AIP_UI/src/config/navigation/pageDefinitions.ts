/**
 * Page Definitions - Canonical list of all pages in the application
 * 
 * This file extracts page definitions from sidebar.ts to ensure consistency
 * between frontend navigation and backend database.
 * 
 * When adding a new page:
 * 1. Add it to sidebar.ts
 * 2. Add it here with proper pageId, category, and description
 * 3. The backend will auto-sync on next API call
 */

import { SIDEBAR_TOP_LINKS, SIDEBAR_SECTIONS } from './sidebar'

export interface PageDefinition {
	pageId: string // Unique identifier (kebab-case, matches backend PageId)
	title: string // Display name
	path: string // Route path (must match sidebar.ts)
	category: string // Category for grouping (Administration, Customer, etc.)
	description?: string // Optional description
	sortOrder: number // Sort order within category
}

/**
 * Helper function to convert path to pageId
 * Example: "/administration/user-setup" -> "user-setup"
 */
const pathToPageId = (path: string): string => {
	return path
		.replace(/^\//, '') // Remove leading slash
		.replace(/\//g, '-') // Replace slashes with hyphens
		.toLowerCase()
}

/**
 * Helper function to determine category from section
 */
const sectionToCategory = (sectionId: string): string => {
	const categoryMap: Record<string, string> = {
		'administration': 'Administration',
		'operations': 'Operations',
		'employee': 'Employee',
		'management': 'Management',
		'compliance': 'Compliance',
		'recruitment': 'Recruitment',
		'customer': 'Company',
	}
	return categoryMap[sectionId] || 'Other'
}

/**
 * Extract all pages from sidebar configuration
 */
const extractPagesFromSidebar = (): PageDefinition[] => {
	const pages: PageDefinition[] = []
	let sortOrder = 1

	// Add top-level links (Action Calendar, Customer Reporting)
	SIDEBAR_TOP_LINKS.forEach((link, index) => {
		const pageId = pathToPageId(link.path)
		const category = link.path.includes('/management/') ? 'Management' : 'Main'
		
		pages.push({
			pageId,
			title: link.label,
			path: link.path,
			category,
			description: `${link.label} page`,
			sortOrder: sortOrder++,
		})
	})

	// Add pages from sections
	SIDEBAR_SECTIONS.forEach((section) => {
		const category = sectionToCategory(section.id)
		const baseSortOrder = getCategoryBaseSortOrder(category)

		section.links.forEach((link, index) => {
			const pageId = pathToPageId(link.path)
			
			pages.push({
				pageId,
				title: link.label,
				path: link.path,
				category,
				description: `${link.label} page`,
				sortOrder: baseSortOrder + index,
			})
		})
	})

	return pages
}

/**
 * Get base sort order for each category
 */
const getCategoryBaseSortOrder = (category: string): number => {
	const categorySortMap: Record<string, number> = {
		'Main': 1,
		'Administration': 10,
		'Operations': 30,
		'Employee': 40,
		'Management': 50,
		'Compliance': 60,
		'Recruitment': 70,
		'Company': 80,
	}
	return categorySortMap[category] || 100
}

/**
 * Canonical list of all pages in the application
 * This is the single source of truth for page definitions
 */
export const PAGE_DEFINITIONS: PageDefinition[] = [
	// Main pages
	{
		pageId: 'dashboard',
		title: 'Dashboard',
		path: '/dashboard',
		category: 'Main',
		description: 'Main dashboard',
		sortOrder: 1,
	},
	{
		pageId: 'profile',
		title: 'Profile',
		path: '/profile',
		category: 'Main',
		description: 'User profile',
		sortOrder: 2,
	},
	{
		pageId: 'settings',
		title: 'Settings',
		path: '/settings',
		category: 'Main',
		description: 'Application settings',
		sortOrder: 4,
	},
	{
		pageId: 'alert-rules',
		title: 'Alert Rules',
		path: '/operations/alert-rules',
		category: 'Main',
		description: 'Configure alert rules for stores and LPM',
		sortOrder: 5,
	},
	{
		pageId: 'data-analytics-hub',
		title: 'Data Analytics Hub',
		path: '/analytics/data-analytics-hub',
		category: 'Main',
		description: 'Analytics dashboard with crime trends, hot products, and resource deployment',
		sortOrder: 6,
	},

	// Administration
	{
		pageId: 'user-setup',
		title: 'User Setup',
		path: '/administration/user-setup',
		category: 'Administration',
		description: 'User management and setup',
		sortOrder: 10,
	},
	{
		pageId: 'employee-registration',
		title: 'Employee Registration',
		path: '/administration/employee-registration',
		category: 'Administration',
		description: 'Employee registration',
		sortOrder: 11,
	},
	{
		pageId: 'customer-setup',
		title: 'Company Setup',
		path: '/administration/customer-setup',
		category: 'Administration',
		description: 'Company management',
		sortOrder: 12,
	},

	// Operations
	{
		pageId: 'incident-report',
		title: 'Incident Report',
		path: '/operations/incident-report',
		category: 'Operations',
		description: 'Incident reporting',
		sortOrder: 20,
	},
	{
		pageId: 'incident-graph',
		title: 'Incident Graph',
		path: '/operations/incident-graph',
		category: 'Operations',
		description: 'Incident graphs',
		sortOrder: 25,
	},
	{
		pageId: 'crime-intelligence',
		title: 'Crime Intelligence',
		path: '/operations/crime-intelligence',
		category: 'Operations',
		description: 'Crime intelligence dashboard',
		sortOrder: 26,
	},
]

/**
 * Get page definition by path
 */
export const getPageDefinitionByPath = (path: string): PageDefinition | undefined => {
	return PAGE_DEFINITIONS.find(p => p.path === path)
}

/**
 * Get page definition by pageId
 */
export const getPageDefinitionById = (pageId: string): PageDefinition | undefined => {
	return PAGE_DEFINITIONS.find(p => p.pageId === pageId)
}

/**
 * Get all pages for a specific category
 */
export const getPagesByCategory = (category: string): PageDefinition[] => {
	return PAGE_DEFINITIONS.filter(p => p.category === category)
}

