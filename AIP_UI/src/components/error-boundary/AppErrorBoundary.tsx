import React from 'react'
import { ErrorFallback } from './ErrorFallback'
import { logger } from '@/utils/logger'

interface ErrorBoundaryState {
	hasError: boolean
	error: Error | null
	errorInfo: React.ErrorInfo | null
}

interface AppErrorBoundaryProps {
	children: React.ReactNode
	fallback?: React.ComponentType<{ error: Error; resetErrorBoundary: () => void }>
}

/**
 * Application-level Error Boundary
 * Catches and handles errors in the React component tree
 * Logs errors for monitoring and displays user-friendly error UI
 */
export class AppErrorBoundary extends React.Component<AppErrorBoundaryProps, ErrorBoundaryState> {
	constructor(props: AppErrorBoundaryProps) {
		super(props)
		this.state = {
			hasError: false,
			error: null,
			errorInfo: null,
		}
	}

	static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
		// Update state so the next render will show the fallback UI
		return {
			hasError: true,
			error,
		}
	}

	componentDidCatch(error: Error, errorInfo: React.ErrorInfo): void {
		// Log error details for debugging and monitoring
		logger.error('React Error Boundary Caught Error', {
			error: {
				name: error.name,
				message: error.message,
				stack: error.stack,
			},
			componentStack: errorInfo.componentStack,
			timestamp: new Date().toISOString(),
		})

		// Update state with error info
		this.setState({
			errorInfo,
		})

		// In production, you would send this to an error tracking service
		// Example: Sentry, LogRocket, etc.
		if (import.meta.env.PROD) {
			// TODO: Send to error tracking service
			// Example: Sentry.captureException(error, { contexts: { react: { componentStack: errorInfo.componentStack } } })
		}
	}

	resetErrorBoundary = (): void => {
		this.setState({
			hasError: false,
			error: null,
			errorInfo: null,
		})
	}

	render(): React.ReactNode {
		if (this.state.hasError && this.state.error) {
			const FallbackComponent = this.props.fallback || ErrorFallback

			return (
				<FallbackComponent
					error={this.state.error}
					resetErrorBoundary={this.resetErrorBoundary}
				/>
			)
		}

		return this.props.children
	}
}
