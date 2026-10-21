import { useState, useCallback, useEffect, useMemo, useRef, Suspense, lazy } from "react"
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { formatMoney } from '@/lib/money'
import { EmptyState } from '@/components/common/EmptyState'
import { Segmented } from '@/components/common/Segmented'
import { FilterBar, FilterChip, DateRangeField } from '@/components/common/FilterBar'
import { StatLedger } from '@/components/common/StatLedger'
import { PageHeader } from '@/components/layout/PageHeader'
import { Incident, StolenItem } from "@/types/incidents"
import { IncidentDetailView } from '@/components/operations/IncidentDetailView'
const IncidentForm = lazy(() =>
  import('@/components/operations/IncidentForm').then((m) => ({ default: m.IncidentForm })),
)

const LazyLoadingFallback = () => (
  <div className="flex items-center justify-center py-8 text-sm text-muted-foreground">
    Loading...
  </div>
)

import { Button } from "@/components/ui/button"
import { useToast } from '@/components/ui/use-toast'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
// Removed mockIncidents import - now using API service
import { 
  PlusCircle, 
  PoundSterling, 
  Store, 
  AlertCircle, 
  Edit2,
  Trash2,
  Eye,
  FileText,
  Search,
} from "lucide-react"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import BarcodeScanner from '@/components/BarcodeScanner'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { incidentsApi } from "@/services/api/incidents"
import { productService } from "@/services/productService"
import { mapProductToStolenItem } from "@/lib/stolen-items/map-product-to-stolen-item"
import { mergeCatalogDepartment } from "@/lib/stolen-items/catalog-departments"
import { regionService } from "@/services/regionService"
import type { Region } from "@/types/customer"
import { Toaster } from '@/components/ui/toaster'
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { useAuth } from '@/hooks/useAuth'
import { filterByAssignedSiteIds, getAssignedSiteIds, isSiteScopeEnforcedForUser } from '@/utils/siteAccess'

const getIncidentFinancials = (incident: Incident) => {
  const stolenItems = Array.isArray(incident?.stolenItems) ? incident.stolenItems : []
  const totalStolenValue =
    typeof incident?.totalStolenValue === 'number' && !Number.isNaN(incident.totalStolenValue)
      ? incident.totalStolenValue
      : stolenItems.reduce((sum, item) => sum + (typeof item?.totalAmount === 'number' ? item.totalAmount : 0), 0)
  const totalRecoveredValue =
    typeof incident?.totalRecoveredValue === 'number' && !Number.isNaN(incident.totalRecoveredValue)
      ? incident.totalRecoveredValue
      : typeof incident?.totalValueRecovered === 'number' && !Number.isNaN(incident.totalValueRecovered)
        ? incident.totalValueRecovered
        : stolenItems.reduce((sum, item) => sum + (typeof item?.recoveredAmount === 'number' ? item.recoveredAmount : 0), 0)
  const totalLostValue =
    typeof incident?.totalLostValue === 'number' && !Number.isNaN(incident.totalLostValue)
      ? incident.totalLostValue
      : Math.max(totalStolenValue - totalRecoveredValue, 0)

  return {
    totalStolenValue,
    totalRecoveredValue,
    totalLostValue,
  }
}

interface IncidentReportPageProps {
  isCustomerView?: boolean;
  customerId?: string;
  siteId?: string | null;
}

