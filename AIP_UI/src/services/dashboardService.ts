import { StoreData, RegionalData, Period, UserRole, OfficerDashboardData, RecentIncident, CustomerStoreData, Region, Site, IncidentDataPoint } from '@/types/dashboard';
import axios from 'axios';
import { BASE_API_URL, api, type ApiResponse } from '@/config/api';
import { extractApiResponseData } from '@/utils/apiResponseHelper';
import { extractCustomerId } from '@/utils/customerId';
import { sessionStore } from '@/state/sessionStore';
import { applyCsrfHeader } from '@/utils/csrf';

const API_BASE_URL = BASE_API_URL;

const getActiveUser = () => {
  const activeUser = sessionStore.getUser()
  if (!activeUser) {
    throw new Error('User session is not available')
  }
  return activeUser
}

const getIncidentFinancials = (incident: any) => {
  const stolenItems = Array.isArray(incident?.StolenItems)
    ? incident.StolenItems
    : Array.isArray(incident?.stolenItems)
      ? incident.stolenItems
      : []

  const totalStolenValue =
    incident?.TotalStolenValue ??
    incident?.totalStolenValue ??
    stolenItems.reduce((sum: number, item: any) => {
      const explicitAmount = item?.TotalAmount ?? item?.totalAmount
      if (explicitAmount !== undefined && explicitAmount !== null) {
        const parsed = Number(explicitAmount)
        return sum + (Number.isFinite(parsed) ? parsed : 0)
      }

      const computed = Number(item?.Cost ?? item?.cost ?? 0) * Number(item?.Quantity ?? item?.quantity ?? 0)
      return sum + (Number.isFinite(computed) ? computed : 0)
    }, 0)

  const totalRecoveredValue =
    incident?.TotalRecoveredValue ??
    incident?.totalRecoveredValue ??
    incident?.TotalValueRecovered ??
    incident?.totalValueRecovered ??
    incident?.ValueRecovered ??
    incident?.valueRecovered ??
    incident?.Value ??
    incident?.value ??
    stolenItems.reduce((sum: number, item: any) => {
      const amount = item?.RecoveredAmount ?? item?.recoveredAmount ?? 0
      const parsed = Number(amount)
      return sum + (Number.isFinite(parsed) ? parsed : 0)
    }, 0)

  const explicitLostValue =
    incident?.TotalLostValue ??
    incident?.totalLostValue ??
    incident?.ValueLost ??
    incident?.valueLost ??
    incident?.LostValue ??
    incident?.lostValue

  const totalLostValue =
    explicitLostValue !== undefined && explicitLostValue !== null
      ? Number(explicitLostValue) || 0
      : Math.max((Number(totalStolenValue) || 0) - (Number(totalRecoveredValue) || 0), 0)

  return {
    totalStolenValue: Number(totalStolenValue) || 0,
    totalRecoveredValue: Number(totalRecoveredValue) || 0,
    totalLostValue: Number(totalLostValue) || 0,
  }
}

class APIError extends Error {
  constructor(
    message: string,
    public status?: number,
    public code?: string
  ) {
    super(message);
    this.name = 'APIError';
  }
}

