export interface RemovablePage {
	id?: string
	path?: string
}

const PROTECTED_PAGE_IDS = new Set([
	'dashboard',
	'profile',
	'settings',
	'alert-rules',
	'data-analytics-hub',
	'user-setup',
	'employee-registration',
	'customer-setup',
	'barcode-catalog-import',
	'product-catalog',
	'incident-report',
	'incident-graph',
	'crime-intelligence',
	'customer-incident-report',
	'customer-incident-graph',
	'customer-crime-intelligence',
])

const REMOVED_PAGE_IDS = new Set([
	'action-calendar',
	'bank-holiday',
	'bank-holidays',
	'holiday-request',
	'holiday-requests',
	'daily-occurrence-book',
	'daily-occurrence',
	'occurrence-book',
	'mystery-shopper',
	'site-visit',
	'officer-support',
	'manager-support',
	'safe-duress',
	'safe-duress-words',
	'crm',
	'contacts',
	'contacts-crm',
	'management-customer-reporting',
	'customer-reporting-page',
	'customer-reporting',
	'customer-satisfaction-report',
	'satisfaction-report',
	'customer-views-config',
	'be-safe-be-secure',
	'daily-activity-report',
	'daily-activity-reports',
	'incident-list',
	'stock-management',
	'employee-activity',
])

const containsAny = (value: string, needles: string[]): boolean =>
	needles.some((needle) => value.includes(needle))

export const isRemovedPage = ({ id = '', path = '' }: RemovablePage): boolean => {
	const pageId = id.toLowerCase().trim()
	const pagePath = path.toLowerCase().trim()

	if (!pageId && !pagePath) {
		return false
	}

	if (pageId && PROTECTED_PAGE_IDS.has(pageId)) {
		return false
	}

	if (pageId && REMOVED_PAGE_IDS.has(pageId)) {
		return true
	}

	if (pageId) {
		if (
			pageId.startsWith('compliance-') ||
			pageId.startsWith('management-') ||
			pageId.startsWith('recruitment-')
		) {
			return true
		}

		if (
			containsAny(pageId, [
				'holiday',
				'bank-holiday',
				'occurrence',
				'mystery-shopper',
				'site-visit',
				'officer-support',
				'manager-support',
				'safe-duress',
				'customer-reporting',
				'customer-satisfaction',
				'satisfaction-report',
				'action-calendar',
				'incident-list',
				'employee-activity',
				'daily-activity',
				'be-safe',
			])
		) {
			return true
		}

		if (pageId === 'crm' || pageId === 'contacts' || pageId === 'contacts-crm') {
			return true
		}
	}

	if (!pagePath) {
		return false
	}

	if (
		pagePath.startsWith('/compliance/') ||
		pagePath.startsWith('/management/') ||
		pagePath.startsWith('/recruitment/') ||
		pagePath.startsWith('/crm') ||
		pagePath === '/contacts' ||
		pagePath.startsWith('/contacts/')
	) {
		return true
	}

	return containsAny(pagePath, [
		'/action-calendar',
		'/holiday',
		'/bank-holiday',
		'/mystery-shopper',
		'/site-visit',
		'/daily-occurrence',
		'/occurrence-book',
		'/officer-support',
		'/manager-support',
		'/safe-duress',
		'/customer-reporting',
		'/customer-satisfaction',
		'/satisfaction-report',
		'/incident-list',
		'/employee-activity',
		'/stock-management',
		'/daily-activity-report',
		'/be-safe-be-secure',
	])
}

export const withoutRemovedPages = <T extends RemovablePage>(pages: T[]): T[] =>
	pages.filter((page) => !isRemovedPage(page))

export const withoutRemovedPageIds = (pageIds: string[]): string[] =>
	pageIds.filter((id) => !isRemovedPage({ id }))
