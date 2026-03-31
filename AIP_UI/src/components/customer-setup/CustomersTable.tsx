import { useState, useEffect, useCallback } from "react"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog"
import { Plus, ChevronLeft, ChevronRight, Search, Pencil, Trash2 } from "lucide-react"
import { useToast } from "@/hooks/use-toast"
import { CustomerDialog } from "./CustomerDialog"
import { CustomerTableRow } from "./CustomerTableRow"
import { customerService } from "@/services/customerService"
import type { Customer } from "@/types/customer"

interface CustomersTableProps {
  onCustomerSelect: (customerId: string | null) => void
  selectedCustomerId: string | null
  onDataChange?: () => void
}

export function CustomersTable({ onCustomerSelect, selectedCustomerId, onDataChange }: CustomersTableProps) {
  const { toast } = useToast()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | undefined>()
  const [selectedRows, setSelectedRows] = useState<string[]>([])
  const [searchQuery, setSearchQuery] = useState("")
  const [currentPage, setCurrentPage] = useState(1)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [customerToDelete, setCustomerToDelete] = useState<Customer | null>(null)
  const itemsPerPage = 10

  const cleanup = useCallback(() => {
    setDialogOpen(false)
    setSelectedCustomer(undefined)
    setSelectedRows([])
    setDeleteDialogOpen(false)
    setCustomerToDelete(null)
  }, [])

  useEffect(() => {
    console.log("CustomersTable mounted")
    return () => {
      console.log("CustomersTable unmounting")
      cleanup()
    }
  }, [cleanup])

  // Cleanup when component becomes hidden
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.hidden) {
        cleanup()
      }
    }
    
    document.addEventListener('visibilitychange', handleVisibilityChange)
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [cleanup])

  const handleEdit = (customer: Customer) => {
    if (!customer) {
      toast({
        title: "Error",
        description: "Unable to load customer data for editing.",
        variant: "destructive"
      })
      return
    }
    
    setSelectedCustomer(customer)
    setDialogOpen(true)
  }

  const handleDelete = (customer: Customer) => {
    console.log('🔧 [CustomersTable] handleDelete called for customer:', {
      id: customer.id,
      name: customer.companyName,
      idType: typeof customer.id,
      idString: String(customer.id)
    })
    setCustomerToDelete(customer)
    setDeleteDialogOpen(true)
  }

  const confirmDelete = async () => {
    if (customerToDelete) {
      try {
        console.log('🔧 [CustomersTable] confirmDelete - attempting to delete customer:', {
          id: customerToDelete.id,
          name: customerToDelete.companyName,
          idType: typeof customerToDelete.id
        })
        
        const result = await customerService.deleteCustomer(String(customerToDelete.id))
        
        if (result.success) {
          console.log('🔧 [CustomersTable] confirmDelete - customer deleted successfully')
          
          // If the deleted customer was selected, clear the selection
          if (selectedCustomerId === String(customerToDelete.id)) {
            onCustomerSelect(null)
          }
          
          toast({
            title: "Customer Deleted",
            description: `Customer has been successfully deleted.`,
            variant: "default"
          })
          
          forceUpdate()
          onDataChange?.() // Notify parent of data change
        } else {
          throw new Error(result.error || 'Failed to delete customer')
        }
      } catch (error) {
        console.error('❌ [CustomersTable] confirmDelete - error:', error)
        toast({
          title: "Delete Failed",
          description: error instanceof Error ? error.message : "Failed to delete customer. Please try again.",
          variant: "destructive"
        })
      }
      
      setDeleteDialogOpen(false)
      setCustomerToDelete(null)
    }
  }

  const handleSave = async (updatedCustomer: Customer) => {
    try {
      console.log('🔧 [CustomersTable] handleSave - received customer data:', {
        id: updatedCustomer.id,
        companyName: updatedCustomer.companyName,
        pageAssignmentsCount: Object.keys(updatedCustomer.pageAssignments || {}).length,
        enabledPagesCount: updatedCustomer.viewConfig?.enabledPages?.length || 0,
        pageAssignments: updatedCustomer.pageAssignments,
        enabledPages: updatedCustomer.viewConfig?.enabledPages
      })
      
      // Determine if this is a new customer based on ID
      // New customers have IDs starting with 'CUST' (temporary IDs)
      // Existing customers have numeric IDs from the database
      const idString = String(updatedCustomer.id || '')
      const isNew = idString.startsWith('CUST')
      
      console.log('🔧 [CustomersTable] handleSave - customer type:', { 
        id: updatedCustomer.id, 
        idString, 
        isNew 
      })
      
      let result
      if (isNew) {
        // Create new customer via customerService
        console.log('🔧 [CustomersTable] handleSave - creating new customer')
        result = await customerService.createCustomer(updatedCustomer)
      } else {
        // Update existing customer via customerService
        console.log('🔧 [CustomersTable] handleSave - updating existing customer')
        result = await customerService.updateCustomer(updatedCustomer)
      }
      
      console.log('🔧 [CustomersTable] handleSave - service response:', result)
      
      if (result.success) {
        toast({
          title: isNew ? "Customer Created" : "Customer Updated",
          description: `${updatedCustomer.companyName} has been successfully ${isNew ? 'created' : 'updated'}.`,
          variant: "default"
        })
        
        setDialogOpen(false)
        forceUpdate()
        onDataChange?.() // Notify parent of data change
      } else {
        throw new Error(result.error || 'Failed to save customer')
      }
    } catch (error) {
      console.error('❌ [CustomersTable] handleSave - error:', error)
      toast({
        title: "Save Failed",
        description: error instanceof Error ? error.message : "Failed to save customer. Please try again.",
        variant: "destructive"
      })
    }
  }

  // Force re-render function to reflect data changes
  const [updateTrigger, setUpdateTrigger] = useState(0)
  const forceUpdate = () => setUpdateTrigger(prev => prev + 1)

  // Get customers from API and filter based on search query
  const [allCustomers, setAllCustomers] = useState<Customer[]>([])
  const [isLoadingCustomers, setIsLoadingCustomers] = useState(true)

  useEffect(() => {
    const fetchCustomers = async () => {
      try {
        setIsLoadingCustomers(true)
        console.log('🔄 [CustomersTable] Fetching customers from backend...')
        
        const customers = await customerService.getAllCustomers()
        console.log('✅ [CustomersTable] Successfully fetched customers:', customers.length)
        
        setAllCustomers(customers)
      } catch (error) {
        console.error('❌ [CustomersTable] Error fetching customers:', error)
        setAllCustomers([])
        toast({
          title: "Error",
          description: "Failed to load customers. Please try again.",
          variant: "destructive"
        })
      } finally {
        setIsLoadingCustomers(false)
      }
    }

    fetchCustomers()
  }, [updateTrigger, toast])

  // Listen for customer events to refresh data
  useEffect(() => {
    const handleCustomerEvent = () => {
      forceUpdate()
    }

    window.addEventListener('customer-created', handleCustomerEvent)
    window.addEventListener('customer-updated', handleCustomerEvent)
    window.addEventListener('customer-deleted', handleCustomerEvent)
    
    return () => {
      window.removeEventListener('customer-created', handleCustomerEvent)
      window.removeEventListener('customer-updated', handleCustomerEvent)
      window.removeEventListener('customer-deleted', handleCustomerEvent)
    }
  }, [])

  const filteredCustomers = allCustomers.filter(customer => {
    const searchLower = searchQuery.toLowerCase()
    return (
      customer.companyName.toLowerCase().includes(searchLower) ||
      customer.contact.email.toLowerCase().includes(searchLower) ||
      customer.contact.phone.toLowerCase().includes(searchLower) ||
      customer.address.town.toLowerCase().includes(searchLower) ||
      customer.status.toLowerCase().includes(searchLower)
    )
  })
  
  // Calculate pagination
  const totalPages = Math.ceil(filteredCustomers.length / itemsPerPage)
  const startIndex = (currentPage - 1) * itemsPerPage
  const endIndex = startIndex + itemsPerPage
  const currentCustomers = filteredCustomers.slice(startIndex, endIndex)

  const toggleRowSelection = (customerId: string) => {
    if (selectedRows.includes(customerId)) {
      setSelectedRows([])
      onCustomerSelect(null)
    } else {
      setSelectedRows([customerId])
      onCustomerSelect(customerId)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3">
        <h2 className="text-base sm:text-lg font-semibold text-foreground">Customers</h2>
        <Button 
          onClick={() => {
            setSelectedCustomer(undefined)
            setDialogOpen(true)
          }}
          className="bg-purple-600 hover:bg-purple-700 w-full sm:w-auto"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Customer
        </Button>
      </div>

      {/* Search Bar */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
        <Input
          placeholder="Search customers..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Loading State */}
      {isLoadingCustomers && (
        <div className="flex items-center justify-center py-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600 mx-auto mb-2"></div>
            <p className="text-sm text-muted-foreground">Loading companies...</p>
          </div>
        </div>
      )}

      {/* Error State */}
      {!isLoadingCustomers && allCustomers.length === 0 && (
        <div className="text-center py-8">
          <p className="text-sm text-muted-foreground mb-4">No customers found.</p>
          <Button 
            onClick={() => forceUpdate()}
            variant="outline"
          >
            Retry
          </Button>
        </div>
      )}

      {/* Mobile Card Layout - visible only on small screens */}
      {!isLoadingCustomers && allCustomers.length > 0 && (
        <>
          <div className="block md:hidden space-y-3">
            {currentCustomers.map((customer) => {
              const isSelected = selectedRows.includes(String(customer.id))
              const rawType = Array.isArray((customer as any).customerType)
                ? (customer as any).customerType[0]
                : (customer as any).customerType
              const typeStr = typeof rawType === 'string' ? rawType : ''
              const typeLabel = typeStr
                ? typeStr.charAt(0).toUpperCase() + typeStr.slice(1).replace(/-/g, ' ')
                : '—'
              const companyName = (customer as any).companyName ?? 'Unnamed'
              const companyNumber = (customer as any).companyNumber ?? '—'
              const vatNumber = (customer as any).vatNumber ?? '—'
              const status = (customer as any).status === 'inactive' ? 'inactive' : 'active'

              return (
                <div 
                  key={customer.id} 
                  className={`rounded-lg border border-border bg-card/80 text-card-foreground backdrop-blur-lg shadow-sm p-4 space-y-3 cursor-pointer transition-colors ${
                    isSelected ? 'border-purple-500 bg-purple-50/80 dark:bg-purple-950/30' : 'hover:bg-accent/40'
                  }`}
                  onClick={() => toggleRowSelection(String(customer.id))}
                >
                  {/* Header */}
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex-1 min-w-0">
                      <div className="font-semibold text-sm truncate">{companyName}</div>
                      <div className="text-xs text-muted-foreground mt-0.5">ID: {customer.id}</div>
                    </div>
                    <div className="flex gap-1.5 flex-shrink-0">
                      <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${
                        status === 'active' 
                          ? 'bg-green-100 text-green-700 dark:bg-green-950/60 dark:text-green-200' 
                          : 'bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-200'
                      }`}>
                        {status === 'active' ? 'Active' : 'Inactive'}
                      </span>
                    </div>
                  </div>

                  {/* Details Grid */}
                  <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-border">
                    <div>
                      <span className="text-muted-foreground block mb-0.5">Company Number</span>
                      <div className="font-medium">{companyNumber}</div>
                    </div>
                    <div>
                      <span className="text-muted-foreground block mb-0.5">VAT Number</span>
                      <div className="font-medium">{vatNumber}</div>
                    </div>
                    <div className="col-span-2">
                      <span className="text-muted-foreground block mb-0.5">Company Type</span>
                      <span className="inline-flex px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-200">
                        {typeLabel}
                      </span>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex gap-2 pt-2 border-t border-border">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleEdit(customer)
                      }}
                      className="flex-1 h-9 text-xs"
                    >
                      <Pencil className="h-3.5 w-3.5 mr-1.5" />
                      Edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleDelete(customer)
                      }}
                      className="h-9 px-3 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-300 dark:hover:text-red-200 dark:hover:bg-red-950/40"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </div>
              )
            })}
          </div>

          {/* Desktop Table Layout - visible on medium screens and above */}
          <div className="hidden md:block bg-card/80 text-card-foreground backdrop-blur-lg border border-border shadow-md rounded-lg overflow-hidden">
            <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                    <TableHead className="text-sm">ID</TableHead>
                    <TableHead className="text-sm">Company Name</TableHead>
                    <TableHead className="text-sm">Company Number</TableHead>
                    <TableHead className="text-sm">VAT Number</TableHead>
                    <TableHead className="text-sm">Status</TableHead>
                    <TableHead className="text-sm">Company Type</TableHead>
                    <TableHead className="text-right text-sm">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {currentCustomers.map((customer) => (
                  <CustomerTableRow
                    key={customer.id}
                    customer={customer}
                    isSelected={selectedRows.includes(String(customer.id))}
                    onSelect={() => toggleRowSelection(String(customer.id))}
                    onEdit={() => handleEdit(customer)}
                    onDelete={() => handleDelete(customer)}
                  />
                ))}
              </TableBody>
            </Table>
            </div>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-4">
              <div className="text-xs sm:text-sm text-muted-foreground text-center sm:text-left">
                Showing {startIndex + 1} to {Math.min(endIndex, filteredCustomers.length)} of {filteredCustomers.length} customers
              </div>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                  disabled={currentPage === 1}
                  className="h-9"
                >
                  <ChevronLeft className="h-4 w-4" />
                  <span className="hidden xs:inline ml-1">Previous</span>
                </Button>
                <span className="text-xs sm:text-sm whitespace-nowrap">
                  Page {currentPage} of {totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                  disabled={currentPage === totalPages}
                  className="h-9"
                >
                  <span className="hidden xs:inline mr-1">Next</span>
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {/* Customer Dialog */}
      <CustomerDialog
        open={dialogOpen}
        onOpenChange={(open) => {
          setDialogOpen(open)
          if (!open) {
            setSelectedCustomer(undefined)
          }
        }}
        customer={selectedCustomer}
        onSave={handleSave}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Customer</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{customerToDelete?.companyName}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete} className="bg-red-600 hover:bg-red-700">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}