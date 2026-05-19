import { describe, it, expect } from 'vitest'
import { z } from 'zod'

const apiBaseUrlSchema = z
	.string()
	.refine(
		(value) => value.startsWith('/') || /^https?:\/\//i.test(value),
		{ message: 'Must be an absolute URL or a path such as /api' },
	)

describe('VITE_API_BASE_URL validation', () => {
	it('accepts same-origin relative path /api', () => {
		expect(apiBaseUrlSchema.parse('/api')).toBe('/api')
	})

	it('accepts absolute https URL', () => {
		expect(apiBaseUrlSchema.parse('https://api.example.com/api')).toBe(
			'https://api.example.com/api',
		)
	})

	it('rejects bare hostname without scheme', () => {
		expect(() => apiBaseUrlSchema.parse('api.example.com/api')).toThrow()
	})
})
