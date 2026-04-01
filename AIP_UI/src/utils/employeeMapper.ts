import { Employee } from '@/types/employee'
import { EmployeeRegistrationRequest, EmployeeDetailResponse } from '@/services/employeeService'

/**
 * Maps frontend Employee interface to backend EmployeeRegistrationRequest
 * Converts camelCase to PascalCase and handles data transformations
 */
export const mapToBackendRequest = (employee: Partial<Employee>): EmployeeRegistrationRequest => {
  const request: EmployeeRegistrationRequest = {
    // Required fields
    Title: employee.title || '',
    FirstName: employee.firstName || '',
    Surname: employee.surname || '',
    StartDate: employee.startDate ? new Date(employee.startDate) : new Date(),
    Position: employee.position || '',
    EmployeeStatus: employee.employeeStatus || '',
    EmploymentType: employee.employmentType || '',
    
    // Optional fields
    AipAccessLevel: employee.aipAccessLevel,
    Region: employee.region,
    Email: employee.email,
    ContactNumber: employee.contactNumber,
    
    // Address Information
    HouseName: employee.houseName,
    NumberAndStreet: employee.numberAndStreet,
    Town: employee.town,
    County: employee.county,
    PostCode: employee.postCode,

    // Personal Information
    Nationality: employee.nationality,
    RightToWorkCondition: employee.rightToWorkCondition,
    
  }

  if (employee.employeeNumber?.trim()) {
    request.EmployeeNumber = employee.employeeNumber.trim()
  }

  return request
}

/**
 * Maps backend EmployeeDetailResponse to frontend Employee interface
 * Converts PascalCase to camelCase and handles data transformations
 */
export const mapFromBackendResponse = (response: EmployeeDetailResponse): Employee => {
  return {
    // Primary Key
    id: response.id,
    
    // Required Fields
    employeeNumber: response.employeeNumber,
    title: response.title || '',
    firstName: response.firstName,
    surname: response.surname,
    startDate: response.startDate,
    position: response.position,
    employeeStatus: response.employeeStatus || '',
    employmentType: response.employmentType || '',
    
    // Optional Fields
    aipAccessLevel: response.aipAccessLevel,
    region: response.region,
    email: response.email,
    contactNumber: response.contactNumber,
    
    // Address Information
    houseName: response.houseName,
    numberAndStreet: response.numberAndStreet,
    town: response.town,
    county: response.county,
    postCode: response.postCode,
    
    // Personal Information
    nationality: response.nationality,
    rightToWorkCondition: response.rightToWorkCondition,

    // Relationships
    userId: response.userId,
    
    // Audit Fields
    createdAt: response.createdAt,
    createdBy: response.createdBy || '',
    updatedAt: response.updatedAt,
    updatedBy: response.updatedBy || '',

    // Computed Properties
    fullName: response.fullName,
  }
}

/**
 * Maps an array of backend responses to frontend Employee interfaces
 */
export const mapFromBackendResponseArray = (responses: EmployeeDetailResponse[]): Employee[] => {
  return responses.map(mapFromBackendResponse)
}

/**
 * Maps backend EmployeeListResponseDto to frontend Employee interface
 * This is specifically for list responses which have limited fields
 */
export const mapFromListResponse = (response: any): Employee => {
  return {
    // Primary Key  
    id: response.employeeId || response.id,
    
    // Basic Info from FullName
    employeeNumber: response.employeeNumber,
    title: '',
    firstName: response.fullName ? response.fullName.split(' ')[0] : '',
    surname: response.fullName ? response.fullName.split(' ').slice(1).join(' ') : '',
    startDate: response.startDate ? new Date(response.startDate) : new Date(),
    position: response.position,
    employeeStatus: response.employeeStatus,
    employmentType: response.employmentType,
    email: response.email,
    
    // Default values for fields not in list response
    aipAccessLevel: '',
    region: '',
    contactNumber: '',
    houseName: '',
    numberAndStreet: '',
    town: '',
    county: '',
    postCode: '',
    nationality: '',
    rightToWorkCondition: '',
    userId: response.userId,
    createdAt: response.createdAt ? new Date(response.createdAt) : new Date(),
    createdBy: '',
    updatedAt: null,
    updatedBy: '',
    fullName: response.fullName,
  }
}

