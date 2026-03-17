import React from 'react';
import { Link } from 'react-router-dom';
import { ContactInfo } from './types';
import { FooterSection } from './FooterSection';

interface ContactInformationProps {
  info: ContactInfo;
}

export const ContactInformation: React.FC<ContactInformationProps> = () => {
  return (
		<FooterSection title="Contact Support">
			<div className="space-y-2 sm:space-y-3">
				<p className="text-xs sm:text-sm text-gray-300">
					For implementation help, onboarding, or incident-response questions, contact{' '}
					<span className="font-medium">David Ibanga</span> using the secure contact form.
				</p>
				<Link
					to="/contact"
					className="inline-flex items-center text-sm font-medium text-blue-400 hover:text-blue-300 transition-colors"
					aria-label="Open contact form to reach David Ibanga"
				>
					Open contact form
					<span aria-hidden className="ml-1">
						&gt;
					</span>
				</Link>
			</div>
		</FooterSection>
  );
}; 