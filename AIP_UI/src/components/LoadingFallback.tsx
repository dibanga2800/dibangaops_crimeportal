import React from 'react'

/**
 * Loading fallback component for lazy-loaded routes
 * Shows a clean loading state while route chunks are being loaded
 */
export const LoadingFallback: React.FC = () => {
	return (
		<div className="flex h-screen w-full items-center justify-center bg-background">
			<div className="flex flex-col items-center gap-4">
				{/* Spinner */}
				<div className="h-12 w-12 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
				
				{/* Loading text */}
				<p className="text-sm text-muted-foreground">Loading...</p>
			</div>
		</div>
	)
}

/**
 * Minimal loading fallback for faster perceived performance
 */
export const MinimalLoadingFallback: React.FC = () => {
	return (
		<div className="flex min-h-[200px] w-full items-center justify-center">
			<div className="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent"></div>
		</div>
	)
}