class DashboardService {
  async getOfficerDashboard(): Promise<OfficerDashboardData | null> {
    // Build a minimal officer dashboard without a dedicated /dashboard/officer endpoint.
    const user = getActiveUser()
    const now = new Date()

    try {
      const base: OfficerDashboardData = {
        name: user.username || (user as any).fullName || user.email || 'Officer',
        badgeNumber: (user as any).id ?? '',
        role: (user.role || (user as any).pageAccessRole || 'security-officer').toString(),
        avatar: (user as any).profilePicture || '',
        shiftStatus: 'On Duty',
        shiftStart: now.toISOString(),
        shiftEnd: new Date(now.getTime() + 8 * 60 * 60 * 1000).toISOString(),
        location: 'Current assigned store',
        // Stats are mostly computed on the frontend from incidents; keep these minimal
        stats: {
          incidentsThisMonth: 0,
          incidentsLastMonth: 0,
          totalValueSaved: 0,
          expensesYTD: 0,
          completionRate: 0,
          hoursWorked: 0,
          sitesVisited: 0
        },
        monthlyTarget: {
          incidents: 10,
          valueSaved: 5000,
          current: {
            incidents: 0,
            valueSaved: 0
          }
        },
        recentActivities: [],
        upcomingTasks: []
      }

      return base
    } catch {
      // Fall back to a minimal, but valid, dashboard shape
      return {
        name: user.username || (user as any).fullName || user.email || 'Officer',
        badgeNumber: (user as any).id ?? '',
        role: (user.role || (user as any).pageAccessRole || 'security-officer').toString(),
        avatar: (user as any).profilePicture || '',
        shiftStatus: 'On Duty',
        shiftStart: now.toISOString(),
        shiftEnd: new Date(now.getTime() + 8 * 60 * 60 * 1000).toISOString(),
        location: 'Current assigned store',
        stats: {
          incidentsThisMonth: 0,
          incidentsLastMonth: 0,
          totalValueSaved: 0,
          expensesYTD: 0,
          completionRate: 0,
          hoursWorked: 0,
          sitesVisited: 0
        },
        monthlyTarget: {
          incidents: 10,
          valueSaved: 5000,
          current: {
            incidents: 0,
            valueSaved: 0
          }
        },
        recentActivities: [],
        upcomingTasks: []
      }
    }
  }

  async getRecentIncidents(params?: { customerId?: number; siteId?: string; fromDate?: string; toDate?: string }): Promise<RecentIncident[]> {
    try {
      const queryParams: Record<string, string | number> = { page: 1, pageSize: 100 }
      if (params?.customerId != null) queryParams.customerId = params.customerId
      if (params?.siteId) queryParams.siteId = params.siteId
      if (params?.fromDate) queryParams.fromDate = params.fromDate
      if (params?.toDate) queryParams.toDate = params.toDate
      const response = await api.get<ApiResponse<any>>('/incidents', { params: queryParams })
      const incidents = response.data?.data || []
      return incidents.map((inc: any) => {
        const financials = getIncidentFinancials(inc)
        return ({
        id: inc.Id || inc.id?.toString() || '',
        customerId: inc.CustomerId || inc.customerId || 0,
        date: inc.DateOfIncident || inc.Date || inc.date || inc.incidentDate || '',
        timeOfIncident: inc.TimeOfIncident || inc.timeOfIncident || inc.timeOfDay || '',
        regionId: inc.RegionId?.toString() || inc.regionId?.toString() || '',
        regionName: inc.RegionName || inc.regionName || '',
        siteId: inc.SiteId?.toString() || inc.siteId?.toString() || '',
        siteName: inc.SiteName || inc.siteName || '',
        type: inc.IncidentType || inc.incidentType || inc.type || '',
        value: inc.TotalValueRecovered || inc.Value || inc.value || 0,
        assignedTo: inc.AssignedTo || inc.assignedTo || '',
        customerName: inc.CustomerName || inc.customerName || '',
        store: inc.SiteName || inc.siteName || '',
        officerName: inc.OfficerName || inc.officerName || '',
        amount: financials.totalRecoveredValue,
        recoveredValue: financials.totalRecoveredValue,
        lostValue: financials.totalLostValue,
        incidentType: inc.IncidentType || inc.incidentType || inc.type || ''
      })})
    } catch {
      return []
    }
  }
}

export const dashboardService = new DashboardService()

export const dashboardApi = {
  async getStoreData(storeId: string): Promise<StoreData> {
    try {
      const response = await axios.get(`${API_BASE_URL}/stores/${storeId}`);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        throw new APIError(
          error.response?.data?.message || 'Failed to fetch store data',
          error.response?.status,
          error.response?.data?.code
        );
      }
      throw error;
    }
  },

  async getRegionalData(regionId: string): Promise<RegionalData> {
    try {
      const response = await axios.get(`${API_BASE_URL}/regions/${regionId}`);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        throw new APIError(
          error.response?.data?.message || 'Failed to fetch regional data',
          error.response?.status,
          error.response?.data?.code
        );
      }
      throw error;
    }
  },

  async getMetrics(storeId: string, userRole: UserRole) {
    try {
      const response = await axios.get(`${API_BASE_URL}/metrics`, {
        params: { storeId, userRole }
      });
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        throw new APIError(
          error.response?.data?.message || 'Failed to fetch metrics',
          error.response?.status,
          error.response?.data?.code
        );
      }
      throw error;
    }
  },

  async getIncidentData(storeId: string, period: Period) {
    try {
      const response = await axios.get(`${API_BASE_URL}/incidents`, {
        params: { storeId, period }
      });
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        throw new APIError(
          error.response?.data?.message || 'Failed to fetch incident data',
          error.response?.status,
          error.response?.data?.code
        );
      }
      throw error;
    }
  },

  async getRecentIncidents(storeId: string) {
    try {
      const response = await axios.get(`${API_BASE_URL}/incidents/recent`, {
        params: { storeId }
      });
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        throw new APIError(
          error.response?.data?.message || 'Failed to fetch recent incidents',
          error.response?.status,
          error.response?.data?.code
        );
      }
      throw error;
    }
  }
};

