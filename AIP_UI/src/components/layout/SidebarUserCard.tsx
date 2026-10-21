import { Link } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { UserAvatar } from '@/components/common/UserAvatar'

const ROLE_LABELS: Record<string, string> = {
	administrator: 'Administrator',
	manager: 'Loss prevention manager',
	'security-officer': 'Security officer',
	store: 'Store team',
}

export const formatRole = (role?: string | null) => {
	if (!role) return ''
	const key = role.trim().toLowerCase()
	return ROLE_LABELS[key] ?? role
}

/** Signed-in user, pinned to the bottom of the sidebar. Links to the profile. */
export function SidebarUserCard() {
	const { user } = useAuth()
	if (!user) return null

	return (
		<div className="shrink-0 border-t border-sidebar-border p-3">
			<Link
				to="/profile"
				className="flex items-center gap-3 rounded-lg border border-white/10 bg-white/[0.06] px-2.5 py-2 shadow-[inset_0_1px_0_hsl(0_0%_100%/0.08)] transition-colors hover:bg-white/[0.11] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-signal"
			>
				<UserAvatar size="sm" className="ring-1 ring-white/15 [&>span]:bg-signal/15 [&>span]:text-signal" />
				<span className="min-w-0 leading-tight">
					<span className="block truncate text-sm font-medium text-white">
						{user.firstName} {user.lastName}
					</span>
					<span className="block truncate text-xs text-sidebar-foreground/75">{formatRole(user.role)}</span>
				</span>
			</Link>
		</div>
	)
}
