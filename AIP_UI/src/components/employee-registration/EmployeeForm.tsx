import { Button } from "@/components/ui/button"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import * as z from "zod"
import { useState, useEffect } from "react"
import { Employee } from "@/types/employee"
import { lookupTableService, LookupTableItem } from "@/services/lookupTableService"
import { employeeService } from "@/services/employeeService"
import { User, Shield, MapPin, Briefcase, Mail } from "lucide-react"

const ROLE_DISPLAY_NAMES: Record<string, string> = {
  'administrator': 'Admin',
  'manager': 'Manager',
  'store': 'Store User',
}

const formatRoleDisplay = (value: string): string =>
  ROLE_DISPLAY_NAMES[value.toLowerCase()] || value

const formatDateInputLocal = (date: Date): string => {
	const year = date.getFullYear()
	const month = String(date.getMonth() + 1).padStart(2, '0')
	const day = String(date.getDate()).padStart(2, '0')
	return `${year}-${month}-${day}`
}

const parseDateInputLocal = (value: string): Date | null => {
	if (!value) return null

	const parts = value.split('-')
	if (parts.length !== 3) return null

	const [year, month, day] = parts.map((p) => Number(p))
	if (!year || !month || !day) return null

	const parsed = new Date(year, month - 1, day)
	// Guard against invalid dates like NaN
	if (Number.isNaN(parsed.getTime())) return null

	return parsed
}

const formSchema = z.object({
  // Basic Information - Backend Required Fields
  employeeNumber: z.string().optional(),
  title: z.string().min(1, "Title is required"),
  firstName: z.string().min(2, "First name must be at least 2 characters"),
  surname: z.string().min(2, "Last name must be at least 2 characters"),
  startDate: z.string().min(1, 'Start date is required').refine((date) => {
    if (!date) return false

    // Parse `YYYY-MM-DD` as a local date to avoid UTC timezone shifts
    const selectedDate = parseDateInputLocal(date)
    if (!selectedDate) return false

    const today = new Date()
    today.setHours(0, 0, 0, 0)

    selectedDate.setHours(0, 0, 0, 0)
    return selectedDate <= today
  }, {
    message: 'Start date cannot be in the future'
  }),
  position: z.string().min(1, "Position is required"),
  employeeStatus: z.string().min(1, "Employee status is required"),
  employmentType: z.string().min(1, "Employment type is required"),
  
  // Optional Basic Information
  aipAccessLevel: z.string().optional(),
  
  // Contact Information
  email: z.string().email("Invalid email format").optional(),
  contactNumber: z.string().optional(),
  
  // Address Information
  houseName: z.string().optional(),
  numberAndStreet: z.string().optional(),
  town: z.string().optional(),
  county: z.string().optional(),
  postCode: z.string().optional(),
  region: z.string().optional(),
  
  // Personal Information
  nationality: z.string().optional(),
  rightToWorkCondition: z.string().optional(),
  
  status: z.enum(["active", "inactive"]).optional(),
})

type FormData = z.infer<typeof formSchema>

interface EmployeeFormProps {
  onSubmit?: (data: FormData) => Promise<void>
  onCancel: () => void
  initialData?: Employee
  isLoading?: boolean
}

// All dropdown data is now loaded dynamically from lookup tables