// Helper function to get customer ID from auth context (no static fallback – production only)
const getCustomerIdFromAuth = () => {
  const user = getActiveUser();
  return user.customerId ?? (user as any).CustomerId ?? undefined;
};

// Helper function to add customer ID to headers (only when present)
const getHeaders = (): Record<string, string> => {
  const customerId = getCustomerIdFromAuth();
  if (customerId == null) return {};
  return { 'X-Customer-Id': String(customerId) };
};

// Helper function to calculate incident chart data from incidents
const calculateIncidentChartData = (incidents: Array<{ date: string; officerRole?: string; officerType?: string; value?: number; amount?: number }>): {
  daily: IncidentDataPoint[];
  weekly: IncidentDataPoint[];
  monthly: IncidentDataPoint[];
  yearly: IncidentDataPoint[];
} => {
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth();

  // Helper to determine if incident is from Uniform Officer or Store Detective
  const getOfficerCategory = (incident: { officerRole?: string; officerType?: string }): 'uniform' | 'detective' | 'unknown' => {
    const role = (incident.officerRole || incident.officerType || '').toLowerCase();
    if (role.includes('uniform') || role.includes('officer')) {
      return 'uniform';
    }
    if (role.includes('detective') || role.includes('store detective')) {
      return 'detective';
    }
    return 'unknown';
  };

  // Helper to get incident value
  const getIncidentValue = (incident: { value?: number; amount?: number }): number => {
    return incident.value || incident.amount || 0;
  };

  // Helper to parse date string to Date object
  const parseDate = (dateStr: string): Date | null => {
    if (!dateStr) return null;
    try {
      // Try ISO format first (YYYY-MM-DD)
      if (dateStr.includes('T')) {
        return new Date(dateStr);
      }
      // Try YYYY-MM-DD format
      if (/^\d{4}-\d{2}-\d{2}/.test(dateStr)) {
        return new Date(dateStr);
      }
      // Try other common formats
      const parsed = new Date(dateStr);
      if (!isNaN(parsed.getTime())) {
        return parsed;
      }
      return null;
    } catch {
      return null;
    }
  };

  // Helper to format date to YYYY-MM-DD string
  const formatDateStr = (date: Date): string => {
    return date.toISOString().split('T')[0];
  };

  // Generate daily data for last 30 days
  const dailyData: IncidentDataPoint[] = [];
  for (let i = 29; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);
    const dateStr = date.toISOString().split('T')[0];
    
    const dayIncidents = incidents.filter(incident => {
      if (!incident.date) return false;
      const incidentDate = parseDate(incident.date);
      if (!incidentDate) return false;
      return formatDateStr(incidentDate) === dateStr;
    });
    
    let uniformOfficers = 0;
    let storeDetectives = 0;
    
    dayIncidents.forEach(incident => {
      const category = getOfficerCategory(incident);
      const value = getIncidentValue(incident);
      if (category === 'uniform') {
        uniformOfficers += value;
      } else if (category === 'detective') {
        storeDetectives += value;
      } else {
        // If unknown, split evenly or assign based on some logic
        // For now, we'll count them as uniform officers if no role specified
        uniformOfficers += value;
      }
    });

    dailyData.push({
      date: date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
      uniformOfficers,
      storeDetectives
    });
  }

  // Generate weekly data for last 12 weeks
  const weeklyData: IncidentDataPoint[] = [];
  for (let i = 11; i >= 0; i--) {
    const weekStart = new Date(now);
    weekStart.setDate(weekStart.getDate() - (i * 7 + now.getDay()));
    weekStart.setHours(0, 0, 0, 0);
    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);
    weekEnd.setHours(23, 59, 59, 999);
    
    const weekIncidents = incidents.filter(incident => {
      if (!incident.date) return false;
      const incidentDate = parseDate(incident.date);
      if (!incidentDate) return false;
      return incidentDate >= weekStart && incidentDate <= weekEnd;
    });
    
    let uniformOfficers = 0;
    let storeDetectives = 0;
    
    weekIncidents.forEach(incident => {
      const category = getOfficerCategory(incident);
      const value = getIncidentValue(incident);
      if (category === 'uniform') {
        uniformOfficers += value;
      } else if (category === 'detective') {
        storeDetectives += value;
      } else {
        uniformOfficers += value;
      }
    });

    weeklyData.push({
      week: `Week ${weeklyData.length + 1}`,
      uniformOfficers,
      storeDetectives
    });
  }

  // Generate monthly data for last 12 months
  const monthlyData: IncidentDataPoint[] = [];
  for (let i = 11; i >= 0; i--) {
    const monthDate = new Date(currentYear, currentMonth - i, 1);
    const monthStr = monthDate.toISOString().substring(0, 7); // YYYY-MM format
    
    const monthIncidents = incidents.filter(incident => {
      if (!incident.date) return false;
      const incidentDate = parseDate(incident.date);
      if (!incidentDate) return false;
      return formatDateStr(incidentDate).startsWith(monthStr);
    });
    
    let uniformOfficers = 0;
    let storeDetectives = 0;
    
    monthIncidents.forEach(incident => {
      const category = getOfficerCategory(incident);
      const value = getIncidentValue(incident);
      if (category === 'uniform') {
        uniformOfficers += value;
      } else if (category === 'detective') {
        storeDetectives += value;
      } else {
        uniformOfficers += value;
      }
    });

    monthlyData.push({
      month: monthDate.toLocaleDateString('en-US', { month: 'short', year: 'numeric' }),
      uniformOfficers,
      storeDetectives
    });
  }

  // Generate yearly data for last 5 years
  const yearlyData: IncidentDataPoint[] = [];
  for (let i = 4; i >= 0; i--) {
    const year = currentYear - i;
    const yearStr = year.toString();
    
    const yearIncidents = incidents.filter(incident => {
      if (!incident.date) return false;
      const incidentDate = parseDate(incident.date);
      if (!incidentDate) return false;
      return formatDateStr(incidentDate).startsWith(yearStr);
    });
    
    let uniformOfficers = 0;
    let storeDetectives = 0;
    
    yearIncidents.forEach(incident => {
      const category = getOfficerCategory(incident);
      const value = getIncidentValue(incident);
      if (category === 'uniform') {
        uniformOfficers += value;
      } else if (category === 'detective') {
        storeDetectives += value;
      } else {
        uniformOfficers += value;
      }
    });

    yearlyData.push({
      year: year.toString(),
      uniformOfficers,
      storeDetectives
    });
  }

  return {
    daily: dailyData,
    weekly: weeklyData,
    monthly: monthlyData,
    yearly: yearlyData
  };
};


