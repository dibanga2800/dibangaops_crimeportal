import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import type { Customer } from "@/types/customer"
import useAuth from "@/hooks/useAuth"
import { RefreshCw } from "lucide-react"
import { customerService } from "@/services/customerService"
import { customerPageAccessCache } from "@/services/customerPageAccessCache"
import type { CustomerPageAccessPage } from "@/api/customerPageAccess"

export default function CustomerReporting() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [selectedCustomer, setSelectedCustomer] = useState<string>("")
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loading, setLoading] = useState(true)
  const [refreshTrigger, setRefreshTrigger] = useState(0)
  const [pageState, setPageState] = useState<{
    isLoading: boolean
    error: string | null
    pages: CustomerPageAccessPage[]
  }>({
    isLoading: false,
    error: null,
    pages: []
  })

  // Refresh data when needed
  const refreshData = () => setRefreshTrigger(prev => prev + 1)

  useEffect(() => {
    const loadCustomers = async () => {
      try {
        setLoading(true)
        
        // Load customer data from customer store (which uses cached data)
        let customerData = await customerService.getAllCustomers()
        
        // For officers, filter to only assigned customers
        if (user?.role === 'store') {
          const assignedCustomerIds = user.assignedCustomerIds || []
          // Normalize IDs to numbers for comparison (customer.id might be string or number)
          const assignedIdsAsNumbers = assignedCustomerIds.map(id => Number(id)).filter(id => !isNaN(id))
          
          customerData = customerData.filter((customer: any) => {
            const customerId = Number(customer.id)
            const isAssigned = !isNaN(customerId) && assignedIdsAsNumbers.includes(customerId)
            return isAssigned
          })
          
          console.log('🔄 [CustomerReporting] Filtered customers for officer:', {
            assignedCustomerIds: assignedCustomerIds,
            assignedIdsAsNumbers: assignedIdsAsNumbers,
            filteredCount: customerData.length,
            filteredCustomerIds: customerData.map((c: any) => c.id)
          })
        }
        
        setCustomers(customerData)
        console.log('✅ [CustomerReporting] Loaded customers from store:', customerData.length)
        
      } catch (error) {
        console.error('❌ [CustomerReporting] Error loading customers from store:', error)
        setCustomers([])
      } finally {
        setLoading(false)
      }
    }

    loadCustomers()
  }, [user, refreshTrigger])

  // Listen for customer data updates to refresh data automatically
  useEffect(() => {
    const handleCustomerDataUpdate = (event: CustomEvent) => {
      console.log('🔄 [CustomerReporting] Received customer data update:', event.detail)
      refreshData()
    }

    window.addEventListener('customer-data-updated', handleCustomerDataUpdate as EventListener)
    
    return () => {
      window.removeEventListener('customer-data-updated', handleCustomerDataUpdate as EventListener)
    }
  }, [refreshData])

  useEffect(() => {
    let isActive = true

    if (!selectedCustomer) {
      setPageState({
        isLoading: false,
        error: null,
        pages: []
      })
      return
    }

    const loadCustomerPages = async () => {
      try {
        setPageState(prev => ({ ...prev, isLoading: true, error: null, pages: [] }))
        const customerId = Number(selectedCustomer)
        if (Number.isNaN(customerId)) {
          throw new Error('Invalid customer selection')
        }

        const access = await customerPageAccessCache.get(customerId)
        const assignedPages = access.availablePages
          .filter(page => access.assignedPageIds.includes(page.pageId))
          .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))

        if (!isActive) return

        setPageState({
          isLoading: false,
          error: null,
          pages: assignedPages
        })

        console.log('🔍 [CustomerReporting] Loaded assigned pages for customer:', {
          customerId,
          assignedCount: assignedPages.length
        })
      } catch (error) {
        if (!isActive) return
        const message = error instanceof Error ? error.message : 'Failed to load company pages'
        console.error('❌ [CustomerReporting] Error loading pages:', error)
        setPageState({
          isLoading: false,
          error: message,
          pages: []
        })
      }
    }

    loadCustomerPages()

    return () => {
      isActive = false
    }
  }, [selectedCustomer])

  const handlePageSelect = (page: CustomerPageAccessPage) => {
    if (page?.path) {
      navigate(page.path)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[200px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600" />
      </div>
    )
  }

  if (!loading && customers.length === 0) {
    return (
      <div className="container mx-auto p-4">
        <div className="flex flex-col gap-2">
          <h1 className="text-2xl font-bold">Company Reporting</h1>
          <Card className="p-6 text-center">
            <p className="text-muted-foreground">
              No customers are currently assigned to you.
              Please contact your administrator to get access to customer reports.
            </p>
          </Card>
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto p-4 space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold">Company Reporting</h1>
          <p className="text-muted-foreground">
            Select a customer to view their reports and metrics
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={refreshData}
          disabled={loading}
          className="flex items-center gap-2"
        >
          <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      <div className="w-full max-w-md">
        <Select value={selectedCustomer} onValueChange={setSelectedCustomer}>
          <SelectTrigger>
            <SelectValue placeholder="Select a customer" />
          </SelectTrigger>
          <SelectContent>
            {customers.map(customer => (
              <SelectItem key={customer.id} value={customer.id.toString()}>
                {customer.companyName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {selectedCustomer && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {pageState.isLoading && (
            <div className="col-span-full flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600" />
            </div>
          )}

          {!pageState.isLoading && pageState.error && (
            <div className="col-span-full text-center text-destructive text-sm">
              {pageState.error}
            </div>
          )}

          {!pageState.isLoading && !pageState.error && pageState.pages.map(page => (
            <Card
              key={page.pageId}
              className="p-4 cursor-pointer hover:bg-accent transition-colors"
              onClick={() => handlePageSelect(page)}
            >
              <h3 className="font-semibold mb-2">{page.title}</h3>
              <p className="text-sm text-muted-foreground">{page.description}</p>
              {page.category && (
                <span className="inline-block mt-2 text-xs bg-muted px-2 py-1 rounded">
                  {page.category}
                </span>
              )}
            </Card>
          ))}
        </div>
      )}

      {selectedCustomer && !pageState.isLoading && !pageState.error && pageState.pages.length === 0 && (
        <div className="text-center py-8">
          <p className="text-muted-foreground">
            No pages have been configured for this company.
            Please contact your administrator to set up page access.
          </p>
        </div>
      )}
    </div>
  )
} 