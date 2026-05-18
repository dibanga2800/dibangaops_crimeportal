import type { LinkedIncidentStolenProduct } from '@/types/analytics'

interface LinkedIncidentProductsProps {
	summary?: string
	products?: LinkedIncidentStolenProduct[]
}

export const LinkedIncidentProducts = ({
	summary,
	products = [],
}: LinkedIncidentProductsProps) => {
	const displaySummary =
		summary?.trim() ||
		(products.length > 0
			? products
					.slice(0, 3)
					.map((product) => product.productName)
					.join('; ')
			: 'No stolen product lines recorded')

	return (
		<div className="space-y-1 max-w-md">
			<p className="text-xs text-muted-foreground leading-relaxed">{displaySummary}</p>
			{products.length > 0 && (
				<ul className="text-xs space-y-0.5 list-disc pl-4 text-foreground/90">
					{products.map((product, index) => (
						<li key={`${product.barcode}-${product.productName}-${index}`}>
							<span className="font-medium">{product.productName}</span>
							{product.barcode ? (
								<span className="text-muted-foreground font-mono"> · {product.barcode}</span>
							) : null}
							<span className="text-muted-foreground">
								{' '}
								· ×{product.quantity} · £
								{product.lostValue.toLocaleString('en-GB', {
									minimumFractionDigits: 2,
									maximumFractionDigits: 2,
								})}{' '}
								lost
							</span>
						</li>
					))}
				</ul>
			)}
		</div>
	)
}
