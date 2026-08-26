import type { ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ShieldCheck } from 'lucide-react'

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

export const PublicPageShell = ({ title, description, children }: PublicPageShellProps) => {
	const location = useLocation()

	return (
		<div className="min-h-screen flex flex-col bg-slate-50 text-slate-900">
			<header className="border-b border-slate-200 bg-white">
				<div className="mx-auto flex h-16 max-w-5xl items-center justify-between gap-4 px-4 sm:px-6">
					<Link
						to="/login"
						className="flex min-w-0 items-center gap-2.5 text-slate-900 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2"
					>
						<span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-slate-900">
							<ShieldCheck className="h-4 w-4 text-brand-400" aria-hidden="true" />
						</span>
						<span className="truncate text-sm font-semibold tracking-tight">
							DibangOps Crime Portal
						</span>
					</Link>

					<nav
						aria-label="Legal"
						className="hidden items-center gap-5 text-[13px] text-slate-600 sm:flex"
					>
						{NAV_LINKS.map(({ to, label }) => {
							const isActive = location.pathname === to
							return (
								<Link
									key={to}
									to={to}
									aria-current={isActive ? 'page' : undefined}
									className={`underline-offset-4 hover:text-slate-900 hover:underline focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 ${
										isActive ? 'font-semibold text-slate-900' : ''
									}`}
								>
									{label}
								</Link>
							)
						})}
					</nav>

					<Link
						to="/login"
						className="shrink-0 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-[13px] font-medium text-slate-800 hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2"
					>
						Sign in
					</Link>
				</div>
			</header>

			<main className="flex-1">
				<div className="mx-auto w-full max-w-3xl px-4 py-10 sm:px-6 sm:py-14">
					<div className="border border-slate-200 bg-white px-6 py-8 sm:px-10 sm:py-10">
						<p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-500">
							DibangOps Crime Portal
						</p>
						<h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-900 sm:text-[1.65rem]">
							{title}
						</h1>
						{description ? (
							<p className="mt-3 max-w-2xl text-sm leading-relaxed text-slate-600">
								{description}
							</p>
						) : null}
						<div className="mt-8 space-y-6 text-sm leading-relaxed text-slate-700">
							{children}
						</div>
					</div>
				</div>
			</main>

			<footer className="border-t border-slate-200 bg-white">
				<div className="mx-auto flex max-w-5xl flex-col gap-2 px-4 py-5 text-[12px] text-slate-500 sm:flex-row sm:items-center sm:justify-between sm:px-6">
					<p>
						&copy; {new Date().getFullYear()} DibangOps. Developed by David Ibanga.
						{' '}Proprietary and confidential.
					</p>
					<nav className="flex items-center gap-4 sm:hidden" aria-label="Legal">
						{NAV_LINKS.map(({ to, label }) => (
							<Link
								key={to}
								to={to}
								className="hover:text-slate-800 underline-offset-4 hover:underline"
							>
								{label}
							</Link>
						))}
					</nav>
				</div>
			</footer>
		</div>
	)
}
