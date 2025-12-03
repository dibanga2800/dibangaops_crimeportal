import React, { Suspense } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { 
  Activity, 
  AlertCircle, 
  Calendar,
  Clock,
  Users,
  Shield,
  TrendingUp,
  Bell,
  ArrowUpRight,
  ArrowDownRight,
  ChevronRight,
  Home,
  Settings,
  HelpCircle,
  LogOut,
  User,
  Plus,
  Currency,
  Star,
  CheckCircle,
  FileText,
  Briefcase,
  MapPin,
  MessageSquare
} from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { TaskCard } from '@/components/dashboard/TaskCard'
import { IncidentTable } from '@/components/dashboard/IncidentTable'
import { OfficerPerformance } from '@/components/dashboard/OfficerPerformance'
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend, BarChart, Bar } from 'recharts'
import { MOCK_INCIDENTS } from '@/data/mockIncidents'
import { format } from 'date-fns'
import { cn } from '@/lib/utils'
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { ScrollArea } from "@/components/ui/scroll-area";
import { usePageAccess } from "@/contexts/PageAccessContext"
import { DashboardGreeting } from '@/components/dashboard/DashboardGreeting'
import { analyticsService } from '@/services/analyticsService'
import { customerDashboardService } from '@/services/dashboardService'
import { incidentsApi } from '@/services/api/incidents'
import type { AnalyticsHubData } from '@/types/analytics'
import { BASE_API_URL } from '@/config/api'

// Lazy load the dashboard components
const OfficerDashboard = React.lazy(() => import('@/pages/Dashboard/OfficerDashboard'))
const CustomerDashboard = React.lazy(() => import('@/pages/Dashboard/CustomerDashboard'))

// Customer-specific data
const customerData = {
  'customer1': {
    metrics: [
      { title: 'Total Saved YTD', value: '£196K', change: '+15%', trend: 'up', icon: Currency, color: 'green' },
      { title: 'Customer Satisfaction', value: '4.8/5', change: '+0.3', trend: 'up', icon: Star, color: 'yellow' },
      { title: 'Incidents Today', value: '8', change: '-3%', trend: 'down', icon: AlertCircle, color: 'red' },
      { title: 'Active Guards', value: '342', change: '+12%', trend: 'up', icon: Users, color: 'blue' }
    ],
    dailyIncidents: [
      { date: 'Mon', uniformOfficers: 12, storeDetectives: 8 },
      { date: 'Tue', uniformOfficers: 19, storeDetectives: 14 },
      { date: 'Wed', uniformOfficers: 15, storeDetectives: 11 },
      { date: 'Thu', uniformOfficers: 20, storeDetectives: 17 },
      { date: 'Fri', uniformOfficers: 25, storeDetectives: 20 },
      { date: 'Sat', uniformOfficers: 22, storeDetectives: 19 },
      { date: 'Sun', uniformOfficers: 18, storeDetectives: 15 }
    ],
    weeklyIncidents: [
      { week: 'Week 1', uniformOfficers: 42, storeDetectives: 35 },
      { week: 'Week 2', uniformOfficers: 38, storeDetectives: 30 },
      { week: 'Week 3', uniformOfficers: 45, storeDetectives: 36 },
      { week: 'Week 4', uniformOfficers: 40, storeDetectives: 32 }
    ],
    monthlyIncidents: [
      { month: 'Jan', uniformOfficers: 40, storeDetectives: 32 },
      { month: 'Feb', uniformOfficers: 30, storeDetectives: 24 },
      { month: 'Mar', uniformOfficers: 45, storeDetectives: 36 },
      { month: 'Apr', uniformOfficers: 25, storeDetectives: 20 },
      { month: 'May', uniformOfficers: 35, storeDetectives: 28 },
      { month: 'Jun', uniformOfficers: 20, storeDetectives: 16 },
      { month: 'Jul', uniformOfficers: 28, storeDetectives: 22 },
      { month: 'Aug', uniformOfficers: 32, storeDetectives: 26 },
      { month: 'Sep', uniformOfficers: 38, storeDetectives: 30 },
      { month: 'Oct', uniformOfficers: 42, storeDetectives: 34 },
      { month: 'Nov', uniformOfficers: 36, storeDetectives: 29 },
      { month: 'Dec', uniformOfficers: 30, storeDetectives: 24 }
    ],
    yearlyIncidents: [
      { year: '2020', uniformOfficers: 280, storeDetectives: 224 },
      { year: '2021', uniformOfficers: 320, storeDetectives: 256 },
      { year: '2022', uniformOfficers: 350, storeDetectives: 280 },
      { year: '2023', uniformOfficers: 375, storeDetectives: 300 },
      { year: '2024', uniformOfficers: 401, storeDetectives: 321 }
    ],
    incidentReports: [
      {
        id: '1',
        customerName: 'Central England COOP',
        store: 'Store #1234',
        officerName: 'John Smith',
        date: '2025-01-30',
        amount: 1250.00,
        incidentType: 'Theft - Loss?'
      },
      {
        id: '2',
        customerName: 'Central England COOP',
        store: 'Store #1235',
        officerName: 'Jane Doe',
        date: '2025-01-29',
        amount: 850.00,
        incidentType: 'Suspicious Behaviour?'
      },
      {
        id: '3',
        customerName: 'Central England COOP',
        store: 'Store #1236',
        officerName: 'Mike Johnson',
        date: '2025-01-28',
        amount: 2100.00,
        incidentType: 'Theft - Loss?'
      }
    ]
  },
  'customer2': {
    metrics: [
      { title: 'Total Saved YTD', value: '£250K', change: '+20%', trend: 'up', icon: Currency, color: 'green' },
      { title: 'Customer Satisfaction', value: '4.5/5', change: '+0.2', trend: 'up', icon: Star, color: 'yellow' },
      { title: 'Incidents Today', value: '5', change: '-10%', trend: 'down', icon: AlertCircle, color: 'red' },
      { title: 'Active Guards', value: '420', change: '+15%', trend: 'up', icon: Users, color: 'blue' }
    ],
    dailyIncidents: [
      { date: 'Mon', uniformOfficers: 10, storeDetectives: 8 },
      { date: 'Tue', uniformOfficers: 15, storeDetectives: 12 },
      { date: 'Wed', uniformOfficers: 13, storeDetectives: 10 },
      { date: 'Thu', uniformOfficers: 18, storeDetectives: 14 },
      { date: 'Fri', uniformOfficers: 22, storeDetectives: 18 },
      { date: 'Sat', uniformOfficers: 20, storeDetectives: 16 },
      { date: 'Sun', uniformOfficers: 16, storeDetectives: 13 }
    ],
    weeklyIncidents: [
      { week: 'Week 1', uniformOfficers: 38, storeDetectives: 30 },
      { week: 'Week 2', uniformOfficers: 35, storeDetectives: 28 },
      { week: 'Week 3', uniformOfficers: 40, storeDetectives: 32 },
      { week: 'Week 4', uniformOfficers: 37, storeDetectives: 30 }
    ],
    monthlyIncidents: [
      { month: 'Jan', uniformOfficers: 35, storeDetectives: 28 },
      { month: 'Feb', uniformOfficers: 28, storeDetectives: 22 },
      { month: 'Mar', uniformOfficers: 40, storeDetectives: 32 },
      { month: 'Apr', uniformOfficers: 22, storeDetectives: 18 },
      { month: 'May', uniformOfficers: 30, storeDetectives: 24 },
      { month: 'Jun', uniformOfficers: 18, storeDetectives: 14 },
      { month: 'Jul', uniformOfficers: 25, storeDetectives: 20 },
      { month: 'Aug', uniformOfficers: 30, storeDetectives: 24 },
      { month: 'Sep', uniformOfficers: 35, storeDetectives: 28 },
      { month: 'Oct', uniformOfficers: 38, storeDetectives: 30 },
      { month: 'Nov', uniformOfficers: 32, storeDetectives: 26 },
      { month: 'Dec', uniformOfficers: 28, storeDetectives: 22 }
    ],
    yearlyIncidents: [
      { year: '2020', uniformOfficers: 260, storeDetectives: 208 },
      { year: '2021', uniformOfficers: 290, storeDetectives: 232 },
      { year: '2022', uniformOfficers: 320, storeDetectives: 256 },
      { year: '2023', uniformOfficers: 345, storeDetectives: 276 },
      { year: '2024', uniformOfficers: 361, storeDetectives: 289 }
    ],
    incidentReports: [
      {
        id: '1',
        customerName: 'Heart of England',
        store: 'Store #5678',
        officerName: 'Sarah Wilson',
        date: '2025-01-30',
        amount: 1500.00,
        incidentType: 'Theft - Loss?'
      },
      {
        id: '2',
        customerName: 'Heart of England',
        store: 'Store #5679',
        officerName: 'Tom Brown',
        date: '2025-01-29',
        amount: 950.00,
        incidentType: 'Credit Card Fraud?'
      },
      {
        id: '3',
        customerName: 'Heart of England',
        store: 'Store #5680',
        officerName: 'Lisa Chen',
        date: '2025-01-28',
        amount: 1800.00,
        incidentType: 'Theft - Loss?'
      }
    ]
  },
  'customer3': {
    metrics: [
      { title: 'Total Saved YTD', value: '£175K', change: '+12%', trend: 'up', icon: Currency, color: 'green' },
      { title: 'Customer Satisfaction', value: '4.9/5', change: '+0.4', trend: 'up', icon: Star, color: 'yellow' },
      { title: 'Incidents Today', value: '3', change: '-15%', trend: 'down', icon: AlertCircle, color: 'red' },
      { title: 'Active Guards', value: '280', change: '+8%', trend: 'up', icon: Users, color: 'blue' }
    ],
    dailyIncidents: [
      { date: 'Mon', uniformOfficers: 8, storeDetectives: 6 },
      { date: 'Tue', uniformOfficers: 12, storeDetectives: 10 },
      { date: 'Wed', uniformOfficers: 10, storeDetectives: 8 },
      { date: 'Thu', uniformOfficers: 14, storeDetectives: 11 },
      { date: 'Fri', uniformOfficers: 18, storeDetectives: 14 },
      { date: 'Sat', uniformOfficers: 16, storeDetectives: 13 },
      { date: 'Sun', uniformOfficers: 13, storeDetectives: 10 }
    ],
    weeklyIncidents: [
      { week: 'Week 1', uniformOfficers: 32, storeDetectives: 26 },
      { week: 'Week 2', uniformOfficers: 30, storeDetectives: 24 },
      { week: 'Week 3', uniformOfficers: 35, storeDetectives: 28 },
      { week: 'Week 4', uniformOfficers: 33, storeDetectives: 26 }
    ],
    monthlyIncidents: [
      { month: 'Jan', uniformOfficers: 30, storeDetectives: 24 },
      { month: 'Feb', uniformOfficers: 25, storeDetectives: 20 },
      { month: 'Mar', uniformOfficers: 35, storeDetectives: 28 },
      { month: 'Apr', uniformOfficers: 20, storeDetectives: 16 },
      { month: 'May', uniformOfficers: 28, storeDetectives: 22 },
      { month: 'Jun', uniformOfficers: 15, storeDetectives: 12 },
      { month: 'Jul', uniformOfficers: 22, storeDetectives: 18 },
      { month: 'Aug', uniformOfficers: 26, storeDetectives: 21 },
      { month: 'Sep', uniformOfficers: 32, storeDetectives: 26 },
      { month: 'Oct', uniformOfficers: 36, storeDetectives: 29 },
      { month: 'Nov', uniformOfficers: 30, storeDetectives: 24 },
      { month: 'Dec', uniformOfficers: 25, storeDetectives: 20 }
    ],
    yearlyIncidents: [
      { year: '2020', uniformOfficers: 220, storeDetectives: 176 },
      { year: '2021', uniformOfficers: 250, storeDetectives: 200 },
      { year: '2022', uniformOfficers: 280, storeDetectives: 224 },
      { year: '2023', uniformOfficers: 300, storeDetectives: 240 },
      { year: '2024', uniformOfficers: 324, storeDetectives: 259 }
    ],
    incidentReports: [
      {
        id: '1',
        customerName: 'Midcounties COOP',
        store: 'Store #9012',
        officerName: 'David Lee',
        date: '2025-01-30',
        amount: 1750.00,
        incidentType: 'Theft - Loss?'
      },
      {
        id: '2',
        customerName: 'Midcounties COOP',
        store: 'Store #9013',
        officerName: 'Emma White',
        date: '2025-01-29',
        amount: 1100.00,
        incidentType: 'Suspicious Behaviour?'
      },
      {
        id: '3',
        customerName: 'Midcounties COOP',
        store: 'Store #9014',
        officerName: 'Chris Taylor',
        date: '2025-01-28',
        amount: 2300.00,
        incidentType: 'Theft - Loss?'
      },
      {
        id: '4',
        customerName: 'Midcounties COOP',
        store: 'Store #9015',
        officerName: 'Rachel Parker',
        date: '2025-01-27',
        amount: 1850.00,
        incidentType: 'Deter - Saved?'
      },
      {
        id: '5',
        customerName: 'Midcounties COOP',
        store: 'Store #9016',
        officerName: 'Mark Thompson',
        date: '2025-01-26',
        amount: 2100.00,
        incidentType: 'Theft - Loss?'
      },
      {
        id: '6',
        customerName: 'Midcounties COOP',
        store: 'Store #9017',
        officerName: 'Sophie Anderson',
        date: '2025-01-25',
        amount: 1950.00,
        incidentType: 'Credit Card Fraud?'
      }
    ]
  }
};

