import React from 'react';
import { Link } from 'react-router-dom';
import { COMPANY_INFO } from './constants';
import { FooterProps } from './types';

const FOOTER_LINKS = [
	{ to: '/about', label: 'About' },
	{ to: '/privacy', label: 'Privacy and security' },
	{ to: '/terms', label: 'Terms' },
	{ to: '/contact', label: 'Contact support' },
] as const;

/**
 * Application footer: product name, version and the support/legal links.
 * Kept deliberately quiet so it never competes with page content.
 */
export const Footer: React.FC<Partial<FooterProps>> = ({
	className = '',
	companyInfo = COMPANY_INFO,
}) => {
	return (
		<footer className={`border-t border-border bg-background ${className}`}>
			<div className="mx-auto flex w-full max-w-[1480px] flex-col gap-3 px-4 py-5 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
				<p>
					<span className="font-medium text-foreground/80">{companyInfo.name}</span>
					<span className="ml-2">Version 1.0.1</span>
					<span className="ml-2">&copy; {new Date().getFullYear()}</span>
				</p>
				<nav aria-label="Footer" className="flex flex-wrap items-center gap-x-5 gap-y-1">
					{FOOTER_LINKS.map(({ to, label }) => (
						<Link
							key={to}
							to={to}
							className="rounded-sm hover:text-foreground hover:underline underline-offset-4"
						>
							{label}
						</Link>
					))}
				</nav>
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