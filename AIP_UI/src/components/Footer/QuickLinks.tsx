import React from 'react';
import { Link } from 'react-router-dom';
import { usePageAccess } from '@/contexts/PageAccessContext';
import { QuickLink } from './types';
import { FooterSection } from './FooterSection';

interface QuickLinksProps {
  links: QuickLink[];
}

export const QuickLinks: React.FC<QuickLinksProps> = ({ links }) => {
	const { hasAccess } = usePageAccess();
	const accessibleLinks = React.useMemo(
		() => links.filter((link) => hasAccess(link.to)),
		[links, hasAccess],
	);

	if (accessibleLinks.length === 0) {
		return null;
	}

	return (
		<FooterSection title="Quick Links">
			<nav className="space-y-2 sm:space-y-3">
				{accessibleLinks.map((link) => (
					<Link
						key={link.to}
						to={link.to}
						className="block text-gray-300 hover:text-blue-400 transition-colors py-1 sm:py-0.5"
						aria-label={link.ariaLabel}
					>
						{link.label}
					</Link>
				))}
			</nav>
		</FooterSection>
	);
}; 