const tasks = [
  {
    id: '1',
    title: 'Review Security Protocols',
    description: 'Conduct a comprehensive review of current security protocols and identify areas for improvement.',
    assignee: 'John Smith',
    dueDate: new Date(2025, 1, 15),
    priority: 'high',
    status: 'in-progress'
  },
  {
    id: '2',
    title: 'Staff Training Session',
    description: 'Organize and conduct quarterly security training session for new staff members.',
    assignee: 'Sarah Johnson',
    dueDate: new Date(2025, 1, 20),
    priority: 'medium',
    status: 'pending'
  }
] as const;

const equipmentData = [
  { name: 'Laptops', value: 245, color: '#0ea5e9' },  // sky-500
  { name: 'Phones', value: 180, color: '#22c55e' },   // green-500
  { name: 'iPads', value: 120, color: '#f59e0b' },    // amber-500
  { name: 'Radios', value: 95, color: '#ef4444' },    // red-500
  { name: 'Other', value: 75, color: '#8b5cf6' }      // violet-500
];

const customers = [
  { id: 'customer1', name: 'Central England COOP' },
  { id: 'customer2', name: 'Heart of England' },
  { id: 'customer3', name: 'Midcounties COOP' }
] as const;

const notifications = [
  {
    id: '1',
    title: 'New Incident Report',
    description: 'Location B reported unauthorized access attempt',
    time: '10 minutes ago',
    type: 'alert'
  },
  {
    id: '2',
    title: 'Guard Schedule Updated',
    description: 'Changes to night shift rotation for next week',
    time: '1 hour ago',
    type: 'info'
  }
]

