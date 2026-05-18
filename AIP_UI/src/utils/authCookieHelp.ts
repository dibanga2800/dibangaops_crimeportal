/** Shown when login succeeded but HttpOnly auth cookies were not sent to the API. */
export const COOKIE_REQUIRED_MESSAGE =
	'Sign-in could not complete because your browser did not keep the secure session cookie. ' +
	'On mobile: turn off Private/Incognito mode, allow cookies for this site, and use the official app URL (not a bookmark to an old address). ' +
	'If the problem continues, your organisation may need the API on the same domain as this app (e.g. www and api under dibangops.com).'

export const isUnauthorizedStatus = (status: number | undefined): boolean =>
	status === 401 || status === 403
