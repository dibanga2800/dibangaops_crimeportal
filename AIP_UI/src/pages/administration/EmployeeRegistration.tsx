import { useState, useEffect } from "react"
import { PageHeader } from '@/components/layout/PageHeader'
import { Card, CardContent } from "@/components/ui/card"
import { EmployeeForm } from "@/components/employee-registration/EmployeeForm"
import { EmployeesTable } from "@/components/employee-registration/EmployeesTable"
import { EmployeeStats } from "@/components/employee-registration/EmployeeStats"
import { Employee } from "@/types/employee"
import { useToast } from "@/hooks/use-toast"
import { employeeService, EmployeeStatistics } from "@/services/employeeService"
import { handleApiError } from "@/config/api"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

export default function EmployeeRegistration() {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize] = useState(10)
  const [totalEmployees, setTotalEmployees] = useState(0)
  const [employeeStatistics, setEmployeeStatistics] = useState<EmployeeStatistics | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const { toast } = useToast()

  // Fetch employees on component mount
  useEffect(() => {
    fetchEmployees()
  }, [currentPage, pageSize, searchQuery])

  useEffect(() => {
    fetchEmployeeStatistics()
  }, [])

  const fetchEmployees = async (page = currentPage) => {
    try {
      const result = await employeeService.getEmployeesAsFrontendInterface({
        page,
        pageSize,
        search: searchQuery || undefined,
      })
      setEmployees(result.employees)
      setTotalEmployees(result.total)
      setCurrentPage(result.page)
    } catch (error) {
      toast({
        title: "Error",
        description: "Failed to fetch employees",
        variant: "destructive",
      })
    } finally {
      setIsInitialLoading(false)
    }
  }

  const fetchEmployeeStatistics = async () => {
    try {
      const statistics = await employeeService.getEmployeeStatistics()
      setEmployeeStatistics(statistics)
    } catch (error) {
      toast({
        title: "Error",
        description: "Failed to fetch employee statistics",
        variant: "destructive",
      })
    }
  }

  const handleNewEmployee = () => {
    setSelectedEmployee(null)
    setIsDialogOpen(true)
  }

  const handleEditEmployee = (employee: Employee) => {
    setSelectedEmployee(employee)
    setIsDialogOpen(true)
  }

  const handleDeleteEmployee = async (employee: Employee) => {
    try {
      await employeeService.deleteEmployee(Number(employee.id))
      const nextPage = employees.length === 1 && currentPage > 1 ? currentPage - 1 : currentPage
      await fetchEmployees(nextPage)
      await fetchEmployeeStatistics()
      toast({
        title: "Success",
        description: `${employee.firstName} ${employee.surname} has been deleted`,
      })
    } catch (error) {
      toast({
        title: "Error",
        description: "Failed to delete employee",
        variant: "destructive",
      })
    }
  }

  const handleSubmit = async (data: any) => {
    setIsLoading(true)
    try {
      if (selectedEmployee) {
        // Update
        await employeeService.updateEmployee(Number(selectedEmployee.id), data)
        const updatedEmployee = await employeeService.getEmployeeByIdAsFrontendInterface(
          Number(selectedEmployee.id)
        )
        await fetchEmployees(currentPage)
        await fetchEmployeeStatistics()
        toast({
          title: "Success",
          description: `${updatedEmployee.firstName} ${updatedEmployee.surname} has been updated`,
        })
      } else {
        // Create
        const created = await employeeService.registerEmployeeFromFrontend(data)
        const createdEmployee = await employeeService.getEmployeeByIdAsFrontendInterface(created.id)
        await fetchEmployees(1)
        await fetchEmployeeStatistics()
        toast({
          title: "Success",
          description: `${createdEmployee.firstName} ${createdEmployee.surname} has been created as ${createdEmployee.employeeNumber}`,
        })
      }
      // Clear selected employee and close dialog
      setSelectedEmployee(null)
      setIsDialogOpen(false)
    } catch (error) {
      const errorMessage = handleApiError(error)
      toast({
        title: "Error",
        description: errorMessage,
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="w-full">
      <div className="space-y-6">
        <PageHeader
          title="Employees"
          description="Register store detectives, officers and managers, and keep their details current."
          className="pb-0"
        />

        {/* Employee Stats - Responsive Grid */}
        <div className="w-full overflow-hidden">
          <EmployeeStats statistics={employeeStatistics} />
        </div>

        {/* Main Content - Responsive Container */}
        <div className="w-full">
            <Card>
            <CardContent className="p-4 sm:p-5">
                {isInitialLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <div className="text-center">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
                      <p className="mt-2 text-sm text-muted-foreground">Loading employees…</p>
                    </div>
                  </div>
                ) : (
                  <EmployeesTable
                    employees={employees}
                    currentPage={currentPage}
                    totalPages={Math.max(1, Math.ceil(totalEmployees / pageSize))}
                    itemsPerPage={pageSize}
                    totalItems={totalEmployees}
                    searchQuery={searchQuery}
                    onSearchChange={(query) => {
                      setSearchQuery(query)
                      setCurrentPage(1)
                    }}
                    onPageChange={setCurrentPage}
                    onNewEmployee={handleNewEmployee}
                    onEditEmployee={handleEditEmployee}
                    onDeleteEmployee={handleDeleteEmployee}
                  />
                )}
              </CardContent>
            </Card>
        </div>

        {/* Employee Form Dialog */}
        <Dialog open={isDialogOpen} onOpenChange={(open) => {
          setIsDialogOpen(open)
          if (!open) {
            setSelectedEmployee(null)
          }
        }}>
          <DialogContent className="max-w-[95vw] sm:max-w-[500px] xl:max-w-[800px] p-4 sm:p-6 xl:p-8 max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>
                {selectedEmployee ? "Edit employee" : "New employee"}
              </DialogTitle>
              <DialogDescription className="text-xs sm:text-sm">
                {selectedEmployee
                  ? "Edit the details of the selected employee."
                  : "Fill in the details to register a new employee."}
              </DialogDescription>
            </DialogHeader>
            <EmployeeForm
              onSubmit={handleSubmit}
              onCancel={() => {
                setIsDialogOpen(false)
                setSelectedEmployee(null)
              }}
              initialData={selectedEmployee || undefined}
              isLoading={isLoading}
            />
          </DialogContent>
        </Dialog>
      </div>
    </div>
  )
}