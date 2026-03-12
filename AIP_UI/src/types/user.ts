// 4-role model: admin, manager, security-officer, store
export type UserRole = 'administrator' | 'manager' | 'security-officer' | 'store';

export interface Customer {
  id: string;
  companyName: string;
  companyNumber: string;
  vatNumber: string;
  status: 'active' | 'inactive';
  customerType: CustomerType[];
  regions: Region[];
  sites: Site[];
  createdAt: string;
  updatedAt: string;
}

export interface Region {
  id: string;
  name: string;
  customerId: string;
  manager: string;
  status: 'active' | 'inactive';
  createdAt: string;
  updatedAt: string;
}

export interface Site {
  id: string;
  name: string;
  regionId: string;
  customerId: string;
  address: {
    buildingName: string;
    street: string;
    town: string;
    county: string;
    postcode: string;
  };
  isCoreSite: boolean;
  sinNumber: string;
  telephone: string;
  status: 'active' | 'inactive';
  createdAt: string;
  updatedAt: string;
}

export type CustomerType = 
  | 'Event'
  | 'Static'
  | 'Gatehouse'
  | 'Retail'
  | 'Mobile Patrol'
  | 'Keyholding & Alarm Response'
  | 'Other';

export interface BaseUser {
  id: string;
  username: string;
  password?: string;  // Optional since we don't want to expose it in responses
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  pageAccessRole: UserRole;
  signature?: string;
  signatureCode?: string;
  jobTitle?: string;
  profilePicture?: string;
  recordIsDeleted?: boolean;
  createdAt: string;
  updatedAt: string;
  employeeId?: number;
  employeeName?: string;
  twoFactorEnabled?: boolean;
  emailNotificationsEnabled?: boolean;
  loginAlertsEnabled?: boolean;
  // Store / site assignments
  primarySiteId?: string;
  assignedSiteIds?: string[];
}

export interface CustomerUser extends BaseUser {
  customerId: number;
  customerName?: string;
}

export interface PlatformUser extends BaseUser {
  assignedCustomerIds?: number[];
  assignedCustomerNames?: string[];
}

export type User = CustomerUser | PlatformUser;

export interface Employee {
  id: string;
  userId: string;
  name: string;
  jobRole: string;
  department: string;
  startDate: string;
  status: 'active' | 'inactive';
  createdAt: string;
  updatedAt: string;
}

export interface AuthResponse {
  success: boolean;
  data?: {
    user: Omit<User, 'password'>;
    accessToken: string; // Changed from 'token' to 'accessToken' to match backend
  };
  message?: string;
}

export interface UserResponse {
  success: boolean;
  data: User;
  message?: string;
}

export interface UsersResponse {
  success: boolean;
  data: User[];
  pagination: {
    currentPage: number;
    totalPages: number;
    pageSize: number;
    totalCount: number;
  };
  message?: string;
}

/**
 * Simple user record shape used by legacy user-setup components.
 * Not used in the main application flow — the active user types are CustomerUser and PlatformUser above.
 */
export interface UserRecord {
  id: string
  username: string
  email: string
  role: 'Admin' | 'Manager' | 'User' | 'Support'
  status: 'active' | 'inactive'
  lastLogin: string
  createdAt: string
  assignedCustomers?: { id: string; name: string }[]
  officerType?: string
}

export interface CreateUserInput extends Omit<User, 'id' | 'createdAt' | 'updatedAt'> {
  confirmPassword?: string;
}
export interface UpdateUserInput extends Partial<Omit<User, 'createdAt' | 'updatedAt'>> {
  id: string;
}
