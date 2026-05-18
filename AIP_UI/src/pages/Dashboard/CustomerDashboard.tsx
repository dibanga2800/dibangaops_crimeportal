import * as React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import {
  AlertCircle,
  ArrowUpRight,
  ArrowDownRight,
  Building2,
} from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { cn } from '@/lib/utils'
import { IncidentTable } from '@/components/dashboard/IncidentTable'
import { DashboardGreeting } from '@/components/dashboard/DashboardGreeting'
import { useState, useEffect, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { customerDashboardService } from '@/services/dashboardService'
import { CustomerRole, Region, CustomerStoreData, Site } from '@/types/dashboard'
import { LoadingSpinner } from '@/components/ui/loading-spinner'
import { useAuth } from '@/hooks/useAuth'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { extractCustomerId } from '@/utils/customerId'
import { getCustomerNameById } from '@/services/customerMappingService'

interface CustomerDashboardProps {
  userRole: CustomerRole
}

const CustomerDashboard = ({ userRole }: CustomerDashboardProps) => {
  const { user } = useAuth()
  const {
    selectedCustomerId: contextCustomerId,
    setSelectedCustomerId,
    isManager,
    needsCustomerSelection,
    assignedCustomers,
  } = useCustomerSelection()
  
  // State
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [regions, setRegions] = useState<Region[]>([]);
  const [selectedRegion, setSelectedRegion] = useState<string>('');
  const [siteData, setSiteData] = useState<CustomerStoreData | null>(null);
  const [activePeriod, setActivePeriod] = useState<'daily' | 'weekly' | 'monthly' | 'yearly'>('monthly');
  const [showAllMonths, setShowAllMonths] = useState(true);
  const [sites, setSites] = useState<Site[]>([]);
  const [selectedSite, setSelectedSite] = useState<string>('');
  const [customerName, setCustomerName] = useState<string>('Customer')

  // Effective customer for API calls: manager uses context selection (fallback to first assigned so regions load before context updates)
  const effectiveCustomerId = (() => {
    const role = (user?.role ?? (user as any)?.Role ?? '').toLowerCase()
    if (role === 'manager') {
      return contextCustomerId ?? assignedCustomers[0]?.id ?? extractCustomerId(user ?? null)
    }
    return extractCustomerId(user ?? null)
  })()

  // Fetch customer name from effective customer
  useEffect(() => {
    const fetchCustomerName = async () => {
      if (effectiveCustomerId != null) {
        try {
          const name = await getCustomerNameById(effectiveCustomerId)
          setCustomerName(name || 'Customer')
        } catch {
          const fromAssigned = isManager && assignedCustomers.find((c) => c.id === effectiveCustomerId)
          setCustomerName(fromAssigned?.name ?? 'Customer')
        }
      } else {
        setCustomerName('Customer')
      }
    }
    fetchCustomerName()
  }, [effectiveCustomerId, isManager, assignedCustomers])

  // Helper to get the list of site IDs to aggregate
  const getSiteIdsToAggregate = () => {
    if (selectedRegion === 'all' || !selectedRegion) {
      if (selectedSite === 'all' || !selectedSite) {
        return sites.map(site => site.id);
      }
      return [selectedSite];
    } else {
      const regionSites = sites.filter(site => site.regionId === selectedRegion);
      if (selectedSite === 'all' || !selectedSite) {
        return regionSites.map(site => site.id);
      }
      return [selectedSite];
    }
  };

  const filteredSites = useMemo(() => {
    if (!selectedRegion || selectedRegion === 'all') {
      return sites;
    }
    return sites.filter(site => site.regionId === selectedRegion);
  }, [selectedRegion, sites]);

  // Load initial data for the effective customer (manager: selected in context; store: user's customer)
  useEffect(() => {
    const abortController = new AbortController()
    let isActive = true

    const loadInitialData = async () => {
      try {
        if (!isActive) return
        const userRoleRaw = user?.role ?? (user as any)?.Role ?? ''
        const userRole = String(userRoleRaw).toLowerCase()
        const isCustomerRole = userRole === 'store' || userRole === 'manager'

        if (!isCustomerRole) {
          setError('Access denied. This dashboard is only available for company users.')
          setLoading(false)
          return
        }
        if (effectiveCustomerId == null) {
          setError('Company ID not found. Please log out and log in again to refresh your session.')
          setLoading(false)
          return
        }

        setLoading(true)
        setError(null)

        const [storesData, regionsData, sitesData] = await Promise.all([
          customerDashboardService.getStores(abortController.signal, effectiveCustomerId),
          customerDashboardService.getRegions(abortController.signal, effectiveCustomerId),
          customerDashboardService.getSites(abortController.signal, effectiveCustomerId),
        ])

        // API was already called with customerId; use response as-is (coerce filter only if backend returns multiple customers)
        const effectiveNum = Number(effectiveCustomerId)
        const filteredRegions = regionsData.filter((r: Region) => Number(r.customerId) === effectiveNum || r.customerId === effectiveCustomerId)
        const filteredSites = sitesData.filter((s: Site) => Number(s.customerId) === effectiveNum || s.customerId === effectiveCustomerId)
        setRegions(filteredRegions.length > 0 ? filteredRegions : regionsData)
        setSites(filteredSites.length > 0 ? filteredSites : sitesData)
        if (filteredRegions.length > 0) setSelectedRegion('all')
        if (filteredSites.length > 0) setSelectedSite('all')
        setLoading(false)
      } catch (err) {
        if (!(err instanceof Error && err.name === 'AbortError')) {
          console.error('Error loading initial data:', err)
          setError('Failed to load initial data')
        }
        setLoading(false)
      }
    }

    loadInitialData()
    return () => {
      isActive = false
      abortController.abort()
    }
  }, [user, effectiveCustomerId])

  // Reset site selection when region changes
  useEffect(() => {
    // When region changes, reset site selection to "all" by default
    if (selectedRegion === 'all') {
      setSelectedSite('all'); // Show all sites when all regions selected
    } else {
      // When a specific region is selected, show all sites in that region
      setSelectedSite('all');
    }
  }, [selectedRegion]);

  // Load site or aggregate data when selection changes
  useEffect(() => {
    const siteIds = getSiteIdsToAggregate()
    if (!siteIds.length || effectiveCustomerId == null) return

    let isActive = true
    const abortController = new AbortController()

    const loadData = async () => {
      try {
        setLoading(true)
        setError(null)
        setSiteData(null)

        const data = siteIds.length === 1
          ? await customerDashboardService.getSiteData(siteIds[0], abortController.signal, effectiveCustomerId)
          : await customerDashboardService.getAggregatedSitesData(siteIds, abortController.signal, effectiveCustomerId)
        
        if (!isActive) return;
        setSiteData(data);
      } catch (err) {
        if (isActive) {
          console.error('Error loading dashboard data:', err);
          setError('Failed to load dashboard data');
          setSiteData(null);
        }
      } finally {
        if (isActive) setLoading(false);
      }
    };
    
    loadData();
    return () => {
      isActive = false;
      abortController.abort();
    };
  }, [selectedRegion, selectedSite, sites, effectiveCustomerId])

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center" role="status">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center" role="alert">
        <div className="text-center p-4 rounded-lg bg-white shadow-lg">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" aria-hidden="true" />
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Error Loading Dashboard</h2>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button 
            variant="outline" 
            onClick={() => window.location.reload()}
            className="min-w-[120px] h-10"
          >
            Try Again
          </Button>
        </div>
      </div>
    );
  }

  // Wait for store data to be loaded
  if (!siteData) {
    return (
      <div className="min-h-screen flex items-center justify-center" role="status">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  // For HO Managers, we can show the dashboard without store data
  // For Site Managers, we need store data
  const metrics = siteData?.metrics?.[userRole] || [];

  const getMetricLink = (title: string) => {
    const normalizedTitle = title.toLowerCase()

    if (normalizedTitle.includes('total incidents')) {
      return '/operations/incident-report?open=new'
    }

    if (normalizedTitle.includes('incidents today') || normalizedTitle.includes('today incidents')) {
      return '/operations/incident-report?open=new&preset=today'
    }

    if (normalizedTitle.includes('theft')) {
      return '/operations/incident-report?open=new&incidentType=Theft'
    }

    return null
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-[90rem] py-4 sm:py-6 lg:py-8">
        <header className="mb-6 sm:mb-8">
          <DashboardGreeting className="mb-6" />

          {/* Dashboard Header */}
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <h1 className="text-xl sm:text-2xl lg:text-3xl font-semibold">
                {customerName} Overview
              </h1>
              <Building2 className="h-6 w-6 sm:h-7 sm:w-7 text-gray-500" aria-hidden="true" />
            </div>

            <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto flex-wrap">
              {isManager && needsCustomerSelection && assignedCustomers.length > 1 && (
                <Select
                  value={effectiveCustomerId?.toString() ?? ''}
                  onValueChange={(value) => {
                    const id = parseInt(value, 10)
                    if (!isNaN(id)) setSelectedCustomerId(id)
                  }}
                >
                  <SelectTrigger className="w-full sm:w-[220px] h-11" aria-label="Select customer">
                    <SelectValue placeholder="Select customer" />
                  </SelectTrigger>
                  <SelectContent>
                    {assignedCustomers.map((c) => (
                      <SelectItem key={c.id} value={c.id.toString()}>
                        {c.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              <Select value={selectedRegion} onValueChange={setSelectedRegion}>
                <SelectTrigger className="w-full sm:w-[200px] h-11" aria-label="Select region">
                  <SelectValue placeholder="Select Region" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Regions</SelectItem>
                  {regions.map((region) => (
                    <SelectItem key={region.id} value={region.id}>
                      {region.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select value={selectedSite} onValueChange={setSelectedSite}>
                <SelectTrigger className="w-full sm:w-[250px] h-11" aria-label="Select site">
                  <SelectValue placeholder="Select Site" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Sites</SelectItem>
                  {filteredSites.map((site) => (
                    <SelectItem key={site.id} value={site.id}>
                      {site.locationName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </header>

        <div className="space-y-6 sm:space-y-8">
          {/* Metrics Grid */}
          <section aria-label="Key Metrics" className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {metrics.map((metric, index) => {
              const metricLink = getMetricLink(metric.title)
              const cardContent = (
                <Card
                  className={cn(
                    "relative overflow-hidden border-none shadow-lg transition-transform hover:scale-[1.02]",
                    metricLink ? "cursor-pointer hover:shadow-xl" : "",
                    metrics.length % 2 !== 0 && index === metrics.length - 1 ? "col-span-2 sm:col-span-1" : "",
                    metric.color === 'green' ? 'bg-[#198754]' :
                    metric.color === 'amber' ? 'bg-[#FFC107]' :
                    metric.color === 'blue' ? 'bg-[#0D6EFD]' :
                    'bg-[#DC3545]'
                  )}
                >
                  <CardHeader className="pb-2 sm:pb-3">
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-base sm:text-lg text-white/90 font-medium">
                        {metric.title}
                      </CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent className="p-4 pt-2">
                    <div className="text-2xl sm:text-3xl lg:text-4xl font-bold text-white">
                      {metric.value}
                    </div>
                    <div className="flex items-center mt-2 sm:mt-3">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-white/20 text-white text-sm">
                        {metric.trend === 'up' ? 
                          <ArrowUpRight className="h-4 w-4 sm:h-5 sm:w-5 mr-1.5" aria-hidden="true" /> : 
                          <ArrowDownRight className="h-4 w-4 sm:h-5 sm:w-5 mr-1.5" aria-hidden="true" />
                        }
                        {metric.change}
                      </span>
                      <span className="ml-2 text-sm text-white/70">
                        {metric.trend === 'up' ? 'increase' : 'decrease'}
                      </span>
                    </div>
                  </CardContent>
                </Card>
              )

              if (metricLink) {
                return (
                  <Link key={index} to={metricLink} className="block" aria-label={`View ${metric.title.toLowerCase()}`}>
                    {cardContent}
                  </Link>
                )
              }

              return (
                <div key={index}>
                  {cardContent}
                </div>
              )
            })}
          </section>

          {/* Main Content */}
          <div className="space-y-6">
            <section className="space-y-6" aria-label="Charts and Reports">
              {/* Incident Graph */}
              <Card>
                <CardHeader className="p-4 sm:p-5 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                  <div className="space-y-2">
                    <CardTitle className="text-lg sm:text-xl font-semibold">
                      Incident Reports
                    </CardTitle>
                    <div className="flex flex-wrap gap-4 text-sm">
                      <div className="flex items-center gap-2">
                        <span className="w-3 h-3 rounded-full bg-[#10B981]" aria-hidden="true" />
                        <span>Uniform Officers</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="w-3 h-3 rounded-full bg-[#F59E0B]" aria-hidden="true" />
                        <span>Store Detectives</span>
                      </div>
                    </div>
                  </div>

                  <div className="w-full sm:w-auto">
                    <div className="bg-gray-100 rounded-lg p-1 flex text-sm">
                      {(["daily", "weekly", "monthly", "yearly"] as const).map((period) => (
                        <button
                          key={period}
                          onClick={() => setActivePeriod(period)}
                          className={cn(
                            "flex-1 sm:flex-none px-3 py-1.5 rounded-md transition-colors capitalize",
                            period === activePeriod
                              ? "bg-white shadow-sm text-emerald-600 font-medium"
                              : "text-gray-600 hover:text-gray-900"
                          )}
                        >
                          {period}
                        </button>
                      ))}
                    </div>
                  </div>
                </CardHeader>

                <CardContent className="p-4">
                  <div className="h-[300px] sm:h-[350px] lg:h-[400px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart
                        data={siteData.incidentData[activePeriod]}
                        margin={{ top: 10, right: 10, left: 0, bottom: 0 }}
                      >
                        <defs>
                          <linearGradient id="uniformOfficersGradient" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor="#10B981" stopOpacity={0.9} />
                            <stop offset="100%" stopColor="#10B981" stopOpacity={0.2} />
                          </linearGradient>
                          <linearGradient id="storeDetectivesGradient" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor="#F59E0B" stopOpacity={0.9} />
                            <stop offset="100%" stopColor="#F59E0B" stopOpacity={0.2} />
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" className="stroke-gray-200" />
                        <XAxis
                          dataKey={activePeriod === 'daily' ? 'date' : activePeriod === 'weekly' ? 'week' : activePeriod === 'monthly' ? 'month' : 'year'}
                          tick={{ fontSize: 12 }}
                          tickLine={false}
                          axisLine={false}
                          className="text-gray-500"
                        />
                        <YAxis
                          tick={{ fontSize: 12 }}
                          tickLine={false}
                          axisLine={false}
                          className="text-gray-500"
                        />
                        <Tooltip
                          contentStyle={{
                            backgroundColor: 'white',
                            border: 'none',
                            borderRadius: '8px',
                            boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
                          }}
                        />
                        <Area
                          type="monotone"
                          dataKey="uniformOfficers"
                          stackId="1"
                          stroke="#10B981"
                          fill="url(#uniformOfficersGradient)"
                          name="Uniform Officers"
                        />
                        <Area
                          type="monotone"
                          dataKey="storeDetectives"
                          stackId="1"
                          stroke="#F59E0B"
                          fill="url(#storeDetectivesGradient)"
                          name="Store Detectives"
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                </CardContent>
              </Card>

              {/* Recent Incidents Table */}
              <Card>
                <CardHeader className="p-4 sm:p-5">
                  <CardTitle className="text-lg sm:text-xl font-semibold">
                    Recent Incidents
                  </CardTitle>
                </CardHeader>
                <CardContent className="p-0">
                  <div className="overflow-x-auto">
                    <div className="min-w-full p-4">
                      <IncidentTable data={siteData?.recentIncidents || []} />
                    </div>
                  </div>
                </CardContent>
              </Card>
            </section>
          </div>
        </div>
      </div>
    </main>
  );
}

export default CustomerDashboard;

