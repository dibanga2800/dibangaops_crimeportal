import { Bell } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { toast } from '@/components/ui/use-toast'
import { dismissAllNotificationsFromServer } from '@/lib/notifications/dismissed-notifications'

interface NotificationBellProps {
	className?: string
	alertCount: number
	isLoading: boolean
	/** Same customer scope as dashboard summary (null = all customers). */
	alertCustomerId?: number | null
}

export const NotificationBell = ({
	className = '',
	alertCount,
	isLoading,
	alertCustomerId = null,
}: NotificationBellProps) => {
	const showBadge = alertCount > 0

	const handleClearNotifications = () => {
		void dismissAllNotificationsFromServer({
			customerId: alertCustomerId ?? undefined,
		}).then(() => {
			toast({
				title: 'Notifications cleared',
				description:
					'These alerts are hidden on your devices until new alerts arrive.',
			})
		})
	}

	return (
		<DropdownMenu>
			<DropdownMenuTrigger asChild>
				<Button
					type="button"
					variant="ghost"
					size="icon"
					className={`relative shrink-0 p-1 h-9 w-9 rounded-md ${className}`}
					aria-label={`Notifications menu — ${alertCount} ${alertCount === 1 ? 'alert' : 'alerts'}`}
					aria-haspopup="menu"
				>
					<Bell className="h-5 w-5" />
					{showBadge && !isLoading && (
						<span className="absolute -top-0.5 -right-0.5 min-w-[20px] h-5 px-1 rounded-full bg-red-500 flex items-center justify-center text-[11px] font-medium text-white">
							{alertCount > 99 ? '99+' : alertCount}
						</span>
					)}
				</Button>
			</DropdownMenuTrigger>
			<DropdownMenuContent align="end" className="min-w-[14rem]" role="menu" aria-label="Notification actions">
				<DropdownMenuItem asChild>
					<Link to="/dashboard" className="cursor-pointer">
						View alerts on dashboard
					</Link>
				</DropdownMenuItem>
				<DropdownMenuItem
					className="cursor-pointer focus:bg-destructive/10 focus:text-destructive"
					onSelect={(e) => {
						e.preventDefault()
						handleClearNotifications()
					}}
				>
					Clear notifications
				</DropdownMenuItem>
			</DropdownMenuContent>
		</DropdownMenu>
	)
}
