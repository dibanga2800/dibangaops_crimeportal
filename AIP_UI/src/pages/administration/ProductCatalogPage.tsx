import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { Package, Search, Loader2, Pencil, Upload } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog'
import { productService, type Product } from '@/services/productService'
import { useToast } from '@/hooks/use-toast'

const PAGE_SIZE = 50
const BARCODE_PATTERN = /^\d{8,14}$/

const formatPrice = (price?: number) => {
	if (price == null) return '—'
	return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' }).format(price)
}

export const ProductCatalogPage = () => {
	const { toast } = useToast()
	const [searchInput, setSearchInput] = useState('')
	const [debouncedSearch, setDebouncedSearch] = useState('')
	const [page, setPage] = useState(1)
	const [items, setItems] = useState<Product[]>([])
	const [totalCount, setTotalCount] = useState(0)
	const [isLoading, setIsLoading] = useState(true)
	const [editingProduct, setEditingProduct] = useState<Product | null>(null)
	const [priceInput, setPriceInput] = useState('')
	const [isSaving, setIsSaving] = useState(false)

	useEffect(() => {
		const timer = window.setTimeout(() => {
			setDebouncedSearch(searchInput.trim())
			setPage(1)
		}, 350)
		return () => window.clearTimeout(timer)
	}, [searchInput])

	const loadProducts = useCallback(async () => {
		setIsLoading(true)
		try {
			const result = await productService.getProducts({
				page,
				pageSize: PAGE_SIZE,
				search: debouncedSearch || undefined,
			})
			setItems(result.items)
			setTotalCount(result.totalCount)
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Failed to load products'
			toast({ title: 'Load failed', description: message, variant: 'destructive' })
			setItems([])
			setTotalCount(0)
		} finally {
			setIsLoading(false)
		}
	}, [page, debouncedSearch, toast])

	useEffect(() => {
		void loadProducts()
	}, [loadProducts])

	const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
	const isBarcodeSearch = BARCODE_PATTERN.test(debouncedSearch)

	const pageLabel = useMemo(() => {
		if (totalCount === 0) return 'No products'
		const from = (page - 1) * PAGE_SIZE + 1
		const to = Math.min(page * PAGE_SIZE, totalCount)
		return `Showing ${from}–${to} of ${totalCount.toLocaleString()}`
	}, [page, totalCount])

	const handleOpenEdit = (product: Product) => {
		setEditingProduct(product)
		setPriceInput(product.price != null ? String(product.price) : '')
	}

	const handleSavePrice = async () => {
		if (!editingProduct) return
		const trimmed = priceInput.trim()
		let price: number | null = null
		if (trimmed.length > 0) {
			const parsed = Number(trimmed)
			if (Number.isNaN(parsed) || parsed < 0) {
				toast({ title: 'Invalid price', description: 'Enter a valid amount (0 or greater).', variant: 'destructive' })
				return
			}
			price = parsed
		}

		setIsSaving(true)
		try {
			await productService.updatePrice(editingProduct.productId, price)
			toast({ title: 'Price updated', description: `${editingProduct.ean} retail price saved.` })
			setEditingProduct(null)
			void loadProducts()
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Failed to update price'
			toast({ title: 'Update failed', description: message, variant: 'destructive' })
		} finally {
			setIsSaving(false)
		}
	}

	return (
		<div className="min-h-screen w-full max-w-[100vw] overflow-x-hidden bg-gradient-to-br from-background via-muted/30 to-background">
			<div className="container mx-auto max-w-screen-2xl space-y-4 px-3 py-4 sm:px-4 md:space-y-6 md:px-6 lg:px-8">
				<div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
					<div className="flex items-center gap-2">
						<Package className="h-7 w-7 shrink-0 text-primary" aria-hidden />
						<div>
							<h1 className="text-xl font-bold tracking-tight sm:text-2xl md:text-3xl">Product catalog</h1>
							<p className="text-sm text-muted-foreground">
								Search by barcode, department, or name. Barcode is stored as <strong>EAN</strong> in the database.
							</p>
						</div>
					</div>
					<Button variant="outline" size="sm" asChild>
						<Link to="/administration/barcode-catalog-import">
							<Upload className="mr-2 h-4 w-4" aria-hidden />
							Import CSV
						</Link>
					</Button>
				</div>

				<Card>
					<CardHeader>
						<CardTitle>Search catalog</CardTitle>
						<CardDescription>
							{isBarcodeSearch
								? 'Barcode-style search: matching EAN and related fields.'
								: 'Search product name, barcode (EAN), department, or VME code.'}
						</CardDescription>
					</CardHeader>
					<CardContent className="space-y-4">
						<div className="relative max-w-md">
							<Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden />
							<Input
								className="pl-9"
								placeholder="Barcode, department, or product name…"
								value={searchInput}
								onChange={(e) => setSearchInput(e.target.value)}
								aria-label="Search products by barcode or name"
							/>
						</div>

						<div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
							<span>{pageLabel}</span>
							<div className="flex items-center gap-2">
								<Button
									type="button"
									variant="outline"
									size="sm"
									disabled={page <= 1 || isLoading}
									onClick={() => setPage((p) => Math.max(1, p - 1))}
								>
									Previous
								</Button>
								<span>
									Page {page} of {totalPages}
								</span>
								<Button
									type="button"
									variant="outline"
									size="sm"
									disabled={page >= totalPages || isLoading}
									onClick={() => setPage((p) => p + 1)}
								>
									Next
								</Button>
							</div>
						</div>

						<div className="rounded-md border">
							<Table>
								<TableHeader>
									<TableRow>
										<TableHead>Barcode</TableHead>
										<TableHead>Department</TableHead>
										<TableHead>VMECode</TableHead>
										<TableHead>Product name</TableHead>
										<TableHead className="text-right">Retail price</TableHead>
										<TableHead className="w-24">Actions</TableHead>
									</TableRow>
								</TableHeader>
								<TableBody>
									{isLoading ? (
										<TableRow>
											<TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
												<Loader2 className="mx-auto mb-2 h-6 w-6 animate-spin" aria-hidden />
												Loading products…
											</TableCell>
										</TableRow>
									) : items.length === 0 ? (
										<TableRow>
											<TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
												No products found.
											</TableCell>
										</TableRow>
									) : (
										items.map((product) => (
											<TableRow key={product.productId}>
												<TableCell className="font-mono text-xs">{product.ean}</TableCell>
												<TableCell className="max-w-[140px] truncate text-xs">{product.department ?? '—'}</TableCell>
												<TableCell className="max-w-[100px] truncate text-xs text-muted-foreground">
													{product.description ?? '—'}
												</TableCell>
												<TableCell className="max-w-[200px] truncate text-sm">{product.productName}</TableCell>
												<TableCell className="text-right text-sm font-medium">{formatPrice(product.price)}</TableCell>
												<TableCell>
													<Button
														type="button"
														variant="ghost"
														size="sm"
														onClick={() => handleOpenEdit(product)}
														aria-label={`Edit price for ${product.ean}`}
													>
														<Pencil className="h-4 w-4" aria-hidden />
													</Button>
												</TableCell>
											</TableRow>
										))
									)}
								</TableBody>
							</Table>
						</div>
					</CardContent>
				</Card>

				<Dialog open={editingProduct != null} onOpenChange={(open) => !open && setEditingProduct(null)}>
					<DialogContent>
						<DialogHeader>
							<DialogTitle>Update retail price</DialogTitle>
							<DialogDescription>
								{editingProduct?.ean} — {editingProduct?.productName}
							</DialogDescription>
						</DialogHeader>
						<div className="space-y-2 py-2">
							<Label htmlFor="retail-price">Retail price (£)</Label>
							<Input
								id="retail-price"
								type="number"
								min={0}
								step="0.01"
								placeholder="Leave empty to clear price"
								value={priceInput}
								onChange={(e) => setPriceInput(e.target.value)}
							/>
						</div>
						<DialogFooter>
							<Button type="button" variant="outline" onClick={() => setEditingProduct(null)} disabled={isSaving}>
								Cancel
							</Button>
							<Button type="button" onClick={() => void handleSavePrice()} disabled={isSaving}>
								{isSaving ? (
									<>
										<Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden />
										Saving…
									</>
								) : (
									'Save price'
								)}
							</Button>
						</DialogFooter>
					</DialogContent>
				</Dialog>
			</div>
		</div>
	)
}

export default ProductCatalogPage
