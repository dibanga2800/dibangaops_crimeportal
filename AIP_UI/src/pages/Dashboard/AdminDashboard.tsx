import React from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { formatMoney } from '@/lib/money'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { 
  Activity, 
  AlertCircle, 
  Calendar,
  Clock,
  Users,
  Shield,
  TrendingUp,
  Bell,
  ArrowUpRight,
  ArrowDownRight,
  ChevronRight,
  Home,
  Settings,
  HelpCircle,
  LogOut,
  User,
  Plus,
  Currency,
  Star,
  CheckCircle,
  FileText,
  Briefcase,
  MapPin,
  MessageSquare
} from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { TaskCard } from '@/components/dashboard/TaskCard'
import { IncidentTable } from '@/components/dashboard/IncidentTable'
import { OfficerPerformance } from '@/components/dashboard/OfficerPerformance'
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend, BarChart, Bar } from 'recharts'
import { format } from 'date-fns'
import { cn } from '@/lib/utils'
import { usePageAccess } from "@/contexts/PageAccessContext"
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { DashboardGreeting } from '@/components/dashboard/DashboardGreeting'
import { WeekHourGrid } from '@/components/dashboard/WeekHourGrid'
import { StoreLeaderboard } from '@/components/dashboard/StoreLeaderboard'
import { StatLedger } from '@/components/common/StatLedger'
import { FilterBar, FilterChip, DateRangeField } from '@/components/common/FilterBar'
import { Segmented } from '@/components/common/Segmented'
import { EmptyState } from '@/components/common/EmptyState'
import { useChartTheme, chartTooltipStyle, axisTick } from '@/lib/chart-theme'
import { analyticsService } from '@/services/analyticsService'
import { customerDashboardService } from '@/services/dashboardService'
import { incidentsApi } from '@/services/api/incidents'
import { alertInstancesApi } from '@/services/api/alertInstances'
import { classificationApi } from '@/services/api/classification'
import type { AnalyticsHubData } from '@/types/analytics'
import type { AlertSummary } from '@/types/alertInstances'
import type { IncidentAnalyticsSummary, RiskIndicator } from '@/types/classification'
import type { Incident } from '@/types/incidents'
import type { IncidentsResponse, IncidentListSummary, PaginationInfo } from '@/types/api'
import {
	isIncidentPriority,
	isIncidentStatus,
	isIncidentToday,
	isIncidentTypeShoplifting,
} from '@/pages/Dashboard/incident-stat-predicates'
import { BASE_API_URL } from '@/config/api'
import { sessionStore } from '@/state/sessionStore'
import { getAssignedSiteIds } from '@/utils/siteAccess'
import { toast } from '@/components/ui/use-toast'
import {
	dismissAllNotificationsFromServer,
	getDismissedAlertInstanceIds,
	NOTIFICATIONS_DISMISSED_EVENT,
} from '@/lib/notifications/dismissed-notifications'


const getIncidentFinancials = (incident: any) => {
  const stolenItems = Array.isArray(incident?.StolenItems)
    ? incident.StolenItems
    : Array.isArray(incident?.stolenItems)
      ? incident.stolenItems
      : []

  const totalStolenValue =
    incident?.TotalStolenValue ??
    incident?.totalStolenValue ??
    stolenItems.reduce((sum: number, item: any) => {
      const explicitAmount = item?.TotalAmount ?? item?.totalAmount
      if (explicitAmount !== undefined && explicitAmount !== null) {
        const parsed = Number(explicitAmount)
        return sum + (Number.isFinite(parsed) ? parsed : 0)
      }
      const computed = Number(item?.Cost ?? item?.cost ?? 0) * Number(item?.Quantity ?? item?.quantity ?? 0)
      return sum + (Number.isFinite(computed) ? computed : 0)
    }, 0)

  const totalRecoveredValue =
    incident?.TotalRecoveredValue ??
    incident?.totalRecoveredValue ??
    incident?.TotalValueRecovered ??
    incident?.totalValueRecovered ??
    incident?.ValueRecovered ??
    incident?.valueRecovered ??
    incident?.Value ??
    incident?.value ??
    stolenItems.reduce((sum: number, item: any) => {
      const parsed = Number(item?.RecoveredAmount ?? item?.recoveredAmount ?? 0)
      return sum + (Number.isFinite(parsed) ? parsed : 0)
    }, 0)

  const explicitLostValue =
    incident?.TotalLostValue ??
    incident?.totalLostValue ??
    incident?.ValueLost ??
    incident?.valueLost ??
    incident?.LostValue ??
    incident?.lostValue

  const totalLostValue =
    explicitLostValue !== undefined && explicitLostValue !== null
      ? Number(explicitLostValue) || 0
      : Math.max((Number(totalStolenValue) || 0) - (Number(totalRecoveredValue) || 0), 0)

  return {
    totalStolenValue: Number(totalStolenValue) || 0,
    totalRecoveredValue: Number(totalRecoveredValue) || 0,
    totalLostValue: Number(totalLostValue) || 0,
  }
}

// Dashboard quick-stat fallback predicates live in a sibling module so
// they can be unit-tested in isolation and so we don't drift away from
// the backend `IncidentListSummary` semantics. See
// ./incident-stat-predicates.ts for the parity rationale.

interface AdminDashboardProps {
  viewRole?: 'administrator' | 'manager' | 'security-officer' | 'store'
}

