import { Card, CardContent } from "@/components/ui/card"
import { Building2, MapPin, Users, Star, TrendingUp, Shield } from "lucide-react"
import { useMemo, useState, useEffect } from "react"
import { StatLedger } from "@/components/common/StatLedger"
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
        const regionsResult = await regionService.getRegions({ page: 1, pageSize: 1000 })
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
      title: selectedCustomerId ? "Selected company" : "Companies",
      value: statsData.customers.length,
      description: selectedCustomerId 
        ? `${statsData.activeCustomers.length} active` 
        : `${statsData.activeCustomers.length} active`,
      icon: Users,
      tone: "neutral" as const
    },
    {
      title: selectedCustomerId ? "Company type" : "Company types",
      value: statsData.customerTypes,
      description: selectedCustomerId 
        ? "Service type" 
        : "Service types in use",
      icon: TrendingUp,
      tone: "info" as const
    },
    {
      title: selectedCustomerId ? "Regions" : "Regions",
      value: statsData.regions.length,
      description: selectedCustomerId 
        ? "Coverage areas" 
        : "Coverage areas",
      icon: MapPin,
      tone: "signal" as const
    },
    {
      title: selectedCustomerId ? "Sites" : "Core sites",
      value: selectedCustomerId ? statsData.sites.length : statsData.coreSites.length,
      description: selectedCustomerId 
        ? `${statsData.coreSites.length} core sites` 
        : `${statsData.sites.length} total sites`,
      icon: Shield,
      tone: "warning" as const
    }
  ]

  return (
    <StatLedger
      loading={isLoading}
      items={stats.map((stat) => ({
        label: stat.title,
        value: stat.value,
        hint: stat.description,
        icon: <stat.icon />,
        tone: stat.tone,
      }))}
    />
  )
}