const getSites = async (signal?: AbortSignal): Promise<Site[]> => {
  const response = await fetch(`${BASE_API_URL}/dashboard/sites`, { 
    signal,
    credentials: 'include',
    headers: applyCsrfHeader({
      'Content-Type': 'application/json',
      ...getHeaders(),
    }),
  });
  if (!response.ok) {
    throw new Error('Failed to fetch sites');
  }
  return response.json();
};

const getStores = async (signal?: AbortSignal): Promise<StoreData[]> => {
  const response = await fetch(`${BASE_API_URL}/dashboard/stores`, { 
    signal,
    credentials: 'include',
    headers: applyCsrfHeader({
      'Content-Type': 'application/json',
      ...getHeaders(),
    }),
  });
  if (!response.ok) {
    throw new Error('Failed to fetch stores');
  }
  return response.json();
};

const getRegions = async (signal?: AbortSignal): Promise<Region[]> => {
  const response = await fetch(`${BASE_API_URL}/dashboard/regions`, { 
    signal,
    credentials: 'include',
    headers: applyCsrfHeader({
      'Content-Type': 'application/json',
      ...getHeaders(),
    }),
  });
  if (!response.ok) {
    throw new Error('Failed to fetch regions');
  }
  return response.json();
};

class CustomerDashboardService {
  private baseUrl = BASE_API_URL;

