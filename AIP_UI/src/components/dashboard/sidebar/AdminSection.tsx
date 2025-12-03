import { Link } from "react-router-dom"
import {
  Users,
  Database,
  FileText,
  UserPlus,
  Settings,
} from "lucide-react"
import {
  SidebarMenuButton,
} from "@/components/ui/sidebar"

export function AdminSection() {
  return (
    <div className="pl-4 space-y-2">
      <SidebarMenuButton asChild>
        <Link to="/administration/user-setup">
          <Users className="w-4 h-4" />
          <span>User Management</span>
        </Link>
      </SidebarMenuButton>
      <SidebarMenuButton asChild>
        <Link to="/administration/employee-registration">
          <UserPlus className="w-4 h-4" />
          <span>Employee Registration</span>
        </Link>
      </SidebarMenuButton>
      <SidebarMenuButton asChild>
        <Link to="/administration/customer-setup">
          <Database className="w-4 h-4" />
          <span>Customer Setup</span>
        </Link>
      </SidebarMenuButton>
    </div>
  )
}