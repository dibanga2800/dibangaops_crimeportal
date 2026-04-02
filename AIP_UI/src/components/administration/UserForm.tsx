import React, { useState, useMemo, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { DualListBox } from './DualListBox';
import {
  CreateUserInput,
  UpdateUserInput,
  User,
  UserRole,
  CustomerUser,
  AdvantageOneUser,
} from '@/types/user';
import { useAvailableCustomers } from '@/hooks/useAvailableCustomers';
import { Users, Eye, EyeOff, Building2, Lock, FileText, Shield, Briefcase, ChevronRight, ChevronLeft, AlertTriangle, Loader2, MapPin } from 'lucide-react';
import { Checkbox } from '@/components/ui/checkbox';
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { employeeService } from '@/services/employeeService';
import { siteService } from '@/services/siteService';
import { userService } from '@/services/userService';
import { Employee } from '@/types/employee';
import { toast } from 'sonner';

interface UserFormProps {
  initialData?: User;
  onSubmit: (data: CreateUserInput | UpdateUserInput) => void;
  onCancel: () => void;
}

const USER_ROLES: UserRole[] = [
  'administrator',
  'manager',
  'security-officer',
  'store',
];

const formatRoleForDisplay = (role: UserRole): string => {
  const roleMap: Record<UserRole, string> = {
    'administrator': 'Admin',
    'manager': 'Manager',
    'security-officer': 'Security Officer',
    'store': 'Store User',
  };
  return roleMap[role] || role;
};

type FormState = {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  password?: string;
  role: UserRole;
  pageAccessRole: UserRole;
  signature?: string;
  signatureCode?: string;
  jobTitle?: string;
  customerId?: number;
  recordIsDeleted?: boolean;
  employeeId?: number;
  // Store / site assignments
  primarySiteId?: string;
  assignedSiteIds?: string[];
} & (
  | { role: UserRole; customerId?: number }
  | { role: UserRole; assignedCustomerIds: number[] }
);

export const UserForm = ({ initialData, onSubmit, onCancel }: UserFormProps) => {
  const { availableCustomers, isLoading } = useAvailableCustomers();
  
  // Debug logging for initialData
  useEffect(() => {
    if (initialData) {
      console.log('🔍 [UserForm] Initial data received:', {
        id: initialData.id,
        username: initialData.username,
        employeeId: (initialData as any).employeeId,
        employeeName: (initialData as any).employeeName,
        role: initialData.role
      });
      console.log('🔍 [UserForm] Full initialData object:', initialData);
      console.log('🔍 [UserForm] EmployeeId from initialData:', (initialData as any).employeeId);
      console.log('🔍 [UserForm] EmployeeName from initialData:', (initialData as any).employeeName);
      
      // Debug customer assignments
      if ('assignedCustomerIds' in initialData) {
        console.log('🔍 [UserForm] AssignedCustomerIds:', initialData.assignedCustomerIds);
        console.log('🔍 [UserForm] AssignedCustomerIds type:', typeof initialData.assignedCustomerIds);
        console.log('🔍 [UserForm] AssignedCustomerIds length:', Array.isArray(initialData.assignedCustomerIds) ? initialData.assignedCustomerIds.length : 'Not an array');
      }
      
      // Debug customerId for customer users
      if ((initialData as CustomerUser)?.customerId) {
        console.log('🔍 [UserForm] CustomerId from initialData:', (initialData as CustomerUser)?.customerId);
      }
    }
  }, [initialData]);
  
  // Helper function to parse assignedCustomerIds
  const parseAssignedCustomerIds = (customerIds: any): number[] => {
    if (!customerIds) return [];
    if (Array.isArray(customerIds)) return customerIds;
    if (typeof customerIds === 'string') {
      try {
        const parsed = JSON.parse(customerIds);
        return Array.isArray(parsed) ? parsed : [];
      } catch (error) {
        console.error('Error parsing assignedCustomerIds:', error);
        return [];
      }
    }
    return [];
  };

  const [formData, setFormData] = useState<FormState>(() => {
    const baseData = {
      firstName: initialData?.firstName || '',
      lastName: initialData?.lastName || '',
      username: initialData?.username || '',
      email: initialData?.email || '',
      password: '',
      role: initialData?.role || USER_ROLES[0],
      pageAccessRole: initialData?.pageAccessRole || USER_ROLES[0],
      signature: initialData?.signature || '',
      signatureCode: initialData?.signatureCode || '',
      jobTitle: initialData?.jobTitle || '',
      customerId: (initialData as CustomerUser)?.customerId ?? undefined,
      recordIsDeleted: initialData?.recordIsDeleted || false,
      employeeId: (initialData as any)?.employeeId ?? undefined,
      primarySiteId: (initialData as any)?.primarySiteId ?? undefined,
      assignedSiteIds: ((initialData as any)?.assignedSiteIds as string[] | undefined) ?? [],
    };

    console.log('🔍 [UserForm] Form data initialization:', {
      initialDataEmployeeId: (initialData as any)?.employeeId,
      baseDataEmployeeId: baseData.employeeId,
      fullBaseData: baseData
    });

    if ((initialData as CustomerUser)?.customerId) {
      return {
        ...baseData,
        role: initialData!.role,
        customerId: (initialData as CustomerUser)?.customerId ?? undefined,
      } as FormState;
    } else {
      const assignedCustomerIds = initialData && 'assignedCustomerIds' in initialData 
        ? parseAssignedCustomerIds(initialData.assignedCustomerIds)
        : [];
      
      console.log('🔍 [UserForm] Parsed assignedCustomerIds:', assignedCustomerIds);
      
      return {
        ...baseData,
        role: (initialData?.role as UserRole) || 'store',
        assignedCustomerIds,
      } as FormState;
    }
  });

  const [showPassword, setShowPassword] = useState(false);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loadingEmployees, setLoadingEmployees] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [sites, setSites] = useState<any[]>([]);
  const [loadingSites, setLoadingSites] = useState(false);

  useEffect(() => {
    const load = async () => {
      setLoadingEmployees(true);
      try {
        // For editing, include the currently linked employee even if inactive/unlinked.
        if (initialData) {
          const [activeEmployees, currentLinkedEmployee] = await Promise.all([
            employeeService.getActiveEmployees(),
            formData.employeeId
              ? employeeService.getEmployeeByIdAsFrontendInterface(Number(formData.employeeId))
              : Promise.resolve(null),
          ]);

          const merged = [...activeEmployees];
          if (
            currentLinkedEmployee &&
            !merged.some((employee) => Number(employee.id) === Number(currentLinkedEmployee.id))
          ) {
            merged.push(currentLinkedEmployee);
          }

          console.log('🔍 [UserForm] Loaded employees for editing:', merged);
          setEmployees(merged);
          return;
        }

        // When creating, get only unlinked employees.
        const unlinked = await userService.getUnlinkedEmployees();
        console.log('🔍 [UserForm] Loaded unlinked employees for creating:', unlinked);
        setEmployees(unlinked);
      } catch (err) {
        console.error('❌ [UserForm] Error loading employees:', err);
        setEmployees([]);
        toast.error('Failed to load employees', {
          description: 'Please ensure the backend server is running and try again.'
        });
      } finally {
        setLoadingEmployees(false);
      }
    };

    load();
  }, [initialData, formData.employeeId]);

  // Load sites for the selected customer (store users) or all assigned customers (officers/managers)
  useEffect(() => {
    const loadSites = async () => {
      setLoadingSites(true);
      try {
        // Store role: use single customerId
        if (formData.role === 'store' && formData.customerId) {
          const result = await siteService.getSitesByCustomer(formData.customerId);
          setSites(result.success ? result.data : []);
        }
        // Any internal role with assigned customers: aggregate sites for all assigned customers
        else if ('assignedCustomerIds' in formData && formData.assignedCustomerIds.length > 0) {
          const allSites: any[] = [];
          for (const cid of formData.assignedCustomerIds) {
            const result = await siteService.getSitesByCustomer(cid);
            if (result.success) {
              allSites.push(...result.data);
            }
          }
          // Deduplicate by siteID
          const seen = new Set<number>();
          const uniqueSites = allSites.filter((s) => {
            if (seen.has(s.siteID)) return false;
            seen.add(s.siteID);
            return true;
          });
          setSites(uniqueSites);
        } else {
          setSites([]);
        }
      } catch (err) {
        console.error('❌ [UserForm] Error loading sites:', err);
        setSites([]);
      } finally {
        setLoadingSites(false);
      }
    };

    loadSites();
  }, [formData.role, (formData as any).assignedCustomerIds, formData.customerId]);

  // Convert availableCustomers to Customer objects for DualListBox
  const availableCustomersForDualList = useMemo(() => {
    return availableCustomers.map(customer => ({
      id: String(customer.id),
      name: customer.name,
      companyName: customer.name,
      companyNumber: '',
      vatNumber: '',
      status: 'active' as const,
      customerType: 'retail' as const,
      address: {
        building: '',
        street: '',
        village: '',
        town: '',
        county: '',
        postcode: ''
      },
      contact: {
        title: '',
        forename: '',
        surname: '',
        position: '',
        email: '',
        phone: ''
      },
      viewConfig: {
        id: '',
        customerId: String(customer.id),
        customerType: 'retail' as const,
        enabledPages: [],
        createdAt: '',
        updatedAt: ''
      },
      createdAt: '',
      updatedAt: ''
    }));
  }, [availableCustomers]);

  // Get selected customers for DualListBox
  const selectedCustomersForDualList = useMemo(() => {
    if (!('assignedCustomerIds' in formData)) return [];
    
    return formData.assignedCustomerIds
      .map(customerId => {
        const customer = availableCustomers.find(c => c.id === customerId);
        if (!customer) return null;
        
        return {
          id: String(customer.id),
          name: customer.name,
          companyName: customer.name,
          companyNumber: '',
          vatNumber: '',
          status: 'active' as const,
          customerType: 'retail' as const,
          address: {
            building: '',
            street: '',
            village: '',
            town: '',
            county: '',
            postcode: ''
          },
          contact: {
            title: '',
            forename: '',
            surname: '',
            position: '',
            email: '',
            phone: ''
          },
          viewConfig: {
            id: '',
            customerId: String(customer.id),
            customerType: 'retail' as const,
            enabledPages: [],
            createdAt: '',
            updatedAt: ''
          },
          createdAt: '',
          updatedAt: ''
        };
      })
      .filter(Boolean) as any[];
  }, [formData, availableCustomers]);

  // Get available customers (not yet selected) for DualListBox
  const availableCustomersNotSelected = useMemo(() => {
    if (!('assignedCustomerIds' in formData)) return availableCustomersForDualList;
    
    return availableCustomersForDualList.filter(
      customer => !formData.assignedCustomerIds.includes(Number(customer.id))
    );
  }, [availableCustomersForDualList, formData]);

  const handleCheckboxChange = (name: string, checked: boolean) => {
    setFormData(prev => ({ ...prev, [name]: checked }));
  };

  const handleAddCustomer = (customer: any) => {
    if ('assignedCustomerIds' in formData && !formData.assignedCustomerIds.includes(customer.id)) {
      setFormData({
        ...formData,
        assignedCustomerIds: [...formData.assignedCustomerIds, Number(customer.id)],
      } as FormState);
    }
  };

  const handleRemoveCustomer = (customer: any) => {
    if ('assignedCustomerIds' in formData) {
      setFormData({
        ...formData,
        assignedCustomerIds: formData.assignedCustomerIds.filter(id => id !== Number(customer.id)),
      } as FormState);
    }
  };

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const { name, value } = e.target;
    
    // Validate username field
    if (name === 'username') {
      // Remove spaces automatically
      const sanitizedValue = value.replace(/\s+/g, '');
      
      // Validate format (alphanumeric, dots, hyphens, underscores only)
      const usernamePattern = /^[a-zA-Z0-9._-]*$/;
      if (!usernamePattern.test(sanitizedValue)) {
        setValidationErrors(prev => ({
          ...prev,
          username: 'Username can only contain letters, numbers, dots (.), hyphens (-), and underscores (_)'
        }));
      } else if (sanitizedValue.length > 0 && sanitizedValue.length < 3) {
        setValidationErrors(prev => ({
          ...prev,
          username: 'Username must be at least 3 characters long'
        }));
      } else {
        // Clear username error if valid
        setValidationErrors(prev => {
          const { username, ...rest } = prev;
          return rest;
        });
      }
      
      setFormData(prev => ({ ...prev, [name]: sanitizedValue }));
    } else if (name === 'email') {
      // Validate email format
      const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (value.length > 0 && !emailPattern.test(value)) {
        setValidationErrors(prev => ({
          ...prev,
          email: 'Please enter a valid email address'
        }));
      } else {
        // Clear email error if valid
        setValidationErrors(prev => {
          const { email, ...rest } = prev;
          return rest;
        });
      }
      setFormData(prev => ({ ...prev, [name]: value }));
    } else {
      setFormData(prev => ({ ...prev, [name]: value }));
    }
  };

  const handleSelectChange = (name: string, value: string) => {
    if (name === 'role') {
      const role = value as UserRole;
      setFormData({
        ...formData,
        role,
        pageAccessRole: role,
        assignedCustomerIds: [],
        employeeId: undefined,
        customerId: undefined,
        primarySiteId: undefined,
        assignedSiteIds: [],
      } as FormState);
    } else if (name === 'customerId') {
      setFormData(prev => ({
        ...prev,
        customerId: value ? parseInt(value) : undefined,
        // When customer changes for a store user, reset the selected primary site
        primarySiteId: prev.role === 'store' ? undefined : prev.primarySiteId,
      }));
    }
  };

  // Treat store users – and only non-security roles with an explicit customerId –
  // as "customer users" so they see the Customer + Store assignment UI.
  // Security officers should always use the multi-customer + multi-store
  // assignment card, even if a customerId happens to be set.
  const isCustomerRole =
    formData.role === 'store' ||
    (formData.role !== 'security-officer' && !!formData.customerId);

  // Debug logging for customerId changes
  useEffect(() => {
    if (isCustomerRole) {
      console.log('🔍 [UserForm] CustomerId in formData:', formData.customerId);
      console.log('🔍 [UserForm] Available customers count:', availableCustomers.length);
      if (formData.customerId) {
        const selectedCustomer = availableCustomers.find(c => c.id === formData.customerId);
        console.log('🔍 [UserForm] Selected customer:', selectedCustomer?.name || 'Not found');
      }
    }
  }, [formData.customerId, isCustomerRole, availableCustomers]);

  // Debug logging for employee dropdown
  useEffect(() => {
    if (!isCustomerRole) {
      console.log('🔍 [UserForm] Employee dropdown debug:', {
        formDataEmployeeId: formData.employeeId,
        dropdownValue: formData.employeeId ? String(formData.employeeId) : '',
        employeesCount: employees.length,
        employees: employees.map(emp => ({ id: emp.id, name: `${emp.firstName} ${emp.surname}` })),
        hasMatchingEmployee: employees.some(emp => emp.id === formData.employeeId)
      });
    }
  }, [formData.employeeId, employees, isCustomerRole]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    // Check for validation errors first
    if (Object.keys(validationErrors).length > 0) {
      const errorFields = Object.keys(validationErrors).join(', ');
      toast.error(`Please fix validation errors in: ${errorFields}`, {
        description: validationErrors[Object.keys(validationErrors)[0]]
      });
      return;
    }
    
    // Validate employee requirement for non-customer users
    if (!isCustomerRole && !formData.employeeId) {
      toast.error('Employee selection is required for non-company users');
      return;
    }
    
    // Validate customerId requirement for customer users
    if (isCustomerRole && !formData.customerId) {
      toast.error('Company selection is required for company users');
      return;
    }

    // For store users, ensure a primary site is selected
    if (formData.role === 'store' && !formData.primarySiteId) {
      toast.error('Store selection is required for store users');
      return;
    }
    
    console.log('🔄 [UserForm] Form submission started', {
      isEdit: !!initialData,
      userId: initialData?.id,
      formData: {
        username: formData.username,
        email: formData.email,
        role: formData.role,
        employeeId: formData.employeeId,
        customerId: formData.customerId,
        assignedCustomerIds: 'assignedCustomerIds' in formData ? formData.assignedCustomerIds : 'N/A'
      }
    });
    
    const { password, ...restData } = formData;
    
    try {
      if (initialData) {
        const updateData = {
          id: initialData.id,
          ...(password ? { password } : {}),
          ...restData,
        } as UpdateUserInput & { employeeId?: number; customerId?: number };
        
        // Explicitly ensure customerId is included for customer users
        // Always send customerId for customer roles, even if it's undefined (to allow clearing)
        if (isCustomerRole) {
          (updateData as any).customerId = formData.customerId ?? null;
        } else {
          // For non-customer roles, explicitly set to null to clear any existing customerId
          (updateData as any).customerId = null;
        }
        
        console.log('🔄 [UserForm] Submitting update data', updateData);
        console.log('🔄 [UserForm] CustomerId in update data:', (updateData as any).customerId);
        console.log('🔄 [UserForm] Is customer role:', isCustomerRole);
        onSubmit(updateData);
      } else {
        const createData = {
          ...formData,
          confirmPassword: formData.password || '',
        } as CreateUserInput & { employeeId?: number; customerId?: number };
        
        console.log('🔄 [UserForm] Submitting create data', createData);
        onSubmit(createData);
      }
      
      console.log('✅ [UserForm] Form submission completed successfully');
    } catch (error) {
      console.error('❌ [UserForm] Form submission failed:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Personal Information Section */}
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg font-semibold flex items-center gap-2">
            <Users className="h-5 w-5 text-primary" />
            Personal Information
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <Label htmlFor="firstName">First Name</Label>
            <Input
              id="firstName"
              name="firstName"
              value={formData.firstName}
              onChange={handleInputChange}
              required
            />
          </div>
          <div>
            <Label htmlFor="lastName">Last Name</Label>
            <Input
              id="lastName"
              name="lastName"
              value={formData.lastName}
              onChange={handleInputChange}
              required
            />
          </div>
          <div>
            <Label htmlFor="username" className={validationErrors.username ? 'text-red-600' : ''}>
              Username * {validationErrors.username && <span className="text-red-600">← Fix this field</span>}
            </Label>
            <Input
              id="username"
              name="username"
              value={formData.username}
              onChange={handleInputChange}
              pattern="[a-zA-Z0-9._-]+"
              title="Username can only contain letters, numbers, dots, hyphens, and underscores (no spaces)"
              placeholder="e.g., james.haigh or jhaigh"
              className={validationErrors.username ? 'border-red-500 focus:border-red-600 focus:ring-red-600' : ''}
              required
            />
            {validationErrors.username ? (
              <p className="text-xs text-red-600 mt-1 font-medium flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                {validationErrors.username}
              </p>
            ) : (
              <p className="text-xs text-gray-500 mt-1">
                Letters, numbers, dots (.), hyphens (-), underscores (_) only. No spaces.
              </p>
            )}
          </div>
          <div>
            <Label htmlFor="email" className={validationErrors.email ? 'text-red-600' : ''}>
              Email * {validationErrors.email && <span className="text-red-600">← Fix this field</span>}
            </Label>
            <Input
              id="email"
              name="email"
              type="email"
              value={formData.email}
              onChange={handleInputChange}
              className={validationErrors.email ? 'border-red-500 focus:border-red-600 focus:ring-red-600' : ''}
              placeholder="user@example.com"
              required
            />
            {validationErrors.email && (
              <p className="text-xs text-red-600 mt-1 font-medium flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                {validationErrors.email}
              </p>
            )}
          </div>
          <div>
            <Label htmlFor="password">Password</Label>
            <div className="relative">
              <Input
                id="password"
                name="password"
                type={showPassword ? 'text' : 'password'}
                value={formData.password || ''}
                onChange={handleInputChange}
                required={!initialData}
              />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                onClick={() => setShowPassword(!showPassword)}
              >
                {showPassword ? (
                  <EyeOff className="h-4 w-4" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
              </Button>
            </div>
          </div>
          <div>
            <Label htmlFor="role">Role</Label>
            <Select
              value={formData.role}
              onValueChange={(value) => handleSelectChange('role', value)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {USER_ROLES.map((role) => (
                  <SelectItem key={role} value={role}>
                    {formatRoleForDisplay(role)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Employee selection – always shown so we can link a user to an employee record */}
          <div className="md:col-span-2">
            <Label htmlFor="employeeId" className={!formData.employeeId ? 'text-red-600' : ''}>
              Select Employee *
            </Label>
            <Select
              value={formData.employeeId ? String(formData.employeeId) : ''}
              onValueChange={(value) =>
                setFormData((prev) => ({
                  ...prev,
                  employeeId: value ? parseInt(value) : undefined,
                }))
              }
              required
            >
              <SelectTrigger className={!formData.employeeId ? 'border-red-500' : ''}>
                <SelectValue placeholder={loadingEmployees ? 'Loading employees...' : 'Select an employee'} />
              </SelectTrigger>
              <SelectContent>
                {loadingEmployees ? (
                  <SelectItem value="loading" disabled>
                    <div className="flex items-center gap-2">
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Loading employees...
                    </div>
                  </SelectItem>
                ) : employees.length === 0 ? (
                  <SelectItem value="no-employees" disabled>
                    <div className="flex items-center gap-2">
                      <AlertTriangle className="h-4 w-4 text-amber-500" />
                      No employees available - Check backend connection
                    </div>
                  </SelectItem>
                ) : (
                  employees.map((emp) => (
                    <SelectItem key={emp.id} value={String(emp.id)}>
                      {emp.firstName} {emp.surname}
                    </SelectItem>
                  ))
                )}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Customer Assignment Section - Only for internal security roles */}
      {!isCustomerRole && (
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-lg font-semibold flex items-center gap-2">
              <Building2 className="h-5 w-5 text-primary" />
              Customer Assignments
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="text-center py-4">
                <div className="text-sm text-gray-500">Loading companies...</div>
              </div>
            ) : (
              <div className="space-y-4">
                <Label className="text-sm font-medium">
                  {formData.role === 'manager'
                    ? 'Assign customers to this manager'
                    : 'Assign customers to this security officer'}
                </Label>
                <DualListBox
                  available={availableCustomersNotSelected}
                  selected={selectedCustomersForDualList}
                  onAdd={handleAddCustomer}
                  onRemove={handleRemoveCustomer}
                />
                <div className="text-sm text-gray-500">
                  {selectedCustomersForDualList.length} customer(s) assigned
                </div>

                {/* Store / site assignments for internal roles with assigned customers */}
                {'assignedCustomerIds' in formData && formData.assignedCustomerIds.length > 0 && (
                  <div className="mt-4 space-y-2">
                    <Label className="text-sm font-medium flex items-center gap-2">
                      <MapPin className="h-4 w-4 text-primary" />
                      Assign stores (sites){' '}
                      {formData.role === 'security-officer'
                        ? 'to this security officer'
                        : formData.role === 'manager'
                        ? 'to this manager'
                        : 'to this user'}
                    </Label>
                    {loadingSites ? (
                      <div className="flex items-center gap-2 text-sm text-gray-500">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Loading sites for assigned customers...
                      </div>
                    ) : sites.length === 0 ? (
                      <div className="text-sm text-gray-500">
                        No sites found for the assigned customers yet.
                      </div>
                    ) : (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-2 max-h-64 overflow-y-auto border rounded-md p-2">
                        {sites.map((site) => {
                          const id = String(site.siteID ?? site.siteId ?? site.id);
                          const checked = formData.assignedSiteIds?.includes(id) ?? false;
                          return (
                            <label key={id} className="flex items-center gap-2 text-sm cursor-pointer">
                              <Checkbox
                                checked={checked}
                                onCheckedChange={(isChecked) => {
                                  setFormData((prev) => {
                                    const current = prev.assignedSiteIds ?? [];
                                    const next = isChecked
                                      ? Array.from(new Set([...current, id]))
                                      : current.filter((s) => s !== id);
                                    return { ...prev, assignedSiteIds: next } as FormState;
                                  });
                                }}
                              />
                              <span>
                                {site.locationName || site.LocationName || `Site ${id}`}
                                {site.sinNumber && (
                                  <span className="ml-1 text-xs text-gray-500">
                                    ({site.sinNumber})
                                  </span>
                                )}
                              </span>
                            </label>
                          );
                        })}
                      </div>
                    )}
                    {formData.assignedSiteIds && formData.assignedSiteIds.length > 0 && (
                      <div className="text-xs text-gray-500">
                        {formData.assignedSiteIds.length} site(s) assigned
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Additional Information Section */}
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-lg font-semibold flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-primary" />
            Additional Information
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <Label htmlFor="jobTitle">Job Title</Label>
            <Input
              id="jobTitle"
              name="jobTitle"
              value={formData.jobTitle || ''}
              onChange={handleInputChange}
            />
          </div>
          {isCustomerRole && (
            <div>
              <Label htmlFor="customerId" className={!formData.customerId ? 'text-red-600' : ''}>
                Customer *
              </Label>
              <Select
                value={formData.customerId ? String(formData.customerId) : ''}
                onValueChange={(value) => handleSelectChange('customerId', value)}
                required
              >
                <SelectTrigger className={!formData.customerId ? 'border-red-500' : ''}>
                  <SelectValue placeholder={isLoading ? 'Loading companies...' : 'Select company'} />
                </SelectTrigger>
                <SelectContent>
                  {isLoading ? (
                    <SelectItem value="loading" disabled>
                      <div className="flex items-center gap-2">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Loading companies...
                      </div>
                    </SelectItem>
                  ) : availableCustomers.length === 0 ? (
                    <SelectItem value="no-customers" disabled>
                      <div className="flex items-center gap-2">
                        <AlertTriangle className="h-4 w-4" />
                        No customers available
                      </div>
                    </SelectItem>
                  ) : (
                    availableCustomers.map((customer) => (
                      <SelectItem key={customer.id} value={String(customer.id)}>
                        {customer.name}
                      </SelectItem>
                    ))
                  )}
                </SelectContent>
              </Select>
              {!formData.customerId && (
                <p className="text-xs text-red-600 mt-1">Company selection is required for company users</p>
              )}
            </div>
          )}
          {formData.role === 'store' && isCustomerRole && (
            <div>
              <Label htmlFor="primarySiteId" className={!formData.primarySiteId ? 'text-red-600' : ''}>
                Store (Site) *
              </Label>
              <Select
                value={formData.primarySiteId ?? ''}
                onValueChange={(value) =>
                  setFormData((prev) => ({ ...prev, primarySiteId: value || undefined } as FormState))
                }
                required
              >
                <SelectTrigger className={!formData.primarySiteId ? 'border-red-500' : ''}>
                  <SelectValue placeholder={loadingSites ? 'Loading stores...' : 'Select store'} />
                </SelectTrigger>
                <SelectContent>
                  {loadingSites ? (
                    <SelectItem value="loading" disabled>
                      <div className="flex items-center gap-2">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Loading stores...
                      </div>
                    </SelectItem>
                  ) : sites.length === 0 ? (
                    <SelectItem value="no-sites" disabled>
                      <div className="flex items-center gap-2">
                        <AlertTriangle className="h-4 w-4" />
                        No stores available for this customer
                      </div>
                    </SelectItem>
                  ) : (
                    sites.map((site) => {
                      const id = String(site.siteID ?? site.siteId ?? site.id);
                      return (
                        <SelectItem key={id} value={id}>
                          {site.locationName || site.LocationName || `Site ${id}`}
                        </SelectItem>
                      );
                    })
                  )}
                </SelectContent>
              </Select>
              {!formData.primarySiteId && (
                <p className="text-xs text-red-600 mt-1">Store selection is required for store users</p>
              )}
            </div>
          )}
          <div className={isCustomerRole ? "" : "md:col-start-1"}>
            <Label htmlFor="signature">Signature</Label>
            <Input
              id="signature"
              name="signature"
              value={formData.signature || ''}
              onChange={handleInputChange}
            />
          </div>
          <div>
            <Label htmlFor="signatureCode">Signature Code</Label>
            <Input
              id="signatureCode"
              name="signatureCode"
              value={formData.signatureCode || ''}
              onChange={handleInputChange}
            />
          </div>
          <div className="md:col-span-2">
            <div className="flex items-center space-x-2">
              <Checkbox
                id="recordIsDeleted"
                checked={formData.recordIsDeleted || false}
                onCheckedChange={(checked) => handleCheckboxChange('recordIsDeleted', !!checked)}
              />
              <Label htmlFor="recordIsDeleted">Record is Deleted</Label>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={Object.keys(validationErrors).length > 0}
          title={
            Object.keys(validationErrors).length > 0
              ? 'Please fix validation errors before submitting'
              : ''
          }
        >
          {initialData ? 'Update' : 'Create'} User
        </Button>
      </div>
    </form>
  );
}; 