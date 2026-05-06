const SITE_SCOPED_ROLES = new Set(['manager', 'security-officer', 'store'])

const ROLE_ALIAS_MAP: Record<string, string> = {
	'store user': 'store',
	'store-user': 'store',
	'store_user': 'store',
	'security officer': 'security-officer',
	'security_officer': 'security-officer',
}

const normalizeRole = (value: unknown): string => {
	const rawRole = String(value ?? '').trim().toLowerCase()
	return ROLE_ALIAS_MAP[rawRole] ?? rawRole
}

const normalizeSiteId = (value: unknown): string =>
	String(value ?? '').trim()

const parseSiteIds = (value: unknown): string[] => {
	if (!value) return []

	if (Array.isArray(value)) {
		return value.map(normalizeSiteId).filter((id) => id.length > 0)
	}

	if (typeof value !== 'string') {
		return [normalizeSiteId(value)].filter((id) => id.length > 0)
	}

	const rawValue = value.trim()
	if (!rawValue) return []

	try {
		const parsed = JSON.parse(rawValue) as unknown
		if (Array.isArray(parsed)) {
			return parsed.map(normalizeSiteId).filter((id) => id.length > 0)
		}
	} catch {
		// Fallback to CSV parsing when value is not JSON
	}

	return rawValue
		.split(',')
		.map(normalizeSiteId)
		.filter((id) => id.length > 0)
}

export const getUserRole = (user: unknown): string => {
	const source = user as Record<string, unknown> | null | undefined
	return normalizeRole(source?.role ?? source?.pageAccessRole)
}

export const isSiteScopeEnforcedForUser = (user: unknown): boolean =>
	SITE_SCOPED_ROLES.has(getUserRole(user))

export const getAssignedSiteIds = (user: unknown): string[] => {
	const source = user as Record<string, unknown> | null | undefined
	const assignedSiteIdsRaw = source?.assignedSiteIds ?? source?.AssignedSiteIds
	const primarySiteId =
		source?.primarySiteId ??
		source?.PrimarySiteId ??
		source?.siteId ??
		source?.SiteId

	const assignedSiteIds = parseSiteIds(assignedSiteIdsRaw)
	if (assignedSiteIds.length > 0) {
		return Array.from(new Set(assignedSiteIds))
	}

	const normalizedPrimarySiteId = normalizeSiteId(primarySiteId)
	if (normalizedPrimarySiteId.length > 0) {
		return [normalizedPrimarySiteId]
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
