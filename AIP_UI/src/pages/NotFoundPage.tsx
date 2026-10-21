import React from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowLeft, LayoutGrid } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useAppChrome } from '@/lib/app-chrome'

export const NotFoundPage: React.FC = () => {
	useAppChrome()
	const navigate = useNavigate()

	return (
		<div className="flex min-h-[70vh] w-full items-center justify-center bg-background px-4 py-16">
			<div className="w-full max-w-lg">
				<p className="figures text-sm font-medium text-muted-foreground">Error 404</p>
				<h1 className="mt-2 text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
					This page doesn&apos;t exist
				</h1>
				<p className="mt-3 text-base leading-relaxed text-muted-foreground">
					The link may be out of date, or the page may have moved. Check the address, or carry on from the dashboard.
				</p>

				<div className="mt-8 flex flex-col gap-2 sm:flex-row">
					<Button onClick={() => navigate('/')}>
						<LayoutGrid className="h-4 w-4" />
						Go to dashboard
					</Button>
					<Button variant="outline" onClick={() => navigate(-1)}>
						<ArrowLeft className="h-4 w-4" />
						Go back
					</Button>
				</div>

				<div className="mt-10 border-t border-border pt-5 text-sm text-muted-foreground">
					Looking for something else? Try{' '}
					<Link to="/operations/incident-report" className="font-medium text-foreground underline-offset-4 hover:underline">
						incident reports
					</Link>
					,{' '}
					<Link to="/profile" className="font-medium text-foreground underline-offset-4 hover:underline">
						your profile
					</Link>{' '}
					or{' '}
					<Link to="/contact" className="font-medium text-foreground underline-offset-4 hover:underline">
						contact support
					</Link>
					.
				</div>
			</div>
		</div>
	)
}

export default NotFoundPage
