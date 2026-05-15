import type { StolenItem } from '@/types/incidents'
import { resolveCatalogDepartment } from '@/lib/stolen-items/catalog-departments'

export interface ProductLookupForIncident {
	productName: string
	department?: string
	description?: string
	price?: number
}

/** @deprecated use resolveCatalogDepartment */
export const resolveIncidentDepartment = resolveCatalogDepartment

const calculateStolenItemValues = (item: StolenItem): StolenItem => {
	const cost = Number.isFinite(item.cost) ? Math.max(item.cost, 0) : 0
	const quantity = Number.isFinite(item.quantity) ? Math.max(item.quantity, 0) : 0
	const totalAmount = cost * quantity
	const wasRecovered = Boolean(item.wasRecovered)
	const requestedRecoveredQuantity = Number.isFinite(item.recoveredQuantity)
		? Math.max(item.recoveredQuantity ?? 0, 0)
		: 0
	const recoveredQuantity = wasRecovered ? Math.min(requestedRecoveredQuantity, quantity) : 0
	const recoveredAmount = cost * recoveredQuantity
	const lostAmount = totalAmount - recoveredAmount

	return {
		...item,
		cost,
		quantity,
		totalAmount,
		wasRecovered,
		recoveredQuantity,
		recoveredAmount,
		lostAmount,
	}
}

export const mapProductToStolenItem = (
	barcode: string,
	product: ProductLookupForIncident,
	catalogDepartments: string[] = []
): StolenItem => {
	const cost =
		product.price != null && Number.isFinite(Number(product.price))
			? Math.max(Number(product.price), 0)
			: 0
	const quantity = 1

	return calculateStolenItemValues({
		id: `${Date.now()}-${barcode}`,
		barcode,
		category: resolveCatalogDepartment(product.department, catalogDepartments),
		productName: product.productName?.trim() || '',
		description: product.description?.trim() || '',
		cost,
		quantity,
		totalAmount: cost * quantity,
		wasRecovered: false,
		recoveredQuantity: 0,
		recoveredAmount: 0,
		lostAmount: cost * quantity,
	})
}
