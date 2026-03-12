import React from 'react'
import { logger } from '@/utils/logger'

interface ErrorFallbackProps {
	error: Error
	resetErrorBoundary?: () => void
}

export const ErrorFallback: React.FC<ErrorFallbackProps> = ({ error, resetErrorBoundary }) => {
	const isDev = import.meta.env.DEV

	// Log error for monitoring
	React.useEffect(() => {
		logger.error('Application Error Boundary Triggered', {
			message: error.message,
			stack: error.stack,
			name: error.name,
		})
	}, [error])

	const handleReload = () => {
		window.location.href = '/'
	}

	const handleReset = () => {
		if (resetErrorBoundary) {
			resetErrorBoundary()
		} else {
			handleReload()
		}
	}

	return (
		<div className="flex min-h-screen w-full flex-col items-center justify-center bg-background p-8">
			<div className="max-w-2xl space-y-6 text-center">
				{/* Error Icon */}
				<div className="flex justify-center">
					<div className="flex h-20 w-20 items-center justify-center rounded-full bg-destructive/10">
						<svg
							className="h-10 w-10 text-destructive"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
							aria-hidden="true"
						>
							<path
								strokeLinecap="round"
								strokeLinejoin="round"
								strokeWidth={2}
								d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
							/>
						</svg>
					</div>
				</div>

				{/* Error Title */}
				<div className="space-y-2">
					<h1 className="text-3xl font-bold tracking-tight text-foreground">
						Oops! Something went wrong
					</h1>
					<p className="text-lg text-muted-foreground">
						We're sorry for the inconvenience. The application encountered an unexpected error.
					</p>
				</div>

				{/* Error Details (Development Only) */}
				{isDev && (
					<div className="space-y-3 rounded-lg border border-destructive/20 bg-destructive/5 p-6 text-left">
						<div className="space-y-2">
							<h2 className="text-sm font-semibold text-destructive">Error Message:</h2>
							<p className="text-sm font-mono text-foreground">{error.message}</p>
						</div>
						
						{error.stack && (
							<div className="space-y-2">
								<h2 className="text-sm font-semibold text-destructive">Stack Trace:</h2>
								<pre className="max-h-60 overflow-auto rounded bg-muted p-3 text-xs">
									{error.stack}
								</pre>
							</div>
						)}
					</div>
				)}

				{/* Action Buttons */}
				<div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
					<button
						onClick={handleReset}
						className="inline-flex items-center justify-center rounded-md bg-primary px-6 py-3 text-sm font-medium text-primary-foreground shadow transition-colors hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
					>
						Try Again
					</button>
					<button
						onClick={handleReload}
						className="inline-flex items-center justify-center rounded-md border border-input bg-background px-6 py-3 text-sm font-medium shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
					>
						Reload Application
					</button>
				</div>

				{/* Help Text */}
				<p className="text-sm text-muted-foreground">
					If this problem persists, please contact support or try again later.
				</p>
			</div>
		</div>
	)
}
