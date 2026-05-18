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
