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
      <div className="flex items-center justify-between py-2 md:py-4">
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search incidents..."
            value={searchQuery}
            onChange={handleSearchChange}
            className="h-9 w-[180px] pl-8 text-xs md:text-sm lg:w-[280px] border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
          />
        </div>
        {filteredAndSortedData.length > 0 && (
          <div className="text-xs md:text-sm text-muted-foreground rounded-md border border-slate-200 dark:border-slate-700 px-2.5 py-1 bg-slate-50/70 dark:bg-slate-900/70">
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
            <div key={report.id} className="rounded-xl border border-slate-200 dark:border-slate-700 bg-card p-4 space-y-2 shadow-sm">
              <div className="flex justify-between items-start gap-2">
                <div className="flex-1 min-w-0">
                  <div className="font-semibold text-sm truncate">{report.customerName}</div>
                  <div className="text-xs text-muted-foreground mt-0.5">{report.store || report.siteName}</div>
                </div>
                <div className="text-right">
                  <div className="font-semibold text-sm text-green-600 dark:text-green-400 whitespace-nowrap">
                    Rec: £{(report.recoveredValue ?? report.amount).toLocaleString(undefined, {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </div>
                  <div className="font-semibold text-xs text-red-600 dark:text-red-400 whitespace-nowrap">
                    Lost: £{(report.lostValue ?? 0).toLocaleString(undefined, {
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
                  <span className="text-muted-foreground">Date / Time:</span>
                  <div className="font-medium flex items-center gap-1.5">
                    <span>{new Date(report.date).toLocaleDateString()}</span>
                    <span className="text-blue-600 dark:text-blue-400 text-[11px] font-semibold bg-blue-50 dark:bg-blue-950/40 px-1.5 py-0.5 rounded-md">
                      {getDisplayTime(report)}
                    </span>
                  </div>
                </div>
                <div className="col-span-2">
                  <span className="text-muted-foreground">Type:</span>
                  <div className="mt-0.5 inline-flex items-center rounded-full bg-slate-100 dark:bg-slate-800 px-2 py-0.5 text-[11px] font-medium text-slate-700 dark:text-slate-200">
                    {report.incidentType}
                  </div>
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
      <div className="hidden md:block rounded-xl border border-slate-200/90 dark:border-slate-700 bg-white dark:bg-slate-900 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full caption-bottom text-sm">
          <thead>
            <tr className="border-b border-slate-200 dark:border-slate-700 bg-gradient-to-r from-slate-200 via-slate-100 to-slate-200 dark:from-slate-700 dark:via-slate-700 dark:to-slate-700">
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('customerName')}
              >
                <div className="flex items-center gap-1">
                  Company Name {getSortIcon('customerName')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('store')}
              >
                <div className="flex items-center gap-1">
                  Store Name {getSortIcon('store')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('officerName')}
              >
                <div className="flex items-center gap-1">
                  Staff Member Name {getSortIcon('officerName')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('date')}
              >
                <div className="flex items-center gap-1">
                  Incident Date / Time {getSortIcon('date')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-right align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('recoveredValue')}
              >
                <div className="flex items-center justify-end gap-1">
                  Recovered Value {getSortIcon('recoveredValue')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-right align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('lostValue')}
              >
                <div className="flex items-center justify-end gap-1">
                  Lost Value {getSortIcon('lostValue')}
                </div>
              </th>
              <th 
                  className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100 cursor-pointer hover:bg-slate-200/60 dark:hover:bg-white/10"
                onClick={() => sortData('incidentType')}
              >
                <div className="flex items-center gap-1">
                  Incident Type {getSortIcon('incidentType')}
                </div>
              </th>
              <th className="h-12 px-4 text-left align-middle font-semibold tracking-tight text-slate-700 dark:text-slate-100">
                AI Insight
              </th>
            </tr>
          </thead>
          <tbody className="tracking-normal">
            {filteredAndSortedData.length === 0 && data.length > 0 && (
              <tr>
                <td colSpan={8} className="h-12 text-center text-sm text-amber-600">
                  Data available but filtered out: {data.length} records found
                </td>
              </tr>
            )}
            
            {currentPageData.length > 0 ? (
              currentPageData.map((report) => (
                <tr key={report.id} className="border-b border-slate-100 dark:border-slate-800 odd:bg-white odd:dark:bg-slate-900 even:bg-slate-50/40 even:dark:bg-slate-800/30 transition-colors hover:bg-blue-50/60 dark:hover:bg-slate-800/70">
                    <td className="p-4 align-middle font-semibold leading-relaxed text-slate-900 dark:text-slate-100">{report.customerName}</td>
                    <td className="p-4 align-middle text-slate-700 dark:text-slate-200 leading-relaxed">{report.store || report.siteName}</td>
                    <td className="p-4 align-middle text-slate-700 dark:text-slate-200 leading-relaxed">{report.officerName}</td>
                    <td className="p-4 align-middle text-slate-700 dark:text-slate-200 tabular-nums leading-relaxed">
                    <div className="flex items-center gap-2">
                      <span>{new Date(report.date).toLocaleDateString()}</span>
                      <span className="text-blue-600 dark:text-blue-400 text-xs font-semibold bg-blue-50 dark:bg-blue-950/40 px-1.5 py-0.5 rounded-md">
                        {getDisplayTime(report)}
                      </span>
                    </div>
                  </td>
                    <td className="p-4 align-middle text-right font-medium tabular-nums leading-relaxed text-green-600 dark:text-green-400">
                    £{(report.recoveredValue ?? report.amount).toLocaleString(undefined, {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </td>
                    <td className="p-4 align-middle text-right font-medium tabular-nums leading-relaxed text-red-600 dark:text-red-400">
                    £{(report.lostValue ?? 0).toLocaleString(undefined, {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2
                    })}
                  </td>
                    <td className="p-4 align-middle leading-relaxed">
                      <span className="inline-flex items-center rounded-full bg-slate-100 dark:bg-slate-800 px-2 py-0.5 text-xs font-medium text-slate-700 dark:text-slate-200">
                        {report.incidentType}
                      </span>
                    </td>
                    <td className="p-4 align-middle text-xs space-y-1">
                      {report.incidentCategory && (
                        <div className="inline-flex items-center rounded-full bg-indigo-50 dark:bg-indigo-900/40 px-2 py-0.5 text-[11px] font-medium text-indigo-700 dark:text-indigo-300">
                          {report.incidentCategory}
                          {typeof report.incidentCategoryConfidence === 'number' && (
                            <span className="ml-1 text-[10px] text-indigo-500 dark:text-indigo-300/80">
                              ({Math.round(report.incidentCategoryConfidence * 100)}% conf.)
                            </span>
                          )}
                        </div>
                      )}
                      {report.riskLevel && (
                        <div
                          className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-semibold capitalize ${
                            report.riskLevel === 'high'
                              ? 'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300'
                              : report.riskLevel === 'medium'
                              ? 'bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300'
                              : 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300'
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
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={8} className="h-24 text-center text-sm text-muted-foreground">
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
