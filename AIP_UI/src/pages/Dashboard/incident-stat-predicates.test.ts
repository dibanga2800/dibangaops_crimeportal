import { describe, it, expect } from 'vitest'
import type { Incident } from '@/types/incidents'
import {
	isIncidentPriority,
	isIncidentStatus,
	isIncidentToday,
	isIncidentTypeShoplifting,
} from './incident-stat-predicates'

/**
 * These tests pin the parity contract between the dashboard quick-stat
 * fallback predicates and the backend `IncidentListSummary` semantics.
 * Each `describe` block below corresponds to a parity bug that previously
 * shipped silently to the dashboard:
 *
 *   1. Substring shoplifting predicate folded "Attempted Shoplifting" in
 *      and the ASCII-only `[a-z]` boundary disagreed with .NET's
 *      `char.IsLetter` for non-ASCII letters like "É".
 *   2. "Today" used browser local time while the server uses UTC, so
 *      users in non-UTC timezones miscounted rows near midnight.
 *   3. Priority/status equality was case-sensitive while the server's
 *      `string.Equals(..., OrdinalIgnoreCase)` bucketed "high"/"High"/
 *      "HIGH" together.
 *
 * Keep these green if either side of the predicate pair changes.
 */

const buildIncident = (overrides: Partial<Incident> = {}): Incident => ({
	id: 'id-1',
	customerId: 1,
	customerName: 'Test Customer',
	siteName: 'Test Site',
	officerName: 'Test Officer',
	dateOfIncident: '2026-05-26T00:00:00Z',
	incidentType: 'Shoplifting',
	...overrides,
})

describe('isIncidentTypeShoplifting', () => {
	it.each([
		'Shoplifting',
		'shoplifting',
		'SHOPLIFTING',
		'Shoplifting / Theft',
		'Shoplifting (confirmed)',
		'  Shoplifting  ',
	])('counts %s as a shoplifting variant', (incidentType) => {
		expect(isIncidentTypeShoplifting(buildIncident({ incidentType }))).toBe(true)
	})

	it.each([
		'Attempted Shoplifting',
		'Theft',
		'Theft Prevention',
		'Self Scan Tills',
		'Violent Behaviour',
		'Others',
		'',
		'   ',
	])('does NOT count %s', (incidentType) => {
		expect(isIncidentTypeShoplifting(buildIncident({ incidentType }))).toBe(false)
	})

	it('treats non-ASCII letter boundaries the same way as the backend char.IsLetter', () => {
		// Backend: char.IsLetter('É') === true  → NOT shoplifting (Shopliftingé is one word).
		// The previous ASCII-only [a-z] regex returned the opposite verdict.
		expect(isIncidentTypeShoplifting(buildIncident({ incidentType: 'ShopliftingÉ' }))).toBe(false)
	})

	it('falls back to the legacy `type` field when `incidentType` is empty', () => {
		const inc: Incident = {
			...buildIncident(),
			incidentType: '',
			type: 'Shoplifting / Theft',
		}
		expect(isIncidentTypeShoplifting(inc)).toBe(true)
	})
})

describe('isIncidentToday', () => {
	// Pin "now" to 2026-05-26 04:00 UTC so the assertions are deterministic.
	// In any timezone west of UTC (e.g. UTC-5) local time at this instant is
	// still on 2026-05-25 — so a naive `toDateString()` comparison would
	// drift by one day.
	const now = new Date('2026-05-26T04:00:00Z')

	it('counts an incident dated to the same UTC day', () => {
		expect(isIncidentToday(buildIncident({ dateOfIncident: '2026-05-26T02:00:00Z' }), now)).toBe(true)
	})

	it('counts an incident dated to the same UTC day even when local time is on the previous day', () => {
		// 2026-05-26 00:30 UTC is still 2026-05-25 in any UTC-N timezone.
		// The backend would count this row; the frontend must too.
		expect(isIncidentToday(buildIncident({ dateOfIncident: '2026-05-26T00:30:00Z' }), now)).toBe(true)
	})

	it('does NOT count an incident on the previous UTC day', () => {
		expect(isIncidentToday(buildIncident({ dateOfIncident: '2026-05-25T23:59:00Z' }), now)).toBe(false)
	})

	it('does NOT count an incident on the next UTC day', () => {
		expect(isIncidentToday(buildIncident({ dateOfIncident: '2026-05-27T00:00:01Z' }), now)).toBe(false)
	})

	it('returns false for missing or unparseable dates', () => {
		expect(isIncidentToday(buildIncident({ dateOfIncident: '' }), now)).toBe(false)
		expect(isIncidentToday(buildIncident({ dateOfIncident: 'not-a-date' }), now)).toBe(false)
	})

	it('falls back to the legacy `date` field when `dateOfIncident` is empty', () => {
		const inc: Incident = {
			...buildIncident(),
			dateOfIncident: '',
			date: '2026-05-26T02:00:00Z',
		}
		expect(isIncidentToday(inc, now)).toBe(true)
	})
})

describe('isIncidentPriority / isIncidentStatus', () => {
	it.each(['high', 'High', 'HIGH'])('matches priority "%s" case-insensitively', (priority) => {
		expect(isIncidentPriority(buildIncident({ priority: priority as Incident['priority'] }), 'high')).toBe(true)
	})

	it('does NOT match a different priority value', () => {
		expect(isIncidentPriority(buildIncident({ priority: 'medium' }), 'high')).toBe(false)
	})

	it('treats an absent priority as no match', () => {
		expect(isIncidentPriority(buildIncident({ priority: undefined }), 'high')).toBe(false)
	})

	it.each(['pending', 'Pending', 'PENDING'])('matches status "%s" case-insensitively', (status) => {
		expect(isIncidentStatus(buildIncident({ status: status as Incident['status'] }), 'pending')).toBe(true)
	})

	it.each(['resolved', 'Resolved', 'RESOLVED'])('matches resolved "%s" case-insensitively', (status) => {
		expect(isIncidentStatus(buildIncident({ status: status as Incident['status'] }), 'resolved')).toBe(true)
	})

	it('treats an absent status as no match', () => {
		expect(isIncidentStatus(buildIncident({ status: undefined }), 'pending')).toBe(false)
	})
})
