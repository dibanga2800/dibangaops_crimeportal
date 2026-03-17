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
  Zap, ChevronLeft, ChevronRightIcon, Plus
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'
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
  const percentage = Math.min((current / target) * 100, 100)
  
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
              <span className="text-gray-600">Current: {current.toLocaleString()} {unit}</span>
              <span className="text-gray-600">Target: {target.toLocaleString()} {unit}</span>
            </div>
            <Progress value={percentage} className="h-1.5 sm:h-2" />
          </div>
          
          <div className="text-[10px] sm:text-xs text-gray-600">
            {target - current > 0 ? (
              <>Need {(target - current).toLocaleString()} more {unit} to reach target</>
            ) : (
              <>🎉 Target achieved! {(current - target).toLocaleString()} {unit} ahead</>
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
                  <TableHead className="text-right w-[80px] sm:w-[100px]">Value</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedIncidents.map((incident) => (
                  <TableRow key={incident.id}>
                    <TableCell className="font-medium text-xs sm:text-sm">{incident.officerName}</TableCell>
                    <TableCell className="text-xs sm:text-sm">{new Date(incident.date).toLocaleDateString()}</TableCell>
                    <TableCell className="max-w-[160px] sm:max-w-[200px] truncate text-xs sm:text-sm">{incident.siteName}</TableCell>
                    <TableCell className="text-xs sm:text-sm">{incident.type}</TableCell>
                    <TableCell className="text-right text-xs sm:text-sm">
                      {incident.value > 0 ? `£${incident.value.toFixed(2)}` : '-'}
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
    queryKey: ['recentIncidents', selectedCustomerId ?? 'all', selectedSiteId ?? 'all'],
    queryFn: () =>
      dashboardService.getRecentIncidents({
        customerId: selectedCustomerId ?? undefined,
        siteId: selectedSiteId ?? undefined
      })
  })

  // Fetch AI risk indicators for assigned stores
  const { data: aiAnalytics } = useQuery({
    queryKey: ['officerAnalytics', selectedCustomerId ?? 'all', selectedSiteId ?? 'all'],
    queryFn: () =>
      classificationApi.getAnalyticsSummary({
        customerId: selectedCustomerId ?? undefined,
        siteId: selectedSiteId ?? undefined
      }),
    enabled: selectedCustomerId != null || selectedSiteId != null
  })

  const isLoading = isDashboardLoading || isIncidentsLoading
  const error = dashboardError || incidentsError

  // Always show Incident Report button on OfficerDashboard – this dashboard is for store/officer users
  // whose primary workflow is reporting incidents. Route protection handles unauthorized access.
  const showNewIncidentButton = true

  const computedStats = React.useMemo(() => {
    if (!incidentsData || incidentsData.length === 0) {
      return {
        incidentsThisMonth: dashboardData?.stats.incidentsThisMonth ?? 0,
        incidentsLastMonth: dashboardData?.stats.incidentsLastMonth ?? 0,
        totalValueThisMonth: dashboardData?.stats.totalValueSaved ?? 0,
        totalValueLastMonth: 0,
        theftThisMonth: 0,
        theftLastMonth: 0,
      }
    }

    const now = new Date()
    const currentMonth = now.getMonth()
    const currentYear = now.getFullYear()

    const prevMonthDate = new Date(currentYear, currentMonth - 1, 1)
    const prevMonth = prevMonthDate.getMonth()
    const prevYear = prevMonthDate.getFullYear()

    const parseDate = (value: string): Date | null => {
      if (!value) return null
      const d = new Date(value)
      return isNaN(d.getTime()) ? null : d
    }

    let incidentsThisMonth = 0
    let incidentsLastMonth = 0
    let totalValueThisMonth = 0
    let totalValueLastMonth = 0
    let theftThisMonth = 0
    let theftLastMonth = 0

    for (const inc of incidentsData) {
      const d = parseDate(inc.date)
      if (!d) continue
      const y = d.getFullYear()
      const m = d.getMonth()

      const value = typeof inc.value === 'number' && !isNaN(inc.value)
        ? inc.value
        : (typeof inc.amount === 'number' && !isNaN(inc.amount) ? inc.amount : 0)

      const incidentType = (inc.type || inc.incidentType || '').toLowerCase()
      const isTheftIncident =
        incidentType.includes('theft') ||
        incidentType.includes('stolen') ||
        incidentType.includes('shoplifting')

      if (y === currentYear && m === currentMonth) {
        incidentsThisMonth += 1
        totalValueThisMonth += value
        if (isTheftIncident) {
          theftThisMonth += 1
        }
      } else if (y === prevYear && m === prevMonth) {
        incidentsLastMonth += 1
        totalValueLastMonth += value
        if (isTheftIncident) {
          theftLastMonth += 1
        }
      }
    }

    // Fallback to backend values if present and non-zero
    incidentsThisMonth = dashboardData?.stats.incidentsThisMonth || incidentsThisMonth
    incidentsLastMonth = dashboardData?.stats.incidentsLastMonth || incidentsLastMonth

    if (dashboardData?.stats.totalValueSaved && dashboardData.stats.totalValueSaved > 0) {
      totalValueThisMonth = dashboardData.stats.totalValueSaved
    }

    return {
      incidentsThisMonth,
      incidentsLastMonth,
      totalValueThisMonth,
      totalValueLastMonth,
      theftThisMonth,
      theftLastMonth,
    }
  }, [incidentsData, dashboardData])

  const incidentsChangeLabel = React.useMemo(() => {
    const { incidentsThisMonth, incidentsLastMonth } = computedStats
    if (incidentsLastMonth <= 0) {
      return incidentsThisMonth > 0 ? 'vs last month' : 'No data for last month'
    }
    const diff = incidentsThisMonth - incidentsLastMonth
    const pct = (diff / Math.max(incidentsLastMonth, 1)) * 100
    const sign = diff >= 0 ? '+' : ''
    return `${sign}${diff} (${pct.toFixed(0)}%) vs last month`
  }, [computedStats])

  const theftChangeLabel = React.useMemo(() => {
    const { theftThisMonth, theftLastMonth } = computedStats
    if (theftLastMonth <= 0) {
      return theftThisMonth > 0 ? 'vs last month' : 'No data for last month'
    }
    const diff = theftThisMonth - theftLastMonth
    const pct = (diff / Math.max(theftLastMonth, 1)) * 100
    const sign = diff >= 0 ? '+' : ''
    return `${sign}${diff} (${pct.toFixed(0)}%) vs last month`
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
                className="flex-shrink-0 h-9 sm:h-10 px-3 sm:px-4 gap-2 bg-red-600 hover:bg-red-700"
              >
                <Link to="/operations/incident-report" className="flex items-center">
                  <Plus className="h-4 w-4" aria-hidden />
                  <span>Incident Report</span>
                </Link>
              </Button>
            )}
          </header>

          {/* Stats Grid */}
          <section aria-label="Dashboard Statistics" className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
            <StatCard
              title="Incidents This Month"
              value={computedStats.incidentsThisMonth}
              change={incidentsChangeLabel}
              trend={computedStats.incidentsThisMonth >= computedStats.incidentsLastMonth ? 'up' : 'down'}
              icon={Shield}
              gradient="bg-gradient-to-br from-blue-500 to-blue-700"
              subtitle={`Target: ${dashboardData.monthlyTarget.incidents}`}
              link="/operations/incident-report"
            />
            <StatCard
              title="No. of Theft Reported"
              value={computedStats.theftThisMonth}
              change={theftChangeLabel}
              trend={computedStats.theftThisMonth >= computedStats.theftLastMonth ? 'up' : 'down'}
              icon={TrendingUp}
              gradient="bg-gradient-to-br from-emerald-500 to-emerald-700"
              subtitle="This month"
              link="/operations/incident-report"
            />
            <StatCard
              title="Sites in Scope"
              value={
                incidentsData && incidentsData.length > 0
                  ? new Set(incidentsData.map((i) => i.siteId || i.siteName)).size
                  : dashboardData.stats.sitesVisited || 0
              }
              icon={MapPin}
              gradient="bg-gradient-to-br from-purple-500 to-purple-700"
              subtitle="Based on your recent incidents"
            />
            <StatCard
              title="Incidents Today"
              value={
                incidentsData
                  ? incidentsData.filter((i) => {
                      const d = new Date(i.date)
                      const now = new Date()
                      return (
                        d.getFullYear() === now.getFullYear() &&
                        d.getMonth() === now.getMonth() &&
                        d.getDate() === now.getDate()
                      )
                    }).length
                  : 0
              }
              icon={Calendar}
              gradient="bg-gradient-to-br from-amber-500 to-orange-600"
              subtitle="For your current store scope"
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
                  Monthly Progress
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <ProgressCard
                    title="Incidents Handled"
                    current={computedStats.incidentsThisMonth}
                    target={dashboardData.monthlyTarget.incidents}
                    unit="incidents"
                    color="blue"
                  />
                  <ProgressCard
                    title="Value Saved"
                    current={Math.round(computedStats.totalValueThisMonth)}
                    target={dashboardData.monthlyTarget.valueSaved}
                    unit="£"
                    color="green"
                  />
                </div>
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