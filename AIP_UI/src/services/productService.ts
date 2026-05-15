import { api, ApiResponse } from '@/config/api'

export interface Product {
	productId: number
	/** Barcode — stored as EAN in the database */
	ean: string
	productName: string
	department?: string
	description?: string
	price?: number
	isActive: boolean
}

export interface ProductLookupResponse {
	productId: number
	ean: string
	productName: string
	department?: string
	/** VME code (Products.Description). */
	description?: string
	/** Retail price (Products.Price). */
	price?: number
}

export interface ProductListResponse {
	items: Product[]
	totalCount: number
	page: number
	pageSize: number
}

const normalizeProduct = (raw: Record<string, unknown>): Product => ({
	productId: (raw.productId ?? raw.ProductId) as number,
	ean: (raw.ean ?? raw.EAN ?? '') as string,
	productName: (raw.productName ?? raw.ProductName ?? '') as string,
	department: (raw.department ?? raw.Department) as string | undefined,
	description: (raw.description ?? raw.Description) as string | undefined,
	price: (raw.price ?? raw.Price) as number | undefined,
	isActive: (raw.isActive ?? raw.IsActive ?? true) as boolean,
})

const normalizeListResponse = (raw: Record<string, unknown>): ProductListResponse => {
	const itemsRaw = (raw.items ?? raw.Items ?? []) as Record<string, unknown>[]
	return {
		items: itemsRaw.map(normalizeProduct),
		totalCount: (raw.totalCount ?? raw.TotalCount ?? 0) as number,
		page: (raw.page ?? raw.Page ?? 1) as number,
		pageSize: (raw.pageSize ?? raw.PageSize ?? 50) as number,
	}
}

export const productService = {
	getProductByEAN: async (ean: string): Promise<ProductLookupResponse | null> => {
		try {
			const response = await api.get<ApiResponse<ProductLookupResponse>>(`/product/ean/${encodeURIComponent(ean)}`)
			if (response.data.success && response.data.data) {
				const d = response.data.data as unknown as Record<string, unknown>
				return {
					productId: (d.productId ?? d.ProductId) as number,
					ean: (d.ean ?? d.EAN) as string,
					productName: (d.productName ?? d.ProductName) as string,
					department: (d.department ?? d.Department) as string | undefined,
					description: (d.description ?? d.Description) as string | undefined,
					price: (d.price ?? d.Price) as number | undefined,
				}
			}
			return null
		} catch (error: unknown) {
			const status = (error as { response?: { status?: number } })?.response?.status
			if (status === 404) return null
			throw error
		}
	},

	getDepartments: async (): Promise<string[]> => {
		const response = await api.get<ApiResponse<string[]>>(`/product/departments`)
		if (response.data.success && response.data.data) {
			const raw = response.data.data as unknown
			if (Array.isArray(raw)) {
				return raw
					.map((d) => (typeof d === 'string' ? d : String(d)).trim())
					.filter((d) => d.length > 0)
			}
		}
		return []
	},

	getProducts: async (params?: {
		page?: number
		pageSize?: number
		search?: string
	}): Promise<ProductListResponse> => {
		const queryParams = new URLSearchParams()
		if (params?.page) queryParams.append('page', String(params.page))
		if (params?.pageSize) queryParams.append('pageSize', String(params.pageSize))
		if (params?.search) queryParams.append('search', params.search)

		const response = await api.get<ApiResponse<ProductListResponse>>(`/product?${queryParams}`)
		if (response.data.success && response.data.data) {
			return normalizeListResponse(response.data.data as unknown as Record<string, unknown>)
		}
		return { items: [], totalCount: 0, page: 1, pageSize: params?.pageSize ?? 50 }
	},

	getProductById: async (id: number): Promise<Product | null> => {
		try {
			const response = await api.get<ApiResponse<Product>>(`/product/${id}`)
			if (response.data.success && response.data.data) {
				return normalizeProduct(response.data.data as unknown as Record<string, unknown>)
			}
			return null
		} catch (error: unknown) {
			const status = (error as { response?: { status?: number } })?.response?.status
			if (status === 404) return null
			throw error
		}
	},

	updatePrice: async (productId: number, price: number | null): Promise<Product> => {
		const response = await api.patch<ApiResponse<Product>>(`/product/${productId}/price`, { price })
		if (!response.data.success || !response.data.data) {
			throw new Error(response.data.message || 'Failed to update price')
		}
		return normalizeProduct(response.data.data as unknown as Record<string, unknown>)
	},
}
