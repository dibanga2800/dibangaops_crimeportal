import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import type { Customer, CustomerPageId } from "@/types/customer"
import { CUSTOMER_PAGES } from "@/config/customerPages"
import { cn } from "@/lib/utils"
import { FileText, BarChart, Calendar, Wrench, FileCheck, Building, ArrowLeft } from "lucide-react"
import { customerPageAccessCache } from "@/services/customerPageAccessCache"
import type { CustomerPageAccessPage } from "@/api/customerPageAccess"

interface CustomerReportingSectionProps {
  customers: Customer[]
  onNavigate: (customerId: string, pageId: CustomerPageId) => void
}

const PAGE_ICONS = {
  'incident-report': FileText,
  'incident-graph': BarChart,
} as const

export function CustomerReportingSection({ customers, onNavigate }: CustomerReportingSectionProps) {
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null)
  const [pageState, setPageState] = useState<{
    isLoading: boolean
    error: string | null
    pages: CustomerPageAccessPage[]
  }>({
    isLoading: false,
    error: null,
    pages: []
  })

  const handleCustomerSelect = async (customer: Customer) => {
    setSelectedCustomer(customer)

    const numericId = Number(customer.id)
    if (Number.isNaN(numericId)) {
      setPageState({
        isLoading: false,
        error: 'Invalid customer identifier',
        pages: []
      })
      return
    }

    setPageState({ isLoading: true, error: null, pages: [] })

    try {
      const access = await customerPageAccessCache.get(numericId)
      const assignedPages = access.availablePages
        .filter(page => access.assignedPageIds.includes(page.pageId))
        .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))

      setPageState({
        isLoading: false,
        error: null,
        pages: assignedPages
      })
    } catch (error) {
      setPageState({
        isLoading: false,
        error: error instanceof Error ? error.message : 'Failed to load company pages',
        pages: []
      })
    }
  }

  const handleBack = () => {
    setSelectedCustomer(null)
    setPageState({
      isLoading: false,
      error: null,
      pages: []
    })
  }

  const resolvePageKey = (page: CustomerPageAccessPage): CustomerPageId | null => {
    const entry = Object.entries(CUSTOMER_PAGES).find(([_, config]) => config.id === page.pageId)
    return entry ? (entry[0] as CustomerPageId) : null
  }

  if (selectedCustomer) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Button variant="ghost" onClick={handleBack} className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to Customers
          </Button>
          <h2 className="text-xl font-semibold">{selectedCustomer.companyName}</h2>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Available Reports</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {pageState.isLoading && (
                <div className="col-span-full text-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-purple-600 mx-auto mb-3" />
                  <p className="text-sm text-muted-foreground">Loading assigned pages...</p>
                </div>
              )}

              {!pageState.isLoading && pageState.error && (
                <div className="col-span-full text-center py-8 text-destructive text-sm">
                  {pageState.error}
                </div>
              )}

              {!pageState.isLoading && !pageState.error && pageState.pages.map(page => {
                const pageKey = resolvePageKey(page)
                const Icon = pageKey ? (PAGE_ICONS[pageKey as keyof typeof PAGE_ICONS] || FileText) : FileText
                return (
                  <Button
                    key={page.pageId}
                    variant="outline"
                    className={cn(
                      "h-auto p-4 flex flex-col items-center text-center gap-2",
                      "hover:bg-purple-50 hover:border-purple-200 transition-colors"
                    )}
                    onClick={() => onNavigate(String(selectedCustomer.id), (pageKey ?? page.pageId) as CustomerPageId)}
                  >
                    <Icon className="h-6 w-6 text-purple-600" />
                    <div>
                      <h3 className="font-medium">{page.title}</h3>
                      <p className="text-sm text-gray-500 mt-1">{page.description}</p>
                    </div>
                    <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                      Assigned
                    </span>
                  </Button>
                )
              })}
            </div>

            {!pageState.isLoading && !pageState.error && pageState.pages.length === 0 && (
              <div className="text-center py-8 text-sm text-muted-foreground">
                This company has no assigned pages.
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Company Reporting</h2>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {customers.map(customer => {
          // Get enabled pages count from pageAssignments if available, fallback to viewConfig
          return (
            <Button
              key={customer.id}
              variant="outline"
              className={cn(
                "h-auto p-4 flex flex-col items-center text-center gap-2",
                "hover:bg-purple-50 hover:border-purple-200 transition-colors"
              )}
              onClick={() => handleCustomerSelect(customer)}
            >
              <Building className="h-6 w-6 text-purple-600" />
              <div>
                <h3 className="font-medium">{customer.companyName}</h3>
                <p className="text-sm text-gray-500 mt-1">
                  {customer.address.town}, {customer.address.county}
                </p>
                <p className="text-xs text-purple-600 mt-1">
                  {customer.pageAssignments ? Object.entries(customer.pageAssignments).filter(([_, assignment]) => (assignment as any).enabled).length
                    : customer.viewConfig?.enabledPages?.length || 0} Reports Available
                </p>
              </div>
            </Button>
          )
        })}
      </div>
    </div>
  )
} 