/**
 * Maps an array of backend EmployeeListResponseDto to frontend Employee interfaces
 */
export const mapFromListResponseArray = (responses: any[]): Employee[] => {
  return responses.map(mapFromListResponse)
}

/**
 * Maps frontend Employee interface to backend EmployeeUpdateRequestDto
 * Used specifically for PUT operations - all fields are optional
 */
export const mapToBackendUpdateRequest = (employee: Partial<Employee>): any => {
  const updateRequest: any = {}
  
  // Only include defined values for update
  if (employee.employeeNumber !== undefined) updateRequest.EmployeeNumber = employee.employeeNumber
  if (employee.title !== undefined) updateRequest.Title = employee.title
  if (employee.aipAccessLevel !== undefined) updateRequest.AipAccessLevel = employee.aipAccessLevel
  if (employee.firstName !== undefined) updateRequest.FirstName = employee.firstName
  if (employee.surname !== undefined) updateRequest.Surname = employee.surname
  if (employee.startDate !== undefined) updateRequest.StartDate = new Date(employee.startDate)
  if (employee.position !== undefined) updateRequest.Position = employee.position
  if (employee.employeeStatus !== undefined) updateRequest.EmployeeStatus = employee.employeeStatus
  if (employee.employmentType !== undefined) updateRequest.EmploymentType = employee.employmentType
  if (employee.region !== undefined) updateRequest.Region = employee.region
  if (employee.email !== undefined) updateRequest.Email = employee.email
  if (employee.contactNumber !== undefined) updateRequest.ContactNumber = employee.contactNumber
  if (employee.houseName !== undefined) updateRequest.HouseName = employee.houseName
  if (employee.numberAndStreet !== undefined) updateRequest.NumberAndStreet = employee.numberAndStreet
  if (employee.town !== undefined) updateRequest.Town = employee.town
  if (employee.county !== undefined) updateRequest.County = employee.county
  if (employee.postCode !== undefined) updateRequest.PostCode = employee.postCode
  if (employee.nationality !== undefined) updateRequest.Nationality = employee.nationality
  if (employee.rightToWorkCondition !== undefined) updateRequest.RightToWorkCondition = employee.rightToWorkCondition
  return updateRequest
}

/**
 * Validates that all required fields are present for employee registration
 */
export const validateEmployeeRegistration = (employee: Partial<Employee>): string[] => {
  const errors: string[] = []
  
  // Required fields validation
  if (!employee.title) errors.push('Title is required')
  if (!employee.firstName) errors.push('First name is required')
  if (!employee.surname) errors.push('Surname is required')
  if (!employee.startDate) errors.push('Start date is required')
  if (!employee.position) errors.push('Position is required')
  if (!employee.employeeStatus) errors.push('Employee status is required')
  if (!employee.employmentType) errors.push('Employment type is required')
  if (!employee.numberAndStreet) errors.push('Number and street is required')
  if (!employee.town) errors.push('Town is required')
  if (!employee.county) errors.push('County is required')
  if (!employee.postCode) errors.push('Post code is required')
  if (!employee.region) errors.push('Region is required')
  if (!employee.nationality) errors.push('Nationality is required')
  if (!employee.rightToWorkCondition) errors.push('Right to work condition is required')
  
  return errors
}

/**
 * Generates a unique employee number if not provided
 */
export const generateEmployeeNumber = (): string => {
  const timestamp = Date.now().toString(36)
  const random = Math.random().toString(36).substring(2, 8)
  return `EMP${timestamp}${random}`.toUpperCase()
}

/**
 * Formats date for backend API (ISO string)
 */
export const formatDateForBackend = (date: Date | string | null): string | null => {
  if (!date) return null
  if (typeof date === 'string') return date
  return date.toISOString()
}

/**
 * Formats date for frontend display
 */
export const formatDateForFrontend = (dateString: string | null): string => {
  if (!dateString) return ''
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-GB')
  } catch {
    return dateString
  }
}