const officerStats = [
  // Top Performers
  {
    id: '1',
    name: 'John Smith',
    incidents: 85,
    valueSaved: 145000,
    responseRate: 98,
    status: 'excellent'
  },
  {
    id: '2',
    name: 'Sarah Wilson',
    incidents: 78,
    valueSaved: 132000,
    responseRate: 97,
    status: 'excellent'
  },
  {
    id: '3',
    name: 'Mike Johnson',
    incidents: 72,
    valueSaved: 128000,
    responseRate: 95,
    status: 'excellent'
  },
  {
    id: '4',
    name: 'Lisa Anderson',
    incidents: 65,
    valueSaved: 115000,
    responseRate: 94,
    status: 'good'
  },
  {
    id: '5',
    name: 'David Chen',
    incidents: 62,
    valueSaved: 108000,
    responseRate: 92,
    status: 'good'
  },
  // Non-Reporters and Needs Improvement
  {
    id: '6',
    name: 'Emily Davis',
    incidents: 25,
    valueSaved: 42000,
    responseRate: 75,
    status: 'needs-improvement'
  },
  {
    id: '7',
    name: 'Chris Brown',
    incidents: 18,
    valueSaved: 28000,
    responseRate: 45,
    status: 'non-reporter'
  },
  {
    id: '8',
    name: 'Alex Turner',
    incidents: 15,
    valueSaved: 22000,
    responseRate: 65,
    status: 'non-reporter'
  },
  {
    id: '9',
    name: 'Maria Garcia',
    incidents: 22,
    valueSaved: 35000,
    responseRate: 72,
    status: 'needs-improvement'
  },
  {
    id: '10',
    name: 'Tom Wilson',
    incidents: 12,
    valueSaved: 18000,
    responseRate: 40,
    status: 'non-reporter'
  }
] as const;

const TestComponents = () => {
  return (
    <div className="space-y-6 p-4 border border-gray-200 rounded-md my-4">
      <h2 className="text-xl font-bold">Test Components</h2>
      
      <div>
        <h3 className="font-medium mb-2">Accordion Test</h3>
        <Accordion type="single">
          <AccordionItem value="item-1">
            <AccordionTrigger>Is this working?</AccordionTrigger>
            <AccordionContent>
              Yes. This is our custom accordion component.
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </div>
      
      <div>
        <h3 className="font-medium mb-2">Dropdown Test</h3>
        <DropdownMenu>
          <DropdownMenuTrigger className="border rounded-md px-4 py-2">
            Click me
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <DropdownMenuItem>
              Item 1
            </DropdownMenuItem>
            <DropdownMenuItem>
              Item 2
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      
      <div>
        <h3 className="font-medium mb-2">ScrollArea Test</h3>
        <ScrollArea className="h-32 w-full border rounded-md">
          <div className="p-4">
            <h4>Scrollable Content</h4>
            <p>This is a test of the ScrollArea component.</p>
            <p>Scroll down to see more content.</p>
            <div className="h-64 bg-gray-100 mt-2 p-4">
              Tall content to enable scrolling
            </div>
          </div>
        </ScrollArea>
      </div>
    </div>
  )
}

// Helper function to format currency values dynamically
const formatCurrency = (value: number): string => {
  if (value >= 1_000_000) {
    return `£${(value / 1_000_000).toFixed(1)}M`
  } else if (value >= 1_000) {
    return `£${(value / 1_000).toFixed(1)}K`
  } else {
    return `£${value.toFixed(0)}`
  }
}

