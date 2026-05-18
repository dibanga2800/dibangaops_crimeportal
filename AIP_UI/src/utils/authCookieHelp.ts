/** Shown when login succeeded but HttpOnly auth cookies were not sent to the API. */
export const COOKIE_REQUIRED_MESSAGE =
	'Sign-in could not complete. Allow cookies for this site. If you use private/incognito mode or block cross-site cookies, ' +
	'try a standard browser window or access the app via the approved production URL (same-site cookies).'

export const isUnauthorizedStatus = (status: number | undefined): boolean =>
	status === 401 || status === 403
