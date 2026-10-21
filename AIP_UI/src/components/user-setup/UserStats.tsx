import { Users2, ShieldCheck, Clock } from "lucide-react"
import type { UserRecord as User } from "@/types/user"
import { StatLedger } from "@/components/common/StatLedger"

interface UserStatsProps {
  users: User[]
}

export function UserStats({ users }: UserStatsProps) {
  const activeUsers = users.filter(u => u.status === 'active').length
  const adminCount = users.filter(u => u.role === 'Admin').length
  const recentLogins = users.filter(u => {
    const loginDate = new Date(u.lastLogin)
    const now = new Date()
    const daysDiff = Math.floor((now.getTime() - loginDate.getTime()) / (1000 * 60 * 60 * 24))
    return daysDiff <= 7
  }).length
  const adminShare = users.length ? ((adminCount / users.length) * 100).toFixed(0) : '0'

  return (
    <StatLedger
      items={[
        { label: "Users", value: users.length, hint: `${activeUsers} active`, icon: <Users2 />, tone: "neutral" },
        { label: "Administrators", value: adminCount, hint: `${adminShare}% of all users`, icon: <ShieldCheck />, tone: "info" },
        { label: "Signed in this week", value: recentLogins, hint: "Last 7 days", icon: <Clock />, tone: "recovered" },
      ]}
    />
  )
}
