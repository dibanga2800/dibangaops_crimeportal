import React from 'react'
import { useNavigate } from 'react-router-dom'

export const NotFoundPage: React.FC = () => {
	const navigate = useNavigate()

	const handleGoHome = () => {
		navigate('/')
	}

	const handleGoBack = () => {
		navigate(-1)
	}

	return (
		<div className="flex min-h-screen w-full flex-col items-center justify-center bg-background p-8">
			<div className="max-w-2xl space-y-6 text-center">
				{/* 404 Illustration */}
				<div className="relative">
					<h1 className="text-[150px] font-bold leading-none text-muted-foreground/20 sm:text-[200px]">
						404
					</h1>
					<div className="absolute inset-0 flex items-center justify-center">
						<svg
							className="h-24 w-24 text-muted-foreground"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
							aria-hidden="true"
						>
							<path
								strokeLinecap="round"
								strokeLinejoin="round"
								strokeWidth={1.5}
								d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
							/>
						</svg>
					</div>
				</div>

				{/* Error Message */}
				<div className="space-y-3">
					<h2 className="text-3xl font-bold tracking-tight text-foreground">
						Page Not Found
					</h2>
					<p className="text-lg text-muted-foreground">
						Sorry, we couldn't find the page you're looking for. It might have been moved or deleted.
					</p>
				</div>

				{/* Navigation Buttons */}
				<div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
					<button
						onClick={handleGoHome}
						className="inline-flex items-center justify-center gap-2 rounded-md bg-primary px-6 py-3 text-sm font-medium text-primary-foreground shadow transition-colors hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
					>
						<svg
							className="h-4 w-4"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
							aria-hidden="true"
						>
							<path
								strokeLinecap="round"
								strokeLinejoin="round"
								strokeWidth={2}
								d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
							/>
						</svg>
						Go to Homepage
					</button>
					<button
						onClick={handleGoBack}
						className="inline-flex items-center justify-center gap-2 rounded-md border border-input bg-background px-6 py-3 text-sm font-medium shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
					>
						<svg
							className="h-4 w-4"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
							aria-hidden="true"
						>
							<path
								strokeLinecap="round"
								strokeLinejoin="round"
								strokeWidth={2}
								d="M10 19l-7-7m0 0l7-7m-7 7h18"
							/>
						</svg>
						Go Back
					</button>
				</div>

				{/* Helpful Links */}
				<div className="pt-8">
					<p className="mb-4 text-sm font-medium text-muted-foreground">
						You might find these helpful:
					</p>
					<div className="flex flex-wrap justify-center gap-4 text-sm">
						<button
							onClick={() => navigate('/dashboard')}
							className="text-primary hover:underline focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
						>
							Dashboard
						</button>
						<span className="text-muted-foreground">•</span>
						<button
							onClick={() => navigate('/settings')}
							className="text-primary hover:underline focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
						>
							Settings
						</button>
						<span className="text-muted-foreground">•</span>
						<button
							onClick={() => navigate('/profile')}
							className="text-primary hover:underline focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
						>
							Profile
						</button>
					</div>
				</div>
			</div>
		</div>
	)
}

export default NotFoundPage
