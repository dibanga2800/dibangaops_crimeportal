import { Card, CardContent } from "@/components/ui/card"
import { Building2, MapPin, Users, Star, TrendingUp, Shield } from "lucide-react"
import { useMemo, useState, useEffect } from "react"
import type { Customer, Region, Site } from "@/types/customer"
import { customerService } from "@/services/customerService"
import { regionService } from "@/services/regionService"
import { siteService } from "@/services/siteService"

interface CustomerStatsProps {
  selectedCustomerId: string | null
  updateTrigger?: number // Add this to force re-renders
}

export function CustomerStats({ selectedCustomerId, updateTrigger }: CustomerStatsProps) {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [regions, setRegions] = useState<Region[]>([])
  const [sites, setSites] = useState<Site[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const fetchData = async () => {
      try {
        setIsLoading(true)
        console.log('🔍 [CustomerStats] Fetching data...')
        
        // Fetch customers
        const customersResult = await customerService.getAllCustomers()
        if (customersResult && customersResult.length > 0) {
          setCustomers(customersResult)
          console.log('✅ [CustomerStats] Successfully fetched customers:', customersResult.length)
        } else {
          console.error('❌ [CustomerStats] Failed to fetch customers')
          setCustomers([])
        }

        // Fetch regions
        if (selectedCustomerId) {
          // Fetch regions for specific customer
          const regionsResult = await regionService.getRegionsByCustomer(parseInt(selectedCustomerId))
          if (regionsResult.success) {
            setRegions(regionsResult.data || [])
            console.log('✅ [CustomerStats] Successfully fetched regions for customer:', regionsResult.data?.length || 0)
          } else {
            console.error('❌ [CustomerStats] Failed to fetch regions')
            setRegions([])
          }
        } else {
          // Fetch all regions for totals
          const regionsResult = await regionService.getRegions()
          if (regionsResult.success) {
            setRegions(regionsResult.data || [])
            console.log('✅ [CustomerStats] Successfully fetched all regions:', regionsResult.data?.length || 0)
          } else {
            console.error('❌ [CustomerStats] Failed to fetch all regions')
            setRegions([])
          }
        }

        // Fetch sites
        if (selectedCustomerId) {
          // Fetch sites for specific customer
          const sitesResult = await siteService.getSitesByCustomer(parseInt(selectedCustomerId))
          if (sitesResult.success) {
            setSites(sitesResult.data || [])
            console.log('✅ [CustomerStats] Successfully fetched sites for customer:', sitesResult.data?.length || 0)
          } else {
            console.error('❌ [CustomerStats] Failed to fetch sites')
            setSites([])
          }
        } else {
          // Fetch all sites for totals
          const sitesResult = await siteService.getSites()
          if (sitesResult.success) {
            setSites(sitesResult.data || [])
            console.log('✅ [CustomerStats] Successfully fetched all sites:', sitesResult.data?.length || 0)
          } else {
            console.error('❌ [CustomerStats] Failed to fetch all sites')
            setSites([])
          }
        }
        
      } catch (error) {
        console.error('❌ [CustomerStats] Error fetching data:', error)
        setCustomers([])
        setRegions([])
        setSites([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
  }, [selectedCustomerId, updateTrigger])

  // Get dynamic data from API and recalculate when data changes
  const statsData = useMemo(() => {
    // Get filtered data based on selected customer
    const filteredCustomers = selectedCustomerId 
      ? customers.filter(customer => String(customer.id) === selectedCustomerId)
      : customers

    const activeCustomers = filteredCustomers.filter(customer => customer.status === 'active')
    const coreSites = sites.filter(site => site.coreSiteYN)
    const customerTypes = selectedCustomerId ? 1 : [...new Set(filteredCustomers.map(c => c.customerType))].length

    return {
      customers: filteredCustomers,
      activeCustomers,
      regions,
      sites,
      coreSites,
      customerTypes
    }
  }, [customers, regions, sites, selectedCustomerId])

  const stats = [
    {
      title: selectedCustomerId ? "Selected Customer" : "Total Customers",
      value: statsData.customers.length,
      description: selectedCustomerId 
        ? `${statsData.activeCustomers.length} active` 
        : `${statsData.activeCustomers.length} active customers`,
      icon: Users,
      gradient: "from-blue-600 to-blue-800",
      iconColor: "text-blue-100"
    },
    {
      title: selectedCustomerId ? "Customer Type" : "Customer Types",
      value: statsData.customerTypes,
      description: selectedCustomerId 
        ? "Service type" 
        : "Different service types",
      icon: TrendingUp,
      gradient: "from-purple-600 to-purple-800",
      iconColor: "text-purple-100"
    },
    {
      title: selectedCustomerId ? "Customer Regions" : "Total Regions",
      value: statsData.regions.length,
      description: selectedCustomerId 
        ? "Coverage areas" 
        : "Coverage areas managed",
      icon: MapPin,
      gradient: "from-green-600 to-green-800",
      iconColor: "text-green-100"
    },
    {
      title: selectedCustomerId ? "Customer Sites" : "Core Sites",
      value: selectedCustomerId ? statsData.sites.length : statsData.coreSites.length,
      description: selectedCustomerId 
        ? `${statsData.coreSites.length} core sites` 
        : `${statsData.sites.length} total sites`,
      icon: Shield,
      gradient: "from-orange-600 to-orange-800",
      iconColor: "text-orange-100"
    }
  ]

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4 mb-6">
        {[...Array(4)].map((_, index) => (
          <Card key={index} className="border-0 shadow-lg overflow-hidden">
            <div className="bg-gradient-to-r from-gray-600 to-gray-800 text-white">
              <CardContent className="p-3 sm:p-4 md:p-6">
                <div className="flex items-center justify-between">
                  <div className="space-y-1 flex-1">
                    <div className="h-3 sm:h-4 bg-white/20 rounded animate-pulse"></div>
                    <div className="h-6 sm:h-8 bg-white/20 rounded animate-pulse"></div>
                    <div className="h-2 sm:h-3 bg-white/20 rounded animate-pulse"></div>
                  </div>
                  <div className="flex-shrink-0 ml-2">
                    <div className="h-6 w-6 sm:h-8 sm:w-8 bg-white/20 rounded animate-pulse"></div>
                  </div>
                </div>
              </CardContent>
            </div>
          </Card>
        ))}
      </div>
    )
  }

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4 mb-6">
      {stats.map((stat, index) => {
        const IconComponent = stat.icon
        return (
          <Card key={index} className="border-0 shadow-lg overflow-hidden">
            <div className={`bg-gradient-to-r ${stat.gradient} text-white`}>
              <CardContent className="p-3 sm:p-4 md:p-6">
                <div className="flex items-center justify-between">
                  <div className="space-y-1 flex-1 min-w-0">
                    <p className="text-[10px] sm:text-xs md:text-sm font-medium text-white/80 truncate">
                      {stat.title}
                    </p>
                    <p className="text-lg sm:text-xl md:text-2xl lg:text-3xl font-bold">
                      {stat.value}
                    </p>
                    <p className="text-[9px] sm:text-xs text-white/70 truncate">
                      {stat.description}
                    </p>
                  </div>
                  <div className="flex-shrink-0 ml-2">
                    <IconComponent className={`h-5 w-5 sm:h-6 sm:w-6 md:h-8 md:w-8 ${stat.iconColor}`} />
                  </div>
                </div>
              </CardContent>
            </div>
          </Card>
        )
      })}
    </div>
  )
}
