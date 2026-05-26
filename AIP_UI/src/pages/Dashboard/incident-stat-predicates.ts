import { Incident, IncidentType } from '@/types/incidents'

/**
 * Predicates used by the dashboard quick-stat cards as a client-side
 * fallback for the brief window before the server-side `IncidentListSummary`
 * arrives (and for older backends that don't populate the optional
 * summary fields).
 *
 * Each predicate is held in lock-step with its backend counterpart so the
 * fallback render produces the same counts the server would, regardless
 * of locale, timezone, or case drift in stored values:
 *   - `isIncidentTypeShoplifting` mirrors `IncidentRepository.IsShopliftingIncidentType`.
 *   - `isIncidentToday` mirrors `incident.DateOfIncident.Date == DateTime.UtcNow.Date`.
 *   - `isIncidentPriority` / `isIncidentStatus` mirror
 *     `string.Equals(..., StringComparison.OrdinalIgnoreCase)`.
 */

const SHOPLIFTING_PREFIX = IncidentType.SHOPLIFTING.toLowerCase()

/**
 * True when the incident type names shoplifting as the primary offense.
 * Matches the canonical "Shoplifting" and qualified variants like
 * "Shoplifting / Theft" (first word is "shoplifting"); does NOT match
 * distinct offenses that merely contain the word as a modifier, notably
 * "Attempted Shoplifting" where the theft never completed.
 *
 * The next character after the "shoplifting" prefix must be a non-letter.
 * We use the Unicode letter property `\p{L}` (with the `u` flag) so this
 * recognises non-ASCII letters such as "É" exactly as the .NET
 * `char.IsLetter` API does on the server — without the `u` flag we'd
 * silently disagree on locale-specific data (e.g. `"ShopliftingÉ"`).
 */
export const isIncidentTypeShoplifting = (inc: Incident): boolean => {
	const raw = (inc.incidentType || inc.type || '').trim().toLowerCase()
	if (!raw) return false
	if (!raw.startsWith(SHOPLIFTING_PREFIX)) return false
	if (raw.length === SHOPLIFTING_PREFIX.length) return true
	return !/^\p{L}/u.test(raw.slice(SHOPLIFTING_PREFIX.length))
}

/**
 * True when the incident occurred on the current UTC day. Comparison is
 * done in UTC because the backend pins "today" to `DateTime.UtcNow.Date`
 * — using browser local time here would silently drift by a day for
 * users in non-UTC timezones whenever an incident's UTC date lies on the
 * other side of local midnight. We pass `now` in so tests can pin it.
 */
export const isIncidentToday = (inc: Incident, now: Date = new Date()): boolean => {
	const raw = inc.dateOfIncident || inc.date
	if (!raw) return false
	const incDate = new Date(raw)
	if (Number.isNaN(incDate.getTime())) return false
	return toUtcYmd(incDate) === toUtcYmd(now)
}

/**
 * Case-insensitive equality on the (free-text) priority value. Mirrors
 * the backend's `string.Equals(..., OrdinalIgnoreCase)` so historical
 * or LLM-driven casing drift ("high" / "High" / "HIGH") is bucketed
 * together by the client-side fallback.
 */
export const isIncidentPriority = (inc: Incident, priority: string): boolean =>
	(inc.priority ?? '').toLowerCase() === priority.toLowerCase()

/**
 * Case-insensitive equality on the (free-text) status value. Mirrors the
 * backend's `string.Equals(..., OrdinalIgnoreCase)`.
 */
export const isIncidentStatus = (inc: Incident, status: string): boolean =>
	(inc.status ?? '').toLowerCase() === status.toLowerCase()

const toUtcYmd = (date: Date): string => date.toISOString().slice(0, 10)
