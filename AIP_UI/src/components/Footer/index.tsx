import React from 'react';
import { Link } from 'react-router-dom';
import { COMPANY_INFO } from './constants';
import { FooterProps } from './types';

/**
 * Application footer with product summary and support contact.
 */
export const Footer: React.FC<Partial<FooterProps>> = ({
	className = '',
	companyInfo = COMPANY_INFO,
}) => {
	return (
		<footer
			className={`w-full bg-gradient-to-b from-black via-slate-950 to-black text-white ${className}`}
		>
			<div className="container mx-auto px-4 sm:px-6 lg:px-8 py-10 sm:py-14 space-y-8 sm:space-y-10">
				<section className="relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/70 px-6 py-8 sm:px-10 sm:py-10">
					<div className="absolute inset-0 pointer-events-none">
						<div className="absolute -top-24 -right-10 h-40 w-40 rounded-full bg-blue-500/10 blur-3xl" />
						<div className="absolute -bottom-24 -left-10 h-40 w-40 rounded-full bg-emerald-500/10 blur-3xl" />
					</div>

					<div className="relative space-y-4 sm:space-y-5 text-center sm:text-left">
						<h2 className="text-2xl sm:text-3xl lg:text-4xl font-semibold leading-tight">
							DibangaOps Crime Portal –{' '}
							<span className="text-emerald-300"> AI Incident Intelligence Platform</span>
						</h2>

						<p className="text-sm sm:text-base text-slate-300">
							Facial Recognition&nbsp;| Incident Analytics&nbsp;| Evidence Tracking
						</p>

						<div className="mt-5 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
							<div className="space-y-2 text-xs sm:text-sm text-slate-300">
								<nav className="flex flex-wrap items-center justify-center sm:justify-start gap-x-3 gap-y-1">
									<Link to="/about" className="hover:text-slate-50 underline-offset-4 hover:underline">
										About
									</Link>
									<span aria-hidden>|</span>
									<Link to="/privacy" className="hover:text-slate-50 underline-offset-4 hover:underline">
										Privacy
									</Link>
									<span aria-hidden>|</span>
									<Link to="/terms" className="hover:text-slate-50 underline-offset-4 hover:underline">
										Terms
									</Link>
									<span aria-hidden>|</span>
									<Link to="/privacy" className="hover:text-slate-50 underline-offset-4 hover:underline">
										Security Policy
									</Link>
									<span aria-hidden>|</span>
									<Link to="/contact" className="hover:text-slate-50 underline-offset-4 hover:underline">
										Support
									</Link>
								</nav>
								<p className="text-[11px] sm:text-xs text-slate-400">
									Powered by AI Vision Engine
								</p>
								<p className="text-[11px] sm:text-xs text-slate-500">
									System Version: 1.0.1
								</p>
							</div>

							<div className="flex justify-center sm:justify-end">
								<Link
									to="/contact"
									className="inline-flex items-center justify-center rounded-full bg-blue-500 px-5 py-2.5 text-sm font-medium text-white shadow-lg shadow-blue-500/30 transition hover:bg-blue-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-400 focus-visible:ring-offset-2 focus-visible:ring-offset-slate-950"
									aria-label="Open contact page to get support"
								>
									Contact Support
									<span aria-hidden className="ml-2 text-base">
										&gt;
									</span>
								</Link>
							</div>
						</div>
					</div>
				</section>

				<section className="border-t border-slate-800 pt-4 sm:pt-6">
					<p className="text-center text-xs sm:text-sm text-slate-400">
						© 2026 {companyInfo.name}
					</p>
				</section>
			</div>
		</footer>
	);
};

// Export all subcomponents and types
export * from './types';
export * from './constants';
export * from './FooterSection';
export * from './CompanyInformation';
export * from './ContactInformation';
export * from './QuickLinks';
export * from './Copyright'; 