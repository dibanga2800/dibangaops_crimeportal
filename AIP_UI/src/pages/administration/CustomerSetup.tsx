import { useState, useCallback } from "react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { Plus } from "lucide-react"
import { PageHeader } from "@/components/layout/PageHeader"
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
    <div className="w-full">
      <div className="space-y-6">
        <PageHeader
          title="Companies"
          description="Companies, their regions and the stores incidents are reported against."
          className="pb-0"
        />

        <CustomerStats 
        selectedCustomerId={selectedCustomerId} 
        updateTrigger={statsUpdateTrigger}
      />

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList>
          <TabsTrigger value="customers">Companies</TabsTrigger>
          <TabsTrigger value="regions" disabled={!selectedCustomerId} title={!selectedCustomerId ? "Select a company first" : undefined}>
            Regions
          </TabsTrigger>
          <TabsTrigger value="sites" disabled={!selectedCustomerId} title={!selectedCustomerId ? "Select a company first" : undefined}>
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
                  <h2 className="text-base font-semibold text-foreground">Regions</h2>
                  <Button
                    onClick={() => {
                      setSelectedRegion(undefined)
                      setRegionDialogOpen(true)
                    }}
                    className="w-full sm:w-auto"
                  >
                    <Plus className="h-4 w-4" />
                    Add region
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
                Select a company on the Companies tab to see its regions.
              </div>
            )
          )}
        </TabsContent>

        <TabsContent value="sites">
          {activeTab === "sites" && (
            selectedCustomerId ? (
              <div className="space-y-4">
                <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3">
                  <h2 className="text-base font-semibold text-foreground">Stores</h2>
                  <Button
                    onClick={() => {
                      setSelectedSite(undefined)
                      setSiteDialogOpen(true)
                    }}
                    className="w-full sm:w-auto"
                  >
                    <Plus className="h-4 w-4" />
                    Add site
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
                Select a company on the Companies tab to see its sites.
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