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

export type ActivityCategory = 
  | 'employment'
  | 'training'
  | 'leave'
  | 'incidents'
  | 'documents'
  | 'performance'
  | 'equipment'
  | 'certifications';

export type ActivityStatus = 
  | 'pending'
  | 'in_progress'
  | 'completed'
  | 'cancelled'
  | 'on_hold';

export type ActivitySource = 
  | 'manual'
  | 'hr_system'
  | 'training_system'
  | 'leave_system'
  | 'performance_system'
  | 'document_system'
  | 'equipment_system'
  | 'certification_system';

export interface EmployeeActivity {
  id: string;
  employeeId: string;
  employeeName: string;
  activityDate: Date;
  activityCategory: ActivityCategory;
  activityType: string;
  description: string;
  status: ActivityStatus;
  source: ActivitySource;
  sourceReference?: string;
  attachments: string[];
  notes?: string;
  relatedDocuments: string[];
  nextReviewDate?: Date;
  actionRequired: boolean;
  actionDeadline?: Date;
  recordedBy: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface ActivitySyncStatus {
  source: ActivitySource;
  status: 'active' | 'inactive' | 'error';
  lastSynced: Date | null;
  error?: string;
} 