const AdminDashboard: React.FC<AdminDashboardProps> = ({ viewRole = 'administrator' }) => {
  const navigate = useNavigate();
  const chartTheme = useChartTheme();
  const { currentRole, isTestMode, testRole, isLoading } = usePageAccess();
  const effectiveRole = isTestMode && testRole ? testRole : currentRole;
  const sessionUser = React.useMemo(() => sessionStore.getUser(), [])
  const assignedSiteIds = React.useMemo(() => getAssignedSiteIds(sessionUser), [sessionUser])
  const assignedSiteIdsSet = React.useMemo(() => new Set(assignedSiteIds), [assignedSiteIds])
  const assignedSiteIdsKey = React.useMemo(() => assignedSiteIds.join(','), [assignedSiteIds])
  const hasAssignedSites = assignedSiteIds.length > 0
  const {
    selectedCustomerId: contextCustomerId,
    selectedSiteId: contextSiteId,
    assignedCustomers
  } = useCustomerSelection();
  const isScopedRole = effectiveRole === 'store' || effectiveRole === 'security-officer' || effectiveRole === 'manager'

  // For scoped roles/managers: use selected (or first assigned) customer so data stays constrained
  const effectiveCustomerId = React.useMemo(() => {
    if (isScopedRole) {
      return contextCustomerId ?? assignedCustomers[0]?.id ?? null;
    }
    if (viewRole !== 'manager') return null;
    return contextCustomerId ?? assignedCustomers[0]?.id ?? null;
  }, [isScopedRole, viewRole, contextCustomerId, assignedCustomers]);

  const effectiveSiteId = React.useMemo(() => {
    if (!isScopedRole) {
      return null
    }
    return contextSiteId ? String(contextSiteId) : null
  }, [isScopedRole, contextSiteId])

  // All state declarations must be at the top before any conditional returns
  const [analyticsData, setAnalyticsData] = React.useState<AnalyticsHubData | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = React.useState<boolean>(true);
  const [analyticsError, setAnalyticsError] = React.useState<string | null>(null);
  const [siteOptions, setSiteOptions] = React.useState<Array<{ id: string; name: string }>>([]);
  const [regionOptions, setRegionOptions] = React.useState<Array<{ id: string; name: string }>>([]);
  const [selectedRegion, setSelectedRegion] = React.useState<string>('all');
  const [activePeriod, setActivePeriod] = React.useState<'Daily' | 'Weekly' | 'Monthly' | 'Yearly'>('Monthly');
  const [fromDate, setFromDate] = React.useState('');
  const [toDate, setToDate] = React.useState('');
  const [fromDateInput, setFromDateInput] = React.useState('');
  const [toDateInput, setToDateInput] = React.useState('');
  const [dateRangeError, setDateRangeError] = React.useState<string | null>(null);
  const [loadedIncidents, setLoadedIncidents] = React.useState<Incident[]>([]);
  const [incidentsPagination, setIncidentsPagination] = React.useState<PaginationInfo | null>(null)
  const [incidentsSummary, setIncidentsSummary] = React.useState<IncidentListSummary | null>(null)
  const [incidentsLoading, setIncidentsLoading] = React.useState(true);
  const [alertSummary, setAlertSummary] = React.useState<AlertSummary | null>(null);
  const [aiAnalytics, setAiAnalytics] = React.useState<IncidentAnalyticsSummary | null>(null);
  const [, bumpNotificationDismissals] = React.useReducer((n: number) => n + 1, 0);

  React.useEffect(() => {
    const onDismissed = () => bumpNotificationDismissals()
    window.addEventListener(NOTIFICATIONS_DISMISSED_EVENT, onDismissed)
    return () => window.removeEventListener(NOTIFICATIONS_DISMISSED_EVENT, onDismissed)
  }, []);

  const isDateRangeActive = Boolean(fromDate || toDate);
  const viewBadgeLabel =
    viewRole === 'administrator'
      ? 'Admin View'
      : viewRole === 'manager'
        ? 'Manager View'
        : viewRole === 'store'
          ? 'Store View'
          : 'Officer View'
  const showIncidentReportCta = isScopedRole
  const scopedStoreLabel = React.useMemo(() => {
    if (!isScopedRole || !effectiveSiteId) {
      return null
    }

    const matchedSite = siteOptions.find(site => String(site.id) === String(effectiveSiteId))
    if (matchedSite?.name) {
      return matchedSite.name
    }

    const incidentSiteName = loadedIncidents.find(incident => String(incident.siteId || '') === String(effectiveSiteId))?.siteName
    if (incidentSiteName) {
      return incidentSiteName
    }

    return `Store ${effectiveSiteId}`
  }, [isScopedRole, effectiveSiteId, siteOptions, loadedIncidents])

  React.useEffect(() => {
    if (isScopedRole && selectedRegion !== 'all') {
      setSelectedRegion('all')
    }
  }, [isScopedRole, selectedRegion])

  const handleApplyDateRange = () => {
    if (!fromDateInput && !toDateInput) {
      setFromDate('');
      setToDate('');
      setDateRangeError(null);
      return;
    }

    if (fromDateInput && toDateInput && new Date(fromDateInput) > new Date(toDateInput)) {
      setDateRangeError('Start date must be before end date.');
      return;
    }

    setFromDate(fromDateInput);
    setToDate(toDateInput);
    setDateRangeError(null);
  };

  const handleClearDateRange = () => {
    setFromDate('');
    setToDate('');
    setFromDateInput('');
    setToDateInput('');
    setDateRangeError(null);
  };

  // Load incidents from API
  React.useEffect(() => {
    console.log('ðŸŽ¯ Incident loading useEffect triggered');
    const abortController = new AbortController();
    let isActive = true;

    const loadIncidents = async () => {
      console.log('ðŸš€ Starting loadIncidents function');
      try {
        setIncidentsLoading(true);
        
        console.log('ðŸ”„ Loading incidents using incidentsApi service');
        
        // Pass every active filter to the API so the server-side summary
        // (which backs the headline quick-stat cards) reflects the exact
        // population the user is viewing. Without these the summary would
        // span the unfiltered tenant scope while the cards advertise the
        // filtered subset, and the two would silently disagree.
        const incidentQueryParams: {
          page: number
          pageSize: number
          customerId?: string
          siteId?: string
          regionId?: string
          fromDate?: string
          toDate?: string
        } = {
          page: 1,
          pageSize: 1000
        }
        if (effectiveCustomerId != null) {
          incidentQueryParams.customerId = String(effectiveCustomerId)
        }
        if (effectiveSiteId) {
          incidentQueryParams.siteId = effectiveSiteId
        }
        if (selectedRegion && selectedRegion !== 'all') {
          incidentQueryParams.regionId = selectedRegion
        }
        if (fromDate) {
          incidentQueryParams.fromDate = fromDate
        }
        if (toDate) {
          incidentQueryParams.toDate = toDate
        }

        const response = await incidentsApi.getIncidents(incidentQueryParams);
        const typed = response as IncidentsResponse
        
        // Extract incidents from response (handle data, Data, items formats)
        type ResWithAlternates = IncidentsResponse & { Data?: unknown[]; items?: unknown[] }
        const res = response as ResWithAlternates
        let incidents: unknown[] = []
        if (Array.isArray(response)) {
          incidents = response
        } else if (res.data && Array.isArray(res.data)) {
          incidents = res.data
        } else if (res.Data && Array.isArray(res.Data)) {
          incidents = res.Data
        } else if (res.items && Array.isArray(res.items)) {
          incidents = res.items
        }

        // Log the first raw incident to see actual backend field names
        if (incidents.length > 0) {
          const first = incidents[0] as Record<string, unknown>
          console.log('ðŸ“‹ Raw backend incident (first):', first)
          console.log('ðŸ’° Value fields check:', {
            TotalValueRecovered: first.TotalValueRecovered,
            totalValueRecovered: first.totalValueRecovered,
            Value: first.Value,
            value: first.value,
            Amount: first.Amount,
            amount: first.amount
          })
          console.log('âœ… Status fields check:', {
            Status: first.Status,
            status: first.status,
            Priority: first.Priority,
            priority: first.priority
          })
        }

        // Transform backend format to frontend format
        // Priority order for site name: SiteName > LocationName > siteName > locationName
        const transformedIncidents = incidents.map((inc: any) => {
          const siteName = inc.SiteName || inc.LocationName || inc.siteName || inc.locationName || inc.Location || '';
          const financials = getIncidentFinancials(inc)

          return {
            id: inc.Id || inc.id || '',
            customerId: inc.CustomerId || inc.customerId || 0,
            customerName: inc.CustomerName || inc.customerName || '',
            siteName: siteName,
            siteId: inc.SiteId?.toString() || inc.siteId?.toString() || '',
            regionId: inc.RegionId?.toString() || inc.regionId?.toString() || '',
            regionName: inc.RegionName || inc.regionName || '',
            location: siteName, // Use same site name for location
            store: siteName, // Use same site name for store
            officerName: inc.OfficerName || inc.officerName || '',
            officerRole: inc.OfficerRole || inc.officerRole || '',
            officerType: inc.OfficerType || inc.officerType || '',
            dateOfIncident: inc.DateOfIncident || inc.dateOfIncident || inc.date || '',
            date: inc.DateOfIncident || inc.dateOfIncident || inc.date || '',
            timeOfIncident: inc.TimeOfIncident || inc.timeOfIncident || '',
            incidentType: inc.IncidentType || inc.incidentType || '',
            type: inc.IncidentType || inc.incidentType || '',
            description: inc.Description || inc.description || '',
            incidentDetails: inc.IncidentDetails || inc.incidentDetails || '',
            storeComments: inc.StoreComments || inc.storeComments || inc.SiteComments || inc.siteComments || '',
            totalStolenValue: financials.totalStolenValue,
            totalRecoveredValue: financials.totalRecoveredValue,
            totalLostValue: financials.totalLostValue,
            totalValueRecovered: financials.totalRecoveredValue,
            value: financials.totalRecoveredValue,
            valueRecovered: financials.totalRecoveredValue,
            lostValue: financials.totalLostValue,
            amount: financials.totalRecoveredValue,
            total: financials.totalRecoveredValue,
            stolenItems: inc.StolenItems || inc.stolenItems || [],
            policeInvolvement: inc.PoliceInvolvement || inc.policeInvolvement || false,
            urnNumber: inc.UrnNumber || inc.urnNumber || '',
            crimeRefNumber: inc.CrimeRefNumber || inc.crimeRefNumber || '',
            status: inc.Status || inc.status || 'pending',
            priority: inc.Priority || inc.priority || 'medium',
            actionTaken: inc.ActionTaken || inc.actionTaken || '',
            evidenceAttached: inc.EvidenceAttached || inc.evidenceAttached || false,
            witnessStatements: inc.WitnessStatements || inc.witnessStatements || [],
            reportNumber: inc.ReportNumber || inc.reportNumber || '',
            offenderName: inc.OffenderName || inc.offenderName || '',
            offenderSex: inc.OffenderSex || inc.offenderSex || '',
            gender: inc.Gender || inc.gender || inc.OffenderSex || '',
            offenderDOB: inc.OffenderDOB || inc.offenderDOB || '',
            offenderMarks: inc.OffenderMarks || inc.offenderMarks || '',
            offenderAddress: inc.OffenderAddress || inc.offenderAddress || undefined,
            dateInputted: inc.DateInputted || inc.dateInputted || '',
            // AI-assisted classification (for AI Insight column)
            incidentCategory: inc.IncidentCategory || inc.incidentCategory || undefined,
            incidentCategoryConfidence:
              typeof inc.IncidentCategoryConfidence === 'number'
                ? inc.IncidentCategoryConfidence
                : typeof inc.incidentCategoryConfidence === 'number'
                ? inc.incidentCategoryConfidence
                : undefined,
            riskLevel: (inc.RiskLevel || inc.riskLevel || '').toLowerCase() || undefined,
            riskScore:
              typeof inc.RiskScore === 'number'
                ? inc.RiskScore
                : typeof inc.riskScore === 'number'
                ? inc.riskScore
                : undefined,
            classificationVersion: inc.ClassificationVersion || inc.classificationVersion || undefined,
          };
        });

        const scopedTransformedIncidents = !isScopedRole
          ? transformedIncidents
          : !hasAssignedSites
            ? []
            : transformedIncidents.filter((incident) => {
                const incidentSiteId = String(incident.siteId || '')
                return incidentSiteId.length > 0 && assignedSiteIdsSet.has(incidentSiteId)
              })

        if (isActive) {
          setLoadedIncidents(scopedTransformedIncidents);
          setIncidentsPagination(typed.pagination ?? null)
          setIncidentsSummary(typed.summary ?? null)
          console.log('âœ… Loaded incidents:', scopedTransformedIncidents.length);
          
          if (scopedTransformedIncidents.length > 0) {
            // Log sample incident with value and status
            console.log('ðŸ“‹ Sample incident:', {
              id: scopedTransformedIncidents[0].id,
              siteName: scopedTransformedIncidents[0].siteName,
              date: scopedTransformedIncidents[0].dateOfIncident,
              value: scopedTransformedIncidents[0].totalValueRecovered || scopedTransformedIncidents[0].value || 0,
              status: scopedTransformedIncidents[0].status,
              priority: scopedTransformedIncidents[0].priority
            });
            
            // Calculate and log key metrics
            const totalValue = scopedTransformedIncidents.reduce((sum, inc) => sum + (inc.totalValueRecovered || inc.value || 0), 0);
            const resolved = scopedTransformedIncidents.filter(inc => inc.status === 'resolved').length;
            console.log('ðŸ’° Total value recovered:', totalValue);
            console.log('âœ… Resolved incidents:', resolved, '/', scopedTransformedIncidents.length);
          }
        }
      } catch (error) {
        if (error instanceof Error && error.name === 'AbortError') {
          console.log('â¹ï¸ Load incidents aborted');
          return; // Ignore abort errors
        }
        
        console.error('âŒ Failed to load incidents:', error);
        console.error('âŒ Error details:', {
          message: error instanceof Error ? error.message : String(error),
          stack: error instanceof Error ? error.stack : undefined
        });
        
        // Check if it's an auth error
        if (error instanceof Error && (error.message.includes('401') || (error as any).response?.status === 401)) {
          console.error('ðŸ” Authentication error - The backend /api/incidents endpoint returned 401');
          console.error('ðŸ’¡ Possible causes:');
          console.error('   1. Token is expired - try logging out and back in');
          console.error('   2. Backend requires different permissions for incidents endpoint');
          console.error('   3. Backend /api/incidents endpoint may not be configured yet');
        }
        
        if (isActive) {
          setLoadedIncidents([]);
          setIncidentsPagination(null)
          setIncidentsSummary(null)
        }
      } finally {
        if (isActive) {
          setIncidentsLoading(false);
        }
      }
    };

    console.log('ðŸ“ž Calling loadIncidents()');
    loadIncidents();

    return () => {
      isActive = false;
      abortController.abort();
    };
  }, [
    effectiveCustomerId,
    effectiveSiteId,
    isScopedRole,
    hasAssignedSites,
    assignedSiteIdsSet,
    assignedSiteIdsKey,
    selectedRegion,
    fromDate,
    toDate,
  ]);

  // Load analytics data for dashboard overview; for manager/scoped roles use assigned customer so regions/sites stay constrained
  React.useEffect(() => {
    const abortController = new AbortController();
    const loadAnalytics = async () => {
      try {
        setAnalyticsLoading(true);
        setAnalyticsError(null);

        // Manager/scoped roles: pass assigned customer so API returns constrained regions/sites; Admin: no override
        const [regions, sites] = await Promise.all([
          customerDashboardService.getRegions(abortController.signal, effectiveCustomerId ?? undefined),
          customerDashboardService.getSites(abortController.signal, effectiveCustomerId ?? undefined),
        ]);

        const allStoreOptions = (sites as any[]).map((site: any) => ({
          id: site.siteID || site.SiteID || site.id,
          name:
            site.locationName ||
            site.LocationName ||
            site.name ||
            `Store ${site.siteID || site.SiteID || site.id}`,
        }));
        const storeOptions = !isScopedRole
          ? allStoreOptions
          : !hasAssignedSites
            ? []
            : allStoreOptions.filter((site) => assignedSiteIdsSet.has(String(site.id)))

        // Use all regions/sites from API â€“ no static blocklist or customer filter; production data only
        const regionOpts = (regions as any[]).map((region: any) => ({
          id: region.id,
          name: region.name,
          customerId: region.customerId ?? region.CustomerId ?? region.fkCustomerID ?? region.FkCustomerID,
        }));

        if (import.meta.env.DEV) {
          console.log('ðŸ¢ Dashboard regions loaded:', regionOpts.length, viewRole === 'manager' ? `(customer ${effectiveCustomerId})` : '');
        }

        setSiteOptions(storeOptions);
        setRegionOptions(regionOpts);

        const data = await analyticsService.getAnalyticsHub({
          stores: storeOptions,
          regions: regionOpts,
        });

        setAnalyticsData(data);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        console.error('Failed to load admin analytics overview:', err);
        setAnalyticsError(
          err instanceof Error ? err.message : 'Failed to load analytics'
        );
      } finally {
        setAnalyticsLoading(false);
      }
    };

    loadAnalytics();
    return () => abortController.abort();
  }, [viewRole, effectiveCustomerId, isScopedRole, hasAssignedSites, assignedSiteIdsSet, assignedSiteIdsKey]);

  // Load alert summary and AI analytics from backend (uses default: last 90 days, user's customer scope)
  React.useEffect(() => {
    const loadEnhancedData = async () => {
      try {
        const analyticsParams = {
          customerId: effectiveCustomerId ?? undefined,
          siteId: effectiveSiteId ?? undefined,
          regionId: selectedRegion !== 'all' ? selectedRegion : undefined,
          from: fromDate || undefined,
          to: toDate || undefined
        }

        const [alertData, analyticsData] = await Promise.allSettled([
          alertInstancesApi.getSummary(effectiveCustomerId ?? undefined),
          classificationApi.getAnalyticsSummary(analyticsParams)
        ])

        if (alertData.status === 'fulfilled') {
          setAlertSummary(alertData.value)
        }
        if (analyticsData.status === 'fulfilled') {
          setAiAnalytics(analyticsData.value)
        }
      } catch (err) {
        console.warn('Enhanced dashboard data not available:', err)
      }
    }

    loadEnhancedData()
  }, [effectiveCustomerId, effectiveSiteId, selectedRegion, fromDate, toDate])

  // Get regions for dropdown â€“ use ONLY real regions from API for the current customer
  const regions = React.useMemo(() => {
    return regionOptions;
  }, [regionOptions]);

  // Filter incidents by selected region and date range
  const filteredIncidents = React.useMemo(() => {
    const scopedIncidents = !isScopedRole
      ? loadedIncidents
      : loadedIncidents.filter(incident => {
          if (effectiveCustomerId != null) {
            const incidentCustomerId = Number(incident.customerId)
            if (Number.isFinite(incidentCustomerId) && incidentCustomerId !== Number(effectiveCustomerId)) {
              return false
            }
          }
          if (effectiveSiteId) {
            const incidentSiteId = incident.siteId ? String(incident.siteId) : ''
            if (incidentSiteId !== String(effectiveSiteId)) {
              return false
            }
          }
          return true
        })

    const regionFiltered = selectedRegion === 'all'
      ? scopedIncidents
      : scopedIncidents.filter(incident => {
          const incidentRegionId = incident.regionId ? String(incident.regionId) : null
          const incidentRegionName = (incident.regionName || '').toLowerCase().trim()
          const selectedRegionName = regions.find(r => r.id === selectedRegion)?.name?.toLowerCase().trim()

          if (incidentRegionId && incidentRegionId === String(selectedRegion)) {
            return true
          }

          if (selectedRegionName && incidentRegionName && incidentRegionName === selectedRegionName) {
            return true
          }

          return false
        })

    if (!isDateRangeActive) {
      return regionFiltered
    }

    const startBoundary = fromDate ? new Date(`${fromDate}T00:00:00`) : null
    const endBoundary = toDate ? new Date(`${toDate}T23:59:59`) : null

    return regionFiltered.filter(incident => {
      const incidentDateValue = incident.dateOfIncident || incident.date
      if (!incidentDateValue) {
        return false
      }

      const incidentDate = new Date(incidentDateValue)
      if (Number.isNaN(incidentDate.getTime())) {
        return false
      }

      if (startBoundary && incidentDate < startBoundary) {
        return false
      }

      if (endBoundary && incidentDate > endBoundary) {
        return false
      }

      return true
    })
  }, [
    isScopedRole,
    effectiveCustomerId,
    effectiveSiteId,
    selectedRegion,
    loadedIncidents,
    regions,
    isDateRangeActive,
    fromDate,
    toDate
  ])

  // Headline quick-stat cards: the server-computed summary is the single
  // source of truth. It spans the *full* filtered population (same
  // customerId / siteId / regionId / fromDate / toDate the dashboard sent
  // on the request), so counts always reconcile with each other regardless
  // of the backend's PageSize ceiling (PageSize is clamped to 100 in
  // IncidentService, which would otherwise truncate any client-side count).
  //
  // Client-side counting from `filteredIncidents` is retained only as a
  // last-resort fallback for two scenarios:
  //   1. The initial render before the API responds (`incidentsSummary` is
  //      still null).
  //   2. An older backend that doesn't populate the optional summary
  //      fields (todayIncidents / highPriorityIncidents / etc.).
  // In normal operation against a current backend, every fallback branch
  // below short-circuits on the `summary?.X ?? …` left-hand side.
  const customerMetrics = React.useMemo(() => {
    const summary = incidentsSummary

    const sumFromIncidents = (incidents: Incident[]) => {
      let totalValue = 0
      let totalLostValue = 0
      for (const inc of incidents) {
        const f = getIncidentFinancials(inc)
        totalValue += f.totalRecoveredValue
        totalLostValue += f.totalLostValue
      }
      return {
        totalIncidents: incidents.length,
        totalValue,
        totalLostValue,
      }
    }

    const fallback = sumFromIncidents(filteredIncidents)

    const totalIncidents = summary?.totalIncidents ?? fallback.totalIncidents
    const totalValue = summary?.totalAmountRecovered ?? fallback.totalValue
    const totalLostValue = summary?.totalAmountLost ?? fallback.totalLostValue

    const todayIncidents = summary?.todayIncidents
      ?? filteredIncidents.filter(inc => isIncidentToday(inc)).length

    const highPriority = summary?.highPriorityIncidents
      ?? filteredIncidents.filter(inc => isIncidentPriority(inc, 'high')).length

    const pending = summary?.pendingIncidents
      ?? filteredIncidents.filter(inc => isIncidentStatus(inc, 'pending')).length
    const resolved = summary?.resolvedIncidents
      ?? filteredIncidents.filter(inc => isIncidentStatus(inc, 'resolved')).length

    const shopliftingIncidents = summary?.shopliftingIncidents
      ?? filteredIncidents.filter(isIncidentTypeShoplifting).length

    // Percentage uses the matching total so the two figures reconcile.
    // Without this guard a server-side count of 75 over a paginated 100
    // would read as 75%, dramatically over-reporting against the true
    // ratio across all rows.
    const shopliftingPercentage =
      totalIncidents > 0
        ? Math.round((shopliftingIncidents / totalIncidents) * 100)
        : 0

    return {
      totalIncidents,
      todayIncidents,
      highPriority,
      totalValue,
      totalLostValue,
      pending,
      resolved,
      shopliftingIncidents,
      shopliftingPercentage,
    }
  }, [incidentsSummary, filteredIncidents])

  // Get recent incidents for table â€“ use real backend data
  const recentIncidents = React.useMemo(() => {
    // Primary: use real loaded incidents from backend
    if (filteredIncidents.length > 0) {
      return filteredIncidents
        .slice()
        .sort((a, b) => new Date(b.dateOfIncident).getTime() - new Date(a.dateOfIncident).getTime())
        .slice(0, 10)
        .map(incident => {
          const financials = getIncidentFinancials(incident)
          return {
            id: incident.id,
            customerName: incident.customerName,
            store: incident.siteName || incident.store || 'N/A',
            siteName: incident.siteName || 'N/A',
            officerName: incident.officerName,
            date: incident.dateOfIncident || incident.date || '',
            timeOfIncident: incident.timeOfIncident,
            amount: financials.totalRecoveredValue,
            recoveredValue: financials.totalRecoveredValue,
            lostValue: financials.totalLostValue,
            incidentType: incident.incidentType,
            incidentCategory: incident.incidentCategory,
            incidentCategoryConfidence: incident.incidentCategoryConfidence,
            riskLevel: incident.riskLevel,
            riskScore: incident.riskScore,
          }
        })
    }

    return []
  }, [filteredIncidents, selectedRegion, isDateRangeActive])

  const scopedRecentAlertsBase = React.useMemo(() => {
    if (!alertSummary) {
      return []
    }
    if (!isScopedRole) {
      return alertSummary.recentAlerts
    }

    const scopedIncidentIds = new Set(
      loadedIncidents
        .map(incident => Number(incident.id))
        .filter(incidentId => Number.isFinite(incidentId))
    )

    return alertSummary.recentAlerts.filter(alert => {
      if (alert.incidentId == null) {
        return false
      }
      return scopedIncidentIds.has(Number(alert.incidentId))
    })
  }, [alertSummary, isScopedRole, loadedIncidents])

  const scopedRecentAlerts = React.useMemo(() => {
    const dismissedInstances = getDismissedAlertInstanceIds()
    return scopedRecentAlertsBase.filter((a) => {
      const id = Number(a.alertInstanceId)
      return Number.isFinite(id) && !dismissedInstances.has(id)
    })
  }, [scopedRecentAlertsBase, bumpNotificationDismissals])

  const scopedNewAlertCount = React.useMemo(() => {
    if (scopedRecentAlerts.length === 0) {
      return 0
    }
    return scopedRecentAlerts.filter(alert => alert.status === 'new').length
  }, [scopedRecentAlerts])

  // Generate alerts data from live API when available
  const alerts = React.useMemo(() => {
    if (scopedRecentAlerts.length > 0) {
      return scopedRecentAlerts.map(alert => {
        const createdDate = new Date(alert.createdAt)
        const minutesAgo = Math.round((Date.now() - createdDate.getTime()) / 60000)
        const timeLabel = minutesAgo < 60
          ? `${minutesAgo} minutes ago`
          : minutesAgo < 1440
            ? `${Math.round(minutesAgo / 60)} hours ago`
            : `${Math.round(minutesAgo / 1440)} days ago`

        return {
          id: alert.alertInstanceId.toString(),
          type: alert.severity === 'high' ? 'error' as const : alert.severity === 'medium' ? 'warning' as const : 'info' as const,
          title: alert.alertRuleName || 'Alert Triggered',
          message: alert.message || 'An alert rule was matched',
          time: timeLabel,
          priority: alert.severity as 'high' | 'medium' | 'low',
          status: alert.status
        }
      })
    }

    return []
  }, [scopedRecentAlerts])

  const handleClearDashboardNotifications = React.useCallback(() => {
    const fromSummary = (alertSummary?.recentAlerts ?? [])
      .map((a) => Number(a.alertInstanceId))
      .filter((n) => Number.isFinite(n))
    const fromBase = scopedRecentAlertsBase
      .map((a) => Number(a.alertInstanceId))
      .filter((n) => Number.isFinite(n))
    const fromVisibleRows = alerts
      .map((row) => Number(row.id))
      .filter((n) => Number.isFinite(n))
    const explicitInstanceIds = [...new Set([...fromSummary, ...fromBase, ...fromVisibleRows])]

    void dismissAllNotificationsFromServer({
      customerId: effectiveCustomerId ?? undefined,
      explicitInstanceIds,
    }).then(() => {
      toast({
        title: 'Notifications cleared',
        description:
          'These alerts are hidden until new alerts arrive. This does not resolve alerts on the server.',
      })
    })
  }, [alertSummary, scopedRecentAlertsBase, alerts, effectiveCustomerId])

  // Use real backend data for quick statistics
  const quickStats = React.useMemo(() => {
    // Always prefer real backend metrics
    return customerMetrics;
  }, [customerMetrics])

  // Generate heatmap data (store vs incident count) using real backend data
  const heatmapData = React.useMemo(() => {
    // Primary: derive from real loaded incidents
    if (filteredIncidents.length > 0) {
      const storeMap = new Map<string, number>()
      
      filteredIncidents.forEach(incident => {
        const store = incident.siteName || 'Unknown'
        storeMap.set(store, (storeMap.get(store) || 0) + 1)
      })

      const topStores = Array.from(storeMap.entries())
        .sort((a, b) => b[1] - a[1])
        .slice(0, 6)
        .map(([store, count]) => ({ store, incidents: count }))

      return topStores
    }

    // Fallback: use analytics hub data if no real incidents loaded yet
    if (!isDateRangeActive && analyticsData && analyticsData.hotProducts.storeHeatmap.length > 0) {
      return analyticsData.hotProducts.storeHeatmap
        .map(store => ({
          store: store.storeName,
          incidents: store.totalIncidents
        }))
        .sort((a, b) => b.incidents - a.incidents)
        .slice(0, 6)
    }

    return []
  }, [filteredIncidents, analyticsData, isDateRangeActive])

  // Log region selection and filtered data for debugging
  React.useEffect(() => {
    const selectedRegionName = selectedRegion === 'all' ? 'All regions' : regions.find(r => r.id === selectedRegion)?.name || selectedRegion;
    
    console.log('ðŸ—ºï¸ Selected region:', selectedRegionName, `(ID: ${selectedRegion})`);
    console.log('ðŸ“Š Total loaded incidents:', loadedIncidents.length);
    console.log('ðŸ“Š Filtered incidents count:', filteredIncidents.length);
    console.log('ðŸ“… Active time period:', activePeriod);
    
    if (loadedIncidents.length > 0 && filteredIncidents.length === 0 && selectedRegion !== 'all') {
      console.warn('âš ï¸ No incidents found for selected region:', selectedRegionName);
      console.log('ðŸ“‹ Available regions in loaded data:', 
        [...new Set(loadedIncidents.map(inc => `${inc.regionName} (ID: ${inc.regionId})`))].slice(0, 10).join(', ')
      );
    }
  }, [selectedRegion, filteredIncidents, activePeriod, regions, loadedIncidents]);

  // Generate chart data from filtered incidents based on active period and selected region
  const chartData = React.useMemo(() => {
    console.log('ðŸ” [Chart Data] Generating chart data');
    console.log('ðŸ“Š [Chart Data] Filtered incidents count:', filteredIncidents.length);
    console.log('ðŸ“… [Chart Data] Active period:', activePeriod);
    
    if (filteredIncidents.length > 0) {
      const sample = filteredIncidents[0];
      console.log('ðŸ“‹ [Chart Data] Sample incident:', {
        date: sample.dateOfIncident,
        officerRole: sample.officerRole,
        officerType: sample.officerType,
        id: sample.id
      });
    }
    
    // Generate period buckets with financial totals (recovered vs lost)
    const generateTimeData = () => {
      switch (activePeriod) {
        case 'Daily': {
          // Last 7 days
          const data: Array<{ date: string; recoveredValue: number; lostValue: number }> = []
          const today = new Date()
          
          for (let i = 6; i >= 0; i--) {
            const targetDate = new Date(today)
            targetDate.setDate(today.getDate() - i)
            targetDate.setHours(0, 0, 0, 0)
            
            const dayEnd = new Date(targetDate)
            dayEnd.setHours(23, 59, 59, 999)
            
            const dayKey = format(targetDate, 'EEE') // Mon, Tue, etc.
            
            const dayIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              return incDate >= targetDate && incDate <= dayEnd
            })

            const recoveredValue = dayIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalRecoveredValue,
              0
            )
            const lostValue = dayIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalLostValue,
              0
            )

            data.push({
              date: dayKey,
              recoveredValue,
              lostValue,
            })
          }
          return data
        }
        
        case 'Weekly': {
          // Last 4 weeks
          const data: Array<{ week: string; recoveredValue: number; lostValue: number }> = []
          const today = new Date()
          
          for (let i = 3; i >= 0; i--) {
            const weekStart = new Date(today)
            weekStart.setDate(today.getDate() - (i * 7 + 6))
            weekStart.setHours(0, 0, 0, 0)
            
            const weekEnd = new Date(weekStart)
            weekEnd.setDate(weekStart.getDate() + 6)
            weekEnd.setHours(23, 59, 59, 999)
            
            const weekIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              return incDate >= weekStart && incDate <= weekEnd
            })

            const recoveredValue = weekIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalRecoveredValue,
              0
            )
            const lostValue = weekIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalLostValue,
              0
            )

            data.push({
              week: `Week ${4 - i}`,
              recoveredValue,
              lostValue,
            })
          }
          return data
        }
        
        case 'Monthly': {
          // Last 12 months
          const data: Array<{ month: string; recoveredValue: number; lostValue: number }> = []
          const today = new Date()
          
          for (let i = 11; i >= 0; i--) {
            const monthDate = new Date(today.getFullYear(), today.getMonth() - i, 1)
            const monthKey = format(monthDate, 'MMM')
            
            const monthIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              return incDate.getMonth() === monthDate.getMonth() &&
                     incDate.getFullYear() === monthDate.getFullYear()
            })

            const recoveredValue = monthIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalRecoveredValue,
              0
            )
            const lostValue = monthIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalLostValue,
              0
            )

            data.push({
              month: monthKey,
              recoveredValue,
              lostValue,
            })
          }
          return data
        }
        
        case 'Yearly': {
          // Last 5 years
          const data: Array<{ year: string; recoveredValue: number; lostValue: number }> = []
          const currentYear = new Date().getFullYear()
          
          console.log('ðŸ“… [Yearly Chart] Current year:', currentYear);
          console.log('ðŸ“… [Yearly Chart] Total incidents to process:', filteredIncidents.length);
          
          for (let i = 4; i >= 0; i--) {
            const year = currentYear - i
            
            const yearIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              const incYear = incDate.getFullYear()
              return incYear === year && !isNaN(incYear)
            })
            
            console.log(`ðŸ“… [Yearly Chart] Year ${year}: ${yearIncidents.length} incidents`);

            const recoveredValue = yearIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalRecoveredValue,
              0
            )
            const lostValue = yearIncidents.reduce(
              (sum, inc) => sum + getIncidentFinancials(inc).totalLostValue,
              0
            )

            console.log(`ðŸ“… [Yearly Chart] Year ${year}: recovered=${recoveredValue}, lost=${lostValue}`);
            
            data.push({
              year: year.toString(),
              recoveredValue,
              lostValue,
            })
          }
          return data
        }
        
        default:
          return []
      }
    }
    
    const result = generateTimeData();
    console.log('ðŸ“ˆ [Chart Data] Generated data points:', result.length);
    console.log('ðŸ“ˆ [Chart Data] Sample data:', result.slice(0, 3));
    
    return result;
  }, [filteredIncidents, activePeriod, selectedRegion])

  // Get x-axis key based on active period
  const dataKey = activePeriod === 'Daily' ? 'date' : activePeriod === 'Weekly' ? 'week' : activePeriod === 'Monthly' ? 'month' : 'year'

  // Weekly series (last 8 weeks) behind the ledger sparklines, plus the change
  // of the last 4 weeks against the 4 before. Derived from the same filtered
  // incidents the rest of the page uses, so the lines always agree with the table.
  const ledgerTrends = React.useMemo(() => {
    const WEEKS = 8
    const weekMs = 7 * 24 * 60 * 60 * 1000
    // Anchor on the end of the chosen date range (or today) so the 4-week comparison follows the filter.
    const end = toDate ? Math.min(new Date(`${toDate}T23:59:59`).getTime(), Date.now()) : Date.now()
    const buckets = Array.from({ length: WEEKS }, () => ({ count: 0, lost: 0, recovered: 0, shoplifting: 0 }))
    filteredIncidents.forEach((incident) => {
      const t = new Date(incident.dateOfIncident || (incident as any).date || '').getTime()
      if (!Number.isFinite(t)) return
      const age = Math.floor((end - t) / weekMs)
      if (age < 0 || age >= WEEKS) return
      const b = buckets[WEEKS - 1 - age]
      const f = getIncidentFinancials(incident)
      b.count += 1
      b.lost += f.totalLostValue
      b.recovered += f.totalRecoveredValue
      if (isIncidentTypeShoplifting(incident)) b.shoplifting += 1
    })
    const pick = (key: 'count' | 'lost' | 'recovered' | 'shoplifting') => buckets.map((b) => b[key])
    const change = (series: number[]) => {
      const recent = series.slice(-4).reduce((a, v) => a + v, 0)
      const prior = series.slice(0, 4).reduce((a, v) => a + v, 0)
      if (prior === 0) return undefined
      return ((recent - prior) / prior) * 100
    }
    const series = { count: pick('count'), lost: pick('lost'), recovered: pick('recovered'), shoplifting: pick('shoplifting') }
    const hasHistory = series.count.some((v) => v > 0)
    return {
      hasHistory,
      series,
      change: {
        count: change(series.count),
        lost: change(series.lost),
        recovered: change(series.recovered),
        shoplifting: change(series.shoplifting),
      },
      endsToday: end >= Date.now() - 24 * 60 * 60 * 1000,
      end,
    }
  }, [filteredIncidents, toDate])


  // When incidents happen (day × time) and per-store league table, both from the incidents in view.
  const incidentTimes = React.useMemo(
    () =>
      filteredIncidents
        .map((incident) => {
          const raw = incident.dateOfIncident || (incident as any).date
          if (!raw) return null
          const d = new Date(raw)
          const time = (incident as any).timeOfIncident as string | undefined
          if (time && /^\d{1,2}:\d{2}/.test(time)) {
            const [h, m] = time.split(':').map(Number)
            d.setHours(h, m)
          }
          return d
        })
        .filter((d): d is Date => d instanceof Date && !Number.isNaN(d.getTime())),
    [filteredIncidents],
  )

  // The window the filters describe. With no dates chosen it runs from the
  // earliest incident in view to today, so every panel reads the same span.
  const viewWindow = React.useMemo(() => {
    const times = filteredIncidents
      .map((i) => new Date(i.dateOfIncident || (i as any).date || '').getTime())
      .filter((t) => Number.isFinite(t))
    const end = toDate ? new Date(`${toDate}T23:59:59`).getTime() : Date.now()
    const start = fromDate
      ? new Date(`${fromDate}T00:00:00`).getTime()
      : times.length ? Math.min(...times) : end - 56 * 24 * 60 * 60 * 1000
    return { start, end: Math.max(end, start + 1) }
  }, [filteredIncidents, fromDate, toDate])

  const scopeLabel = React.useMemo(() => {
    if (!fromDate && !toDate) return 'all time'
    const f = (d: string) => format(new Date(d), 'd MMM yyyy')
    if (fromDate && toDate) return `${f(fromDate)} to ${f(toDate)}`
    return fromDate ? `since ${f(fromDate)}` : `up to ${f(toDate)}`
  }, [fromDate, toDate])

  const storeRows = React.useMemo(() => {
    const BUCKETS = 8
    const span = viewWindow.end - viewWindow.start
    const byStore = new Map<string, { store: string; incidents: number; lost: number; recovered: number; trend: number[] }>()
    filteredIncidents.forEach((incident) => {
      const store = incident.siteName || (incident as any).storeName || 'Unknown store'
      const row = byStore.get(store) ?? { store, incidents: 0, lost: 0, recovered: 0, trend: Array(BUCKETS).fill(0) }
      const f = getIncidentFinancials(incident)
      row.incidents += 1
      row.lost += f.totalLostValue
      row.recovered += f.totalRecoveredValue
      const t = new Date(incident.dateOfIncident || (incident as any).date || '').getTime()
      if (Number.isFinite(t) && t >= viewWindow.start && t <= viewWindow.end) {
        const b = Math.min(BUCKETS - 1, Math.floor(((t - viewWindow.start) / span) * BUCKETS))
        row.trend[b] += 1
      }
      byStore.set(store, row)
    })
    return [...byStore.values()].sort((a, b) => b.incidents - a.incidents || b.lost - a.lost).slice(0, 8)
  }, [filteredIncidents, viewWindow])

  // Risk signals from the same incidents as every other panel, so they follow
  // the region and date filters. Thresholds match the classifier service.
  const riskSignals = React.useMemo(() => {
    if (filteredIncidents.length === 0) return []
    const day = 24 * 60 * 60 * 1000
    const end = Math.min(viewWindow.end, Date.now())
    const at = (i: Incident) => new Date(i.dateOfIncident || (i as any).date || '').getTime()
    const recent = filteredIncidents.filter((i) => at(i) > end - 7 * day && at(i) <= end).length
    const prior = filteredIncidents.filter((i) => at(i) > end - 14 * day && at(i) <= end - 7 * day).length
    const velocity = prior > 0 ? Math.min(recent / prior, 2) / 2 : recent > 0 ? 0.5 : 0
    const endLabel = toDate && new Date(`${toDate}T23:59:59`).getTime() < Date.now() ? `the week to ${format(new Date(toDate), 'd MMM')}` : 'the last 7 days'

    const offenderCounts = new Map<string, number>()
    filteredIncidents.forEach((i) => {
      const name = (i.offenderName || '').trim().toLowerCase()
      if (name && !['unknown', 'n/a', 'n/k', 'not known'].includes(name)) offenderCounts.set(name, (offenderCounts.get(name) ?? 0) + 1)
    })
    const repeat = [...offenderCounts.values()].filter((n) => n > 1).length
    const repeatScore = Math.min(repeat / 5, 1)

    let lost = 0
    let total = 0
    filteredIncidents.forEach((i) => {
      const f = getIncidentFinancials(i)
      lost += f.totalLostValue
      total += f.totalLostValue + f.totalRecoveredValue
    })
    const valueScore = total >= 10000 ? 1 : total >= 5000 ? 0.7 : total >= 1000 ? 0.4 : 0.1

    const police = filteredIncidents.filter((i) => i.policeInvolvement).length / filteredIncidents.length
    const level = (score: number, high: number, medium: number) => (score >= high ? 'high' : score >= medium ? 'medium' : 'low')

    return [
      { indicator: 'Incident velocity', level: level(velocity, 0.7, 0.4), score: velocity, description: `${recent} ${recent === 1 ? 'incident' : 'incidents'} in ${endLabel}, ${prior} the week before` },
      { indicator: 'Repeat offenders', level: level(repeatScore, 0.6, 0.3), score: repeatScore, description: repeat === 0 ? 'No named offender appears more than once' : `${repeat} named ${repeat === 1 ? 'offender appears' : 'offenders appear'} more than once` },
      { indicator: 'Value at risk', level: level(valueScore, 0.7, 0.4), score: valueScore, description: `${formatMoney(total, 'headline')} taken, ${formatMoney(lost, 'headline')} of it not recovered` },
      { indicator: 'Police involvement', level: level(police, 0.3, 0.1), score: police, description: `Police involved in ${Math.round(police * 100)}% of incidents` },
    ]
  }, [filteredIncidents, viewWindow, toDate])

  // One-sentence briefing under the greeting: the change that matters most right now.
  const briefing = React.useMemo(() => {
    const lostChange = ledgerTrends.change.lost
    const recent4Lost = ledgerTrends.series.lost.slice(-4).reduce((a, v) => a + v, 0)
    const topStore = storeRows[0]
    const parts: string[] = []
    if (ledgerTrends.hasHistory && recent4Lost > 0) {
      const direction =
        lostChange === undefined ? '' : Math.abs(lostChange) < 5 ? ', about the same as the 4 weeks before' : lostChange > 0 ? `, up ${Math.round(lostChange)}% on the 4 weeks before` : `, down ${Math.abs(Math.round(lostChange))}% on the 4 weeks before`
      const window = ledgerTrends.endsToday ? 'in the last 4 weeks' : `in the 4 weeks to ${format(new Date(ledgerTrends.end), 'd MMM yyyy')}`
      parts.push(`${formatMoney(recent4Lost, 'headline')} lost ${window}${direction}.`)
    }
    if (topStore && storeRows.length > 1) {
      const share = Math.round((topStore.incidents / Math.max(1, filteredIncidents.length)) * 100)
      parts.push(`${topStore.store} accounts for ${share}% of incidents.`)
    }
    return parts.join(' ')
  }, [ledgerTrends, storeRows, filteredIncidents.length])

  // Define background colors for each stat card based on type
  const getStatCardColor = (color: string) => {
    switch(color) {
      case 'green': return 'bg-emerald-800';
      case 'yellow': return 'bg-amber-800';
      case 'red': return 'bg-rose-800';
      case 'blue': return 'bg-blue-800';
      default: return 'bg-slate-800';
    }
  };

  // Show loading state while page access data is being loaded
  if (isLoading) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center" role="status">
        <p className="text-sm text-muted-foreground">Loading dashboard…</p>
      </div>
    );
  }

  const filtersActive = isDateRangeActive || selectedRegion !== 'all'
  const selectedRegionName = regions.find(r => r.id === selectedRegion)?.name
  const periodLabel =
    activePeriod === 'Daily' ? 'Last 7 days' :
    activePeriod === 'Weekly' ? 'Last 4 weeks' :
    activePeriod === 'Monthly' ? 'Last 12 months' : 'Last 5 years'
  const hasChartData = chartData.length > 0 && !chartData.every(d => (d.recoveredValue || 0) + (d.lostValue || 0) === 0)
  const maxHeat = Math.max(1, ...heatmapData.map(d => d.incidents))
  const newAlertCount = scopedNewAlertCount || alerts.filter(a => a.priority === 'high').length
  const riskTone = (level: string) =>
    level === 'high' ? 'bg-loss' : level === 'medium' ? 'bg-warning' : 'bg-recovered'
  const riskBadge = (level: string) =>
    level === 'high' ? 'destructive' as const : level === 'medium' ? 'warning' as const : 'success' as const
  // Scores arrive as 0–1 from the classifier; tolerate 0–100 too.
  const riskWidth = (score: number) => {
    const pct = score <= 1 ? score * 100 : score
    return `${Math.max(2, Math.min(100, Math.round(pct)))}%`
  }

  // Unified dashboard UI (admin/manager/store/security-officer)
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <DashboardGreeting showLastLogin={false} subtitle={briefing || undefined} />
        {showIncidentReportCta && (
          <Button asChild className="self-start sm:self-auto">
            <Link to="/operations/incident-report?open=new">
              <Plus className="h-4 w-4" />
              New incident report
            </Link>
          </Button>
        )}
      </div>

      {incidentsLoading && (
        <div className="flex items-center gap-3 rounded-lg border border-info/20 bg-info-soft px-4 py-3" role="status">
          <span className="h-4 w-4 animate-spin rounded-full border-2 border-info/30 border-t-info" aria-hidden />
          <p className="text-sm text-info">Loading incident data…</p>
        </div>
      )}
      {isScopedRole && !hasAssignedSites && (
        <div className="rounded-lg border border-warning/25 bg-warning-soft px-4 py-3">
          <p className="text-sm text-warning">
            No stores are assigned to this account yet. Metrics and recent incidents appear once an administrator assigns a store.
          </p>
        </div>
      )}

      {/* Scope and filters */}
      <FilterBar
        label="Dashboard filters"
        actions={
          <>
            <Button
              variant="ghost"
              onClick={handleClearDateRange}
              disabled={!isDateRangeActive && !fromDateInput && !toDateInput}
              className="flex-1 sm:flex-none"
            >
              Clear
            </Button>
          </>
        }
        summary={
          <>
            <span className="mr-0.5">Showing</span>
            <FilterChip>{viewBadgeLabel}</FilterChip>
            {isScopedRole && <FilterChip>{scopedStoreLabel || 'Assigned store'}</FilterChip>}
            <FilterChip
              onRemove={selectedRegion !== 'all' && !isScopedRole ? () => setSelectedRegion('all') : undefined}
              removeLabel="Show all regions"
            >
              {selectedRegion !== 'all' ? (selectedRegionName || 'Region filter') : 'All regions'}
            </FilterChip>
            <FilterChip onRemove={isDateRangeActive ? handleClearDateRange : undefined} removeLabel="Clear dates">
              {isDateRangeActive
                ? `${fromDate ? format(new Date(fromDate), 'd MMM yyyy') : 'Start'} to ${toDate ? format(new Date(toDate), 'd MMM yyyy') : 'today'}`
                : 'All dates'}
            </FilterChip>
          </>
        }
      >
        <Select value={selectedRegion} onValueChange={setSelectedRegion} disabled={isScopedRole}>
          <SelectTrigger className="w-full sm:w-[190px]" aria-label="Region">
            <MapPin className="h-4 w-4 shrink-0 text-muted-foreground" />
            <SelectValue placeholder="Select region" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All regions</SelectItem>
            {regions.map((region) => (
              <SelectItem key={region.id} value={region.id}>
                {region.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <DateRangeField
          from={fromDateInput}
          to={toDateInput}
          fromLabel="Filter incidents from date"
          toLabel="Filter incidents to date"
          onFromChange={(nextFrom) => {
            setDateRangeError(null)
            setFromDateInput(nextFrom)
            setFromDate(nextFrom)
          }}
          onToChange={(nextTo) => {
            setDateRangeError(null)
            setToDateInput(nextTo)
            setToDate(nextTo)
          }}
        />
      </FilterBar>
      {dateRangeError && (
        <p className="-mt-3 text-xs text-loss" role="alert">{dateRangeError}</p>
      )}

      {/* Headline figures */}
      <StatLedger
        loading={incidentsLoading}
        items={[
          {
            label: 'Incidents',
            value: quickStats.totalIncidents.toLocaleString('en-GB'),
            hint: filtersActive ? 'Within selected filters' : 'All time',
            tone: 'neutral',
            trend: ledgerTrends.hasHistory ? ledgerTrends.series.count : undefined,
            delta: ledgerTrends.change.count !== undefined ? { value: ledgerTrends.change.count, goodWhenUp: false, against: 'previous 4 weeks' } : undefined,
            onClick: () => navigate('/operations/incident-report'),
            ariaLabel: 'View all incidents',
          },
          {
            label: 'Value lost',
            value: formatMoney(quickStats.totalLostValue || 0, 'headline'),
            hint: filtersActive ? 'Within selected filters' : 'All time',
            tone: 'loss',
            trend: ledgerTrends.hasHistory ? ledgerTrends.series.lost : undefined,
            delta: ledgerTrends.change.lost !== undefined ? { value: ledgerTrends.change.lost, goodWhenUp: false, against: 'previous 4 weeks' } : undefined,
            onClick: () => navigate('/operations/incident-report'),
            ariaLabel: 'View incidents by value lost',
          },
          {
            label: 'Value recovered',
            value: formatMoney(quickStats.totalValue, 'headline'),
            hint: 'Returned to stock or paid for',
            tone: 'recovered',
            trend: ledgerTrends.hasHistory ? ledgerTrends.series.recovered : undefined,
            delta: ledgerTrends.change.recovered !== undefined ? { value: ledgerTrends.change.recovered, goodWhenUp: true, against: 'previous 4 weeks' } : undefined,
            onClick: () => navigate('/operations/incident-report'),
            ariaLabel: 'View incidents by value recovered',
          },
          {
            label: 'Shoplifting',
            value: quickStats.shopliftingIncidents.toLocaleString('en-GB'),
            hint: `${quickStats.shopliftingPercentage}% of incidents in view`,
            tone: 'warning',
            trend: ledgerTrends.hasHistory ? ledgerTrends.series.shoplifting : undefined,
            onClick: () => navigate('/operations/incident-report?incidentType=Shoplifting'),
            ariaLabel: 'View shoplifting incidents',
          },
        ]}
      />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-12">
        <div className="min-w-0 xl:col-span-8">
          {/* Loss vs recovery over time */}
          <Card>
            <CardHeader className="flex flex-col gap-3 space-y-0 pb-2 sm:flex-row sm:items-start sm:justify-between">
              <div className="space-y-0.5">
                <CardTitle>Loss and recovery</CardTitle>
                <p className="text-[13px] text-muted-foreground">
                  {periodLabel}
                  {selectedRegion !== 'all' && `, ${selectedRegionName || 'selected region'}`}
                </p>
              </div>
              <Segmented
                ariaLabel="Chart period"
                value={activePeriod}
                options={['Daily', 'Weekly', 'Monthly', 'Yearly'] as const}
                onChange={(period) => setActivePeriod(period)}
                size="sm"
              />
            </CardHeader>
            <CardContent>
              {!hasChartData && !incidentsLoading ? (
                <EmptyState
                  className="h-[280px]"
                  icon={<AlertCircle />}
                  title="No incidents in this period"
                  description={
                    selectedRegion === 'all'
                      ? `Nothing was recorded for the ${periodLabel.toLowerCase()}. Try a longer period.`
                      : `Nothing was recorded for ${selectedRegionName || 'this region'} in the ${periodLabel.toLowerCase()}.`
                  }
                  action={selectedRegion !== 'all' ? (
                    <Button variant="outline" size="sm" onClick={() => setSelectedRegion('all')}>
                      Show all regions
                    </Button>
                  ) : undefined}
                />
              ) : (
                <>
                  <div className="mb-3 flex items-center gap-4 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1.5">
                      <span className="h-0.5 w-4 rounded-full bg-loss" aria-hidden />
                      Lost
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="h-0.5 w-4 rounded-full bg-recovered" aria-hidden />
                      Recovered
                    </span>
                  </div>
                  <div className="h-[240px] sm:h-[280px]">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart data={chartData} margin={{ top: 6, right: 8, left: -8, bottom: 0 }}>
                        <defs>
                          <linearGradient id="dashLost" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor={chartTheme.loss} stopOpacity={0.18} />
                            <stop offset="100%" stopColor={chartTheme.loss} stopOpacity={0} />
                          </linearGradient>
                          <linearGradient id="dashRecovered" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor={chartTheme.recovered} stopOpacity={0.18} />
                            <stop offset="100%" stopColor={chartTheme.recovered} stopOpacity={0} />
                          </linearGradient>
                        </defs>
                        <CartesianGrid vertical={false} stroke={chartTheme.grid} strokeDasharray="0" />
                        <XAxis dataKey={dataKey} tick={axisTick(chartTheme)} axisLine={{ stroke: chartTheme.grid }} tickLine={false} />
                        <YAxis
                          tick={axisTick(chartTheme)}
                          axisLine={false}
                          tickLine={false}
                          width={56}
                          tickFormatter={(value) => formatMoney(Number(value), 'axis')}
                        />
                        <Tooltip
                          {...chartTooltipStyle(chartTheme)}
                          formatter={(value, name) => [formatMoney(Number(value), 'exact'), name]}
                        />
                        <Area
                          type="monotone"
                          name="Lost"
                          dataKey="lostValue"
                          stroke={chartTheme.loss}
                          fill="url(#dashLost)"
                          strokeWidth={2}
                          activeDot={{ r: 4, strokeWidth: 2, stroke: chartTheme.card, fill: chartTheme.loss }}
                        />
                        <Area
                          type="monotone"
                          name="Recovered"
                          dataKey="recoveredValue"
                          stroke={chartTheme.recovered}
                          fill="url(#dashRecovered)"
                          strokeWidth={2}
                          activeDot={{ r: 4, strokeWidth: 2, stroke: chartTheme.card, fill: chartTheme.recovered }}
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

        </div>
        <aside className="flex min-w-0 flex-col xl:col-span-4" aria-label="Needs attention">
          {/* Alerts */}
          <Card className="flex flex-1 flex-col">
            <CardHeader className="flex flex-row items-center justify-between gap-2 space-y-0 pb-3">
              <CardTitle className="flex items-center gap-2">
                <Bell className="h-4 w-4 text-muted-foreground" />
                Needs attention
                {alerts.length > 0 && newAlertCount > 0 && (
                  <Badge variant="destructive" className="figures">{newAlertCount} new</Badge>
                )}
              </CardTitle>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={handleClearDashboardNotifications}
                aria-label="Clear notifications from dashboard and bell"
                disabled={alerts.length === 0}
              >
                Clear
              </Button>
            </CardHeader>
            <CardContent className="p-0">
              {alerts.length === 0 ? (
                <EmptyState icon={<Bell />} title="No alerts" description="Alerts appear here when an alert rule matches a new incident." className="py-8" />
              ) : (
                <ul className="divide-y divide-border border-t border-border">
                  {alerts.slice(0, 5).map((alert) => (
                    <li key={alert.id} className="flex gap-3 px-5 py-3.5">
                      <span
                        aria-hidden
                        className={cn(
                          'mt-1.5 h-2 w-2 shrink-0 rounded-full',
                          alert.type === 'error' ? 'bg-loss' : alert.type === 'warning' ? 'bg-warning' : 'bg-info'
                        )}
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-baseline justify-between gap-3">
                          <p className="truncate text-sm font-medium text-foreground">{alert.title}</p>
                          <span className="shrink-0 text-xs text-muted-foreground">{alert.time}</span>
                        </div>
                        <p className="mt-0.5 text-[13px] leading-5 text-muted-foreground">{alert.message}</p>
                        {alert.priority === 'high' && (
                          <Badge variant="destructive" className="mt-1.5">High priority</Badge>
                        )}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
              {alerts.length > 5 && (
                <p className="border-t border-border px-5 py-2.5 text-xs text-muted-foreground">
                  {alerts.length - 5} more {alerts.length - 5 === 1 ? 'alert' : 'alerts'} in the notification bell
                </p>
              )}
            </CardContent>
          </Card>

        </aside>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-12">
        {/* Store league table */}
        <Card className="overflow-hidden xl:col-span-7">
          <CardHeader className="flex flex-row items-start justify-between gap-2 space-y-0 pb-3">
            <div className="space-y-0.5">
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-4 w-4 text-muted-foreground" />
                Stores to watch
              </CardTitle>
              <p className="text-[13px] text-muted-foreground">Ranked by incidents for {scopeLabel}, with losses and how each moved across the period</p>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/analytics/data-analytics-hub">
                Analytics
                <ChevronRight className="h-3.5 w-3.5" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent className="p-0">
            {storeRows.length === 0 ? (
              <EmptyState icon={<MapPin />} title="No store activity yet" className="py-8" />
            ) : (
              <StoreLeaderboard rows={storeRows} />
            )}
          </CardContent>
        </Card>

        {/* Day and time pattern */}
        <Card className="xl:col-span-5">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-muted-foreground" />
              When incidents happen
            </CardTitle>
            <p className="text-[13px] text-muted-foreground">Day of week against time of day for {scopeLabel}. Plan cover around the darkest cells.</p>
          </CardHeader>
          <CardContent>
            {incidentTimes.length === 0 ? (
              <EmptyState icon={<Clock />} title="No incidents in view" className="py-8" />
            ) : (
              <WeekHourGrid times={incidentTimes} />
            )}
          </CardContent>
        </Card>
      </div>

      {/* Risk signals */}
      {riskSignals.length > 0 && (
        <section aria-labelledby="risk-signals-title" className="rounded-xl border border-border bg-card">
          <div className="flex flex-wrap items-baseline justify-between gap-2 px-5 pb-3 pt-4">
            <h2 id="risk-signals-title" className="flex items-center gap-2 text-base font-semibold text-foreground">
              <TrendingUp className="h-4 w-4 text-muted-foreground" />
              Risk signals
            </h2>
            <p className="text-[13px] text-muted-foreground">Based on the incidents in view ({scopeLabel})</p>
          </div>
          <ul className="grid grid-cols-1 divide-y divide-border border-t border-border sm:grid-cols-2 sm:divide-y-0 xl:grid-cols-4">
            {riskSignals.map((indicator, idx) => (
              <li
                key={idx}
                className={cn(
                  'space-y-2 px-5 py-4',
                  idx > 0 && 'sm:border-l sm:border-border',
                  idx === 2 && 'sm:border-l-0 sm:border-t xl:border-l xl:border-t-0',
                  idx === 3 && 'sm:border-t xl:border-t-0'
                )}
              >
                <div className="flex items-center justify-between gap-3">
                  <span className="text-sm font-medium text-foreground">{indicator.indicator}</span>
                  <Badge variant={riskBadge(indicator.level)} className="shrink-0 first-letter:uppercase">{indicator.level} risk</Badge>
                </div>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted" aria-hidden>
                  <div className={cn('h-full rounded-full', riskTone(indicator.level))} style={{ width: riskWidth(indicator.score) }} />
                </div>
                {indicator.description && (
                  <p className="text-xs leading-5 text-muted-foreground">{indicator.description}</p>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}

        {/* Recent incidents */}
        <Card className="overflow-hidden">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <div className="space-y-0.5">
              <CardTitle>Recent incidents</CardTitle>
              <p className="text-[13px] text-muted-foreground">Latest reports in the current view</p>
            </div>
            <Button variant="outline" size="sm" asChild>
              <Link to="/operations/incident-report">
                View all
                <ChevronRight className="h-3.5 w-3.5" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent className="p-0">
            <IncidentTable data={recentIncidents} />
          </CardContent>
        </Card>

      {incidentsPagination && incidentsPagination.totalCount > filteredIncidents.length && filteredIncidents.length > 0 && (
        <p className="text-xs text-muted-foreground">
          Totals cover all {incidentsPagination.totalCount.toLocaleString('en-GB')} incidents in view. Trends, stores and times use the latest {filteredIncidents.length.toLocaleString('en-GB')}.
        </p>
      )}
    </div>
  )
}

export default AdminDashboard
