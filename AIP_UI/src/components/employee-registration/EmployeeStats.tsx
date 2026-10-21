import { Users, Briefcase, CalendarCheck } from "lucide-react"
import { EmployeeStatistics } from "@/services/employeeService"
import { StatLedger } from "@/components/common/StatLedger"

interface EmployeeStatsProps {
  statistics?: EmployeeStatistics | null
}

export const EmployeeStats = ({ statistics }: EmployeeStatsProps) => {
  const totalEmployees = statistics?.totalEmployees ?? 0
  const activeEmployees = statistics?.activeEmployees ?? 0
  const positions = Object.keys(statistics?.employeesByPosition ?? {}).length

  return (
    <StatLedger
      items={[
        { label: "Employees", value: totalEmployees, hint: "Registered", icon: <Users />, tone: "neutral" },
        { label: "Active", value: activeEmployees, hint: "Currently working", icon: <CalendarCheck />, tone: "recovered" },
        { label: "Positions", value: positions, hint: "Distinct job titles", icon: <Briefcase />, tone: "info" },
      ]}
    />
  )
}
