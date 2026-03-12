import {
	AlertTriangle,
	BadgeCheck,
	BarChart2,
	BookOpen,
	Calendar,
	CalendarRange,
	CheckSquare,
	DollarSign,
	FileQuestion,
	FileSearch,
	FileText,
	FileText as FileTextIcon,
	FileWarning,
	GitBranch,
	GraduationCap,
	HelpCircle,
	Key,
	LayoutDashboard,
	Shirt,
	ShieldCheck,
	TrendingUp,
	User,
	UserCog,
	UserPlus,
	UserCheck,
	Users as Users2,
	Wallet,
	Building,
	Cog,
	Brain,
	Bell,
} from 'lucide-react'

import type { LucideIcon } from 'lucide-react'

export interface SidebarGuardContext {
	hasAccess: (path: string) => boolean
	isCustomerRole: boolean
	isAdministrator: boolean
	isOfficerRole: boolean
	isStoreUser?: boolean
	isManager?: boolean
}

type GuardFn = (context: SidebarGuardContext) => boolean

export interface SidebarNavLink {
	path: string
	label: string
	icon: LucideIcon
	guard?: GuardFn
	bypassAccessCheck?: boolean
}

export interface SidebarSection {
	id: string
	label: string
	icon: LucideIcon
	links: SidebarNavLink[]
	guard?: GuardFn
	showCustomerSelector?: boolean
}

export const SIDEBAR_TOP_LINKS: SidebarNavLink[] = [
	{
		path: '/analytics/data-analytics-hub',
		label: 'Data Analytics Hub',
		icon: Brain,
	},
	{
		path: '/operations/alert-rules',
		label: 'Alert Rules',
		icon: Bell,
	},
]

export const SIDEBAR_SECTIONS: SidebarSection[] = [
	{
		id: 'administration',
		label: 'Administration',
		icon: UserCog,
		// Remove section-level guard - let individual links be controlled by page access settings
		// This allows CustomerHOManager to see User Setup if granted in settings
		guard: undefined,
		links: [
			{
				path: '/administration/user-setup',
				label: 'User Setup',
				icon: User,
			},
			{
				path: '/administration/employee-registration',
				label: 'Employee Registration',
				icon: UserPlus,
			},
			{
				path: '/administration/customer-setup',
				label: 'Company Setup',
				icon: Building,
			},
		],
	},
	{
		id: 'operations',
		label: 'Operations',
		icon: UserCheck,
		links: [
			{
				path: '/operations/incident-report',
				label: 'Incident Report',
				icon: AlertTriangle,
			},
			{
				path: '/operations/incident-graph',
				label: 'Incident Graph',
				icon: BarChart2,
			},
			{
				path: '/operations/crime-intelligence',
				label: 'Crime Intelligence',
				icon: TrendingUp,
			},
		],
	},
]

