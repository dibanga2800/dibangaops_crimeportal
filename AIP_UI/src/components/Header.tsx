import React, { useState } from "react"
import { useNavigate } from "react-router-dom"
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { LogOut, Settings, User, ChevronDown, Search, X } from "lucide-react"
import { PageAccessContext } from "@/contexts/PageAccessContext"
import { ThemeToggle } from "@/components/theme-toggle"
import { NotificationBell } from "./header/NotificationBell"
import { Logo } from "./header/Logo"
import { SearchInput } from "./header/SearchInput"
import { UserAvatar } from "./common/UserAvatar"
import { formatRole } from "./layout/SidebarUserCard"
import { useAuth } from "@/contexts/AuthContext"
import { useCustomerSelection } from "@/contexts/CustomerSelectionContext"
import { useAlertCount } from "@/hooks/useAlertCount"
import { setAlertCountCustomerScope } from "@/lib/alert-count/alert-count-store"


const UserProfileDropdown = ({ name, email, isAdministrator }: { name: string; email?: string; isAdministrator: boolean }) => {
	const navigate = useNavigate()
	const { logout } = useAuth()

	const handleLogout = () => {
		logout()
		navigate("/login")
	}

	return (
		<DropdownMenuContent align="end" className="w-60">
			<DropdownMenuLabel className="py-2">
				<span className="block truncate text-sm font-medium text-foreground">{name}</span>
				{email ? <span className="block truncate text-xs font-normal text-muted-foreground">{email}</span> : null}
			</DropdownMenuLabel>
			<DropdownMenuSeparator />
			<DropdownMenuItem onClick={() => navigate("/profile")}>
				<User className="mr-2 h-4 w-4" />
				<span>Profile</span>
			</DropdownMenuItem>
			{isAdministrator && (
				<DropdownMenuItem onClick={() => navigate("/settings")}>
					<Settings className="mr-2 h-4 w-4" />
					<span>Role access</span>
				</DropdownMenuItem>
			)}
			<DropdownMenuSeparator />
			<DropdownMenuItem onClick={handleLogout}>
				<LogOut className="mr-2 h-4 w-4" />
				<span>Sign out</span>
			</DropdownMenuItem>
		</DropdownMenuContent>
	)
}

const iconButton =
	"!h-9 !w-9 !p-0 text-muted-foreground hover:bg-muted hover:text-foreground"

/** Top bar. On phones, navigation lives in the bottom tab bar (see MobileTabBar). */
export function Header() {
	const [isSearchExpanded, setIsSearchExpanded] = useState(false)
	const pageAccessContext = React.useContext(PageAccessContext)
	const { user: authenticatedUser } = useAuth()
	const { selectedCustomerId } = useCustomerSelection()

	React.useEffect(() => {
		setAlertCountCustomerScope(selectedCustomerId)
	}, [selectedCustomerId])

	const { alertCount, isLoading: alertCountLoading } = useAlertCount()

	if (!pageAccessContext) {
		return (
			<header className="sticky top-0 z-30 h-16 border-b border-border bg-card">
				<div className="flex h-full items-center px-4 text-sm text-muted-foreground">Loading…</div>
			</header>
		)
	}

	const displayName = authenticatedUser
		? `${authenticatedUser.firstName ?? ""} ${authenticatedUser.lastName ?? ""}`.trim()
		: ""
	const roleLabel = formatRole(authenticatedUser?.role)
	const isAdministrator = authenticatedUser?.role?.toLowerCase() === "administrator"

	return (
		<header className="sticky top-0 z-30 w-full border-b border-border bg-card/90 backdrop-blur supports-[backdrop-filter]:bg-card/80">
			<div className="flex h-16 items-center gap-2 px-3 sm:px-6 lg:gap-4 lg:px-8">
				{/* Client brand */}
				<div className={`flex min-w-0 items-center ${isSearchExpanded ? "hidden sm:flex" : ""}`}>
					<Logo variant="desktop" />
				</div>

				{/* Search */}
				<div className="hidden flex-1 justify-center md:flex">
					<div className="w-full max-w-md">
						<SearchInput />
					</div>
				</div>
				<div className={`flex-1 md:hidden ${isSearchExpanded ? "" : "hidden"}`}>
					{isSearchExpanded && <SearchInput autoFocus />}
				</div>
				<div className={`flex-1 md:hidden ${isSearchExpanded ? "hidden" : ""}`} />

				{/* Actions */}
				<div className="flex shrink-0 items-center gap-1 sm:gap-1.5">
					<Button
						type="button"
						variant="ghost"
						size="icon"
						onClick={() => setIsSearchExpanded((v) => !v)}
						className={`${iconButton} md:hidden`}
						aria-label={isSearchExpanded ? "Close search" : "Search"}
						aria-expanded={isSearchExpanded}
					>
						{isSearchExpanded ? <X className="h-5 w-5" /> : <Search className="h-5 w-5" />}
					</Button>
					<ThemeToggle />
					<NotificationBell alertCount={alertCount} isLoading={alertCountLoading} alertCustomerId={selectedCustomerId} />

					<span aria-hidden className="mx-1 hidden h-6 w-px bg-border sm:block" />

					<DropdownMenu>
						<DropdownMenuTrigger asChild>
							<button
								type="button"
								className="!flex items-center gap-2.5 !rounded-md !px-1.5 !py-1 text-left hover:bg-muted"
								aria-label="Account menu"
							>
								<UserAvatar size="sm" />
								<span className="hidden min-w-0 leading-tight sm:block">
									<span className="block max-w-[11rem] truncate text-sm font-medium text-foreground">{displayName}</span>
									<span className="block max-w-[11rem] truncate text-xs font-normal text-muted-foreground">{roleLabel}</span>
								</span>
								<ChevronDown className="hidden h-4 w-4 text-muted-foreground sm:block" />
							</button>
						</DropdownMenuTrigger>
						<UserProfileDropdown name={displayName} email={authenticatedUser?.email} isAdministrator={isAdministrator} />
					</DropdownMenu>
				</div>
			</div>
		</header>
	)
}