const AdminDashboard = () => {
  const location = useLocation();
  const { currentRole, isTestMode, testRole, isLoading } = usePageAccess();
  const effectiveRole = isTestMode && testRole ? testRole : currentRole;

  // All state declarations must be at the top before any conditional returns
  const [analyticsData, setAnalyticsData] = React.useState<AnalyticsHubData | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = React.useState<boolean>(true);
  const [analyticsError, setAnalyticsError] = React.useState<string | null>(null);
  const [regionOptions, setRegionOptions] = React.useState<Array<{ id: string; name: string }>>([]);
  const [selectedRegion, setSelectedRegion] = React.useState<string>('all');
  const [activePeriod, setActivePeriod] = React.useState<'Daily' | 'Weekly' | 'Monthly' | 'Yearly'>('Monthly');
  const [loadedIncidents, setLoadedIncidents] = React.useState<typeof MOCK_INCIDENTS>([]);
  const [incidentsLoading, setIncidentsLoading] = React.useState(true);

  // Load incidents from API
  React.useEffect(() => {
    console.log('🎯 Incident loading useEffect triggered');
    const abortController = new AbortController();
    let isActive = true;

    const loadIncidents = async () => {
      console.log('🚀 Starting loadIncidents function');
      try {
        setIncidentsLoading(true);
        
        // Check if we have auth token
        const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
        if (!token) {
          console.warn('⚠️ No auth token found - incidents may not load');
        } else {
          console.log('✅ Auth token found:', token.substring(0, 20) + '...');
        }
        
        console.log('🔄 Loading incidents using incidentsApi service');
        
        // Use the proper incidents API service which handles authentication
        const response = await incidentsApi.getIncidents({
          page: 1,
          pageSize: 500
        });
        
        console.log('📦 Incidents API response:', {
          isArray: Array.isArray(response),
          hasData: !!response.data,
          hasItems: !!response.items,
          keys: Object.keys(response)
        });
        
        // Extract incidents from response (handle different response formats)
        let incidents = [];
        if (Array.isArray(response)) {
          incidents = response;
        } else if (response.Data && Array.isArray(response.Data)) {
          incidents = response.Data;
        } else if (response.data && Array.isArray(response.data)) {
          incidents = response.data;
        } else if (response.items && Array.isArray(response.items)) {
          incidents = response.items;
        }

        // Log the first raw incident to see actual backend field names
        if (incidents.length > 0) {
          console.log('📋 Raw backend incident (first):', incidents[0]);
          console.log('💰 Value fields check:', {
            TotalValueRecovered: incidents[0].TotalValueRecovered,
            totalValueRecovered: incidents[0].totalValueRecovered,
            Value: incidents[0].Value,
            value: incidents[0].value,
            Amount: incidents[0].Amount,
            amount: incidents[0].amount
          });
          console.log('✅ Status fields check:', {
            Status: incidents[0].Status,
            status: incidents[0].status,
            Priority: incidents[0].Priority,
            priority: incidents[0].priority
          });
        }

        // Transform backend format to frontend format
        // Priority order for site name: SiteName > LocationName > siteName > locationName
        const transformedIncidents = incidents.map((inc: any) => {
          const siteName = inc.SiteName || inc.LocationName || inc.siteName || inc.locationName || inc.Location || '';
          
          return {
            id: inc.Id || inc.id || '',
            customerId: inc.CustomerId || inc.customerId || 0,
            customerName: inc.CustomerName || inc.customerName || '',
            siteName: siteName,
            siteId: inc.SiteId?.toString() || inc.siteId?.toString() || '',
            regionId: inc.RegionId?.toString() || inc.regionId?.toString() || '',
            regionName: inc.RegionName || inc.regionName || '',
            location: siteName, // Use same site name for location
            store: siteName, // Use same site name for store
            officerName: inc.OfficerName || inc.officerName || '',
            officerRole: inc.OfficerRole || inc.officerRole || '',
            officerType: inc.OfficerType || inc.officerType || '',
            dateOfIncident: inc.DateOfIncident || inc.dateOfIncident || inc.date || '',
            date: inc.DateOfIncident || inc.dateOfIncident || inc.date || '',
            timeOfIncident: inc.TimeOfIncident || inc.timeOfIncident || '',
            incidentType: inc.IncidentType || inc.incidentType || '',
            type: inc.IncidentType || inc.incidentType || '',
            description: inc.Description || inc.description || '',
            incidentDetails: inc.IncidentDetails || inc.incidentDetails || '',
            storeComments: inc.StoreComments || inc.storeComments || inc.SiteComments || inc.siteComments || '',
            totalValueRecovered: inc.TotalValueRecovered || inc.totalValueRecovered || inc.value || 0,
            value: inc.TotalValueRecovered || inc.totalValueRecovered || inc.value || 0,
            valueRecovered: inc.ValueRecovered || inc.valueRecovered || inc.TotalValueRecovered || 0,
            amount: inc.Amount || inc.amount || inc.TotalValueRecovered || 0,
            total: inc.Total || inc.total || inc.TotalValueRecovered || 0,
            stolenItems: inc.StolenItems || inc.stolenItems || [],
            policeInvolvement: inc.PoliceInvolvement || inc.policeInvolvement || false,
            urnNumber: inc.UrnNumber || inc.urnNumber || '',
            crimeRefNumber: inc.CrimeRefNumber || inc.crimeRefNumber || '',
            status: inc.Status || inc.status || 'pending',
            priority: inc.Priority || inc.priority || 'medium',
            actionTaken: inc.ActionTaken || inc.actionTaken || '',
            evidenceAttached: inc.EvidenceAttached || inc.evidenceAttached || false,
            witnessStatements: inc.WitnessStatements || inc.witnessStatements || [],
            reportNumber: inc.ReportNumber || inc.reportNumber || '',
            offenderName: inc.OffenderName || inc.offenderName || '',
            offenderSex: inc.OffenderSex || inc.offenderSex || '',
            gender: inc.Gender || inc.gender || inc.OffenderSex || '',
            offenderDOB: inc.OffenderDOB || inc.offenderDOB || '',
            offenderMarks: inc.OffenderMarks || inc.offenderMarks || '',
            offenderAddress: inc.OffenderAddress || inc.offenderAddress || undefined,
            dateInputted: inc.DateInputted || inc.dateInputted || '',
          };
        });

        if (isActive) {
          setLoadedIncidents(transformedIncidents);
          console.log('✅ Loaded incidents:', transformedIncidents.length);
          
          if (transformedIncidents.length > 0) {
            // Log sample incident with value and status
            console.log('📋 Sample incident:', {
              id: transformedIncidents[0].id,
              siteName: transformedIncidents[0].siteName,
              date: transformedIncidents[0].dateOfIncident,
              value: transformedIncidents[0].totalValueRecovered || transformedIncidents[0].value || 0,
              status: transformedIncidents[0].status,
              priority: transformedIncidents[0].priority
            });
            
            // Calculate and log key metrics
            const totalValue = transformedIncidents.reduce((sum, inc) => sum + (inc.totalValueRecovered || inc.value || 0), 0);
            const resolved = transformedIncidents.filter(inc => inc.status === 'resolved').length;
            console.log('💰 Total value recovered:', totalValue);
            console.log('✅ Resolved incidents:', resolved, '/', transformedIncidents.length);
          }
        }
      } catch (error) {
        if (error instanceof Error && error.name === 'AbortError') {
          console.log('⏹️ Load incidents aborted');
          return; // Ignore abort errors
        }
        
        console.error('❌ Failed to load incidents:', error);
        console.error('❌ Error details:', {
          message: error instanceof Error ? error.message : String(error),
          stack: error instanceof Error ? error.stack : undefined
        });
        
        // Check if it's an auth error
        if (error instanceof Error && (error.message.includes('401') || (error as any).response?.status === 401)) {
          console.error('🔐 Authentication error - The backend /api/incidents endpoint returned 401');
          console.error('💡 Possible causes:');
          console.error('   1. Token is expired - try logging out and back in');
          console.error('   2. Backend requires different permissions for incidents endpoint');
          console.error('   3. Backend /api/incidents endpoint may not be configured yet');
        }
        
        // Set empty array on error - dashboard will use analyticsData as fallback
        if (isActive) {
          setLoadedIncidents([]);
          console.log('ℹ️ Dashboard will display analytics data as fallback');
        }
      } finally {
        if (isActive) {
          setIncidentsLoading(false);
        }
      }
    };

    console.log('📞 Calling loadIncidents()');
    loadIncidents();

    return () => {
      isActive = false;
      abortController.abort();
    };
  }, []); // Load once on mount

  // Load analytics data once for admin overview (reuse same source as Data Analytics Hub)
  React.useEffect(() => {
    const loadAnalytics = async () => {
      try {
        setAnalyticsLoading(true);
        setAnalyticsError(null);

        // Use real regions/sites from dashboard service to drive analytics mock generation
        const [regions, sites] = await Promise.all([
          customerDashboardService.getRegions(),
          customerDashboardService.getSites(),
        ]);

        const storeOptions = sites.map((site: any) => ({
          id: site.siteID || site.SiteID || site.id,
          name:
            site.locationName ||
            site.LocationName ||
            site.name ||
            `Store ${site.siteID || site.SiteID || site.id}`,
        }));

        // Map backend regions to simple options and filter to Central England COOP only
        const rawRegionOpts = regions.map((region: any) => ({
          id: region.id,
          name: region.name,
          customerId: region.customerId || region.CustomerId || region.fkCustomerID || region.FkCustomerID,
        }));

        // Filter to ONLY Central England COOP regions (customer ID 1)
        const centralEnglandRegions = rawRegionOpts.filter((r) => {
          const customerId = r.customerId;
          return customerId === 1 || customerId === '1';
        });

        // Additional filtering: Exclude non-region items
        const blockedRegionNames = ['Store Detective', 'Store Detectives', 'Retail', 'Eastbrook'];
        const regionOpts = centralEnglandRegions.filter(
          (r) => !blockedRegionNames.includes(r.name?.trim())
        );

        console.log('🏢 Central England COOP regions loaded:', regionOpts.length);
        console.log('📍 Region names:', regionOpts.map(r => r.name).join(', '));

        // Store region options for the dropdown
        setRegionOptions(regionOpts);

        const data = await analyticsService.getAnalyticsHub({
          stores: storeOptions,
          regions: regionOpts,
        });

        setAnalyticsData(data);
      } catch (err) {
        console.error('Failed to load admin analytics overview:', err);
        setAnalyticsError(
          err instanceof Error ? err.message : 'Failed to load analytics'
        );
      } finally {
        setAnalyticsLoading(false);
      }
    };

    loadAnalytics();
  }, []);

  // Get regions for dropdown – use ONLY real regions from API for the current customer
  const regions = React.useMemo(() => {
    return regionOptions;
  }, [regionOptions]);

  // Filter incidents by selected region
  const filteredIncidents = React.useMemo(() => {
    if (selectedRegion === 'all') {
      return loadedIncidents
    }
    return loadedIncidents.filter(inc => inc.regionId === selectedRegion)
  }, [selectedRegion, loadedIncidents])

  // Use filtered incidents for all calculations
  const customerMetrics = React.useMemo(() => {
    const todayIncidents = filteredIncidents.filter(inc => {
      const incDate = new Date(inc.dateOfIncident)
      const today = new Date()
      return incDate.toDateString() === today.toDateString()
    }).length
    const highPriority = filteredIncidents.filter(inc => inc.priority === 'high').length
    const totalValue = filteredIncidents.reduce((sum, inc) => sum + (inc.totalValueRecovered || inc.value || 0), 0)
    const pending = filteredIncidents.filter(inc => inc.status === 'pending').length
    const resolved = filteredIncidents.filter(inc => inc.status === 'resolved').length
    
    // Count incidents involving theft (case-insensitive check in incident type)
    const theftIncidents = filteredIncidents.filter(inc => {
      const type = (inc.incidentType || '').toLowerCase()
      return type.includes('theft') || type.includes('stolen') || type.includes('shoplifting')
    }).length
    
    // Calculate theft percentage
    const theftPercentage = filteredIncidents.length > 0 
      ? Math.round((theftIncidents / filteredIncidents.length) * 100) 
      : 0

    return {
      totalIncidents: filteredIncidents.length,
      todayIncidents,
      highPriority,
      totalValue,
      pending,
      resolved,
      theftIncidents,
      theftPercentage
    }
  }, [filteredIncidents])

  // Get recent incidents for table – use real backend data
  const recentIncidents = React.useMemo(() => {
    // Primary: use real loaded incidents from backend
    if (filteredIncidents.length > 0) {
      return filteredIncidents
        .slice()
        .sort((a, b) => new Date(b.dateOfIncident).getTime() - new Date(a.dateOfIncident).getTime())
        .slice(0, 10)
        .map(incident => ({
          id: incident.id,
          customerName: incident.customerName,
          store: incident.siteName || incident.store || 'N/A',
          siteName: incident.siteName || 'N/A',
          officerName: incident.officerName,
          date: incident.dateOfIncident,
          amount: incident.totalValueRecovered || incident.value || 0,
          incidentType: incident.incidentType
        }))
    }

    // Fallback: synthesize from analytics hub data if no real incidents loaded yet
    if (analyticsData) {
      const endDate = new Date(analyticsData.metadata.dateRange.end)
      const stores = analyticsData.hotProducts.storeHeatmap
        .slice()
        .sort((a, b) => b.totalIncidents - a.totalIncidents)
      const incidents: {
        id: string
        customerName: string
        store: string
        siteName: string
        officerName: string
        date: string
        amount: number
        incidentType: string
      }[] = []

      stores.forEach((store, storeIndex) => {
        store.products
          .slice()
          .sort((a, b) => b.frequency - a.frequency)
          .slice(0, 3)
          .forEach((product, productIndex) => {
            const dayOffset = storeIndex * 3 + productIndex
            const date = new Date(endDate)
            date.setDate(endDate.getDate() - dayOffset)

            incidents.push({
              id: `ANA-${store.storeId}-${product.barcode}-${productIndex}`,
              customerName: 'Central England COOP',
              store: store.storeName,
              siteName: store.storeName,
              officerName: productIndex % 2 === 0 ? 'Uniform Officer' : 'Store Detective',
              date: date.toISOString(),
              amount: Math.round(product.value * product.frequency),
              incidentType: `Theft – ${product.productName}`
            })
          })
      })

      return incidents.slice(0, 10)
    }

    return []
  }, [filteredIncidents, analyticsData])

  // Get priority cases – use real backend data
  const priorityCases = React.useMemo(() => {
    // Primary: use real loaded incidents from backend
    if (filteredIncidents.length > 0) {
      return filteredIncidents
        .filter(incident => incident.priority === 'high')
        .sort((a, b) => new Date(b.dateOfIncident).getTime() - new Date(a.dateOfIncident).getTime())
        .slice(0, 5)
        .map(incident => ({
          id: incident.id,
          customerName: incident.customerName,
          siteName: incident.siteName || incident.store || 'N/A',
          incidentType: incident.incidentType,
          date: incident.dateOfIncident,
          priority: incident.priority || 'high',
          status: incident.status || 'pending',
          description: incident.description || 'No description available'
        }))
    }

    // Fallback: synthesize from analytics data if no real incidents loaded yet
    if (analyticsData) {
      const stores = analyticsData.hotProducts.storeHeatmap
        .slice()
        .sort((a, b) => b.totalIncidents - a.totalIncidents)
      const cases: {
        id: string
        customerName: string
        siteName: string
        incidentType: string
        date: string
        priority: 'high' | 'medium'
        status: 'resolved' | 'pending'
        description: string
      }[] = []

      const endDate = new Date(analyticsData.metadata.dateRange.end)

      stores.forEach((store, idx) => {
        if (cases.length >= 5) {
          return
        }
        const date = new Date(endDate)
        date.setDate(endDate.getDate() - idx)

        const topProduct = store.products
          .slice()
          .sort((a, b) => b.frequency - a.frequency)[0]

        const priority =
          store.riskLevel === 'critical' || store.riskLevel === 'high'
            ? 'high'
            : 'medium'

        cases.push({
          id: `CASE-${store.storeId}`,
          customerName: 'Central England COOP',
          siteName: store.storeName,
          incidentType: topProduct
            ? `High loss risk – ${topProduct.productName}`
            : 'High incident volume',
          date: date.toISOString(),
          priority,
          status: priority === 'high' ? 'pending' : 'resolved',
          description: `Store classified as ${store.riskLevel.toUpperCase()} risk with ${store.totalIncidents} incidents in the selected period.`
        })
      })

      return cases.slice(0, 5)
    }

    return []
  }, [filteredIncidents, analyticsData])

  // Generate alerts data
  const alerts = React.useMemo(() => {
    return [
      {
        id: '1',
        type: 'warning' as const,
        title: 'High Priority Incident Reported',
        message: 'New high-priority incident at Leicester City Centre requires immediate attention',
        time: '15 minutes ago',
        priority: 'high' as const
      },
      {
        id: '2',
        type: 'info' as const,
        title: 'Alert Rule Triggered',
        message: 'Bulk theft alert rule matched for Nottingham Victoria store',
        time: '1 hour ago',
        priority: 'medium' as const
      },
      {
        id: '3',
        type: 'error' as const,
        title: 'Police Involvement Required',
        message: 'Incident INC-000142 requires police assistance - URN assigned',
        time: '2 hours ago',
        priority: 'high' as const
      },
      {
        id: '4',
        type: 'warning' as const,
        title: 'Repeat Offender Detected',
        message: 'Known repeat offender identified at Birmingham Bull Ring',
        time: '3 hours ago',
        priority: 'medium' as const
      }
    ]
  }, [])

  // Use real backend data for quick statistics
  const quickStats = React.useMemo(() => {
    // Always prefer real backend metrics
    return customerMetrics;
  }, [customerMetrics])

  // Generate heatmap data (store vs incident count) using real backend data
  const heatmapData = React.useMemo(() => {
    // Primary: derive from real loaded incidents
    if (filteredIncidents.length > 0) {
      const storeMap = new Map<string, number>()
      
      filteredIncidents.forEach(incident => {
        const store = incident.siteName || 'Unknown'
        storeMap.set(store, (storeMap.get(store) || 0) + 1)
      })

      const topStores = Array.from(storeMap.entries())
        .sort((a, b) => b[1] - a[1])
        .slice(0, 6)
        .map(([store, count]) => ({ store, incidents: count }))

      return topStores
    }

    // Fallback: use analytics hub data if no real incidents loaded yet
    if (analyticsData && analyticsData.hotProducts.storeHeatmap.length > 0) {
      return analyticsData.hotProducts.storeHeatmap
        .map(store => ({
          store: store.storeName,
          incidents: store.totalIncidents
        }))
        .sort((a, b) => b.incidents - a.incidents)
        .slice(0, 6)
    }

    return []
  }, [filteredIncidents, analyticsData])

  // Log region selection and filtered data for debugging
  React.useEffect(() => {
    const selectedRegionName = selectedRegion === 'all' ? 'All Regions' : regions.find(r => r.id === selectedRegion)?.name || selectedRegion;
    
    console.log('🗺️ Selected region:', selectedRegionName, `(ID: ${selectedRegion})`);
    console.log('📊 Total loaded incidents:', loadedIncidents.length);
    console.log('📊 Filtered incidents count:', filteredIncidents.length);
    console.log('📅 Active time period:', activePeriod);
    
    if (loadedIncidents.length > 0 && filteredIncidents.length === 0 && selectedRegion !== 'all') {
      console.warn('⚠️ No incidents found for selected region:', selectedRegionName);
      console.log('📋 Available regions in loaded data:', 
        [...new Set(loadedIncidents.map(inc => `${inc.regionName} (ID: ${inc.regionId})`))].slice(0, 10).join(', ')
      );
    }
  }, [selectedRegion, filteredIncidents, activePeriod, regions, loadedIncidents]);

  // Generate chart data from filtered incidents based on active period and selected region
  const chartData = React.useMemo(() => {
    console.log('🔍 [Chart Data] Generating chart data');
    console.log('📊 [Chart Data] Filtered incidents count:', filteredIncidents.length);
    console.log('📅 [Chart Data] Active period:', activePeriod);
    
    if (filteredIncidents.length > 0) {
      const sample = filteredIncidents[0];
      console.log('📋 [Chart Data] Sample incident:', {
        date: sample.dateOfIncident,
        officerRole: sample.officerRole,
        officerType: sample.officerType,
        id: sample.id
      });
    }
    
    // Use filtered incidents to respect region selection
    const incidentsByPeriod = new Map<string, { uniformOfficers: number; storeDetectives: number }>()
    
    // Helper to generate proper time period keys and data
    const generateTimeData = () => {
      switch (activePeriod) {
        case 'Daily': {
          // Last 7 days
          const data: Array<{ date: string; uniformOfficers: number; storeDetectives: number }> = []
          const today = new Date()
          
          for (let i = 6; i >= 0; i--) {
            const targetDate = new Date(today)
            targetDate.setDate(today.getDate() - i)
            targetDate.setHours(0, 0, 0, 0)
            
            const dayEnd = new Date(targetDate)
            dayEnd.setHours(23, 59, 59, 999)
            
            const dayKey = format(targetDate, 'EEE') // Mon, Tue, etc.
            
            const dayIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              return incDate >= targetDate && incDate <= dayEnd
            })
            
            const uniformCount = dayIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type === 'uniform' || 
                     role.includes('advantageone') || 
                     role.includes('uniform') ||
                     type.includes('uniform');
            }).length
            const detectiveCount = dayIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type.includes('detective') || 
                     role.includes('detective') ||
                     type === 'store detective';
            }).length
            
            // Handle uncategorized incidents - split evenly
            const uncategorized = dayIncidents.length - uniformCount - detectiveCount;
            const halfUncategorized = Math.floor(uncategorized / 2);
            
            data.push({
              date: dayKey,
              uniformOfficers: uniformCount + halfUncategorized,
              storeDetectives: detectiveCount + (uncategorized - halfUncategorized)
            })
          }
          return data
        }
        
        case 'Weekly': {
          // Last 4 weeks
          const data: Array<{ week: string; uniformOfficers: number; storeDetectives: number }> = []
          const today = new Date()
          
          for (let i = 3; i >= 0; i--) {
            const weekStart = new Date(today)
            weekStart.setDate(today.getDate() - (i * 7 + 6))
            weekStart.setHours(0, 0, 0, 0)
            
            const weekEnd = new Date(weekStart)
            weekEnd.setDate(weekStart.getDate() + 6)
            weekEnd.setHours(23, 59, 59, 999)
            
            const weekIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              return incDate >= weekStart && incDate <= weekEnd
            })
            
            const uniformCount = weekIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type === 'uniform' || 
                     role.includes('advantageone') || 
                     role.includes('uniform') ||
                     type.includes('uniform');
            }).length
            const detectiveCount = weekIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type.includes('detective') || 
                     role.includes('detective') ||
                     type === 'store detective';
            }).length
            
            // Handle uncategorized incidents - split evenly
            const uncategorized = weekIncidents.length - uniformCount - detectiveCount;
            const halfUncategorized = Math.floor(uncategorized / 2);
            
            data.push({
              week: `Week ${4 - i}`,
              uniformOfficers: uniformCount + halfUncategorized,
              storeDetectives: detectiveCount + (uncategorized - halfUncategorized)
            })
          }
          return data
        }
        
        case 'Monthly': {
          // Last 12 months
          const data: Array<{ month: string; uniformOfficers: number; storeDetectives: number }> = []
          const today = new Date()
          const monthOrder = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
          
          for (let i = 11; i >= 0; i--) {
            const monthDate = new Date(today.getFullYear(), today.getMonth() - i, 1)
            const monthKey = format(monthDate, 'MMM')
            
            const monthIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              return incDate.getMonth() === monthDate.getMonth() &&
                     incDate.getFullYear() === monthDate.getFullYear()
            })
            
            const uniformCount = monthIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type === 'uniform' || 
                     role.includes('advantageone') || 
                     role.includes('uniform') ||
                     type.includes('uniform');
            }).length
            const detectiveCount = monthIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type.includes('detective') || 
                     role.includes('detective') ||
                     type === 'store detective';
            }).length
            
            // Handle uncategorized incidents - split evenly
            const uncategorized = monthIncidents.length - uniformCount - detectiveCount;
            const halfUncategorized = Math.floor(uncategorized / 2);
            
            data.push({
              month: monthKey,
              uniformOfficers: uniformCount + halfUncategorized,
              storeDetectives: detectiveCount + (uncategorized - halfUncategorized)
            })
          }
          return data
        }
        
        case 'Yearly': {
          // Last 5 years
          const data: Array<{ year: string; uniformOfficers: number; storeDetectives: number }> = []
          const currentYear = new Date().getFullYear()
          
          console.log('📅 [Yearly Chart] Current year:', currentYear);
          console.log('📅 [Yearly Chart] Total incidents to process:', filteredIncidents.length);
          
          for (let i = 4; i >= 0; i--) {
            const year = currentYear - i
            
            const yearIncidents = filteredIncidents.filter(incident => {
              const incDate = new Date(incident.dateOfIncident)
              const incYear = incDate.getFullYear()
              return incYear === year && !isNaN(incYear)
            })
            
            console.log(`📅 [Yearly Chart] Year ${year}: ${yearIncidents.length} incidents`);
            
            // If no officer type/role, split evenly as fallback
            const uniformCount = yearIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type === 'uniform' || 
                     role.includes('advantageone') || 
                     role.includes('uniform') ||
                     type.includes('uniform');
            }).length
            const detectiveCount = yearIncidents.filter(inc => {
              const role = (inc.officerRole || '').toLowerCase();
              const type = (inc.officerType || '').toLowerCase();
              return type.includes('detective') || 
                     role.includes('detective') ||
                     type === 'store detective';
            }).length
            
            // Handle incidents without clear officer type - split evenly
            const uncategorized = yearIncidents.length - uniformCount - detectiveCount;
            const halfUncategorized = Math.floor(uncategorized / 2);
            
            if (uncategorized > 0) {
              console.log(`⚠️ [Yearly Chart] Year ${year}: ${uncategorized} uncategorized incidents - splitting evenly`);
            }
            
            const finalUniformCount = uniformCount + halfUncategorized;
            const finalDetectiveCount = detectiveCount + (uncategorized - halfUncategorized);
            
            console.log(`📅 [Yearly Chart] Year ${year}: ${finalUniformCount} uniform, ${finalDetectiveCount} detective`);
            
            data.push({
              year: year.toString(),
              uniformOfficers: finalUniformCount,
              storeDetectives: finalDetectiveCount
            })
          }
          return data
        }
        
        default:
          return []
      }
    }
    
    const result = generateTimeData();
    console.log('📈 [Chart Data] Generated data points:', result.length);
    console.log('📈 [Chart Data] Sample data:', result.slice(0, 3));
    
    return result;
  }, [filteredIncidents, activePeriod, selectedRegion])

  // Get x-axis key based on active period
  const dataKey = activePeriod === 'Daily' ? 'date' : activePeriod === 'Weekly' ? 'week' : activePeriod === 'Monthly' ? 'month' : 'year'

  // Define background colors for each stat card based on type
  const getStatCardColor = (color: string) => {
    switch(color) {
      case 'green': return 'bg-emerald-800';
      case 'yellow': return 'bg-amber-800';
      case 'red': return 'bg-rose-800';
      case 'blue': return 'bg-blue-800';
      default: return 'bg-slate-800';
    }
  };

  // Show loading state while page access data is being loaded
  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="space-y-4 text-center">
          <div className="text-lg font-medium">Loading Dashboard...</div>
          <div className="text-sm text-gray-500">Please wait</div>
        </div>
      </div>
    );
  }

  // Show appropriate dashboard based on role (after all hooks have been called)
  if (effectiveRole === 'advantageoneofficer' || effectiveRole === 'advantageonehoofficer') {
    return (
      <Suspense fallback={
        <div className="flex items-center justify-center min-h-screen">
          <div className="space-y-4 text-center">
            <div className="text-lg font-medium">Loading Officer Dashboard...</div>
            <div className="text-sm text-gray-500">Please wait</div>
          </div>
        </div>
      }>
        <OfficerDashboard />
      </Suspense>
    )
  } else if (effectiveRole === 'customersitemanager' || effectiveRole === 'customerhomanager') {
    return (
      <Suspense fallback={
        <div className="flex items-center justify-center min-h-screen">
          <div className="space-y-4 text-center">
            <div className="text-lg font-medium">Loading Customer Dashboard...</div>
            <div className="text-sm text-gray-500">Please wait</div>
          </div>
        </div>
      }>
        <CustomerDashboard userRole={effectiveRole === 'customersitemanager' ? 'customersitemanager' : 'customerhomanager'} />
      </Suspense>
    )
  }

  // Admin dashboard UI (only reached if role is administrator)
  return (
    <div className="min-h-screen bg-slate-50 p-6">
      <div className="space-y-6">
        <DashboardGreeting />
        
        {/* Loading State */}
        {incidentsLoading && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 flex items-center gap-3">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
            <p className="text-sm text-blue-800">Loading incident data...</p>
          </div>
        )}
        
        {/* Region Selection */}
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-2 md:gap-4">
          <div className="flex items-center gap-2 flex-wrap">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Dashboard Overview</h2>
            <Badge variant="secondary" className="text-xs">Admin View</Badge>
            {selectedRegion !== 'all' && (
              <Badge variant="default" className="text-xs bg-blue-600">
                <MapPin className="h-3 w-3 mr-1" />
                {regions.find(r => r.id === selectedRegion)?.name || 'Region Filter Active'}
              </Badge>
            )}
          </div>
          
          <div className="flex flex-col sm:flex-row gap-2 w-full md:w-auto">
            <Select value={selectedRegion} onValueChange={setSelectedRegion}>
              <SelectTrigger className="w-full text-sm md:w-[200px]">
                <SelectValue placeholder="Select region" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Regions</SelectItem>
                {regions.map((region) => (
                  <SelectItem key={region.id} value={region.id}>
                    {region.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Quick Statistics */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 md:gap-4">
          {/* Total Incidents */}
          <Card className="min-w-[140px] bg-blue-600 text-white border-0 shadow-md overflow-hidden relative">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 p-3 md:p-4 pb-2 md:pb-3">
              <CardTitle className="text-xs font-medium md:text-sm text-white">
                Total Incidents
              </CardTitle>
              <div className="h-8 w-8 rounded-full bg-white/20 flex items-center justify-center">
                <AlertCircle className="h-4 w-4 text-white" />
              </div>
            </CardHeader>
            <CardContent className="p-3 md:p-4 pt-1 md:pt-2 z-10 relative">
              <div className="text-xl font-bold md:text-2xl lg:text-3xl text-white">{quickStats.totalIncidents}</div>
              <div className="text-xs text-white/60 mt-1">All time</div>
            </CardContent>
          </Card>

          {/* Today's Incidents */}
          <Card className="min-w-[140px] bg-emerald-600 text-white border-0 shadow-md overflow-hidden relative">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 p-3 md:p-4 pb-2 md:pb-3">
              <CardTitle className="text-xs font-medium md:text-sm text-white">
                Today
              </CardTitle>
              <div className="h-8 w-8 rounded-full bg-white/20 flex items-center justify-center">
                <Calendar className="h-4 w-4 text-white" />
              </div>
            </CardHeader>
            <CardContent className="p-3 md:p-4 pt-1 md:pt-2 z-10 relative">
              <div className="text-xl font-bold md:text-2xl lg:text-3xl text-white">{quickStats.todayIncidents}</div>
              <div className="text-xs text-white/60 mt-1">Incidents today</div>
            </CardContent>
          </Card>

          {/* Total Value Recovered */}
          <Card className="min-w-[140px] bg-amber-600 text-white border-0 shadow-md overflow-hidden relative">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 p-3 md:p-4 pb-2 md:pb-3">
              <CardTitle className="text-xs font-medium md:text-sm text-white">
                Value Recovered
              </CardTitle>
              <div className="h-8 w-8 rounded-full bg-white/20 flex items-center justify-center">
                <Currency className="h-4 w-4 text-white" />
              </div>
            </CardHeader>
            <CardContent className="p-3 md:p-4 pt-1 md:pt-2 z-10 relative">
              <div className="text-xl font-bold md:text-2xl lg:text-3xl text-white">
                {formatCurrency(quickStats.totalValue)}
              </div>
              <div className="text-xs text-white/60 mt-1">Total recovered</div>
            </CardContent>
          </Card>

          {/* Theft Incidents */}
          <Card className="min-w-[140px] bg-red-600 text-white border-0 shadow-md overflow-hidden relative">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 p-3 md:p-4 pb-2 md:pb-3">
              <CardTitle className="text-xs font-medium md:text-sm text-white">
                Theft Incidents
              </CardTitle>
              <div className="h-8 w-8 rounded-full bg-white/20 flex items-center justify-center">
                <Shield className="h-4 w-4 text-white" />
              </div>
            </CardHeader>
            <CardContent className="p-3 md:p-4 pt-1 md:pt-2 z-10 relative">
              <div className="text-xl font-bold md:text-2xl lg:text-3xl text-white">{quickStats.theftIncidents}</div>
              <div className="text-xs text-white/60 mt-1">
                {quickStats.theftPercentage}% of all incidents
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Main Content Grid */}
        <div className="grid gap-4 grid-cols-1 lg:grid-cols-7">
          {/* Tasks and Incidents Section */}
          <div className="lg:col-span-5 space-y-4">
            {/* Incident Reports Chart with time period filters */}
            <Card>
              <CardHeader className="p-2 md:p-4 flex flex-col sm:flex-row justify-between items-start sm:items-center space-y-2 sm:space-y-0">
                <div className="flex flex-col gap-1">
                  <CardTitle className="text-base font-medium md:text-lg lg:text-xl">Incident Reports</CardTitle>
                  <p className="text-xs text-muted-foreground">
                    {activePeriod === 'Daily' ? 'Last 7 days' : 
                     activePeriod === 'Weekly' ? 'Last 4 weeks' : 
                     activePeriod === 'Monthly' ? 'Last 12 months' : 
                     'Last 5 years'}
                    {selectedRegion !== 'all' && ` • ${regions.find(r => r.id === selectedRegion)?.name || 'Region Filter'}`}
                  </p>
                </div>
                <div className="flex items-center space-x-1">
                  <div className="bg-gray-100 rounded-lg p-0.5 flex text-xs md:text-sm">
                    {["Daily", "Weekly", "Monthly", "Yearly"].map((period, index) => (
                      <button
                        key={period}
                        className={`px-3 py-1 rounded-md transition-colors ${
                          period === activePeriod 
                            ? "bg-white shadow-sm text-emerald-500 font-medium" 
                            : "text-gray-500 hover:text-gray-700"
                        }`}
                        onClick={() => setActivePeriod(period as 'Daily' | 'Weekly' | 'Monthly' | 'Yearly')}
                      >
                        {period}
                      </button>
                    ))}
                  </div>
                </div>
              </CardHeader>
              <CardContent className="h-[200px] md:h-[280px] lg:h-[320px] p-2 md:p-4">
                {(chartData.length === 0 || chartData.every(d => (d.uniformOfficers || 0) + (d.storeDetectives || 0) === 0)) && !incidentsLoading ? (
                  <div className="flex items-center justify-center h-full">
                    <div className="text-center space-y-2">
                      <AlertCircle className="h-12 w-12 text-gray-400 mx-auto" />
                      <p className="text-sm font-medium text-gray-600">No incidents found</p>
                      <p className="text-xs text-gray-500">
                        {selectedRegion === 'all' 
                          ? `No incident data available for the selected time period (${activePeriod})` 
                          : `No incidents found for ${regions.find(r => r.id === selectedRegion)?.name || 'this region'} in the selected time period`}
                      </p>
                      {selectedRegion !== 'all' && (
                        <Button 
                          variant="outline" 
                          size="sm" 
                          onClick={() => setSelectedRegion('all')}
                          className="mt-2"
                        >
                          View All Regions
                        </Button>
                      )}
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="flex items-center justify-end mb-2 space-x-4">
                      <div className="flex items-center">
                        <span className="w-3 h-3 rounded-full bg-emerald-500 inline-block mr-1"></span>
                        <span className="text-xs text-gray-500">Uniform Officers</span>
                      </div>
                      <div className="flex items-center">
                        <span className="w-3 h-3 rounded-full bg-amber-400 inline-block mr-1"></span>
                        <span className="text-xs text-gray-500">Store Detectives</span>
                      </div>
                    </div>
                    <ResponsiveContainer width="100%" height="100%">
                  <AreaChart 
                    data={chartData} 
                    margin={{ top: 5, right: 5, left: -15, bottom: 5 }}
                  >
                    <defs>
                      <linearGradient id="colorUniformOfficers" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#10b981" stopOpacity={0.8}/>
                        <stop offset="95%" stopColor="#10b981" stopOpacity={0}/>
                      </linearGradient>
                      <linearGradient id="colorStoreDetectives" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#F59E0B" stopOpacity={0.8}/>
                        <stop offset="95%" stopColor="#F59E0B" stopOpacity={0}/>
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                    <XAxis 
                      dataKey={dataKey} 
                      tick={{ fontSize: 10, fill: '#6B7280' }} 
                      axisLine={{ stroke: '#E5E7EB' }}
                      tickLine={false}
                    />
                    <YAxis 
                      tick={{ fontSize: 10, fill: '#6B7280' }}
                      axisLine={false}
                      tickLine={false}
                      tickFormatter={(value) => `${value}`}
                    />
                    <Tooltip 
                      contentStyle={{ 
                        backgroundColor: 'white', 
                        borderRadius: '0.5rem', 
                        boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)', 
                        border: 'none', 
                        fontSize: '0.75rem' 
                      }}
                      itemStyle={{ padding: '2px 0' }}
                      formatter={(value) => [`${value}`, '']}
                      labelFormatter={(label) => `${label}`}
                    />
                    <Area
                      type="monotone"
                      name="Uniform Officers"
                      dataKey="uniformOfficers"
                      stroke="#10b981"
                      fillOpacity={1}
                      fill="url(#colorUniformOfficers)"
                      activeDot={{ r: 6, stroke: '#10b981', strokeWidth: 2, fill: 'white' }}
                      strokeWidth={2}
                    />
                    <Area
                      type="monotone"
                      name="Store Detectives"
                      dataKey="storeDetectives"
                      stroke="#F59E0B"
                      fillOpacity={1}
                      fill="url(#colorStoreDetectives)"
                      activeDot={{ r: 6, stroke: '#F59E0B', strokeWidth: 2, fill: 'white' }}
                      strokeWidth={2}
                    />
                  </AreaChart>
                </ResponsiveContainer>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Recent Incidents */}
            <Card>
              <CardHeader className="p-2 md:p-4 flex flex-row items-center justify-between">
                <CardTitle className="text-base font-medium md:text-lg lg:text-xl">Recent Incidents</CardTitle>
                <Button 
                  variant="ghost" 
                  size="sm" 
                  asChild 
                  className="text-xs h-8 px-2"
                >
                  <Link to="/operations/incident-report">
                    <ChevronRight className="h-3.5 w-3.5 mr-1" />
                    View All
                  </Link>
                </Button>
              </CardHeader>
              <CardContent className="p-0">
                <div className="px-2 pb-4 md:px-4 overflow-visible">
                  <IncidentTable data={recentIncidents} />
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar Content */}
          <div className="lg:col-span-2 space-y-4">
            {/* Alerts */}
            <Card>
              <CardHeader className="p-2 md:p-4 flex flex-row items-center justify-between">
                <CardTitle className="text-base font-medium md:text-lg lg:text-xl flex items-center gap-2">
                  <Bell className="h-4 w-4" />
                  Alerts
                </CardTitle>
                <Badge variant="destructive" className="text-xs">
                  {alerts.filter(a => a.priority === 'high').length} New
                </Badge>
              </CardHeader>
              <CardContent className="p-0">
                <div className="divide-y">
                  {alerts.map((alert) => (
                    <div 
                      key={alert.id} 
                      className={`p-3 hover:bg-muted/50 transition-colors ${
                        alert.priority === 'high' ? 'bg-red-50/50 dark:bg-red-950/20' : ''
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <div className={`flex-shrink-0 flex items-center justify-center w-8 h-8 rounded-full ${
                          alert.type === 'error' ? 'bg-red-500' :
                          alert.type === 'warning' ? 'bg-amber-500' :
                          'bg-blue-500'
                        } text-white`}>
                          <AlertCircle className="h-4 w-4" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex justify-between items-start mb-1">
                            <p className="text-sm font-medium text-foreground">{alert.title}</p>
                            <span className="text-xs text-muted-foreground whitespace-nowrap ml-2">{alert.time}</span>
                          </div>
                          <p className="text-xs text-muted-foreground">{alert.message}</p>
                          {alert.priority === 'high' && (
                            <Badge variant="destructive" className="mt-2 text-xs">High Priority</Badge>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Priority Cases */}
            <Card>
              <CardHeader className="p-2 md:p-4 flex flex-row items-center justify-between">
                <CardTitle className="text-base font-medium md:text-lg lg:text-xl flex items-center gap-2">
                  <Shield className="h-4 w-4" />
                  Priority Cases
                </CardTitle>
                <Badge variant="destructive" className="text-xs">
                  {priorityCases.length}
                </Badge>
              </CardHeader>
              <CardContent className="p-0">
                <div className="divide-y">
                  {priorityCases.length > 0 ? (
                    priorityCases.map((case_) => (
                      <div key={case_.id} className="p-3 hover:bg-muted/50 transition-colors">
                        <div className="flex items-start justify-between gap-2 mb-2">
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-foreground truncate">{case_.siteName}</p>
                            <p className="text-xs text-muted-foreground mt-0.5">{case_.customerName}</p>
                          </div>
                          <Badge variant="destructive" className="text-xs whitespace-nowrap">High</Badge>
                        </div>
                        <p className="text-xs font-medium text-foreground mb-1">{case_.incidentType}</p>
                        <p className="text-xs text-muted-foreground line-clamp-2">{case_.description}</p>
                        <div className="flex items-center justify-between mt-2">
                          <span className="text-xs text-muted-foreground">
                            {format(new Date(case_.date), 'MMM dd, yyyy')}
                          </span>
                          <Badge 
                            variant={case_.status === 'resolved' ? 'default' : 'secondary'} 
                            className="text-xs"
                          >
                            {case_.status}
                          </Badge>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="p-6 text-center text-sm text-muted-foreground">
                      No priority cases at this time
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Heatmap Preview */}
            <Card className="overflow-hidden">
              <CardHeader className="p-2 md:p-4 flex flex-row items-center justify-between">
                <CardTitle className="text-base font-medium md:text-lg lg:text-xl flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  Heatmap Preview
                </CardTitle>
                <Button 
                  variant="ghost" 
                  size="sm" 
                  asChild 
                  className="text-xs h-8 px-2"
                >
                  <Link to="/analytics/data-analytics-hub">
                    <ChevronRight className="h-3.5 w-3.5 mr-1" />
                    View Full
                  </Link>
                </Button>
              </CardHeader>
              <CardContent className="p-2 md:p-4">
                <div className="space-y-3">
                  <div className="text-xs text-muted-foreground mb-2">
                    Incident frequency by store (last 30 days)
                  </div>
                  <div className="h-[200px] overflow-auto">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={heatmapData} layout="vertical" margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis type="number" tick={{ fontSize: 10 }} />
                        <YAxis 
                          dataKey="store" 
                          type="category" 
                          width={120} 
                          tick={{ fontSize: 9 }} 
                          interval={0}
                        />
                        <Tooltip 
                          formatter={(value: number) => [`${value} incidents`, '']}
                          contentStyle={{ 
                            backgroundColor: 'white', 
                            borderRadius: '0.5rem', 
                            boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)', 
                            border: 'none', 
                            fontSize: '0.75rem' 
                          }}
                        />
                        <Bar dataKey="incidents" fill="#3b82f6" radius={[0, 4, 4, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                  <div className="text-xs text-muted-foreground text-center pt-2 border-t">
                    Showing top 6 stores. View full heatmap for complete analysis.
                  </div>
                </div>
              </CardContent>
            </Card>

          </div>
        </div>
      </div>
    </div>
  )
}

export default AdminDashboard