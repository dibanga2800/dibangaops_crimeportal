import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Pencil, Trash2 } from "lucide-react"
import { useState } from "react"
import { TableActions } from "./TableActions"
import { TablePagination } from "./TablePagination"
import { Employee } from "@/types/employee"
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

interface EmployeesTableProps {
  employees?: Employee[]
  onNewEmployee: () => void
  onEditEmployee: (employee: Employee) => void
  onDeleteEmployee: (employee: Employee) => void
}

export function EmployeesTable({ employees, onNewEmployee, onEditEmployee, onDeleteEmployee }: EmployeesTableProps) {
  const [currentPage, setCurrentPage] = useState(1)
  const [searchQuery, setSearchQuery] = useState("")
  const [employeeToDelete, setEmployeeToDelete] = useState<Employee | null>(null)
  const itemsPerPage = 10

  const employeesArray = employees || []
  const filteredEmployees = employeesArray.filter(employee => {
    const fullName = `${employee.firstName || ''} ${employee.surname || ''}`.toLowerCase()
    const employeeNumber = (employee.employeeNumber || '').toLowerCase()
    const searchLower = searchQuery.toLowerCase()
    
    return fullName.includes(searchLower) || employeeNumber.includes(searchLower)
  })

  const totalPages = Math.ceil(filteredEmployees.length / itemsPerPage)
  const startIndex = (currentPage - 1) * itemsPerPage
  const paginatedEmployees = filteredEmployees.slice(startIndex, startIndex + itemsPerPage)

  // Function to determine which columns to hide on different screen sizes
  const getResponsiveClasses = (column: string) => {
    switch (column) {
      case 'siaLicenceType':
        return 'hidden lg:table-cell'
      case 'startDate':
        return 'hidden md:table-cell'
      case 'position':
        return 'hidden md:table-cell'
      default:
        return ''
    }
  }

  return (
    <div className="space-y-2 md:space-y-4">
      <TableActions 
        searchQuery={searchQuery}
        onSearchChange={(query) => {
          setSearchQuery(query)
          setCurrentPage(1)
        }}
        onNewEmployee={onNewEmployee}
      />

      {/* Mobile Card Layout - visible only on small screens */}
      <div className="block md:hidden space-y-3">
        {paginatedEmployees.length > 0 ? (
          paginatedEmployees.map((employee, index) => (
            <div key={employee.id || `employee-${index}`} className="rounded-lg border bg-white/50 backdrop-blur-sm shadow-sm p-4 space-y-3">
              {/* Header with name and status */}
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <div className="font-semibold text-sm truncate">
                    {`${employee.firstName || ''} ${employee.surname || ''}`}
                  </div>
                  <div className="text-xs text-gray-500 mt-0.5">
                    {employee.employeeNumber}
                  </div>
                </div>
                <span className={`flex-shrink-0 px-2 py-1 rounded-full text-xs font-medium ${
                  employee.employeeStatus === 'Active' 
                    ? 'bg-green-100 text-green-700' 
                    : 'bg-red-100 text-red-700'
                }`}>
                  {employee.employeeStatus}
                </span>
              </div>

              {/* Details Grid */}
              <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t">
                <div>
                  <span className="text-gray-500 block mb-0.5">Position</span>
                  <div className="font-medium truncate">{employee.position}</div>
                </div>
                <div>
                  <span className="text-gray-500 block mb-0.5">SIA Licence</span>
                  <div className="font-medium truncate">{employee.siaLicenceType || '-'}</div>
                </div>
                <div className="col-span-2">
                  <span className="text-gray-500 block mb-0.5">Start Date</span>
                  <div className="font-medium">
                    {employee.startDate ? new Date(employee.startDate).toLocaleDateString() : '-'}
                  </div>
                </div>
              </div>

              {/* Actions */}
              <div className="flex gap-2 pt-2 border-t">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onEditEmployee(employee)}
                  className="flex-1 h-9 text-xs"
                >
                  <Pencil className="h-3.5 w-3.5 mr-1.5" />
                  Edit
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setEmployeeToDelete(employee)}
                  className="h-9 px-3 text-xs text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          ))
        ) : (
          <div className="rounded-lg border bg-white/50 backdrop-blur-sm shadow-sm p-8 text-center">
            <p className="text-sm text-muted-foreground">No employees found.</p>
          </div>
        )}
      </div>

      {/* Desktop Table Layout - visible on medium screens and above */}
      <div className="hidden md:block rounded-lg border bg-white/50 backdrop-blur-sm shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="text-sm font-medium">Employee Name</TableHead>
                <TableHead className="text-sm font-medium">Employee No.</TableHead>
                <TableHead className={`text-sm font-medium ${getResponsiveClasses('position')}`}>Position</TableHead>
                <TableHead className={`text-sm font-medium ${getResponsiveClasses('siaLicenceType')}`}>SIA Licence Type</TableHead>
                <TableHead className={`text-sm font-medium ${getResponsiveClasses('startDate')}`}>Start Date</TableHead>
                <TableHead className="text-sm font-medium">Status</TableHead>
                <TableHead className="text-right text-sm font-medium">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedEmployees.length > 0 ? (
                paginatedEmployees.map((employee, index) => (
                  <TableRow key={employee.id || `employee-${index}`} className="hover:bg-purple-50/50 text-sm">
                    <TableCell className="font-medium py-3">{`${employee.firstName || ''} ${employee.surname || ''}`}</TableCell>
                    <TableCell className="py-3">{employee.employeeNumber}</TableCell>
                    <TableCell className={`py-3 ${getResponsiveClasses('position')}`}>{employee.position}</TableCell>
                    <TableCell className={`py-3 ${getResponsiveClasses('siaLicenceType')}`}>
                      {employee.siaLicenceType || '-'}
                    </TableCell>
                    <TableCell className={`py-3 ${getResponsiveClasses('startDate')}`}>
                      {employee.startDate ? new Date(employee.startDate).toLocaleDateString() : ''}
                    </TableCell>
                    <TableCell className="py-3">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        employee.employeeStatus === 'Active' 
                          ? 'bg-green-100 text-green-700' 
                          : 'bg-red-100 text-red-700'
                      }`}>
                        {employee.employeeStatus}
                      </span>
                    </TableCell>
                    <TableCell className="text-right py-3">
                      <div className="flex justify-end items-center gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => onEditEmployee(employee)}
                          className="h-8 w-8 p-0 hover:bg-purple-100"
                        >
                          <Pencil className="h-4 w-4 text-purple-600" />
                          <span className="sr-only">Edit</span>
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setEmployeeToDelete(employee)}
                          className="h-8 w-8 p-0 hover:bg-red-100"
                        >
                          <Trash2 className="h-4 w-4 text-red-600" />
                          <span className="sr-only">Delete</span>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                    No employees found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </div>

      <TablePagination 
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
        itemsPerPage={itemsPerPage}
        totalItems={filteredEmployees.length}
        startIndex={startIndex}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={!!employeeToDelete} onOpenChange={(open) => !open && setEmployeeToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This action will delete the employee record for <strong>{employeeToDelete?.firstName} {employeeToDelete?.surname}</strong> (Employee No: {employeeToDelete?.employeeNumber}).
              <br /><br />
              This action cannot be undone. The employee record will be permanently removed from the system.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => setEmployeeToDelete(null)}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (employeeToDelete) {
                  onDeleteEmployee(employeeToDelete)
                  setEmployeeToDelete(null)
                }
              }}
              className="bg-red-600 hover:bg-red-700 focus:ring-red-600"
            >
              Delete Employee
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}