export function EmployeeForm({ onSubmit, onCancel, initialData, isLoading }: EmployeeFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [counties, setCounties] = useState<LookupTableItem[]>([])
  const [regions, setRegions] = useState<LookupTableItem[]>([])
  const [userRoles, setUserRoles] = useState<LookupTableItem[]>([])
  const [positions, setPositions] = useState<LookupTableItem[]>([])
  const [rightToWorkConditions, setRightToWorkConditions] = useState<LookupTableItem[]>([])
  const [isLoadingLookupData, setIsLoadingLookupData] = useState(false)

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      aipAccessLevel: initialData?.aipAccessLevel || "",
      title: initialData?.title || "",
      firstName: initialData?.firstName || "",
      surname: initialData?.surname || "",
      email: initialData?.email || "",
      contactNumber: initialData?.contactNumber || "",
      startDate: initialData?.startDate ? formatDateInputLocal(new Date(initialData.startDate)) : '',
      houseName: initialData?.houseName || "",
      numberAndStreet: initialData?.numberAndStreet || "",
      town: initialData?.town || "",
      county: initialData?.county || "",
      postCode: initialData?.postCode || "",
      region: initialData?.region || "",
      position: initialData?.position || "",
      employeeNumber: initialData?.employeeNumber || "",
      employeeStatus: initialData?.employeeStatus || "Active",
      employmentType: initialData?.employmentType || "Full-time",
      nationality: initialData?.nationality || "",
      rightToWorkCondition: initialData?.rightToWorkCondition || "",
      status: "active",
    },
  })

  // Load lookup data and employee data on component mount
  useEffect(() => {
    const loadData = async () => {
      // Load all lookup table data first
      const requiredCategories = [
        'UK_Counties', 
        'UK_Regions',
        'User_Roles',
        'Positions',
        'Right_To_Work_Conditions'
      ]

      console.log('Loading lookup table data in parallel...')
      setIsLoadingLookupData(true)
      
      try {
        const lookupData = await lookupTableService.getByCategories(requiredCategories)
        
        // Set all the data at once
        setCounties(lookupData['UK_Counties'] || [])
        setRegions(lookupData['UK_Regions'] || [])
        setUserRoles(lookupData['User_Roles'] || [])
        setPositions(lookupData['Positions'] || [])
        setRightToWorkConditions(lookupData['Right_To_Work_Conditions'] || [])
        
        console.log('All lookup table data loaded successfully')
        
        // Now load employee data if editing (after lookup data is loaded)
        if (initialData?.id) {
          try {
            const employeeData = await employeeService.getEmployeeById(Number(initialData.id))
            
            // Helper function to find matching lookup value or use original value
            const findLookupValue = (originalValue: string, lookupArray: LookupTableItem[]) => {
              if (!originalValue) return ""
              const found = lookupArray.find(item => 
                item.value.toLowerCase() === originalValue.toLowerCase() ||
                item.value === originalValue
              )
              return found ? found.value : originalValue
            }
            
            // Update form with loaded data, ensuring dropdown values match lookup data
            form.reset({
              aipAccessLevel: findLookupValue(employeeData.aipAccessLevel, lookupData['User_Roles'] || []),
              title: employeeData.title || "",
              firstName: employeeData.firstName || "",
              surname: employeeData.surname || "",
              startDate: employeeData.startDate ? formatDateInputLocal(new Date(employeeData.startDate)) : '',
              email: employeeData.email || "",
              contactNumber: employeeData.contactNumber || "",
              houseName: employeeData.houseName || "",
              numberAndStreet: employeeData.numberAndStreet || "",
              town: employeeData.town || "",
              county: findLookupValue(employeeData.county, lookupData['UK_Counties'] || []),
              postCode: employeeData.postCode || "",
              region: findLookupValue(employeeData.region, lookupData['UK_Regions'] || []),
              position: findLookupValue(employeeData.position, lookupData['Positions'] || []),
              employeeNumber: employeeData.employeeNumber || "",
              employeeStatus: employeeData.employeeStatus || "",
              employmentType: employeeData.employmentType || "",
              nationality: employeeData.nationality || "",
              rightToWorkCondition: findLookupValue(employeeData.rightToWorkCondition, lookupData['Right_To_Work_Conditions'] || []),
              status: employeeData.employeeStatus === "Active" ? "active" : "inactive",
            })
            
          } catch (error) {
            console.error('Failed to load employee data:', error)
          }
        }
      } catch (error) {
        console.error('Failed to load lookup table data:', error)
      } finally {
        setIsLoadingLookupData(false)
      }
    }

    loadData()
  }, [initialData?.id])

  // Reset form when initialData changes to null (when switching from edit to create mode)
  useEffect(() => {
    if (!initialData) {
      form.reset({
        aipAccessLevel: "",
        title: "",
        firstName: "",
        surname: "",
        email: "",
        contactNumber: "",
        startDate: "",
        houseName: "",
        numberAndStreet: "",
        town: "",
        county: "",
        postCode: "",
        region: "",
        position: "",
        employeeNumber: "",
        employeeStatus: "Active",
        employmentType: "Full-time",
        nationality: "",
        rightToWorkCondition: "",
        status: "active",
      })
      form.clearErrors()
    }
  }, [initialData, form])

  // Force form reset when employee ID changes (for switching between employees)
  useEffect(() => {
    console.log('🔄 [EmployeeForm] Employee ID changed:', initialData?.id)
    if (initialData?.id) {
      // Reset form immediately when switching to a different employee
      console.log('🔄 [EmployeeForm] Resetting form for employee ID:', initialData.id)
      form.reset()
      form.clearErrors()
      setSubmitError(null)
    }
  }, [initialData?.id, form])

  // Cleanup form state when component unmounts
  useEffect(() => {
    return () => {
      form.reset()
      form.clearErrors()
    }
  }, [form])

  const handleSubmit = async (data: FormData) => {
    console.log('🚀 [EmployeeForm] Starting form submission...')
    console.log('📝 [EmployeeForm] Form data received:', data)
    console.log('🆔 [EmployeeForm] Current employee ID:', initialData?.id)
    
    // Validate required fields before submission
    const requiredFields = ['title', 'firstName', 'surname', 'startDate', 'position', 'employeeStatus', 'employmentType']
    const missingFields = requiredFields.filter(field => !data[field as keyof FormData])
    
    console.log('🔍 [EmployeeForm] Form validation - Required fields:', requiredFields)
    console.log('🔍 [EmployeeForm] Form validation - Missing fields:', missingFields)
    console.log('🔍 [EmployeeForm] Form validation - Employee number:', data.employeeNumber)
    
    if (missingFields.length > 0) {
      console.error('❌ [EmployeeForm] Missing required fields:', missingFields)
      setSubmitError(`Please fill in all required fields: ${missingFields.join(', ')}`)
      return
    }
    
    setIsSubmitting(true)
    setSubmitError(null)
    
    try {
      if (onSubmit) {
        console.log('🔄 [EmployeeForm] Using custom onSubmit')
        // Use custom onSubmit if provided
        await onSubmit(data)
      } else {
        console.log('🔄 [EmployeeForm] Using real API service')
        // Use real API service with frontend data format
        console.log('📤 [EmployeeForm] Using frontend data format for API call')
        console.log('📤 [EmployeeForm] Form data being sent:', JSON.stringify(data, null, 2))

        if (initialData?.id) {
          console.log('🔄 [EmployeeForm] Updating existing employee with ID:', initialData.id)
          // Update existing employee - convert form data to Employee format
          const employeeData: Partial<Employee> = {
            ...data,
              startDate: data.startDate ? (parseDateInputLocal(data.startDate) ?? undefined) : undefined,
          }
          const result = await employeeService.updateEmployee(Number(initialData.id), employeeData)
          console.log('✅ [EmployeeForm] Employee update successful:', result)
        } else {
          console.log('🆕 [EmployeeForm] Creating new employee')
          // Create new employee - convert form data to Employee format
          const employeeData: Partial<Employee> = {
            ...data,
            startDate: data.startDate ? (parseDateInputLocal(data.startDate) ?? new Date()) : new Date(),
          }
          console.log('📤 [EmployeeForm] About to send employee data:', JSON.stringify(employeeData, null, 2))
          const result = await employeeService.registerEmployeeFromFrontend(employeeData)
          console.log('✅ [EmployeeForm] Employee creation successful:', result)
        }
      }
      
      console.log('✅ [EmployeeForm] Form submission completed successfully')
      
      // Reset form state to clear any cached data and validation errors
      form.reset()
      form.clearErrors()
      setSubmitError(null)
      
      // Force a complete form reset by resetting all state
      setTimeout(() => {
        form.reset()
        form.clearErrors()
      }, 0)
      
      // Close form or show success message
      onCancel()
    } catch (error) {
      console.error('❌ [EmployeeForm] Error submitting employee form:', error)
      console.error('❌ [EmployeeForm] Error details:', {
        message: error instanceof Error ? error.message : 'Unknown error',
        stack: error instanceof Error ? error.stack : undefined,
        error: error
      })
      
      // Extract specific error message from backend response
      let errorMessage = 'An error occurred while saving the employee'
      if (error instanceof Error) {
        errorMessage = error.message
      } else if (typeof error === 'object' && error !== null) {
        // Handle Axios error response
        const axiosError = error as any
        if (axiosError.response?.data?.message) {
          errorMessage = axiosError.response.data.message
        } else if (axiosError.message) {
          errorMessage = axiosError.message
        }
      }
      
      setSubmitError(errorMessage)
    } finally {
      console.log('🏁 [EmployeeForm] Form submission process finished')
      setIsSubmitting(false)
    }
  }

  return (
    <Form {...form} key={initialData?.id || 'new-employee'}>
      <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-8">
        
        {/* Basic Information Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <User className="h-5 w-5" />
              Basic Information
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="aipAccessLevel"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>AIP Access Level</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={isLoadingLookupData ? "Loading..." : "Select access level"} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {userRoles.map((role) => (
                        <SelectItem key={role.lookupId} value={role.value}>
                          {formatRoleDisplay(role.value)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Title</FormLabel>
                  <FormControl>
                    <Input placeholder="Mr, Mrs, Ms, Dr, etc." {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="firstName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>First Name</FormLabel>
                  <FormControl>
                    <Input placeholder="John" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="surname"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Last Name</FormLabel>
                  <FormControl>
                    <Input placeholder="Doe" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="startDate"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Start Date</FormLabel>
                  <FormControl>
                    <Input 
                      type="date" 
                      max={formatDateInputLocal(new Date())}
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="employeeNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Employee Number</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="Auto-generated when employee is created"
                      readOnly={!initialData}
                      disabled={!initialData}
                      {...field}
                      value={field.value ?? ''}
                    />
                  </FormControl>
                  {!initialData && (
                    <p className="text-xs text-muted-foreground">
                      Generated automatically after the employee is created.
                    </p>
                  )}
                  <FormMessage />
                </FormItem>
              )}
            />
          </CardContent>
        </Card>

        {/* Contact Information Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Mail className="h-5 w-5" />
              Contact Information
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email Address</FormLabel>
                  <FormControl>
                    <Input type="email" placeholder="john.doe@example.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="contactNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Contact Number</FormLabel>
                  <FormControl>
                    <Input placeholder="+44 123 456 7890" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </CardContent>
        </Card>

        {/* Address Information Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MapPin className="h-5 w-5" />
              Address Information
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="houseName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>House Name</FormLabel>
                  <FormControl>
                    <Input placeholder="The Cottage" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="numberAndStreet"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Number and Street</FormLabel>
                  <FormControl>
                    <Input placeholder="123 Main Street" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="town"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Town</FormLabel>
                  <FormControl>
                    <Input placeholder="Birmingham" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="county"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>County</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={isLoadingLookupData ? "Loading..." : "Select county"} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {counties.map((county) => (
                        <SelectItem key={county.lookupId} value={county.value}>
                          {county.value}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="postCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Post Code</FormLabel>
                  <FormControl>
                    <Input placeholder="B1 1AA" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="region"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Region</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={isLoadingLookupData ? "Loading..." : "Select region"} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {regions.map((region) => (
                        <SelectItem key={region.lookupId} value={region.value}>
                          {region.value}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
          </CardContent>
        </Card>

        {/* Employment Information Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Briefcase className="h-5 w-5" />
              Employment Information
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="position"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Position</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={isLoadingLookupData ? "Loading..." : "Select position"} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {positions.map((position) => (
                        <SelectItem key={position.lookupId} value={position.value}>
                          {position.value}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="employeeStatus"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Employee Status</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select status" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="Active">Active</SelectItem>
                      <SelectItem value="Inactive">Inactive</SelectItem>
                      <SelectItem value="Suspended">Suspended</SelectItem>
                      <SelectItem value="Terminated">Terminated</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="employmentType"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Employment Type</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select type" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="Full-time">Full-time</SelectItem>
                      <SelectItem value="Part-time">Part-time</SelectItem>
                      <SelectItem value="Contract">Contract</SelectItem>
                      <SelectItem value="Temporary">Temporary</SelectItem>
                      <SelectItem value="Casual">Casual</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="nationality"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Nationality</FormLabel>
                  <FormControl>
                    <Input placeholder="British" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="rightToWorkCondition"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Right to Work Condition</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={isLoadingLookupData ? "Loading conditions..." : "Select condition"} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {rightToWorkConditions.map((condition) => (
                        <SelectItem key={condition.lookupId} value={condition.value}>
                          {condition.value}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
          </CardContent>
        </Card>

        {submitError && (
          <div className="bg-red-50 border border-red-200 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-red-800">Error</h3>
                <div className="mt-2 text-sm text-red-700">
                  {submitError}
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="flex justify-end space-x-4">
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" disabled={isLoading || isSubmitting}>
            {isLoading || isSubmitting ? "Saving..." : initialData ? "Update" : "Create"}
          </Button>
        </div>
      </form>
    </Form>
  )
}
