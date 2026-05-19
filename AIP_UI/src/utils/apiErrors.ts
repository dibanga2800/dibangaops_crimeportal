/** True when the API rejected a mutating request due to CSRF double-submit mismatch. */
export const isCsrfForbidden = (status: number | undefined, data: unknown): boolean => {
	if (status !== 403 || data == null || typeof data !== 'object') {
		return false
	}

	const message =
		(data as { message?: string }).message ??
		(data as { Message?: string }).Message ??
		''

	return /csrf/i.test(message)
}
