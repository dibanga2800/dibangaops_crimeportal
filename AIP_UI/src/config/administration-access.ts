/** Page IDs for User Setup, Employee Registration, and Company Setup — only administrators may use these. */

export const ADMINISTRATOR_ONLY_PAGE_IDS = [
	'user-setup',
	'employee-registration',
	'customer-setup',
] as const

const ADMINISTRATOR_ONLY_PATH_LIST = [
	'/administration/user-setup',
	'/administration/employee-registration',
	'/administration/customer-setup',
] as const

export const ADMINISTRATOR_ONLY_PAGE_PATH_SET = new Set<string>(
	ADMINISTRATOR_ONLY_PATH_LIST.map((p) => p.toLowerCase())
)

/** Expects a path already normalized like PageAccessContext (no trailing slash except root, `/` → `/dashboard`). */
export const isAdministratorOnlyPath = (normalizedPath: string): boolean =>
	ADMINISTRATOR_ONLY_PAGE_PATH_SET.has(normalizedPath.toLowerCase())

/** Remove administrator-only page IDs from a role's assignment (e.g. managers after API sync or before save). */
export const withoutAdministratorOnlyPageIds = (pageIds: string[]): string[] => {
	const deny = new Set<string>(ADMINISTRATOR_ONLY_PAGE_IDS)
	return pageIds.filter((id) => !deny.has(id))
}
