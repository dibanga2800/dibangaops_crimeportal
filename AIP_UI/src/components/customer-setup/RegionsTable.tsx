import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useState, useEffect, useCallback } from "react"
import { regionService } from "@/services/regionService"
import type { Region } from "@/types/customer"
import { Pencil, Trash2, Search, ChevronLeft, ChevronRight } from "lucide-react"
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
import { useToast } from "@/hooks/use-toast"

interface RegionsTableProps {
  customerId: number | null
  onEdit: (region: Region) => void
  onDataChange: () => void
  updateTrigger?: number
}

export function RegionsTable({ customerId, onEdit, onDataChange, updateTrigger }: RegionsTableProps) {
  const [searchQuery, setSearchQuery] = useState("")
  const [currentPage, setCurrentPage] = useState(1)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [regionToDelete, setRegionToDelete] = useState<Region | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [regions, setRegions] = useState<Region[]>([])
  const { toast } = useToast()
  const itemsPerPage = 10

  // Ensure regions is always an array
  const safeRegions = regions || []

  const handleDeleteClick = (region: Region) => {
    setRegionToDelete(region)
    setDeleteDialogOpen(true)
  }

  const handleDeleteConfirm = async () => {
    if (!regionToDelete) return

    setIsLoading(true)
    try {
      const result = await regionService.deleteRegion(regionToDelete.regionID)
      
      if (result.success) {
        toast({
          title: "Success",
          description: "Region deleted successfully",
        })
        onDataChange() // Refresh the data
      } else {
        toast({
          title: "Error",
          description: result.message || "Failed to delete region",
          variant: "destructive",
        })
      }
    } catch (error) {
      toast({
        title: "Error",
        description: "An unexpected error occurred",
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
      setDeleteDialogOpen(false)
      setRegionToDelete(null)
    }
  }

  // Filter by search query
  const filteredRegions = safeRegions.filter(region => {
    const searchLower = searchQuery.toLowerCase()
    return (
      region.regionName.toLowerCase().includes(searchLower) ||
      (region.regionDescription?.toLowerCase().includes(searchLower) || false)
    )
  })

  // Calculate pagination
  const totalPages = Math.ceil(filteredRegions.length / itemsPerPage)
  const startIndex = (currentPage - 1) * itemsPerPage
  const endIndex = startIndex + itemsPerPage
  const currentRegionsTable = filteredRegions.slice(startIndex, endIndex)

  // Fetch regions when customerId changes or onDataChange is called
  const fetchRegions = useCallback(async () => {
    if (!customerId) {
      setRegions([])
      return
    }

    try {
      console.log('🔄 [RegionsTable] Fetching regions for customer:', customerId)
      const result = await regionService.getRegionsByCustomer(customerId)
      
      if (result.success) {
        console.log('✅ [RegionsTable] Successfully fetched regions:', result.data.length)
        setRegions(result.data)
      } else {
        console.error('❌ [RegionsTable] Failed to fetch regions:', result.data)
        setRegions([])
        toast({
          title: "Error",
          description: "Failed to load regions",
          variant: "destructive",
        })
      }
    } catch (error) {
      console.error('❌ [RegionsTable] Error fetching regions:', error)
      setRegions([])
      toast({
        title: "Error",
        description: "An unexpected error occurred while loading regions",
        variant: "destructive",
      })
    } finally {
      // noop
    }
  }, [customerId, toast])

  // Fetch regions when customerId changes or component mounts
  useEffect(() => {
    fetchRegions()
  }, [fetchRegions, onDataChange, updateTrigger])

  // Reset page when search changes
  useEffect(() => {
    setCurrentPage(1)
  }, [searchQuery])

  if (!customerId) {
    return (
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <h2 className="text-lg font-semibold">Regions</h2>
        </div>
        <div className="text-center py-8 text-gray-500">
          Please select a customer to view regions
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Search */}
      <div className="flex items-center space-x-2">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search regions..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-8"
          />
        </div>
      </div>

      {safeRegions.length === 0 ? (
        <div className="text-center py-8 text-sm text-gray-500">
          No regions found for this customer. Click "Add Region" to create one.
        </div>
      ) : filteredRegions.length === 0 ? (
        <div className="text-center py-8 text-sm text-gray-500">
          No regions match your search criteria.
        </div>
      ) : (
        <>
          {/* Mobile Card Layout */}
          <div className="block md:hidden space-y-3">
            {currentRegionsTable.map((region) => (
              <div key={region.regionID} className="rounded-lg border bg-white shadow-sm p-4 space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="font-semibold text-sm truncate">{region.regionName}</div>
                    <div className="text-xs text-gray-500 mt-1 line-clamp-2">
                      {region.regionDescription || "No description"}
                    </div>
                  </div>
                  <span className={`flex-shrink-0 px-2 py-1 rounded-full text-xs ${
                    region.recordIsDeletedYN 
                      ? 'bg-red-100 text-red-800' 
                      : 'bg-green-100 text-green-800'
                  }`}>
                    {region.recordIsDeletedYN ? 'Deleted' : 'Active'}
                  </span>
                </div>

                <div className="text-xs text-gray-500 pt-2 border-t">
                  <span className="block">Created: {new Date(region.dateCreated).toLocaleDateString()}</span>
                </div>

                <div className="flex gap-2 pt-2 border-t">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onEdit(region)}
                    disabled={region.recordIsDeletedYN}
                    className="flex-1 h-9 text-xs"
                  >
                    <Pencil className="h-3.5 w-3.5 mr-1.5" />
                    Edit
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleDeleteClick(region)}
                    className="h-9 px-3 text-xs text-red-600 hover:text-red-700 hover:bg-red-50"
                    disabled={isLoading}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop Table Layout */}
          <div className="hidden md:block border rounded-lg overflow-hidden">
            <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                    <TableHead className="text-sm">Region Name</TableHead>
                    <TableHead className="text-sm">Description</TableHead>
                    <TableHead className="text-sm">Status</TableHead>
                    <TableHead className="text-sm">Created</TableHead>
                    <TableHead className="text-right text-sm">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {currentRegionsTable.map((region) => (
                  <TableRow key={region.regionID}>
                    <TableCell className="font-medium">{region.regionName}</TableCell>
                    <TableCell className="max-w-xs truncate">
                      {region.regionDescription || "No description"}
                    </TableCell>
                    <TableCell>
                      <span className={`px-2 py-1 rounded-full text-xs ${
                        region.recordIsDeletedYN 
                          ? 'bg-red-100 text-red-800' 
                          : 'bg-green-100 text-green-800'
                      }`}>
                        {region.recordIsDeletedYN ? 'Deleted' : 'Active'}
                      </span>
                    </TableCell>
                    <TableCell>
                      {new Date(region.dateCreated).toLocaleDateString()}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => onEdit(region)}
                          disabled={region.recordIsDeletedYN}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDeleteClick(region)}
                          className="text-red-600 hover:text-red-700"
                          disabled={isLoading}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            </div>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex flex-col sm:flex-row items-center justify-between gap-3 sm:gap-4 mt-6">
              <div className="text-xs sm:text-sm text-muted-foreground text-center sm:text-left">
                Showing {startIndex + 1} to {Math.min(endIndex, filteredRegions.length)} of {filteredRegions.length} regions
              </div>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                  disabled={currentPage === 1}
                  className="h-9"
                >
                  <ChevronLeft className="h-4 w-4" />
                  <span className="hidden xs:inline ml-1">Previous</span>
                </Button>
                <span className="text-xs sm:text-sm whitespace-nowrap">
                  Page {currentPage} of {totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                  disabled={currentPage === totalPages}
                  className="h-9"
                >
                  <span className="hidden xs:inline mr-1">Next</span>
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Region</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{regionToDelete?.regionName}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              className="bg-red-600 hover:bg-red-700"
              disabled={isLoading}
            >
              {isLoading ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}