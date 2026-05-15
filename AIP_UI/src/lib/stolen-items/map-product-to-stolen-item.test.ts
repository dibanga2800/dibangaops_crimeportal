import { describe, it, expect } from 'vitest'
import { mapProductToStolenItem } from './map-product-to-stolen-item'
import { resolveCatalogDepartment } from './catalog-departments'

describe('map-product-to-stolen-item', () => {
	it('maps catalog fields to stolen item', () => {
		const item = mapProductToStolenItem(
			'5012345678900',
			{
				productName: 'Milk 1L',
				department: 'BAKERY',
				description: 'VME-001',
				price: 3.99,
			},
			['BAKERY', 'GROCERY']
		)

		expect(item.barcode).toBe('5012345678900')
		expect(item.productName).toBe('Milk 1L')
		expect(item.category).toBe('BAKERY')
		expect(item.description).toBe('VME-001')
		expect(item.cost).toBe(3.99)
	})

	it('resolves department case-insensitively from catalog list', () => {
		expect(resolveCatalogDepartment('bakery', ['BAKERY', 'GROCERY'])).toBe('BAKERY')
	})
})
