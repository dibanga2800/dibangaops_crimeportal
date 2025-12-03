import { useState, useMemo } from 'react'
import { Search, ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '../ui/button'
import { Input } from '../ui/input'

interface IncidentReport {
  id: string
  customerName: string
  store?: string
  siteName?: string // Add siteName as alternative to store
  officerName: string
  date: string
  amount: number
  incidentType: string
}

interface DataTableProps {
  data: IncidentReport[]
}

const ITEMS_PER_PAGE = 10

export function IncidentTable({ data }: DataTableProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [sortConfig, setSortConfig] = useState<{
    key: keyof IncidentReport | null
    direction: 'asc' | 'desc'
  }>({ key: null, direction: 'asc' })

  const sortData = (key: keyof IncidentReport) => {
    const direction = sortConfig.key === key && sortConfig.direction === 'asc' ? 'desc' : 'asc'
    setSortConfig({ key, direction })
    // Reset to first page when sorting
    setCurrentPage(1)
  }

  const filteredAndSortedData = useMemo(() => {
    let processed = [...data]

    // Debug logging
    console.log('🔍 IncidentTable - Data received:', data)
    console.log('🔍 IncidentTable - Data length:', data?.length || 0)
    console.log('🔍 IncidentTable - First item structure:', data?.[0])

    // If data is empty, return empty array
    if (!data || data.length === 0) {
      console.log('❌ IncidentTable - No data provided')
      return [];
    }

    // Filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase()
      processed = processed.filter(
        item =>
          item.customerName.toLowerCase().includes(query) ||
          item.store?.toLowerCase().includes(query) ||
          item.siteName?.toLowerCase().includes(query) ||
          item.officerName.toLowerCase().includes(query) ||
          new Date(item.date).toLocaleDateString().toLowerCase().includes(query) ||
          item.amount.toString().includes(query) ||
          item.incidentType.toLowerCase().includes(query)
      )
      console.log('🔍 IncidentTable - After filtering:', processed.length)
      // Reset to first page when filtering
      setCurrentPage(1)
    }

    // Sort
    if (sortConfig.key) {
      processed.sort((a, b) => {
        const aValue = a[sortConfig.key!]
        const bValue = b[sortConfig.key!]

        if (typeof aValue === 'string' && typeof bValue === 'string') {
          return sortConfig.direction === 'asc'
            ? aValue.localeCompare(bValue)
            : bValue.localeCompare(aValue)
        }

        if (typeof aValue === 'number' && typeof bValue === 'number') {
          return sortConfig.direction === 'asc'
            ? aValue - bValue
            : bValue - aValue
        }

        // Convert string dates to Date objects for comparison
        if (sortConfig.key === 'date') {
          const aDate = new Date(aValue as string)
          const bDate = new Date(bValue as string)
          return sortConfig.direction === 'asc'
            ? aDate.getTime() - bDate.getTime()
            : bDate.getTime() - aDate.getTime()
        }

        return 0
      })
    }

    console.log('🔍 IncidentTable - Final processed data:', processed.length)
    return processed
  }, [data, searchQuery, sortConfig])

  // Reset current page when search changes
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value)
    setCurrentPage(1)
  }

  // Pagination calculations
  const totalPages = Math.ceil(filteredAndSortedData.length / ITEMS_PER_PAGE)
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE
  const endIndex = startIndex + ITEMS_PER_PAGE
  const currentPageData = filteredAndSortedData.slice(startIndex, endIndex)

  const goToPage = (page: number) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)))
  }

  const getSortIcon = (key: keyof IncidentReport) => {
    if (sortConfig.key !== key) return '↕'
    return sortConfig.direction === 'asc' ? '↑' : '↓'
  }

  return (
    <div>
      <div className="flex items-center justify-between py-2 md:py-4">
        <div className="flex items-center gap-2">
          <Search className="h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search incidents..."
            value={searchQuery}
            onChange={handleSearchChange}
            className="h-8 w-[150px] text-xs md:text-sm lg:w-[250px]"
          />
        </div>
        {filteredAndSortedData.length > 0 && (
          <div className="text-xs md:text-sm text-muted-foreground">
            Showing {startIndex + 1}-{Math.min(endIndex, filteredAndSortedData.length)} of {filteredAndSortedData.length}
          </div>
        )}
      </div>
      
      {/* Mobile Card Layout - visible only on small screens */}
      <div className="block md:hidden space-y-3">
        {filteredAndSortedData.length === 0 && data.length > 0 ? (
          <div className="rounded-md border p-4 text-center text-sm text-amber-600">
            Data available but filtered out: {data.length} records found
          </div>
        ) : currentPageData.length > 0 ? (
          currentPageData.map((report) => (
            <div key={report.id} className="rounded-md border bg-card p-4 space-y-2 shadow-sm">
              <div className="flex justify-between items-start gap-2">
                <div className="flex-1 min-w-0">
                  <div className="font-semibold text-sm truncate">{report.customerName}</div>
                  <div className="text-xs text-muted-foreground mt-0.5">{report.store || report.siteName}</div>
                </div>
                <div className="text-right">
                  <div className="font-semibold text-sm text-green-600 dark:text-green-400 whitespace-nowrap">
                    £{report.amount.toLocaleString(undefined, {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </div>
                </div>
              </div>
              
              <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t">
                <div>
                  <span className="text-muted-foreground">Officer:</span>
                  <div className="font-medium truncate">{report.officerName}</div>
                </div>
                <div>
                  <span className="text-muted-foreground">Date:</span>
                  <div className="font-medium">{new Date(report.date).toLocaleDateString()}</div>
                </div>
                <div className="col-span-2">
                  <span className="text-muted-foreground">Type:</span>
                  <div className="font-medium">{report.incidentType}</div>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="rounded-md border p-8 text-center text-sm text-muted-foreground">
            No results found. Data length: {data.length}
          </div>
        )}
      </div>
      
      {/* Desktop Table Layout - visible on medium screens and above */}
      <div className="hidden md:block rounded-md border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full caption-bottom text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-muted-foreground cursor-pointer hover:bg-muted/70"
                onClick={() => sortData('customerName')}
              >
                <div className="flex items-center gap-1">
                  Customer Name {getSortIcon('customerName')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-muted-foreground cursor-pointer hover:bg-muted/70"
                onClick={() => sortData('store')}
              >
                <div className="flex items-center gap-1">
                  Store Name {getSortIcon('store')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-muted-foreground cursor-pointer hover:bg-muted/70"
                onClick={() => sortData('officerName')}
              >
                <div className="flex items-center gap-1">
                  Officer Name {getSortIcon('officerName')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-muted-foreground cursor-pointer hover:bg-muted/70"
                onClick={() => sortData('date')}
              >
                <div className="flex items-center gap-1">
                  Incident Date {getSortIcon('date')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-right align-middle font-semibold tracking-tight text-muted-foreground cursor-pointer hover:bg-muted/70"
                onClick={() => sortData('amount')}
              >
                <div className="flex items-center justify-end gap-1">
                  Total Amount {getSortIcon('amount')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-muted-foreground cursor-pointer hover:bg-muted/70"
                onClick={() => sortData('incidentType')}
              >
                <div className="flex items-center gap-1">
                  Incident Type {getSortIcon('incidentType')}
                </div>
              </th>
            </tr>
          </thead>
          <tbody className="tracking-normal">
            {filteredAndSortedData.length === 0 && data.length > 0 && (
              <tr>
                <td colSpan={6} className="h-12 text-center text-sm text-amber-600">
                  Data available but filtered out: {data.length} records found
                </td>
              </tr>
            )}
            
            {currentPageData.length > 0 ? (
              currentPageData.map((report) => (
                <tr key={report.id} className="border-b transition-colors hover:bg-muted/50">
                    <td className="p-4 align-middle font-medium leading-relaxed">{report.customerName}</td>
                    <td className="p-4 align-middle text-muted-foreground leading-relaxed">{report.store || report.siteName}</td>
                    <td className="p-4 align-middle text-muted-foreground leading-relaxed">{report.officerName}</td>
                    <td className="p-4 align-middle text-muted-foreground tabular-nums leading-relaxed">
                    {new Date(report.date).toLocaleDateString()}
                  </td>
                    <td className="p-4 align-middle text-right font-medium tabular-nums leading-relaxed text-green-600 dark:text-green-400">
                    £{report.amount.toLocaleString(undefined, {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </td>
                    <td className="p-4 align-middle text-muted-foreground leading-relaxed">{report.incidentType}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={6} className="h-24 text-center text-sm text-muted-foreground">
                  No results found. Data length: {data.length}
                </td>
              </tr>
            )}
          </tbody>
        </table>
        </div>
      </div>

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 py-4">
          <div className="text-xs md:text-sm text-muted-foreground">
            Page {currentPage} of {totalPages}
          </div>
          
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => goToPage(currentPage - 1)}
              disabled={currentPage === 1}
              className="h-8 w-8 p-0"
              aria-label="Previous page"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            
            {/* Page numbers */}
            <div className="flex items-center gap-1">
              {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                let pageNumber: number;
                
                if (totalPages <= 5) {
                  pageNumber = i + 1;
                } else if (currentPage <= 3) {
                  pageNumber = i + 1;
                } else if (currentPage >= totalPages - 2) {
                  pageNumber = totalPages - 4 + i;
                } else {
                  pageNumber = currentPage - 2 + i;
                }
                
                return (
                  <Button
                    key={pageNumber}
                    variant={currentPage === pageNumber ? "default" : "outline"}
                    size="sm"
                    onClick={() => goToPage(pageNumber)}
                    className="h-8 w-8 p-0 text-xs"
                    aria-label={`Go to page ${pageNumber}`}
                    aria-current={currentPage === pageNumber ? "page" : undefined}
                  >
                    {pageNumber}
                  </Button>
                );
              })}
            </div>
            
            <Button
              variant="outline"
              size="sm"
              onClick={() => goToPage(currentPage + 1)}
              disabled={currentPage === totalPages}
              className="h-8 w-8 p-0"
              aria-label="Next page"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
