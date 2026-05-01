import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { useAuth } from '@/contexts/AuthContext'
import { usePageAccess } from '@/contexts/PageAccessContext'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { dashboardService } from '@/services/dashboardService'
import { classificationApi } from '@/services/api/classification'
import { Activity, RecentIncident } from '@/types/dashboard'
import { DashboardGreeting } from '@/components/dashboard/DashboardGreeting'
import {
  FileWarning, FileSearch, Building, Calendar, CalendarRange,
  BadgeCheck, ClipboardCheck, Key, HelpCircle, Wallet, Shirt,
  Bell, Clock, Target, Award, TrendingUp, Shield,
  Users, Eye, MapPin, AlertTriangle, Activity as ActivityIcon,
  Star, Timer, ChevronRight, ArrowUpRight, ArrowDownRight,
  Zap, ChevronLeft, ChevronRightIcon, PlusCircle
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

// Component Props Types
interface StatCardProps {
  title: string
  value: string | number
  change?: string
  trend?: 'up' | 'down'
  icon: React.ElementType
  gradient: string
  subtitle?: string
  link?: string
}

interface ProgressCardProps {
  title: string
  current: number
  target: number
  unit: string
  color: string
}

const formatCurrency = (value: number) => `£${value.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
const formatDateInput = (date: Date) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const getMonthComparisonLabel = (current: number, previous: number) => {
  if (previous <= 0) {
    return current > 0 ? 'vs last month' : 'No data for last month'
  }
  const diff = current - previous
  const pct = (diff / Math.max(previous, 1)) * 100
  const sign = diff >= 0 ? '+' : ''
  return `${sign}${diff.toFixed(2)} (${pct.toFixed(0)}%) vs last month`
}

// Components
const StatCard: React.FC<StatCardProps> = ({ 
  title, 
  value, 
  change, 
  trend, 
  icon: Icon, 
  gradient,
  subtitle,
  link
}) => {
  const content = (
    <Card className={`relative overflow-hidden border-0 shadow-lg ${gradient} ${link ? 'cursor-pointer hover:shadow-xl transition-shadow duration-200' : ''} h-full`}>
      <CardContent className="p-3 sm:p-4">
        <div className="flex items-start justify-between h-full">
          <div className="space-y-1.5">
            <p className="text-white/80 text-[11px] sm:text-xs font-medium line-clamp-1">{title}</p>
            <div className="space-y-1">
              <p className="text-xl sm:text-2xl lg:text-3xl font-bold text-white">{value}</p>
              {subtitle && <p className="text-white/70 text-[10px] sm:text-xs line-clamp-1">{subtitle}</p>}
              {change && (
                <div className="flex items-center gap-1">
                  {trend === 'up' ? (
                    <ArrowUpRight className="h-3 w-3 text-white/80" />
                  ) : (
                    <ArrowDownRight className="h-3 w-3 text-white/80" />
                  )}
                  <span className="text-white/80 text-[10px] sm:text-xs">{change}</span>
                </div>
              )}
            </div>
          </div>
          <div className="h-7 w-7 sm:h-8 sm:w-8 rounded-full bg-white/20 flex items-center justify-center flex-shrink-0">
            <Icon className="h-3.5 w-3.5 sm:h-4 sm:w-4 text-white" />
          </div>
        </div>
      </CardContent>
    </Card>
  );

  if (link) {
    return <Link to={link} className="block h-full">{content}</Link>;
  }

  return content;
}

const ActivityItem: React.FC<{ activity: Activity }> = ({ activity }) => {
  const getActivityIcon = (type: Activity['type']) => {
    switch (type) {
      case 'incident': return AlertTriangle
      case 'patrol': return Shield
      case 'report': return FileWarning
      default: return ActivityIcon
    }
  }

  const getActivityColor = (type: Activity['type']) => {
    switch (type) {
      case 'incident': return 'bg-red-500'
      case 'patrol': return 'bg-blue-500'
      case 'report': return 'bg-green-500'
      default: return 'bg-gray-500'
    }
  }

  const Icon = getActivityIcon(activity.type)

  return (
    <div className="flex items-start gap-2 sm:gap-3 p-2 sm:p-3 rounded-lg border bg-white hover:shadow-md transition-shadow">
      <div className={`rounded-full p-1.5 ${getActivityColor(activity.type)} text-white flex-shrink-0`}>
        <Icon className="h-3 w-3" />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <h4 className="font-medium text-xs sm:text-sm text-gray-900 truncate">{activity.title}</h4>
          <span className="text-[10px] sm:text-xs text-gray-500 whitespace-nowrap">{activity.time}</span>
        </div>
        <p className="text-[10px] sm:text-xs text-gray-600 flex items-center gap-1 mt-1">
          <MapPin className="h-3 w-3 flex-shrink-0" />
          <span className="truncate">{activity.location}</span>
        </p>
        {activity.value && (
          <p className="text-[10px] sm:text-xs font-medium text-green-600 mt-1">
            Value: £{activity.value.toFixed(2)}
          </p>
        )}
        <Badge 
          variant={activity.status === 'resolved' ? 'default' : 'secondary'}
          className="text-[10px] sm:text-xs mt-2"
        >
          {activity.status}
        </Badge>
      </div>
    </div>
  )
}

const ProgressCard: React.FC<ProgressCardProps> = ({ title, current, target, unit, color }) => {
  const percentage = target > 0 ? Math.min((current / target) * 100, 100) : 0
  const formatMetricValue = (value: number) => {
    if (unit === '£') {
      return formatCurrency(value)
    }
    return `${Math.round(value).toLocaleString()} ${unit}`
  }
  
  return (
    <Card>
      <CardContent className="p-3 sm:p-4 md:p-6">
        <div className="space-y-2 sm:space-y-3 md:space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-medium text-xs sm:text-sm md:text-base text-gray-900">{title}</h3>
            <Badge variant={percentage >= 80 ? 'default' : 'secondary'} className="text-[10px] sm:text-xs">
              {percentage.toFixed(0)}%
            </Badge>
          </div>
          
          <div className="space-y-1.5 sm:space-y-2">
            <div className="flex justify-between text-[10px] sm:text-xs">
              <span className="text-gray-600">Current: {formatMetricValue(current)}</span>
              <span className="text-gray-600">Target: {formatMetricValue(target)}</span>
            </div>
            <Progress value={percentage} className="h-1.5 sm:h-2" />
          </div>
          
          <div className="text-[10px] sm:text-xs text-gray-600">
            {target - current > 0 ? (
              <>Need {formatMetricValue(target - current)} more to reach target</>
            ) : (
              <>🎉 Target achieved! {formatMetricValue(current - target)} ahead</>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

const IncidentTable: React.FC<{ incidents: RecentIncident[] }> = ({ incidents }) => {
  const [page, setPage] = React.useState(1)
  const pageSize = 5
  const totalPages = Math.ceil(incidents.length / pageSize)
  
  const paginatedIncidents = React.useMemo(() => 
    incidents.slice((page - 1) * pageSize, page * pageSize),
    [page, incidents]
  )

  return (
    <div className="space-y-3 sm:space-y-4">
      <div className="overflow-x-auto -mx-4 sm:mx-0">
        <div className="inline-block min-w-full align-middle">
          <div className="overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[120px] sm:w-[150px]">Officer</TableHead>
                  <TableHead className="w-[80px] sm:w-[100px]">Date</TableHead>
                  <TableHead className="w-[160px] sm:w-[200px]">Site Name</TableHead>
                  <TableHead className="w-[120px] sm:w-[150px]">Type</TableHead>
                  <TableHead className="text-right w-[90px] sm:w-[120px]">Recovered</TableHead>
                  <TableHead className="text-right w-[90px] sm:w-[120px]">Lost</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedIncidents.map((incident) => (
                  <TableRow key={incident.id}>
                    <TableCell className="font-medium text-xs sm:text-sm">{incident.officerName}</TableCell>
                    <TableCell className="text-xs sm:text-sm">{new Date(incident.date).toLocaleDateString()}</TableCell>
                    <TableCell className="max-w-[160px] sm:max-w-[200px] truncate text-xs sm:text-sm">{incident.siteName}</TableCell>
                    <TableCell className="text-xs sm:text-sm">{incident.type}</TableCell>
                    <TableCell className="text-right text-xs sm:text-sm text-green-700 dark:text-green-300">
                      {((incident.recoveredValue ?? incident.value ?? incident.amount ?? 0) > 0)
                        ? `£${(incident.recoveredValue ?? incident.value ?? incident.amount ?? 0).toFixed(2)}`
                        : '-'}
                    </TableCell>
                    <TableCell className="text-right text-xs sm:text-sm text-red-700 dark:text-red-300">
                      {((incident.lostValue ?? 0) > 0) ? `£${(incident.lostValue ?? 0).toFixed(2)}` : '-'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      </div>
      
      <div className="flex flex-col sm:flex-row items-center justify-between gap-2 sm:gap-4 px-1">
        <div className="text-[10px] sm:text-xs text-gray-500 text-center sm:text-left">
          Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, incidents.length)} of {incidents.length} incidents
        </div>
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="h-7 sm:h-8 px-2 sm:px-3"
          >
            <ChevronLeft className="h-3 w-3 sm:h-4 sm:w-4" />
            <span className="sr-only sm:not-sr-only sm:ml-1">Previous</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="h-7 sm:h-8 px-2 sm:px-3"
          >
            <span className="sr-only sm:not-sr-only sm:mr-1">Next</span>
            <ChevronRightIcon className="h-3 w-3 sm:h-4 sm:w-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}

export default function OfficerDashboard() {
  const { user: loggedInUser } = useAuth()
  const { selectedCustomerId, selectedSiteId } = useCustomerSelection()
  const today = React.useMemo(() => new Date(), [])
  const initialFromDate = React.useMemo(
    () => formatDateInput(new Date(today.getFullYear(), today.getMonth(), 1)),
    [today]
  )
  const initialToDate = React.useMemo(() => formatDateInput(today), [today])
  const [fromDate, setFromDate] = React.useState(initialFromDate)
  const [toDate, setToDate] = React.useState(initialToDate)

  // Fetch dashboard data
  const { 
    data: dashboardData,
    isLoading: isDashboardLoading,
    error: dashboardError
  } = useQuery({
    queryKey: ['officerDashboard'],
    queryFn: () => dashboardService.getOfficerDashboard()
  })

  // Fetch incidents for assigned stores (scoped by customer/site)
  const {
    data: incidentsData,
    isLoading: isIncidentsLoading,
    error: incidentsError
  } = useQuery({
    queryKey: ['recentIncidents', selectedCustomerId ?? 'all', selectedSiteId ?? 'all', fromDate, toDate],
    queryFn: () =>
      dashboardService.getRecentIncidents({
        customerId: selectedCustomerId ?? undefined,
        siteId: selectedSiteId ?? undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined
      })
  })

  // Fetch AI risk indicators for assigned stores
  const { data: aiAnalytics } = useQuery({
    queryKey: ['officerAnalytics', selectedCustomerId ?? 'all', selectedSiteId ?? 'all', fromDate, toDate],
    queryFn: () =>
      classificationApi.getAnalyticsSummary({
        customerId: selectedCustomerId ?? undefined,
        siteId: selectedSiteId ?? undefined,
        from: fromDate || undefined,
        to: toDate || undefined
      }),
    enabled: selectedCustomerId != null || selectedSiteId != null
  })

  const isLoading = isDashboardLoading || isIncidentsLoading
  const error = dashboardError || incidentsError

  // Always show Incident Report button on OfficerDashboard – this dashboard is for store/officer users
  // whose primary workflow is reporting incidents. Route protection handles unauthorized access.
  const showNewIncidentButton = true

  const handleFromDateChange = (value: string) => {
    if (toDate && value && new Date(value) > new Date(toDate)) {
      setToDate(value)
    }
    setFromDate(value)
  }

  const handleToDateChange = (value: string) => {
    if (fromDate && value && new Date(fromDate) > new Date(value)) {
      setFromDate(value)
    }
    setToDate(value)
  }

  const handleResetDateFilter = () => {
    setFromDate(initialFromDate)
    setToDate(initialToDate)
  }

  const selectedRangeLabel = React.useMemo(() => {
    if (fromDate && toDate) {
      return `${new Date(`${fromDate}T00:00:00`).toLocaleDateString()} - ${new Date(`${toDate}T00:00:00`).toLocaleDateString()}`
    }
    if (fromDate) {
      return `From ${new Date(`${fromDate}T00:00:00`).toLocaleDateString()}`
    }
    if (toDate) {
      return `Up to ${new Date(`${toDate}T00:00:00`).toLocaleDateString()}`
    }
    return 'All dates'
  }, [fromDate, toDate])

  const computedStats = React.useMemo(() => {
    if (!incidentsData || incidentsData.length === 0) {
      return {
        incidentsThisMonth: 0,
        incidentsLastMonth: 0,
        totalValueThisMonth: 0,
        totalValueLastMonth: 0,
        recoveredValueThisMonth: 0,
        recoveredValueLastMonth: 0,
        lostValueThisMonth: 0,
        lostValueLastMonth: 0,
      }
    }

    let incidentsThisMonth = 0
    let incidentsLastMonth = 0
    let totalValueThisMonth = 0
    let totalValueLastMonth = 0
    let recoveredValueThisMonth = 0
    let recoveredValueLastMonth = 0
    let lostValueThisMonth = 0
    let lostValueLastMonth = 0
    const startBoundary = fromDate ? new Date(`${fromDate}T00:00:00`) : null
    const endBoundary = toDate ? new Date(`${toDate}T23:59:59`) : null

    for (const inc of incidentsData) {
      const d = new Date(inc.date)
      if (Number.isNaN(d.getTime())) {
        continue
      }
      if (startBoundary && d < startBoundary) {
        continue
      }
      if (endBoundary && d > endBoundary) {
        continue
      }

      const recoveredValue =
        typeof inc.recoveredValue === 'number' && !Number.isNaN(inc.recoveredValue)
          ? inc.recoveredValue
          : typeof inc.value === 'number' && !Number.isNaN(inc.value)
            ? inc.value
            : typeof inc.amount === 'number' && !Number.isNaN(inc.amount)
              ? inc.amount
              : 0

      const lostValue =
        typeof inc.lostValue === 'number' && !Number.isNaN(inc.lostValue)
          ? inc.lostValue
          : 0

      incidentsThisMonth += 1
      totalValueThisMonth += recoveredValue
      recoveredValueThisMonth += recoveredValue
      lostValueThisMonth += lostValue
    }

    return {
      incidentsThisMonth,
      incidentsLastMonth,
      totalValueThisMonth,
      totalValueLastMonth,
      recoveredValueThisMonth,
      recoveredValueLastMonth,
      lostValueThisMonth,
      lostValueLastMonth,
    }
  }, [incidentsData, fromDate, toDate])

  const monthlyFinancialSnapshot = React.useMemo(() => {
    const totalImpact = computedStats.recoveredValueThisMonth + computedStats.lostValueThisMonth
    const recoveryRate = totalImpact > 0
      ? (computedStats.recoveredValueThisMonth / totalImpact) * 100
      : 0

    return {
      recoveryRate,
      incidentsInRangeCount: computedStats.incidentsThisMonth,
      averageRecoveredPerIncident: computedStats.incidentsThisMonth > 0
        ? computedStats.recoveredValueThisMonth / computedStats.incidentsThisMonth
        : 0,
      averageLostPerIncident: computedStats.incidentsThisMonth > 0
        ? computedStats.lostValueThisMonth / computedStats.incidentsThisMonth
        : 0
    }
  }, [computedStats])

  // Recent Activity: prefer incidents for assigned stores; fallback to dashboard activities
  const recentActivities = React.useMemo((): Activity[] => {
    if (incidentsData && incidentsData.length > 0) {
      return incidentsData.map((inc) => ({
        id: inc.id,
        type: 'incident' as const,
        title: inc.type || inc.incidentType || 'Incident reported',
        location: inc.siteName || inc.store || 'Unknown site',
        time: (() => {
          const d = new Date(inc.date)
          return isNaN(d.getTime()) ? '' : d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
        })(),
        value: inc.value ?? inc.amount,
        status: 'submitted' as const
      }))
    }
    return dashboardData?.recentActivities ?? []
  }, [incidentsData, dashboardData])

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50 p-4">
        <Card className="w-full max-w-md mx-auto">
          <CardHeader>
            <CardTitle className="text-red-600 text-base sm:text-lg">Error Loading Dashboard</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-gray-600 text-sm">{error instanceof Error ? error.message : 'An error occurred'}</p>
            <Button 
              className="mt-4 w-full sm:w-auto"
              onClick={() => window.location.reload()}
            >
              Retry
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (isLoading || !dashboardData || !incidentsData) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50">
        <div className="text-center space-y-3">
          <div className="animate-spin rounded-full h-8 w-8 sm:h-10 sm:w-10 border-b-2 border-blue-600 mx-auto"></div>
          <p className="text-gray-600 text-sm">Loading dashboard...</p>
        </div>
      </div>
    )
  }

  return (
    <main className="min-h-screen min-w-0 overflow-x-hidden bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50">
      <div className="w-full max-w-full mx-auto px-3 sm:px-4 lg:px-6 py-4 sm:py-6">
        <div className="w-full min-w-0 max-w-full mx-auto space-y-4 sm:space-y-6">
          {/* Header Section */}
          <header className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
            <div className="flex-1 min-w-0">
              <DashboardGreeting />
            </div>
            {showNewIncidentButton && (
              <Button
                asChild
                className="flex-shrink-0 h-9 sm:h-10 px-3 sm:px-4 gap-2 bg-blue-600 hover:bg-blue-700"
              >
                <Link to="/operations/incident-report?open=new" className="flex items-center">
                  <PlusCircle className="h-4 w-4" aria-hidden />
                  <span>Incident Report</span>
                </Link>
              </Button>
            )}
          </header>

          <section
            aria-label="Date filter"
            className="rounded-lg border bg-white/80 backdrop-blur-sm p-3 sm:p-4"
          >
            <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-3">
              <div className="flex items-center gap-2 text-sm font-medium text-gray-700">
                <CalendarRange className="h-4 w-4 text-blue-600" />
                Date Range
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 w-full lg:w-auto">
                <div className="space-y-1">
                  <label htmlFor="officer-from-date" className="text-xs text-gray-600">From</label>
                  <Input
                    id="officer-from-date"
                    type="date"
                    value={fromDate}
                    onChange={(event) => handleFromDateChange(event.target.value)}
                    className="h-9"
                  />
                </div>
                <div className="space-y-1">
                  <label htmlFor="officer-to-date" className="text-xs text-gray-600">To</label>
                  <Input
                    id="officer-to-date"
                    type="date"
                    value={toDate}
                    onChange={(event) => handleToDateChange(event.target.value)}
                    className="h-9"
                  />
                </div>
                <Button
                  type="button"
                  variant="outline"
                  className="h-9 self-end"
                  onClick={handleResetDateFilter}
                >
                  Reset to this month
                </Button>
              </div>
            </div>
          </section>

          {/* Stats Grid */}
          <section aria-label="Dashboard Statistics" className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
            <StatCard
              title="Incidents in Range"
              value={computedStats.incidentsThisMonth}
              icon={Shield}
              gradient="bg-gradient-to-br from-blue-500 to-blue-700"
              subtitle={selectedRangeLabel}
              link="/operations/incident-report"
            />
            <StatCard
              title="Value Lost"
              value={formatCurrency(computedStats.lostValueThisMonth)}
              icon={AlertTriangle}
              gradient="bg-gradient-to-br from-rose-500 to-rose-700"
              subtitle={selectedRangeLabel}
              link="/operations/incident-report"
            />
            <StatCard
              title="Value Recovered"
              value={formatCurrency(computedStats.recoveredValueThisMonth)}
              icon={Wallet}
              gradient="bg-gradient-to-br from-emerald-500 to-green-700"
              subtitle={selectedRangeLabel}
              link="/operations/incident-report"
            />
            <StatCard
              title="Incidents in Scope"
              value={computedStats.incidentsThisMonth}
              icon={Calendar}
              gradient="bg-gradient-to-br from-amber-500 to-orange-600"
              subtitle={selectedRangeLabel}
            />
          </section>

          {/* Main Content Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            {/* Left Column - Monthly Progress and Incident Table */}
            <div className="lg:col-span-2 space-y-4">
              {/* Monthly Progress */}
              <section aria-label="Monthly Progress">
                <h2 className="text-sm sm:text-base font-semibold mb-3 flex items-center gap-2">
                  <Target className="h-4 w-4 sm:h-5 sm:w-5 text-green-600" />
                  Monthly Progress (Selected Range)
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <ProgressCard
                    title="Operational Target: Incidents Handled"
                    current={computedStats.incidentsThisMonth}
                    target={dashboardData.monthlyTarget.incidents}
                    unit="incidents"
                    color="blue"
                  />
                  <ProgressCard
                    title="Financial Target: Value Recovered"
                    current={computedStats.recoveredValueThisMonth}
                    target={dashboardData.monthlyTarget.valueSaved}
                    unit="£"
                    color="green"
                  />
                </div>

                <Card className="mt-3">
                  <CardContent className="p-3 sm:p-4">
                    <div className="flex items-center justify-between gap-3 mb-3">
                      <h3 className="text-xs sm:text-sm font-semibold text-gray-900">Store Financial Snapshot (Selected Range)</h3>
                      <Badge variant="secondary" className="text-[10px] sm:text-xs">
                        Live from incidents
                      </Badge>
                    </div>

                    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
                      <div className="rounded-md border bg-rose-50/70 p-2 sm:p-3">
                        <p className="text-[10px] sm:text-xs text-gray-600">Value Lost</p>
                        <p className="text-sm sm:text-base font-semibold text-rose-700">{formatCurrency(computedStats.lostValueThisMonth)}</p>
                      </div>
                      <div className="rounded-md border bg-emerald-50/70 p-2 sm:p-3">
                        <p className="text-[10px] sm:text-xs text-gray-600">Value Recovered</p>
                        <p className="text-sm sm:text-base font-semibold text-emerald-700">{formatCurrency(computedStats.recoveredValueThisMonth)}</p>
                      </div>
                      <div className="rounded-md border bg-blue-50/70 p-2 sm:p-3">
                        <p className="text-[10px] sm:text-xs text-gray-600">Recovery Rate</p>
                        <p className="text-sm sm:text-base font-semibold text-blue-700">{monthlyFinancialSnapshot.recoveryRate.toFixed(1)}%</p>
                      </div>
                      <div className="rounded-md border bg-amber-50/70 p-2 sm:p-3">
                        <p className="text-[10px] sm:text-xs text-gray-600">Incidents in Range</p>
                        <p className="text-sm sm:text-base font-semibold text-amber-700">{monthlyFinancialSnapshot.incidentsInRangeCount}</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </section>

              {/* Recent Incidents */}
              <section aria-label="Recent Incidents">
                <h2 className="text-sm sm:text-base font-semibold mb-3 flex items-center gap-2">
                  <FileWarning className="h-4 w-4 sm:h-5 sm:w-5 text-red-600" />
                  Recent Incidents
                </h2>
                <Card>
                  <CardContent className="p-3 sm:p-4">
                    <div className="overflow-x-auto">
                      <IncidentTable incidents={incidentsData} />
                    </div>
                  </CardContent>
                </Card>
              </section>
            </div>

            {/* Right Column - Activity & Tasks */}
            <div className="space-y-4">
              {/* Recent Activity */}
              <section aria-label="Recent Activity">
                <Card>
                  <CardHeader className="p-3 sm:p-4">
                    <CardTitle className="flex items-center gap-2 text-sm sm:text-base">
                      <ActivityIcon className="h-4 w-4 sm:h-5 sm:w-5 text-blue-600" />
                      Recent Activity
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="p-3 sm:p-4 space-y-2">
                    {recentActivities.length > 0 ? (
                      recentActivities.map((activity) => (
                        <ActivityItem key={activity.id} activity={activity} />
                      ))
                    ) : (
                      <p className="text-xs text-gray-500 py-2">No recent activity for your assigned stores.</p>
                    )}
                  </CardContent>
                </Card>
              </section>

              {/* AI Risk Indicators (assigned stores) */}
              <section aria-label="AI Risk Indicators">
                <Card>
                  <CardHeader className="p-3 sm:p-4">
                    <CardTitle className="flex items-center gap-2 text-sm sm:text-base">
                      <TrendingUp className="h-4 w-4 sm:h-5 sm:w-5 text-green-600" />
                      AI Risk Indicators
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="p-3 sm:p-4">
                    {aiAnalytics && aiAnalytics.riskIndicators.length > 0 ? (
                      <div className="divide-y space-y-0">
                        {aiAnalytics.riskIndicators.map((indicator, idx) => (
                          <div key={idx} className="py-3 first:pt-0 last:pb-0">
                            <div className="flex items-center justify-between mb-1">
                              <span className="text-xs sm:text-sm font-medium">{indicator.indicator}</span>
                              <Badge
                                variant={indicator.level === 'high' ? 'destructive' : indicator.level === 'medium' ? 'secondary' : 'default'}
                                className="text-[10px] sm:text-xs"
                              >
                                {indicator.level}
                              </Badge>
                            </div>
                            <div className="w-full bg-gray-200 rounded-full h-1.5 mb-1">
                              <div
                                className={cn(
                                  'h-1.5 rounded-full transition-all',
                                  indicator.level === 'high' ? 'bg-red-500' : indicator.level === 'medium' ? 'bg-amber-500' : 'bg-emerald-500'
                                )}
                                style={{ width: `${Math.round(indicator.score * 100)}%` }}
                              />
                            </div>
                            {indicator.description && (
                              <p className="text-[10px] sm:text-xs text-gray-600 mt-1">{indicator.description}</p>
                            )}
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="text-xs text-gray-500 py-2">
                        {selectedCustomerId != null || selectedSiteId != null
                          ? 'No AI risk indicators available for your assigned stores.'
                          : 'Select a store to view AI risk indicators.'}
                      </p>
                    )}
                  </CardContent>
                </Card>
              </section>
            </div>
          </div>
        </div>
      </div>
    </main>
  )
} 