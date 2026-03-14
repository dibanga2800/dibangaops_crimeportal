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
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from 'recharts'
import { cn } from '@/lib/utils'
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { ScrollArea } from "@/components/ui/scroll-area";
import { usePageAccess } from "@/contexts/PageAccessContext"
import { DashboardGreeting } from '@/components/dashboard/DashboardGreeting'

// Lazy load the dashboard components
const OfficerDashboard = React.lazy(() => import('@/pages/Dashboard/OfficerDashboard'))
const AdminDashboard = React.lazy(() => import('@/pages/Dashboard/AdminDashboard'))

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

const Index = () => {
  const location = useLocation();
  const { currentRole, isTestMode, testRole, isLoading } = usePageAccess();
  const effectiveRole = isTestMode && testRole ? testRole : currentRole;

  // Debug log to help troubleshoot
  React.useEffect(() => {
    console.log('🏠 [Index] Component state:', {
      currentRole,
      isTestMode,
      testRole,
      effectiveRole,
      isLoading
    });
  }, [currentRole, isTestMode, testRole, effectiveRole, isLoading]);

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

  // Show appropriate dashboard based on role
  // Admin/Manager: management dashboard. Officer/Store: operational dashboard.
  if (effectiveRole === 'administrator' || effectiveRole === 'manager') {
    console.log('🏠 [Index] Rendering AdminDashboard for role:', effectiveRole);
    return (
      <Suspense fallback={
        <div className="flex items-center justify-center min-h-screen">
          <div className="space-y-4 text-center">
            <div className="text-lg font-medium">Loading Admin Dashboard...</div>
            <div className="text-sm text-gray-500">Please wait</div>
          </div>
        </div>
      }>
        <AdminDashboard viewRole={effectiveRole === 'manager' ? 'manager' : 'administrator'} />
      </Suspense>
    )
  } else if (effectiveRole === 'security-officer' || effectiveRole === 'store') {
    // Officer and store user get OfficerDashboard with user-specific data filtering
    console.log('🏠 [Index] Rendering OfficerDashboard for role:', effectiveRole);
    return (
      <Suspense fallback={
        <div className="flex items-center justify-center min-h-screen">
          <div className="space-y-4 text-center">
            <div className="text-lg font-medium">Loading Dashboard...</div>
            <div className="text-sm text-gray-500">Please wait</div>
          </div>
        </div>
      }>
        <OfficerDashboard />
      </Suspense>
    )
  }

  // Fallback to loading state if no role matched
  console.warn('🏠 [Index] No matching role found, showing fallback. Role:', effectiveRole);
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="space-y-4 text-center">
        <div className="text-lg font-medium">Loading...</div>
        <div className="text-sm text-gray-500">Please wait while we set up your dashboard</div>
        <div className="text-xs text-gray-400 mt-2">Current role: {effectiveRole || 'Not set'}</div>
      </div>
    </div>
  );
}

export default Index