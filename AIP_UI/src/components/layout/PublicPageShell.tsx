import type { ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ProductMark } from './ProductMark'
import { useAppChrome } from '@/lib/app-chrome'
import { cn } from '@/lib/utils'

interface PublicPageShellProps {
	title: string
	description?: string
	children: ReactNode
}

const NAV_LINKS = [
	{ to: '/about', label: 'About' },
	{ to: '/privacy', label: 'Privacy' },
	{ to: '/terms', label: 'Terms' },
] as const

/**
 * Shell for the public, signed-out pages (About, Privacy, Terms).
 * Reads like a short document: product mark, a left rail of sibling pages,
 * and a single readable column.
 */
export const PublicPageShell = ({ title, description, children }: PublicPageShellProps) => {
	useAppChrome()
	const location = useLocation()

	return (
		<div className="flex min-h-screen flex-col bg-background text-foreground">
			<header className="border-b border-border bg-card">
				<div className="mx-auto flex h-16 max-w-5xl items-center justify-between gap-4 px-4 sm:px-6">
					<ProductMark tone="light" to="/login" />
					<Link
						to="/login"
						className="shrink-0 rounded-md border border-input bg-card px-3 py-1.5 text-[13px] font-medium text-foreground shadow-sm transition-colors hover:bg-muted"
					>
						Sign in
					</Link>
				</div>
			</header>

			<main className="flex-1">
				<div className="mx-auto grid w-full max-w-5xl gap-8 px-4 py-10 sm:px-6 sm:py-14 md:grid-cols-[10rem_minmax(0,1fr)]">
					<nav aria-label="Legal" className="md:pt-1.5">
						<ul className="flex gap-1 md:flex-col">
							{NAV_LINKS.map(({ to, label }) => {
								const isActive = location.pathname === to
								return (
									<li key={to}>
										<Link
											to={to}
											aria-current={isActive ? 'page' : undefined}
											className={cn(
												'relative block rounded-md px-3 py-1.5 text-sm transition-colors',
												isActive
													? 'bg-card font-medium text-foreground shadow-[0_0_0_1px_hsl(var(--border))]'
													: 'text-muted-foreground hover:text-foreground',
											)}
										>
											{label}
										</Link>
									</li>
								)
							})}
						</ul>
					</nav>

					<article className="min-w-0 max-w-[68ch]">
						<h1 className="text-3xl font-semibold leading-tight tracking-tight text-foreground sm:text-[2.25rem]">
							{title}
						</h1>
						{description ? (
							<p className="mt-3 text-base leading-relaxed text-muted-foreground">{description}</p>
						) : null}
						<div
							className={cn(
								'mt-10 space-y-6 border-t border-border pt-8 text-[15px] leading-7 text-foreground/85',
								'[&_h2]:text-base [&_h2]:font-semibold [&_h2]:text-foreground',
								'[&_ul.divide-y]:overflow-hidden [&_ul.divide-y]:rounded-lg [&_ul.divide-y]:bg-card',
							)}
						>
							{children}
						</div>
					</article>
				</div>
			</main>

			<footer className="border-t border-border">
				<div className="mx-auto flex max-w-5xl flex-col gap-1 px-4 py-5 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between sm:px-6">
					<p>&copy; {new Date().getFullYear()} DibangOps. Developed by David Ibanga.</p>
					<p>Proprietary and confidential.</p>
				</div>
			</footer>
		</div>
	)
}
