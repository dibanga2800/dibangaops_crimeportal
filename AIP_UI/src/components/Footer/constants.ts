import { CompanyInfo, ContactInfo, QuickLink } from './types';

export const COMPANY_INFO: CompanyInfo = {
  name: 'DibangOps Crime Portal',
  address: [],
};

export const CONTACT_INFO: ContactInfo = {
  phone: '',
  supportText: 'For product support or deployment help, contact David Ibanga using the secure in-app contact form.',
};

export const QUICK_LINKS: QuickLink[] = [
  {
    to: '/operations/incident-report',
    label: 'Incident Report',
    ariaLabel: 'Create Incident Report',
  },
  {
    to: '/operations/incident-graph',
    label: 'Incident Graph',
    ariaLabel: 'Incident Graph',
  },
  {
    to: '/operations/crime-intelligence',
    label: 'Crime Intelligence',
    ariaLabel: 'Crime Intelligence',
  },
]; 