  private getHeaders(overrideCustomerId?: number | null) {
    const user = getActiveUser();
    const headers: Record<string, string> = applyCsrfHeader({
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    });

    const customerId = overrideCustomerId != null
      ? overrideCustomerId
      : (() => {
          if (!user.role && !user.Role) {
            const storedRole = sessionStore.getUser()?.role || null;
            if (storedRole) (user as any).role = storedRole;
          }
          return extractCustomerId(user);
        })();
    
    if (customerId) {
      headers['X-Customer-Id'] = customerId.toString();
      if (import.meta.env.DEV) console.log('🔍 [DashboardService] Setting X-Customer-Id header:', customerId);
    }
    
    return headers;
  }

  private async fetchWithSignal<T>(endpoint: string, signal?: AbortSignal, overrideCustomerId?: number | null): Promise<T> {
    try {
      const fullUrl = `${this.baseUrl}${endpoint}`;
      if (import.meta.env.DEV) console.log(`🔍 [DashboardService] Fetching: ${fullUrl}`);
      
      const response = await fetch(fullUrl, {
        signal,
        credentials: 'include',
        headers: this.getHeaders(overrideCustomerId),
      });

      if (!response.ok) {
        console.error(`❌ [DashboardService] HTTP error ${response.status} for ${fullUrl}`);
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      
      // Handle ApiResponseDto wrapper from backend
      // Backend uses { Success, Data, Message } format (PascalCase)
      if (result && typeof result === 'object') {
        if ('Data' in result) {
          console.log(`✅ [DashboardService] Response Data property found for ${fullUrl}`);
          return result.Data as T;
        }
        if ('data' in result) {
          console.log(`✅ [DashboardService] Response data property found for ${fullUrl}`);
          return result.data as T;
        }
      }
      
      console.log(`✅ [DashboardService] Returning raw result for ${fullUrl}`);
      return result as T;
    } catch (error) {
      // Don't log AbortError as it's expected during cleanup
      if (error instanceof Error && error.name === 'AbortError') {
        throw error; // Re-throw AbortError without logging
      }
      // Don't log network errors (backend might be down)
      const isNetworkError = error instanceof TypeError && error.message.includes('Failed to fetch');
      if (!isNetworkError) {
        const fullUrl = `${this.baseUrl}${endpoint}`;
        console.error(`❌ [DashboardService] Failed to fetch ${fullUrl}:`, error);
      }
      throw new Error(`Failed to fetch ${fullUrl}: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  async getStores(signal?: AbortSignal, overrideCustomerId?: number | null): Promise<StoreData[]> {
    const sites = await this.getSites(signal, overrideCustomerId);
    // Transform sites to StoreData format
    return sites.map(site => ({
      id: site.id,
      name: site.locationName,
      customerId: site.customerId,
      metrics: {
        'customer-site': [],
        'customer-ho': []
      },
      incidentData: {
        daily: [],
        weekly: [],
        monthly: [],
        yearly: []
      },
      recentIncidents: []
    }));
  }

  async getRegions(signal?: AbortSignal, overrideCustomerId?: number | null): Promise<Region[]> {
    const user = getActiveUser();
    const customerId = overrideCustomerId ?? user.customerId ?? (user as any).CustomerId ?? (user as any).companyId;
    const url = customerId
      ? `/region?page=1&pageSize=1000&customerId=${customerId}`
      : '/region?page=1&pageSize=1000';
    const backendRegions = await this.fetchWithSignal<any[]>(url, signal, overrideCustomerId ?? customerId ?? undefined);
    
    // Ensure we have an array
    const regionsArray = Array.isArray(backendRegions) ? backendRegions : [];
    
    // Map backend RegionDto to frontend Region format
    return regionsArray.map((region: any) => ({
      id: region.RegionID?.toString() || region.regionID?.toString() || region.id?.toString() || '',
      name: region.RegionName || region.regionName || region.name || '',
      customerId: region.FkCustomerID || region.fkCustomerID || region.customerId || region.CustomerId || 0,
      code: region.RegionCode || region.regionCode || region.code || '',
      status: region.RecordIsDeletedYN === false ? 'active' : 'inactive',
      createdAt: region.DateCreated || region.dateCreated || region.createdAt || '',
      updatedAt: region.DateModified || region.dateModified || region.updatedAt || ''
    }));
  }

  async getSites(signal?: AbortSignal, overrideCustomerId?: number | null): Promise<Site[]> {
    const user = getActiveUser();
    const customerId = overrideCustomerId ?? user.customerId ?? (user as any).CustomerId ?? (user as any).companyId;
    const url = customerId
      ? `/site?page=1&pageSize=1000&customerId=${customerId}`
      : '/site?page=1&pageSize=1000';
    const backendSites = await this.fetchWithSignal<any[]>(url, signal, overrideCustomerId ?? customerId ?? undefined);
    
    // Ensure we have an array
    const sitesArray = Array.isArray(backendSites) ? backendSites : [];
    
    // Map backend SiteDto to frontend Site format
    return sitesArray.map((site: any) => ({
      id: site.SiteID?.toString() || site.siteID?.toString() || site.id?.toString() || '',
      locationName: site.LocationName || site.locationName || site.name || '',
      regionId: site.FkRegionID?.toString() || site.fkRegionID?.toString() || site.regionId?.toString() || site.RegionId?.toString() || '',
      customerId: site.FkCustomerID || site.fkCustomerID || site.customerId || site.CustomerId || 0,
      buildingName: site.BuildingName || site.buildingName || '',
      street: site.NumberAndStreet || site.numberandStreet || site.street || '',
      town: site.Town || site.town || '',
      county: site.County || site.county || '',
      postcode: site.Postcode || site.postcode || '',
      isCoreSite: site.CoreSiteYN === true || site.coreSiteYN === true || site.isCoreSite === true,
      sinNumber: site.SinNumber || site.sinNumber || '',
      telephone: site.TelephoneNumber || site.telephoneNumber || site.telephone || '',
      status: site.RecordIsDeletedYN === false ? 'active' : 'inactive',
      createdAt: site.DateCreated || site.dateCreated || site.createdAt || '',
      updatedAt: site.DateModified || site.dateModified || site.updatedAt || ''
    }));
  }

  async getStoreData(storeId: string, signal?: AbortSignal, overrideCustomerId?: number | null): Promise<CustomerStoreData> {
    return this.getSiteData(storeId, signal, overrideCustomerId);
  }

  async getSiteData(siteId: string, signal?: AbortSignal, overrideCustomerId?: number | null): Promise<CustomerStoreData> {
    const siteResponse = await this.fetchWithSignal<{ Success: boolean; Data: any; Message?: string }>(`/site/${siteId}`, signal, overrideCustomerId);
    const site = Array.isArray(siteResponse) ? siteResponse[0] : (siteResponse?.Data || siteResponse);
    
    // Map backend SiteDto fields to frontend format
    const siteIdNum = site.SiteID || site.siteID || parseInt(siteId, 10);
    const customerId = site.fkCustomerID || site.customerId || site.CustomerId;
    const siteName = site.LocationName || site.locationName || site.name || '';
    
    const user = getActiveUser();
    const userCustomerId = overrideCustomerId ?? user.customerId ?? (user as any).CustomerId ?? (user as any).companyId ?? customerId;
    let recentIncidents: RecentIncident[] = [];
    let allIncidentsForChart: Array<{ date: string; officerRole?: string; officerType?: string; value?: number; amount?: number }> = [];
    try {
      const incidentsResponse = await this.fetchWithSignal<{ Success: boolean; Data: { items?: any[] } | any[]; Message?: string }>(
        `/incidents?page=1&pageSize=500&siteId=${siteIdNum}${userCustomerId ? `&customerId=${userCustomerId}` : ''}`,
        signal,
        overrideCustomerId ?? userCustomerId ?? undefined
      );
      const incidentsData = Array.isArray(incidentsResponse) 
        ? incidentsResponse 
        : (incidentsResponse?.Data || []);
      const incidents = Array.isArray(incidentsData) ? incidentsData : (incidentsData?.items || []);
      
      // Map incidents for recent incidents list
      recentIncidents = incidents.slice(0, 10).map((inc: any) => {
        const financials = getIncidentFinancials(inc)
        return ({
        id: inc.Id || inc.id?.toString() || '',
        customerId: inc.CustomerId || inc.customerId || userCustomerId || 0,
        date: inc.DateOfIncident || inc.Date || inc.date || inc.incidentDate || '',
        timeOfIncident: inc.TimeOfIncident || inc.timeOfIncident || inc.timeOfDay || '',
        regionId: inc.RegionId?.toString() || inc.regionId?.toString() || '',
        regionName: inc.RegionName || inc.regionName || '',
        siteId: inc.SiteId?.toString() || inc.siteId?.toString() || siteIdNum.toString(),
        siteName: inc.SiteName || inc.siteName || siteName,
        type: inc.IncidentType || inc.incidentType || inc.type || '',
        value: inc.TotalValueRecovered || inc.Value || inc.value || 0,
        assignedTo: inc.AssignedTo || inc.assignedTo || '',
        customerName: inc.CustomerName || inc.customerName || '',
        store: inc.SiteName || inc.siteName || siteName,
        officerName: inc.OfficerName || inc.officerName || '',
        amount: financials.totalRecoveredValue,
        recoveredValue: financials.totalRecoveredValue,
        lostValue: financials.totalLostValue,
        incidentType: inc.IncidentType || inc.incidentType || inc.type || ''
      })});

      // Map all incidents for chart calculation
      allIncidentsForChart = incidents.map((inc: any) => ({
        date: inc.DateOfIncident || inc.Date || inc.date || inc.incidentDate || '',
        officerRole: inc.OfficerRole || inc.officerRole || '',
        officerType: inc.OfficerType || inc.officerType || '',
        value: inc.TotalValueRecovered || inc.Value || inc.value || 0,
        amount: inc.TotalValueRecovered || inc.Amount || inc.amount || inc.value || 0
      }));
    } catch (error) {
      // AbortError is expected during component cleanup - don't log it
      if (error instanceof Error && error.name === 'AbortError') {
        // Return empty structure on abort
        return {
          id: siteIdNum.toString(),
          name: siteName,
          customerId: customerId || userCustomerId || 0,
          metrics: {
            manager: [],
            store: []
          },
          recentIncidents: [],
          incidentData: {
            daily: [],
            weekly: [],
            monthly: [],
            yearly: []
          }
        };
      }
      console.warn('⚠️ [DashboardService] Could not fetch incidents:', error);
      // Continue with empty incidents on error
    }
    
    const effectiveCustomerId = customerId || userCustomerId || 0;

    const hasRealIncidents = allIncidentsForChart.length > 0;
    const incidentData = hasRealIncidents
      ? calculateIncidentChartData(allIncidentsForChart)
      : { daily: [], weekly: [], monthly: [], yearly: [] };

    const metrics = { manager: [] as Metric[], store: [] as Metric[] };
    
    // Transform to CustomerStoreData format
    return {
      id: siteIdNum.toString(),
      name: siteName,
      customerId: effectiveCustomerId,
      metrics,
      recentIncidents,
      incidentData
    };
  }

  async getAggregatedSitesData(siteIds: string[], signal?: AbortSignal, overrideCustomerId?: number | null): Promise<CustomerStoreData> {
    const user = getActiveUser();
    const customerId = overrideCustomerId ?? user.customerId ?? (user as any).CustomerId ?? (user as any).companyId;
    const sitesPromises = siteIds.map(id =>
      this.fetchWithSignal<{ Success: boolean; Data: any; Message?: string }>(`/site/${id}`, signal, overrideCustomerId ?? customerId ?? undefined).catch(() => null)
    );
    const sitesResults = await Promise.all(sitesPromises);
    const sites = sitesResults
      .filter(r => r !== null)
      .map(r => Array.isArray(r) ? r[0] : (r?.Data || r?.data || r));
    
    // Aggregate incidents from all sites - fetch more for chart data calculation
    let allIncidents: RecentIncident[] = [];
    let allIncidentsForChart: Array<{ date: string; officerRole?: string; officerType?: string; value?: number; amount?: number }> = [];
    try {
      const incidentsPromises = siteIds.map(id =>
        this.fetchWithSignal<{ Success: boolean; Data: { items?: any[] } | any[]; Message?: string }>(
          `/incidents?page=1&pageSize=500&siteId=${id}${customerId ? `&customerId=${customerId}` : ''}`,
          signal,
          overrideCustomerId ?? customerId ?? undefined
        ).catch(() => ({ Success: false, Data: [] }))
      );
      const incidentsResults = await Promise.all(incidentsPromises);
      const incidentsArrays = incidentsResults.map(r => {
        const data = Array.isArray(r) ? r : (r?.Data || []);
        return Array.isArray(data) ? data : (data?.items || []);
      });
      const flatIncidents = incidentsArrays.flat();
      
      // Map for recent incidents list
      allIncidents = flatIncidents.map((inc: any) => {
        const financials = getIncidentFinancials(inc)
        return ({
        id: inc.Id || inc.id?.toString() || '',
        customerId: inc.CustomerId || inc.customerId || customerId || 0,
        date: inc.DateOfIncident || inc.Date || inc.date || inc.incidentDate || '',
        timeOfIncident: inc.TimeOfIncident || inc.timeOfIncident || inc.timeOfDay || '',
        regionId: inc.RegionId?.toString() || inc.regionId?.toString() || '',
        regionName: inc.RegionName || inc.regionName || '',
        siteId: inc.SiteId?.toString() || inc.siteId?.toString() || '',
        siteName: inc.SiteName || inc.siteName || '',
        type: inc.IncidentType || inc.incidentType || inc.type || '',
        value: inc.TotalValueRecovered || inc.Value || inc.value || 0,
        assignedTo: inc.AssignedTo || inc.assignedTo || '',
        customerName: inc.CustomerName || inc.customerName || '',
        store: inc.SiteName || inc.siteName || '',
        officerName: inc.OfficerName || inc.officerName || '',
        amount: financials.totalRecoveredValue,
        recoveredValue: financials.totalRecoveredValue,
        lostValue: financials.totalLostValue,
        incidentType: inc.IncidentType || inc.incidentType || inc.type || ''
      })});

      // Map all incidents for chart calculation
      allIncidentsForChart = flatIncidents.map((inc: any) => ({
        date: inc.DateOfIncident || inc.Date || inc.date || inc.incidentDate || '',
        officerRole: inc.OfficerRole || inc.officerRole || '',
        officerType: inc.OfficerType || inc.officerType || '',
        value: inc.TotalValueRecovered || inc.Value || inc.value || 0,
        amount: inc.TotalValueRecovered || inc.Amount || inc.amount || inc.value || 0
      }));
    } catch (error) {
      // AbortError is expected during component cleanup - don't log it
      if (error instanceof Error && error.name === 'AbortError') {
        // Return empty structure on abort
        return {
          id: 'aggregated',
          name: `Aggregated (${sites.length} sites)`,
          customerId: customerId || 0,
          metrics: {
            manager: [],
            store: []
          },
          recentIncidents: [],
          incidentData: {
            daily: [],
            weekly: [],
            monthly: [],
            yearly: []
          }
        };
      }
      console.warn('⚠️ [DashboardService] Could not fetch aggregated incidents:', error);
      // Continue with empty incidents on error
    }
    
    const effectiveCustomerId = customerId || 0;

    const hasRealIncidents = allIncidentsForChart.length > 0;
    const incidentData = hasRealIncidents
      ? calculateIncidentChartData(allIncidentsForChart)
      : { daily: [], weekly: [], monthly: [], yearly: [] };

    const metrics = { manager: [] as Metric[], store: [] as Metric[] };

    const recent = allIncidents.slice(0, 10);
    
    // Return aggregated data
    return {
      id: 'aggregated',
      name: `Aggregated (${sites.length} sites)`,
      customerId: effectiveCustomerId,
      metrics,
      recentIncidents: recent,
      incidentData
    };
  }
}

export const customerDashboardService = new CustomerDashboardService(); 