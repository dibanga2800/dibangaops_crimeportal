/** Merge a department into the catalog list (case-insensitive dedupe). */
export const mergeCatalogDepartment = (departments: string[], department: string | undefined): string[] => {
	const raw = department?.trim()
	if (!raw) return departments

	if (departments.some((d) => d.trim().toLowerCase() === raw.toLowerCase())) {
		return departments
	}

	return [...departments, raw].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }))
}

/** Pick the canonical catalog department string for a product row (exact/case-insensitive). */
export const resolveCatalogDepartment = (
	productDepartment: string | undefined,
	catalogDepartments: string[]
): string => {
	const raw = productDepartment?.trim()
	if (!raw) return ''

	const match = catalogDepartments.find((d) => d.trim().toLowerCase() === raw.toLowerCase())
	return match ?? raw
}
