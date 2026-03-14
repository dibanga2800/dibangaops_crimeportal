import type { CustomerPage } from "@/types/customer"
import type { CustomerType } from "@/types/customer"

export const CUSTOMER_PAGES: Record<string, CustomerPage> = {
  'incident-graph': {
    id: 'customer-incident-graph',
    title: 'Incident Graph',
    description: 'Visual representation of incident trends and patterns',
    enabled: true,
    requiredForTypes: [], // No required pages - customers needs vary
    path: '/customer/incident-graph',
    readOnly: true,
    category: 'reporting',
    icon: 'BarChart3'
  },
  'incident-report': {
    id: 'customer-incident-report',
    title: 'Incident Report',
    description: 'View and manage incident reports for this company',
    enabled: true,
    requiredForTypes: [], // No required pages - customers needs vary
    path: '/customer/incident-report',
    readOnly: false,
    category: 'security',
    icon: 'AlertTriangle'
  },
}

// Helper function to get pages based on customer type (now returns empty since no pages are required)
export const getPagesByCustomerType = (customerType: CustomerType | string): CustomerPage[] => {
  // Since customer needs vary, no pages are automatically required for any customer type
  // Return empty array - pages should be selected based on individual customer needs
  return [];
};

export const getPagesByCategory = (category: CustomerPage['category']): CustomerPage[] => {
  return Object.values(CUSTOMER_PAGES).filter(page => page.category === category)
}

export const CUSTOMER_PAGE_CATEGORIES = {
  reporting: 'Reporting & Analytics',
  activity: 'Daily Activities', 
  security: 'Security & Safety',
  support: 'Support Services'
} as const 