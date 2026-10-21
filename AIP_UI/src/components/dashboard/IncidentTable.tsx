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
  timeOfIncident?: string
  amount: number
  recoveredValue?: number
  lostValue?: number
  incidentType: string
  incidentCategory?: string
  incidentCategoryConfidence?: number
  riskLevel?: 'low' | 'medium' | 'high'
  riskScore?: number
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

    // If data is empty, return empty array
    if (!data || data.length === 0) {
      return []
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
          (item.recoveredValue ?? item.amount).toString().includes(query) ||
          (item.lostValue ?? 0).toString().includes(query) ||
          item.incidentType.toLowerCase().includes(query)
      )
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

  // Hide columns that carry no information for this data set
  const showCompany = useMemo(() => new Set(data.map(d => d.customerName)).size > 1, [data])
  const showInsight = useMemo(() => data.some(d => d.incidentCategory || d.riskLevel), [data])
  const columnCount = 6 + (showCompany ? 1 : 0) + (showInsight ? 1 : 0)

  const getSortIcon = (key: keyof IncidentReport) => {
    if (sortConfig.key !== key) return '↕'
    return sortConfig.direction === 'asc' ? '↑' : '↓'
  }

  const getDisplayTime = (report: IncidentReport): string => {
    if (report.timeOfIncident && report.timeOfIncident.trim() !== '') {
      return report.timeOfIncident
    }

    const parsedDate = new Date(report.date)
    if (Number.isNaN(parsedDate.getTime())) {
      return 'N/A'
    }

    return parsedDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  }

  return (
    <div>
      <div className="flex items-center justify-between gap-3 px-4 pb-3 sm:px-5">
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search incidents..."
            value={searchQuery}
            onChange={handleSearchChange}
            className="h-9 w-[200px] pl-8 lg:w-[280px]"
          />
        </div>
        {filteredAndSortedData.length > 0 && (
          <div className="figures shrink-0 text-xs text-muted-foreground">
            Showing {startIndex + 1}-{Math.min(endIndex, filteredAndSortedData.length)} of {filteredAndSortedData.length}
          </div>
        )}
      </div>
      
      {/* Mobile Card Layout - visible only on small screens */}
      <div className="block border-t border-border md:hidden">
        {filteredAndSortedData.length === 0 && data.length > 0 ? (
          <div className="m-4 rounded-md border border-warning/25 bg-warning-soft p-4 text-center text-sm text-warning">
            None of the {data.length} incidents match your search
          </div>
        ) : currentPageData.length > 0 ? (
          <ul className="divide-y divide-border">
            {currentPageData.map((report) => {
              const recovered = Number(report.recoveredValue ?? report.amount ?? 0)
              const lost = Number(report.lostValue ?? 0)
              const money = (v: number) => `£${v.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
              return (
                <li key={report.id} className="space-y-1.5 px-4 py-3.5">
                  <div className="flex items-baseline justify-between gap-3">
                    <p className="min-w-0 truncate text-sm font-semibold text-foreground">{report.store || report.siteName}</p>
                    <p className={`figures shrink-0 text-sm font-semibold ${lost > 0 ? 'text-loss' : 'text-muted-foreground'}`}>
                      {lost > 0 ? `−${money(lost)}` : money(0)}
                    </p>
                  </div>
                  <div className="flex items-center justify-between gap-3">
                    <div className="flex min-w-0 items-center gap-2">
                      <span className="inline-flex shrink-0 items-center rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-foreground/80">
                        {report.incidentType}
                      </span>
                      <span className="figures truncate text-xs text-muted-foreground">
                        {new Date(report.date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })}, {getDisplayTime(report)}
                      </span>
                    </div>
                    {recovered > 0 && (
                      <span className="figures shrink-0 text-xs font-medium text-recovered">+{money(recovered)} back</span>
                    )}
                  </div>
                  <p className="truncate text-xs text-muted-foreground">Reported by {report.officerName}</p>
                </li>
              )
            })}
          </ul>
        ) : (
          <div className="m-4 rounded-md border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
            No incidents to show yet.
          </div>
        )}
      </div>

      {/* Desktop Table Layout - visible on medium screens and above */}
      <div className="hidden overflow-hidden border-t border-border md:block">
        <div className="overflow-x-auto">
          <table className="figures w-full caption-bottom text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/60">
              {showCompany && (
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('customerName')}
              >
                <div className="flex items-center gap-1">
                  Company {getSortIcon('customerName')}
                </div>
              </th>
              )}
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('store')}
              >
                <div className="flex items-center gap-1">
                  Store {getSortIcon('store')}
                </div>
              </th>
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('officerName')}
              >
                <div className="flex items-center gap-1">
                  Reported by {getSortIcon('officerName')}
                </div>
              </th>
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('date')}
              >
                <div className="flex items-center gap-1">
                  Date and time {getSortIcon('date')}
                </div>
              </th>
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-right align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('recoveredValue')}
              >
                <div className="flex items-center justify-end gap-1">
                  Recovered {getSortIcon('recoveredValue')}
                </div>
              </th>
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-right align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('lostValue')}
              >
                <div className="flex items-center justify-end gap-1">
                  Lost {getSortIcon('lostValue')}
                </div>
              </th>
              <th 
                  className="h-10 cursor-pointer select-none whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() => sortData('incidentType')}
              >
                <div className="flex items-center gap-1">
                  Type {getSortIcon('incidentType')}
                </div>
              </th>
              {showInsight && (
              <th className="h-10 whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground">
                AI insight
              </th>
              )}
            </tr>
          </thead>
          <tbody className="tracking-normal">
            {filteredAndSortedData.length === 0 && data.length > 0 && (
              <tr>
                <td colSpan={columnCount} className="h-12 text-center text-sm text-warning">
                  None of the {data.length} incidents match your search
                </td>
              </tr>
            )}
            
            {currentPageData.length > 0 ? (
              currentPageData.map((report) => (
                <tr key={report.id} className="border-b border-border/70 transition-colors last:border-0 hover:bg-muted/40">
                    {showCompany && <td className="whitespace-nowrap px-4 py-3 align-middle font-medium text-foreground">{report.customerName}</td>}
                    <td className={`whitespace-nowrap px-4 py-3 align-middle ${showCompany ? 'text-foreground/80' : 'font-medium text-foreground'}`}>{report.store || report.siteName}</td>
                    <td className="whitespace-nowrap px-4 py-3 align-middle text-foreground/80">{report.officerName}</td>
                    <td className="whitespace-nowrap px-4 py-3 align-middle text-foreground/80">
                    <div className="flex items-center gap-2">
                      <span>{new Date(report.date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}</span>
                      <span className="text-xs text-muted-foreground">
                        {getDisplayTime(report)}
                      </span>
                    </div>
                  </td>
                    <td className={`whitespace-nowrap px-4 py-3 text-right align-middle ${Number(report.recoveredValue ?? report.amount) > 0 ? 'font-medium text-recovered' : 'text-muted-foreground'}`}>
                    £{(report.recoveredValue ?? report.amount).toLocaleString('en-GB', {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </td>
                    <td className={`whitespace-nowrap px-4 py-3 text-right align-middle ${Number(report.lostValue ?? 0) > 0 ? 'font-medium text-loss' : 'text-muted-foreground'}`}>
                    £{(report.lostValue ?? 0).toLocaleString('en-GB', {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </td>
                    <td className="whitespace-nowrap px-4 py-3 align-middle">
                      <span className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-foreground/80">
                        {report.incidentType}
                      </span>
                    </td>
                    {showInsight && (
                    <td className="space-y-1 whitespace-nowrap px-4 py-3 align-middle text-xs">
                      {report.incidentCategory && (
                        <div className="mr-1 inline-flex items-center rounded-full bg-info-soft px-2 py-0.5 text-[11px] font-medium text-info">
                          {report.incidentCategory}
                          {typeof report.incidentCategoryConfidence === 'number' && (
                            <span className="ml-1 text-[10px] opacity-75">
                              ({Math.round(report.incidentCategoryConfidence * 100)}% conf.)
                            </span>
                          )}
                        </div>
                      )}
                      {report.riskLevel && (
                        <div
                          className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${
                            report.riskLevel === 'high'
                              ? 'bg-loss-soft text-loss'
                              : report.riskLevel === 'medium'
                              ? 'bg-warning-soft text-warning'
                              : 'bg-recovered-soft text-recovered'
                          }`}
                        >
                          Risk: {report.riskLevel}
                          {typeof report.riskScore === 'number' && (
                            <span className="ml-1 text-[10px] opacity-80">
                              {Math.round(report.riskScore * 100)}/100
                            </span>
                          )}
                        </div>
                      )}
                    </td>
                    )}
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={columnCount} className="h-24 text-center text-sm text-muted-foreground">
                  No incidents to show yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
        </div>
      </div>

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex flex-col items-center justify-between gap-4 border-t border-border px-5 py-3 sm:flex-row">
          <div className="figures text-xs text-muted-foreground">
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
