export interface Employee {
  // Primary Key
  id: number
  
  // Required Fields
  employeeNumber: string
  title: string
  firstName: string
  surname: string
  startDate: Date
  position: string
  employeeStatus: string
  employmentType: string
  
  // Optional Fields
  aipAccessLevel?: string
  region?: string
  email?: string
  contactNumber?: string
  
  // Address Information
  houseName?: string
  numberAndStreet?: string
  town?: string
  county?: string
  postCode?: string

  // Personal Information
  nationality?: string
  rightToWorkCondition?: string
  
  // Relationships
  userId?: string
  
  // Audit Fields
  createdAt?: Date
  createdBy?: string
  updatedAt?: Date | null
  updatedBy?: string

  // Computed Properties (from backend)
  fullName?: string
}
