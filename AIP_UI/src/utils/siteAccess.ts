const SITE_SCOPED_ROLES = new Set(['manager', 'security-officer', 'store'])

const toRole = (value: unknown): string =>
	String(value ?? '')
		.trim()
		.toLowerCase()

export const getUserRole = (user: unknown): string => {
	const source = user as Record<string, unknown> | null | undefined
	return toRole(source?.role ?? source?.pageAccessRole)
}

export const isSiteScopeEnforcedForUser = (user: unknown): boolean =>
	SITE_SCOPED_ROLES.has(getUserRole(user))

export const getAssignedSiteIds = (user: unknown): string[] => {
	const source = user as Record<string, unknown> | null | undefined
	const assignedSiteIds = source?.assignedSiteIds ?? source?.AssignedSiteIds
	const primarySiteId = source?.primarySiteId ?? source?.PrimarySiteId

	if (Array.isArray(assignedSiteIds)) {
		return Array.from(
			new Set(
				assignedSiteIds
					.map((id) => String(id ?? '').trim())
					.filter((id) => id.length > 0)
			)
		)
	}

	if (primarySiteId !== undefined && primarySiteId !== null && String(primarySiteId).trim().length > 0) {
		return [String(primarySiteId).trim()]
	}

	return []
}

export const filterByAssignedSiteIds = <T>(
	items: T[],
	user: unknown,
	getSiteId: (item: T) => string | number | null | undefined
): T[] => {
	if (!isSiteScopeEnforcedForUser(user)) return items

	const assignedSiteIds = getAssignedSiteIds(user)
	if (assignedSiteIds.length === 0) return []

	const allowed = new Set(assignedSiteIds)
	return items.filter((item) => {
		const siteId = getSiteId(item)
		if (siteId === undefined || siteId === null) return false
		return allowed.has(String(siteId))
	})
}
