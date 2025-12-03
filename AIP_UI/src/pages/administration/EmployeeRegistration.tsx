import { useState, useEffect } from "react"
import { Card, CardContent } from "@/components/ui/card"
import { EmployeeForm } from "@/components/employee-registration/EmployeeForm"
import { EmployeesTable } from "@/components/employee-registration/EmployeesTable"
import { EmployeeStats } from "@/components/employee-registration/EmployeeStats"
import { Employee } from "@/types/employee"
import { useToast } from "@/hooks/use-toast"
import { employeeService } from "@/services/employeeService"
import { mapToBackendRequest } from "@/utils/employeeMapper"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

export default function EmployeeRegistration() {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isInitialLoading, setIsInitialLoading] = useState(true)
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const { toast } = useToast()

  // Fetch employees on component mount
  useEffect(() => {
    fetchEmployees()
  }, [])

  const fetchEmployees = async () => {
    try {
      const result = await employeeService.getEmployeesAsFrontendInterface()
      setEmployees(result.employees)
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
      setEmployees(employees.filter(e => e.id !== employee.id))
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
        const updated = await employeeService.updateEmployee(Number(selectedEmployee.id), data)
        const updatedEmployee = await employeeService.getEmployeeByIdAsFrontendInterface(Number(selectedEmployee.id))
        setEmployees(employees.map(e => e.id === selectedEmployee.id ? updatedEmployee : e))
        toast({
          title: "Success",
          description: `${updatedEmployee.firstName} ${updatedEmployee.surname} has been updated`,
        })
      } else {
        // Create
        const created = await employeeService.registerEmployeeFromFrontend(data)
        // Refresh the list to get the new employee with full details
        await fetchEmployees()
        toast({
          title: "Success",
          description: `${created.firstName} ${created.surname} has been created`,
        })
      }
      // Clear selected employee and close dialog
      setSelectedEmployee(null)
      setIsDialogOpen(false)
    } catch (error) {
      toast({
        title: "Error",
        description: "Failed to save employee",
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen w-full max-w-[100vw] overflow-x-hidden" style={{ backgroundColor: '#F4F8FE' }}>
      <div className="container mx-auto px-3 sm:px-4 md:px-6 lg:px-8 xl:px-10 2xl:px-12 py-3 sm:py-4 md:py-6 lg:py-8 xl:py-10 2xl:py-12 space-y-3 sm:space-y-4 md:space-y-6 xl:space-y-8 max-w-screen-2xl">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-2 md:gap-4">
          <div className="space-y-1">
            <h1 className="text-lg sm:text-xl md:text-2xl lg:text-3xl xl:text-4xl 2xl:text-5xl font-bold text-[#324053]">
              Employee Registration
            </h1>
            <p className="text-xs sm:text-sm md:text-base text-gray-500">Register and manage employee information</p>
          </div>
        </div>

        {/* Employee Stats - Responsive Grid */}
        <div className="w-full overflow-hidden">
          <EmployeeStats employees={employees} />
        </div>

        {/* Main Content - Responsive Container */}
        <div className="w-full">
            <Card className="bg-white/70 backdrop-blur-lg border border-gray-100 shadow-md">
            <CardContent className="p-3 sm:p-4 md:p-6">
                {isInitialLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <div className="text-center">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600 mx-auto"></div>
                      <p className="mt-2 text-sm text-gray-600">Loading employees...</p>
                    </div>
                  </div>
                ) : (
                  <EmployeesTable
                    employees={employees}
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
              <DialogTitle className="text-base sm:text-lg md:text-xl">
                {selectedEmployee ? "Edit Employee" : "New Employee"}
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