export default function IncidentReportPage({ isCustomerView = false, customerId: propCustomerId, siteId: propSiteId }: IncidentReportPageProps) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const { selectedCustomerId, selectedSiteId } = useCustomerSelection()
  const customerId = propCustomerId ?? (selectedCustomerId != null ? String(selectedCustomerId) : undefined)
  const siteId = propSiteId ?? selectedSiteId
  const { toast } = useToast()
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  // Keep dashboards "seamless" by auto-opening the dialog without showing
  // the incident list/table underneath when ?open=new is present.
  // Use `location.search` for stable initial value during navigation.
  const initialAutoOpen = typeof window !== 'undefined' && new URLSearchParams(window.location.search).get('open') === 'new'
  const shouldAutoOpenNewIncident = useMemo(() => {
    if (initialAutoOpen) return true
    const params = new URLSearchParams(location.search)
    return params.get('open') === 'new'
  }, [initialAutoOpen, location.search])

  const isAutoOpenRef = useRef(shouldAutoOpenNewIncident)
  const isAutoOpen = isAutoOpenRef.current

  const [open, setOpen] = useState(shouldAutoOpenNewIncident)
  const [editingIncident, setEditingIncident] = useState<Incident | null>(null)
  const [deletingIncident, setDeletingIncident] = useState<Incident | null>(null)
  const [searchTerm, setSearchTerm] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [viewingIncident, setViewingIncident] = useState<Incident | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [scanningBarcode, setScanningBarcode] = useState(false)
  const [isLoadingProduct, setIsLoadingProduct] = useState(false)
  const [createPrefilledStolenItems, setCreatePrefilledStolenItems] = useState<StolenItem[]>([])
  const [datePreset, setDatePreset] = useState<'today' | 'week' | 'month' | 'custom' | null>(null)
  const [fromDate, setFromDate] = useState("")
  const [toDate, setToDate] = useState("")
  const [fromDateInput, setFromDateInput] = useState("")
  const [toDateInput, setToDateInput] = useState("")
  const [incidentTypeFilter, setIncidentTypeFilter] = useState<string | null>(null)
  const [regionFilter, setRegionFilter] = useState<string>('all')
  const [regions, setRegions] = useState<Region[]>([])
  const [isLoadingRegions, setIsLoadingRegions] = useState(false)
  const [catalogDepartments, setCatalogDepartments] = useState<string[]>([])
  const itemsPerPage = 10
  const enforceSiteScope = isSiteScopeEnforcedForUser(user)
  const hasAssignedSites = getAssignedSiteIds(user).length > 0
  const canCreateIncident = !enforceSiteScope || hasAssignedSites

  const didNavigateBackRef = useRef(false)
  const editingIncidentId = editingIncident?.id?.trim() || ''
  const isEditingExistingIncident = editingIncidentId.length > 0

  const handleCloseDialog = useCallback(() => {
    setOpen(false)
    setEditingIncident(null)
    setCreatePrefilledStolenItems([])

    if (!isAutoOpenRef.current) return
    if (didNavigateBackRef.current) return

    didNavigateBackRef.current = true
    navigate(-1)
  }, [navigate])

  // Intentionally do not mutate the URL (no query-param cleanup).
  // Mutating `?open=new` can cause a remount + visible blink and may trigger
  // redirect-loop detection in your navigation tracker.

  const formatDateInput = (date: Date) => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
  }

  const getPresetRange = (preset: 'today' | 'week' | 'month') => {
    const today = new Date()
    const end = formatDateInput(today)
    const startDate = new Date(today)

    if (preset === 'week') {
      startDate.setDate(startDate.getDate() - 6)
    }

    if (preset === 'month') {
      startDate.setDate(startDate.getDate() - 29)
    }

    return {
      from: formatDateInput(startDate),
      to: end
    }
  }

  const handlePresetFilter = (preset: 'today' | 'week' | 'month') => {
    const range = getPresetRange(preset)
    setDatePreset(preset)
    setFromDate(range.from)
    setToDate(range.to)
    setFromDateInput(range.from)
    setToDateInput(range.to)
    setCurrentPage(1)
  }

  const handleApplyDateRange = () => {
    if (!fromDateInput && !toDateInput) {
      handleClearFilters()
      return
    }

    if (fromDateInput && toDateInput && new Date(fromDateInput) > new Date(toDateInput)) {
      toast({
        title: 'Invalid Date Range',
        description: 'The start date must be before the end date.',
        variant: 'destructive',
      })
      return
    }

    setDatePreset('custom')
    setFromDate(fromDateInput)
    setToDate(toDateInput)
    setCurrentPage(1)
  }

  const handleClearFilters = () => {
    setDatePreset(null)
    setFromDate("")
    setToDate("")
    setFromDateInput("")
    setToDateInput("")
    setIncidentTypeFilter(null)
    setRegionFilter('all')
    setCurrentPage(1)
  }

  const isDateRangeActive = Boolean(fromDate || toDate)
  const isIncidentTypeFilterActive = Boolean(incidentTypeFilter)
  const isRegionFilterActive = regionFilter !== 'all'
  const isClientFilterActive = isDateRangeActive || isIncidentTypeFilterActive || isRegionFilterActive
  const shouldUseClientPagination = isClientFilterActive || enforceSiteScope
  const pageForApi = shouldUseClientPagination ? 1 : currentPage
  const pageSizeForApi = shouldUseClientPagination ? 1000 : itemsPerPage

  useEffect(() => {
    const presetParam = searchParams.get('preset')
    const incidentTypeParam = searchParams.get('incidentType')
    const fromDateParam = searchParams.get('fromDate')
    const toDateParam = searchParams.get('toDate')
    const searchParam = searchParams.get('search')
    if (searchParam) setSearchTerm(searchParam)

    if (!presetParam && !incidentTypeParam && !fromDateParam && !toDateParam) {
      return
    }

    if (presetParam === 'today' || presetParam === 'week' || presetParam === 'month') {
      const range = getPresetRange(presetParam)
      setDatePreset(presetParam)
      setFromDate(range.from)
      setToDate(range.to)
      setFromDateInput(range.from)
      setToDateInput(range.to)
      setCurrentPage(1)
    } else if (fromDateParam || toDateParam) {
      setDatePreset('custom')
      setFromDate(fromDateParam || "")
      setToDate(toDateParam || "")
      setFromDateInput(fromDateParam || "")
      setToDateInput(toDateParam || "")
      setCurrentPage(1)
    } else {
      setDatePreset(null)
      setFromDate("")
      setToDate("")
      setFromDateInput("")
      setToDateInput("")
    }

    if (incidentTypeParam) {
      setIncidentTypeFilter(incidentTypeParam)
    } else {
      setIncidentTypeFilter(null)
    }
  }, [searchParams])

  // Note: dialog auto-open is handled via `shouldAutoOpenNewIncident` initializer.

  useEffect(() => {
    let isMounted = true

    const loadRegions = async () => {
      setIsLoadingRegions(true)
      try {
        const response = isCustomerView && customerId
          ? await regionService.getRegionsByCustomer(Number(customerId))
          : await regionService.getRegions({ page: 1, pageSize: 1000 })
        if (isMounted) {
          setRegions(response.success ? response.data : [])
        }
      } catch (error) {
        if (isMounted) {
          setRegions([])
        }
      } finally {
        if (isMounted) {
          setIsLoadingRegions(false)
        }
      }
    }

    loadRegions()

    return () => {
      isMounted = false
    }
  }, [isCustomerView, customerId])

  // Debounce search term so we don't refetch on every keypress
  useEffect(() => {
    const handle = setTimeout(() => {
      setDebouncedSearch(searchTerm.trim())
      setCurrentPage(1)
    }, 300)

    return () => clearTimeout(handle)
  }, [searchTerm])

  // Fetch incidents using the API service
  const { data: incidentsResponse = { data: [], pagination: { currentPage: 1, totalPages: 1, pageSize: 10, totalCount: 0, hasPrevious: false, hasNext: false } }, isLoading, error } = useQuery({
    queryKey: ['incidents', pageForApi, pageSizeForApi, debouncedSearch, customerId, siteId, fromDate, toDate, incidentTypeFilter, regionFilter],
    queryFn: () => incidentsApi.getIncidents({
      page: pageForApi,
      pageSize: pageSizeForApi,
      search: debouncedSearch || undefined,
      ...(fromDate && { fromDate }),
      ...(toDate && { toDate }),
      ...(incidentTypeFilter && { incidentType: incidentTypeFilter }),
      ...(regionFilter !== 'all' && { regionId: regionFilter }),
      ...(isCustomerView && customerId && { customerId }),
      ...(siteId && { siteId })
    }),
    enabled: !isAutoOpenRef.current && (!enforceSiteScope || hasAssignedSites)
  })

  // Create/Update incident mutation using the API service
  const mutation = useMutation({
    onMutate: () => {
      return { isUpdate: Boolean(editingIncident?.id?.trim()) }
    },
    mutationFn: async (incident: Incident) => {
      const incidentIdToUpdate = editingIncident?.id?.trim()
      if (incidentIdToUpdate) {
        return await incidentsApi.updateIncident(incidentIdToUpdate, incident)
      } else {
        return await incidentsApi.createIncident(incident)
      }
    },
    onSuccess: (_data, _variables, context) => {
      queryClient.invalidateQueries({ queryKey: ['incidents'] })
      toast({
        title: 'Success',
        description: context?.isUpdate ? 'Incident updated successfully' : 'Incident created successfully',
      })
      handleCloseDialog()
    },
    onError: (error) => {
      console.error('Error saving incident:', error)
      toast({
        title: 'Error',
        description: `Failed to save incident: ${error instanceof Error ? error.message : 'Unknown error'}`,
        variant: 'destructive',
      })
    }
  })

  // Delete incident mutation using the API service
  const deleteMutation = useMutation({
    mutationFn: async (incidentId: string) => {
      await incidentsApi.deleteIncident(incidentId)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['incidents'] })
      toast({
        title: 'Success',
        description: 'Incident deleted successfully',
      })
      setDeletingIncident(null)
    },
    onError: (error) => {
      console.error('Error deleting incident:', error)
      toast({
        title: 'Error',
        description: `Failed to delete incident: ${error instanceof Error ? error.message : 'Unknown error'}`,
        variant: 'destructive',
      })
    }
  })

  const filteredIncidents = useMemo(() => {
    const baseData = filterByAssignedSiteIds(
      incidentsResponse.data,
      user,
      (incident) => incident.siteId ?? (incident as any).SiteId ?? (incident as any).siteID ?? (incident as any).SiteID ?? null
    )

    if (!isClientFilterActive) {
    	return baseData
    }

    const fromBoundary = fromDate ? new Date(`${fromDate}T00:00:00`) : null
    const toBoundary = toDate ? new Date(`${toDate}T23:59:59`) : null

    const selectedRegion = regions.find((region) => region.regionID.toString() === regionFilter)
    const selectedRegionName = selectedRegion?.regionName?.toLowerCase()

    return baseData.filter((incident) => {
      if (regionFilter !== 'all') {
        const regionIdMatches = incident?.regionId?.toString() === regionFilter
        const regionNameMatches = selectedRegionName
          ? (incident?.regionName || '').toLowerCase() === selectedRegionName
          : false
        if (!regionIdMatches && !regionNameMatches) {
          return false
        }
      }

      const incidentTypeValue = (incident?.incidentType || incident?.type || '').toLowerCase()
      if (incidentTypeFilter && incidentTypeValue !== incidentTypeFilter.toLowerCase()) {
        return false
      }

      const incidentDateValue = incident?.dateOfIncident || incident?.date
      if (!incidentDateValue) return false

      const incidentDate = new Date(incidentDateValue)
      if (Number.isNaN(incidentDate.getTime())) return false

      if (fromBoundary && incidentDate < fromBoundary) return false
      if (toBoundary && incidentDate > toBoundary) return false
      return true
    })
  }, [incidentsResponse.data, isClientFilterActive, fromDate, toDate, incidentTypeFilter, regionFilter, regions, user])

  // Calculate statistics — same rules as the Saved/Lost column (getIncidentFinancials).
  const stats = useMemo(() => {
    const sumFromIncidents = (incidents: Incident[]) => ({
      totalAmountRecovered: incidents.reduce(
        (acc, incident) => acc + getIncidentFinancials(incident).totalRecoveredValue,
        0
      ),
      totalAmountLost: incidents.reduce(
        (acc, incident) => acc + getIncidentFinancials(incident).totalLostValue,
        0
      ),
      uniqueSites: new Set(incidents.map((incident) => incident?.siteName).filter(Boolean)).size,
      totalIncidents: incidents.length,
    })

    const totalCount = incidentsResponse.pagination?.totalCount
    const hasFullDatasetLoaded =
      totalCount != null && totalCount > 0 && filteredIncidents.length >= totalCount

    // When every matching row is on screen, sum the table rows directly (guarantees card = column totals).
    if (hasFullDatasetLoaded || enforceSiteScope) {
      return sumFromIncidents(filteredIncidents)
    }

    // Paginated list: server summary across all pages (same financial rules as getIncidentFinancials).
    if (incidentsResponse.summary) {
      return {
        totalAmountRecovered: incidentsResponse.summary.totalAmountRecovered,
        totalAmountLost: incidentsResponse.summary.totalAmountLost,
        uniqueSites: incidentsResponse.summary.uniqueSites,
        totalIncidents: incidentsResponse.summary.totalIncidents,
      }
    }

    return sumFromIncidents(filteredIncidents)
  }, [
    incidentsResponse.summary,
    incidentsResponse.pagination?.totalCount,
    filteredIncidents,
    enforceSiteScope,
  ])

  const handleSubmit = useCallback((incident: Incident) => {
    mutation.mutate(incident)
  }, [mutation])

  const handleEdit = useCallback((incident: Incident) => {
    setCreatePrefilledStolenItems([])
    setEditingIncident(incident)
    setOpen(true)
  }, [])

  const handleView = useCallback((incident: Incident) => {
    console.log('Viewing Incident:', incident)
    setViewingIncident(incident)
  }, [])

  const handleDelete = useCallback((incident: Incident) => {
    setDeletingIncident(incident)
  }, [])

  const handleConfirmDelete = useCallback(() => {
    if (deletingIncident) {
      deleteMutation.mutate(deletingIncident.id)
    }
  }, [deletingIncident, deleteMutation])

  const clientTotalPages = Math.max(1, Math.ceil(filteredIncidents.length / itemsPerPage))
  const totalPages = shouldUseClientPagination ? clientTotalPages : incidentsResponse.pagination?.totalPages || 1

  // Update the filtered and paginated incidents
  const paginatedIncidents = shouldUseClientPagination
    ? filteredIncidents.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage)
    : incidentsResponse.data

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages)
    }
  }, [currentPage, totalPages])

  const hasIncidents = paginatedIncidents.length > 0
  const isSearchActive = Boolean(searchTerm)

  const handlePageChange = useCallback((page: number) => {
    if (page < 1 || page > totalPages) return
    setCurrentPage(page)
  }, [totalPages])

  useEffect(() => {
    productService
      .getDepartments()
      .then(setCatalogDepartments)
      .catch(() => setCatalogDepartments([]))
  }, [])

  const handleBarcodeScanned = useCallback(async (barcode: string) => {
    try {
      setIsLoadingProduct(true)

      const productData = await productService.getProductByEAN(barcode)

      if (!productData) {
        toast({
          title: 'Product Not Found',
          description: `No product found with barcode ${barcode}. Please enter product details manually.`,
          variant: 'destructive',
        })
        return
      }

      let departments = catalogDepartments
      if (departments.length === 0) {
        departments = await productService.getDepartments()
      }
      departments = mergeCatalogDepartment(departments, productData.department)
      setCatalogDepartments(departments)

      const newItem = mapProductToStolenItem(
        barcode,
        {
          productName: productData.productName,
          department: productData.department,
          description: productData.description,
          price: productData.price,
        },
        departments
      )

      if (editingIncident?.id?.trim()) {
        setEditingIncident({
          ...editingIncident,
          stolenItems: [...(editingIncident.stolenItems || []), newItem],
        })
      } else {
        setEditingIncident(null)
        setCreatePrefilledStolenItems((prev) => [...prev, newItem])
        setOpen(true)
      }

      const filled: string[] = ['product name']
      if (newItem.category) filled.push('department')
      if (newItem.description) filled.push('VME code')
      if (newItem.cost > 0) filled.push('retail price')

      toast({
        title: 'Product Added',
        description: `${productData.productName} added with ${filled.join(', ')} from the catalog. Adjust quantity if needed.`,
      })
    } catch (error) {
      console.error('Error with barcode:', error)
      toast({
        title: 'Error',
        description: 'Error processing barcode. Please try again.',
        variant: 'destructive',
      })
    } finally {
      setIsLoadingProduct(false)
      setScanningBarcode(false)
    }
  }, [editingIncident, catalogDepartments, toast])

  const clearDateFilter = () => {
    setDatePreset(null)
    setFromDate("")
    setToDate("")
    setFromDateInput("")
    setToDateInput("")
    setCurrentPage(1)
  }
  const formatIncidentDate = (value: string) => new Date(value).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
  const incidentTypeOptions = Array.from(
    new Set([...(incidentsResponse.data ?? []).map((incident: Incident) => incident.incidentType).filter(Boolean), ...(incidentTypeFilter ? [incidentTypeFilter] : [])])
  ).sort() as string[]
  const pageWindow = (() => {
    const size = Math.min(totalPages, 5)
    const start = Math.max(1, Math.min(currentPage - 2, totalPages - size + 1))
    return Array.from({ length: size }, (_, i) => start + i)
  })()

  // Loading state
  // If we were navigated here with ?open=new, avoid showing the loading screen/blink.
  if (isLoading && !isAutoOpenRef.current) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center" role="status">
        <div className="text-center">
          <div className="mx-auto mb-3 h-6 w-6 animate-spin rounded-full border-2 border-muted border-t-primary"></div>
          <p className="text-sm text-muted-foreground">Loading incidents…</p>
        </div>
      </div>
    )
  }

  // Error state with retry option
  // If we were navigated here with ?open=new, still show the "new incident" form dialog.
  if (error && !isAutoOpenRef.current) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div className="surface mx-auto max-w-md p-6 text-center">
          <div className="mb-4">
            <AlertCircle className="h-12 w-12 text-loss mx-auto" />
          </div>
          <h2 className="mb-2 text-lg font-semibold text-foreground">Incidents could not be loaded</h2>
          <p className="text-muted-foreground mb-4">
            {error instanceof Error ? error.message : 'An error occurred while loading incident data.'}
          </p>
          <div className="space-y-3">
            <Button
              onClick={() => window.location.reload()}
              className="w-full"
            >
              Reload page
            </Button>
            <Button
              variant="outline"
              onClick={() => {
                // Force refetch the data
                queryClient.invalidateQueries({ queryKey: ['incidents'] })
              }}
              className="w-full"
            >
              Try again
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="w-full min-w-0">
      <Toaster />
      {!isAutoOpenRef.current && (
        <div className="w-full max-w-full">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="Incident reports"
            description="Every incident recorded across your stores. Filter, review and open a report to see its evidence."
            className="pb-0"
            actions={
              <Button
                onClick={() => {
                  setEditingIncident(null)
                  setCreatePrefilledStolenItems([])
                  setOpen(true)
                }}
                className="w-full sm:w-auto"
                disabled={!canCreateIncident}
              >
                <PlusCircle className="h-4 w-4" />
                New incident report
              </Button>
            }
          />
          {enforceSiteScope && !hasAssignedSites && (
            <div className="rounded-lg border border-warning/30 bg-warning-soft px-4 py-3 text-sm text-warning">
              No stores are assigned to this account yet. Incident data appears once an administrator assigns a store.
            </div>
          )}

          <StatLedger
            items={[
              { label: 'Incidents', value: stats.totalIncidents.toLocaleString('en-GB'), hint: 'In the current filter', icon: <AlertCircle />, tone: 'neutral' },
              { label: 'Value lost', value: formatMoney(stats.totalAmountLost, 'headline'), hint: 'Not recovered', icon: <PoundSterling />, tone: 'loss' },
              { label: 'Value recovered', value: formatMoney(stats.totalAmountRecovered, 'headline'), hint: 'Returned or paid for', icon: <PoundSterling />, tone: 'recovered' },
              { label: 'Stores affected', value: stats.uniqueSites, hint: 'With at least one incident', icon: <Store />, tone: 'info' },
            ]}
          />
        </div>

        {/* Filters */}
        <div className="mt-6 space-y-3">
          <FilterBar
            label="Incident filters"
            activeCount={[isDateRangeActive, isRegionFilterActive, isIncidentTypeFilterActive].filter(Boolean).length}
            primary={
              <div className="relative w-full sm:w-60">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Search store, person, type…"
                  aria-label="Search incidents"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="h-9 pl-9"
                />
              </div>
            }
            actions={
              <>
                <Button
                  variant="ghost"
                  onClick={handleClearFilters}
                  className="flex-1 sm:flex-none"
                  disabled={!isDateRangeActive && !datePreset && !isIncidentTypeFilterActive && !isRegionFilterActive}
                >
                  Clear all
                </Button>
              </>
            }
            summary={
              <>
                <span className="figures mr-1 font-medium text-foreground">
                  {(shouldUseClientPagination ? filteredIncidents.length : incidentsResponse.pagination?.totalCount ?? paginatedIncidents.length).toLocaleString('en-GB')} incidents
                </span>
                {isDateRangeActive && (
                  <FilterChip onRemove={clearDateFilter} removeLabel="Clear dates">
                    {fromDate ? new Date(fromDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : 'Start'} to{' '}
                    {toDate ? new Date(toDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : 'today'}
                  </FilterChip>
                )}
                {isRegionFilterActive && (
                  <FilterChip onRemove={() => { setRegionFilter('all'); setCurrentPage(1) }} removeLabel="Show all regions">
                    {regions.find((r) => r.regionID.toString() === regionFilter)?.regionName ?? 'Region'}
                  </FilterChip>
                )}
                {isIncidentTypeFilterActive && (
                  <FilterChip onRemove={() => { setIncidentTypeFilter(null); setCurrentPage(1) }} removeLabel="Show all types">
                    {incidentTypeFilter}
                  </FilterChip>
                )}
                {!isClientFilterActive && <span>across all stores and dates</span>}
              </>
            }
          >
            <Segmented
              ariaLabel="Period"
              value={datePreset === 'today' || datePreset === 'week' || datePreset === 'month' ? datePreset : 'all'}
              options={[
                { value: 'all', label: 'All' },
                { value: 'today', label: 'Today' },
                { value: 'week', label: 'Week' },
                { value: 'month', label: 'Month' },
              ] as const}
              onChange={(value) => (value === 'all' ? clearDateFilter() : handlePresetFilter(value))}
              className="w-full justify-between sm:w-auto [&>button]:flex-1 sm:[&>button]:flex-none"
            />
            <DateRangeField
              from={fromDateInput}
              to={toDateInput}
              onFromChange={(value) => {
                setFromDateInput(value)
                setFromDate(value)
                setDatePreset(value ? 'custom' : null)
                setCurrentPage(1)
              }}
              onToChange={(value) => {
                setToDateInput(value)
                setToDate(value)
                setCurrentPage(1)
              }}
              fromLabel="Filter from date"
              toLabel="Filter to date"
            />
            <Select
              value={regionFilter}
              onValueChange={(value) => {
                setRegionFilter(value)
                setCurrentPage(1)
              }}
            >
              <SelectTrigger className="h-9 w-full sm:w-[170px]" aria-label="Region">
                <SelectValue placeholder={isLoadingRegions ? 'Loading regions…' : 'All regions'} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All regions</SelectItem>
                {regions.map((region) => (
                  <SelectItem key={region.regionID} value={region.regionID.toString()}>
                    {region.regionName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {incidentTypeOptions.length > 0 && (
              <Select
                value={incidentTypeFilter ?? 'all'}
                onValueChange={(value) => {
                  setIncidentTypeFilter(value === 'all' ? null : value)
                  setCurrentPage(1)
                }}
              >
                <SelectTrigger className="h-9 w-full sm:w-[170px]" aria-label="Incident type">
                  <SelectValue placeholder="All types" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All types</SelectItem>
                  {incidentTypeOptions.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </FilterBar>

          {/* Results */}
          <div className="min-w-0 overflow-hidden rounded-lg border border-border bg-card">
            {!hasIncidents && (
              <EmptyState
                className="py-16"
                icon={<FileText />}
                title={
                  isClientFilterActive
                    ? 'No incidents match these filters'
                    : isSearchActive
                      ? 'No incidents match your search'
                      : 'No incidents recorded yet'
                }
                description={
                  isClientFilterActive
                    ? 'Widen the dates or clear a filter to see more.'
                    : isSearchActive
                      ? 'Check the spelling, or search by store, person or incident type.'
                      : 'Reports filed by store teams will appear here.'
                }
                action={
                  isClientFilterActive ? (
                    <Button variant="outline" onClick={handleClearFilters}>Clear filters</Button>
                  ) : isSearchActive ? (
                    <Button variant="outline" onClick={() => setSearchTerm('')}>Clear search</Button>
                  ) : (
                    <Button
                      onClick={() => {
                        setEditingIncident(null)
                        setCreatePrefilledStolenItems([])
                        setOpen(true)
                      }}
                      disabled={!canCreateIncident}
                    >
                      <PlusCircle className="h-4 w-4" />
                      New incident report
                    </Button>
                  )
                }
              />
            )}

            {/* Phone: one tappable row per incident */}
            {hasIncidents && (
              <ul className="divide-y divide-border md:hidden">
                {paginatedIncidents.map((incident) => {
                  const f = getIncidentFinancials(incident)
                  const when = incident.dateOfIncident || incident.date
                  return (
                    <li key={incident.id}>
                      <button
                        type="button"
                        onClick={() => handleView(incident)}
                        className="!flex w-full !flex-col !items-stretch gap-1.5 !rounded-none !border-0 !px-4 !py-3.5 text-left !font-normal transition-colors active:bg-muted/60"
                      >
                        <span className="flex items-baseline justify-between gap-3">
                          <span className="min-w-0 truncate text-sm font-semibold text-foreground">{incident.siteName || 'Unknown store'}</span>
                          <span className={`figures shrink-0 text-sm font-semibold ${f.totalLostValue > 0 ? 'text-loss' : 'text-muted-foreground'}`}>
                            {f.totalLostValue > 0 ? `−${formatMoney(f.totalLostValue)}` : formatMoney(0)}
                          </span>
                        </span>
                        <span className="flex items-center justify-between gap-3">
                          <span className="flex min-w-0 items-center gap-2">
                            <span className="inline-flex shrink-0 items-center rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium text-foreground/80">
                              {incident.incidentType}
                            </span>
                            <span className="figures truncate text-xs text-muted-foreground">
                              {when ? formatIncidentDate(when) : 'No date'}
                              {incident.timeOfIncident ? <>, <span className="font-medium text-blue-600 dark:text-blue-400">{incident.timeOfIncident}</span></> : ''}
                            </span>
                          </span>
                          {f.totalRecoveredValue > 0 && (
                            <span className="figures shrink-0 text-xs font-medium text-recovered">+{formatMoney(f.totalRecoveredValue)} back</span>
                          )}
                        </span>
                        <span className="truncate text-xs text-muted-foreground">Reported by {incident.officerName || 'unknown'}</span>
                      </button>
                    </li>
                  )
                })}
              </ul>
            )}

            {/* Tablet and desktop table */}
            {hasIncidents && (
              <div className="hidden min-w-0 overflow-x-auto md:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-[150px]">Date and time</TableHead>
                      <TableHead>Store</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead className="hidden lg:table-cell">Reported by</TableHead>
                      <TableHead className="text-right">Recovered</TableHead>
                      <TableHead className="text-right">Lost</TableHead>
                      <TableHead className="w-[112px] text-right"><span className="sr-only">Actions</span></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {paginatedIncidents.map((incident) => {
                      const f = getIncidentFinancials(incident)
                      const when = incident.dateOfIncident || incident.date
                      return (
                        <TableRow
                          key={incident.id}
                          className="group cursor-pointer"
                          onClick={() => handleView(incident)}
                        >
                          <TableCell className="whitespace-nowrap">
                            <span className="block text-foreground">{when ? formatIncidentDate(when) : 'No date'}</span>
                            <span className="figures block text-xs font-medium text-blue-600 dark:text-blue-400">
                              {incident.timeOfIncident || (when ? new Date(when).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' }) : '')}
                            </span>
                          </TableCell>
                          <TableCell className="min-w-[180px]">
                            <span className="block font-medium text-foreground">{incident.siteName || 'Unknown store'}</span>
                            <span className="block text-xs text-muted-foreground">{incident.customerName}</span>
                          </TableCell>
                          <TableCell className="whitespace-nowrap">
                            <span className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-foreground/80">
                              {incident.incidentType}
                            </span>
                          </TableCell>
                          <TableCell className="hidden whitespace-nowrap text-foreground/80 lg:table-cell">
                            {incident.officerName || 'Unknown'}
                          </TableCell>
                          <TableCell className={`whitespace-nowrap text-right ${f.totalRecoveredValue > 0 ? 'font-medium text-recovered' : 'text-muted-foreground'}`}>
                            {formatMoney(f.totalRecoveredValue)}
                          </TableCell>
                          <TableCell className={`whitespace-nowrap text-right ${f.totalLostValue > 0 ? 'font-medium text-loss' : 'text-muted-foreground'}`}>
                            {formatMoney(f.totalLostValue)}
                          </TableCell>
                          <TableCell className="py-2" onClick={(e) => e.stopPropagation()}>
                            <div className="flex items-center justify-end gap-0.5 opacity-70 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
                              <Button variant="ghost" size="icon" onClick={() => handleView(incident)} className="!h-8 !w-8 !p-0" title="Open">
                                <Eye className="h-4 w-4" />
                                <span className="sr-only">Open incident</span>
                              </Button>
                              <Button variant="ghost" size="icon" onClick={() => handleEdit(incident)} className="!h-8 !w-8 !p-0" title="Edit">
                                <Edit2 className="h-4 w-4" />
                                <span className="sr-only">Edit incident</span>
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleDelete(incident)}
                                className="!h-8 !w-8 !p-0 hover:bg-loss-soft hover:text-loss"
                                title="Delete"
                              >
                                <Trash2 className="h-4 w-4" />
                                <span className="sr-only">Delete incident</span>
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </div>
            )}

            {/* Pagination */}
            {hasIncidents && totalPages > 1 && (
              <div className="flex items-center justify-between gap-3 border-t border-border px-4 py-3">
                <p className="figures text-xs text-muted-foreground">
                  Page <span className="font-medium text-foreground">{currentPage}</span> of {totalPages}
                </p>
                <div className="flex items-center gap-1">
                  <Button variant="outline" size="sm" onClick={() => handlePageChange(currentPage - 1)} disabled={currentPage === 1}>
                    Previous
                  </Button>
                  <div className="hidden items-center gap-1 sm:flex">
                    {pageWindow.map((page) => (
                      <Button
                        key={page}
                        variant={page === currentPage ? 'secondary' : 'ghost'}
                        size="sm"
                        onClick={() => handlePageChange(page)}
                        aria-current={page === currentPage ? 'page' : undefined}
                        aria-label={`Page ${page}`}
                        className="figures !w-8 !px-0"
                      >
                        {page}
                      </Button>
                    ))}
                  </div>
                  <Button variant="outline" size="sm" onClick={() => handlePageChange(currentPage + 1)} disabled={currentPage === totalPages}>
                    Next
                  </Button>
                </div>
              </div>
            )}
          </div>
        </div>
        </div>
      )}

      {/* Dialogs */}
      <Dialog 
        open={open} 
        onOpenChange={(isOpen) => {
          if (!isOpen) handleCloseDialog()
        }}
      >
        <DialogContent className="flex h-[100dvh] w-full max-w-none flex-col gap-0 overflow-clip rounded-none border-0 bg-background p-0 sm:h-[92vh] sm:w-[calc(100%-32px)] sm:max-w-[1200px] sm:rounded-lg sm:border">
          <DialogHeader className="shrink-0 border-b border-border bg-card px-4 py-4 pr-12 text-left sm:px-6">
            <DialogTitle className="font-display font-semibold">
              {isEditingExistingIncident ? 'Edit incident report' : 'New incident report'}
            </DialogTitle>
            <DialogDescription className="text-sm text-muted-foreground">
              {isEditingExistingIncident ? 'Change any details and save. Fields marked * are required.' : 'Fields marked * are required. Be as specific as you can about what was taken and who was involved.'}
            </DialogDescription>
          </DialogHeader>
          <div className="min-h-0 flex-1 overflow-y-auto bg-background">
            <Suspense fallback={<LazyLoadingFallback />}>
              <IncidentForm
                initialData={editingIncident}
                isEditMode={isEditingExistingIncident}
                prefilledStolenItems={createPrefilledStolenItems}
                onSubmit={handleSubmit}
                onCancel={() => {
                  handleCloseDialog()
                }}
                onScanBarcode={() => setScanningBarcode(true)}
                onBarcodeScanned={handleBarcodeScanned}
                isLoading={mutation.isPending}
                customerId={customerId}
                siteId={siteId}
              />
            </Suspense>
          </div>
        </DialogContent>
      </Dialog>

      <Sheet open={!!viewingIncident} onOpenChange={(isOpen) => !isOpen && setViewingIncident(null)}>
        <SheetContent side="right" className="flex w-full flex-col gap-0 p-0 sm:max-w-xl">
          {viewingIncident && (() => {
            const f = getIncidentFinancials(viewingIncident)
            const when = viewingIncident.dateOfIncident || viewingIncident.date
            return (
              <>
                <SheetHeader className="space-y-3 border-b border-border px-5 pb-5 pt-5 text-left sm:px-6">
                  <div className="flex flex-wrap items-center gap-2 pr-8">
                    <span className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-foreground/80">
                      {viewingIncident.incidentType}
                    </span>
                    <span className="figures text-xs text-muted-foreground">
                      {when ? formatIncidentDate(when) : 'No date'}
                      {viewingIncident.timeOfIncident ? `, ${viewingIncident.timeOfIncident}` : ''}
                    </span>
                  </div>
                  <SheetTitle className="font-display text-2xl font-semibold leading-tight tracking-tight">
                    {viewingIncident.siteName || 'Incident'}
                  </SheetTitle>
                  <SheetDescription className="text-sm">
                    {viewingIncident.customerName}{viewingIncident.officerName ? `, reported by ${viewingIncident.officerName}` : ''}
                  </SheetDescription>
                  <dl className="figures grid grid-cols-3 overflow-hidden rounded-lg border border-border">
                    <div className="px-3 py-2.5">
                      <dt className="text-xs text-muted-foreground">Stolen</dt>
                      <dd className="font-display text-lg font-semibold text-foreground">{formatMoney(f.totalStolenValue || f.totalRecoveredValue + f.totalLostValue)}</dd>
                    </div>
                    <div className="border-l border-border px-3 py-2.5">
                      <dt className="text-xs text-muted-foreground">Recovered</dt>
                      <dd className="font-display text-lg font-semibold text-recovered">{formatMoney(f.totalRecoveredValue)}</dd>
                    </div>
                    <div className="border-l border-border px-3 py-2.5">
                      <dt className="text-xs text-muted-foreground">Lost</dt>
                      <dd className="font-display text-lg font-semibold text-loss">{formatMoney(f.totalLostValue)}</dd>
                    </div>
                  </dl>
                </SheetHeader>
                <div className="min-h-0 flex-1 overflow-y-auto">
                  <IncidentDetailView incident={viewingIncident} />
                </div>
                <div className="flex items-center justify-between gap-2 border-t border-border bg-card px-5 py-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))] sm:px-6">
                  <Button
                    variant="ghost"
                    onClick={() => {
                      const incident = viewingIncident
                      setViewingIncident(null)
                      handleDelete(incident)
                    }}
                    className="text-loss hover:bg-loss-soft hover:text-loss"
                  >
                    <Trash2 className="h-4 w-4" />
                    Delete
                  </Button>
                  <div className="flex gap-2">
                    <Button variant="outline" onClick={() => setViewingIncident(null)}>
                      Close
                    </Button>
                    <Button onClick={() => handleEdit(viewingIncident)}>
                      <Edit2 className="h-4 w-4" />
                      Edit report
                    </Button>
                  </div>
                </div>
              </>
            )
          })()}
        </SheetContent>
      </Sheet>

      <AlertDialog 
        open={!!deletingIncident} 
        onOpenChange={(isOpen) => !isOpen && setDeletingIncident(null)}
      >
        <AlertDialogContent className="w-[calc(100%-32px)] max-w-md">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this incident report?</AlertDialogTitle>
            <AlertDialogDescription className="text-sm">
              It will be removed for everyone, including from analytics and alert history. This can't be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="gap-2 sm:gap-0">
            <AlertDialogCancel className="text-sm">Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDelete}
              className="bg-destructive hover:bg-destructive/90 text-white text-sm"
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'Deleting…' : 'Delete report'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <BarcodeScanner
        isOpen={scanningBarcode}
        onClose={() => setScanningBarcode(false)}
        onScan={handleBarcodeScanned}
      />
    </div>
  )
}