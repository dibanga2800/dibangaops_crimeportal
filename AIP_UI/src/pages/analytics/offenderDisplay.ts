const NON_IDENTIFIED_OFFENDER_NAMES = new Set([
	'n/a',
	'na',
	'n.a.',
	'n.a',
	'not applicable',
	'unknown',
	'none',
	'nil',
	'-',
	'—',
	'tbc',
	'tba',
])

/** Stable key for merging offender rows (matches backend name: grouping). */
export const getOffenderMergeKey = (offender: {
	offenderId?: string
	name?: string
}): string => {
	const id = offender.offenderId?.trim()
	if (id?.startsWith('name:')) return id

	const trimmedName = offender.name?.trim()
	if (trimmedName && !NON_IDENTIFIED_OFFENDER_NAMES.has(trimmedName.toLowerCase())) {
		return `name:${trimmedName.toLowerCase().replace(/\s+/g, ' ')}`
	}

	return id ?? ''
}

export const formatOffenderDisplayName = (name?: string, offenderId?: string): string => {
	const trimmedName = name?.trim()
	if (trimmedName && !NON_IDENTIFIED_OFFENDER_NAMES.has(trimmedName.toLowerCase())) {
		return trimmedName
	}

	const trimmedId = offenderId?.trim()
	if (trimmedId) {
		return `Offender ${trimmedId}`
	}

	return 'Unidentified'
}
