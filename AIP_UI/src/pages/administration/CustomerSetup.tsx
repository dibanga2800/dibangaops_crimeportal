import { useState, useCallback } from "react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { CustomersTable } from "@/components/customer-setup/CustomersTable"
import { RegionsTable } from "@/components/customer-setup/RegionsTable"
import { SitesTable } from "@/components/customer-setup/SitesTable"
import { CustomerStats } from "@/components/customer-setup/CustomerStats"
import { RegionDialog } from "@/components/customer-setup/RegionDialog"
import { SiteDialog } from "@/components/customer-setup/SiteDialog"
import { usePageAccess } from "@/contexts/PageAccessContext"
import type { Region, Site } from "@/types/customer"

export default function CustomerSetup() {
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState("customers")
  const [regionDialogOpen, setRegionDialogOpen] = useState(false)
  const [siteDialogOpen, setSiteDialogOpen] = useState(false)
  const [selectedRegion, setSelectedRegion] = useState<Region | undefined>()
  const [selectedSite, setSelectedSite] = useState<Site | undefined>()
  const [statsUpdateTrigger, setStatsUpdateTrigger] = useState(0)
  const [regionsUpdateTrigger, setRegionsUpdateTrigger] = useState(0)
  const [sitesUpdateTrigger, setSitesUpdateTrigger] = useState(0)
  const { clearCacheAndReload } = usePageAccess()

  // Function to trigger stats update
  const updateStats = useCallback(() => {
    setStatsUpdateTrigger(prev => prev + 1)
  }, [])

  const handleCustomerSelect = useCallback((customerId: string | null) => {
    setSelectedCustomerId(customerId)
    updateStats() // Update stats when customer selection changes
  }, [updateStats])

  const handleRegionSuccess = useCallback(() => {
    // Refresh regions list
    console.log('🔧 [CustomerSetup] Region updated, refreshing list')
    setRegionsUpdateTrigger(prev => prev + 1)
    updateStats()
  }, [updateStats])

  const handleSiteSuccess = useCallback(() => {
    // Refresh sites list
    console.log('🔧 [CustomerSetup] Site updated, refreshing list')
    setSitesUpdateTrigger(prev => prev + 1)
    updateStats()
  }, [updateStats])

  const handleEditRegion = (region: Region) => {
    setSelectedRegion(region)
    setRegionDialogOpen(true)
  }

  const handleEditSite = (site: Site) => {
    setSelectedSite(site)
    setSiteDialogOpen(true)
  }

  return (
    <div className="min-h-screen w-full max-w-[100vw] overflow-x-hidden bg-gradient-to-br from-background via-muted/30 to-background">
      <div className="container mx-auto px-3 sm:px-4 md:px-6 lg:px-8 xl:px-10 2xl:px-12 py-3 sm:py-4 md:py-6 lg:py-8 xl:py-10 2xl:py-12 space-y-3 sm:space-y-4 md:space-y-6 xl:space-y-8 max-w-screen-2xl">
        <div className="flex items-center justify-between">
          <h1 className="text-lg sm:text-xl md:text-2xl lg:text-3xl xl:text-4xl 2xl:text-5xl font-bold text-foreground">Company Setup</h1>
        </div>

        <CustomerStats 
        selectedCustomerId={selectedCustomerId} 
        updateTrigger={statsUpdateTrigger}
      />

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="w-full grid grid-cols-3 h-auto">
          <TabsTrigger value="customers" className="text-xs sm:text-sm py-2">Companies</TabsTrigger>
          <TabsTrigger value="regions" disabled={!selectedCustomerId} className="text-xs sm:text-sm py-2">
            Regions
          </TabsTrigger>
          <TabsTrigger value="sites" disabled={!selectedCustomerId} className="text-xs sm:text-sm py-2">
            Sites
          </TabsTrigger>
        </TabsList>

        <TabsContent value="customers">
          <CustomersTable
            selectedCustomerId={selectedCustomerId}
            onCustomerSelect={handleCustomerSelect}
            onDataChange={updateStats}
          />
        </TabsContent>

        <TabsContent value="regions">
          {activeTab === "regions" && (
            selectedCustomerId ? (
              <div className="space-y-4">
                <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3">
                  <h2 className="text-base sm:text-lg font-semibold text-foreground">Regions</h2>
                  <Button
                    onClick={() => {
                      setSelectedRegion(undefined)
                      setRegionDialogOpen(true)
                    }}
                    className="bg-purple-600 hover:bg-purple-700 w-full sm:w-auto"
                  >
                    Add Region
                  </Button>
                </div>
                <RegionsTable 
                  customerId={parseInt(selectedCustomerId) || 0}
                  onEdit={handleEditRegion}
                  onDataChange={handleRegionSuccess}
                  updateTrigger={regionsUpdateTrigger}
                />
              </div>
            ) : (
              <div className="text-center py-8 text-sm text-muted-foreground">
                Please select a company to view regions
              </div>
            )
          )}
        </TabsContent>

        <TabsContent value="sites">
          {activeTab === "sites" && (
            selectedCustomerId ? (
              <div className="space-y-4">
                <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3">
                  <h2 className="text-base sm:text-lg font-semibold text-foreground">Sites</h2>
                  <Button
                    onClick={() => {
                      setSelectedSite(undefined)
                      setSiteDialogOpen(true)
                    }}
                    className="bg-purple-600 hover:bg-purple-700 w-full sm:w-auto"
                  >
                    Add Site
                  </Button>
                </div>
                <SitesTable 
                  customerId={parseInt(selectedCustomerId) || 0}
                  onEdit={handleEditSite}
                  onDataChange={handleSiteSuccess}
                  updateTrigger={sitesUpdateTrigger}
                />
              </div>
            ) : (
              <div className="text-center py-8 text-sm text-muted-foreground">
                Please select a company to view sites
              </div>
            )
          )}
        </TabsContent>
      </Tabs>

      {/* Region Dialog */}
      {selectedCustomerId && (
        <RegionDialog
          open={regionDialogOpen}
          onOpenChange={setRegionDialogOpen}
          region={selectedRegion}
          selectedCustomerId={selectedCustomerId}
          onSuccess={handleRegionSuccess}
        />
      )}

      {/* Site Dialog */}
      {selectedCustomerId && (
        <SiteDialog
          open={siteDialogOpen}
          onOpenChange={setSiteDialogOpen}
          site={selectedSite}
          selectedCustomerId={parseInt(selectedCustomerId) || 0}
          onSuccess={handleSiteSuccess}
        />
      )}
      </div>
    </div>
  )
}