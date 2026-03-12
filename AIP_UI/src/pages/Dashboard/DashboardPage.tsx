import React from 'react'
import { DashboardMetrics } from '@/components/dashboard/DashboardMetrics'
import { RecentActivity } from '@/components/dashboard/RecentActivity'
import { TaskScheduler } from '@/components/dashboard/TaskScheduler'
import { PerformanceGraph } from '@/components/dashboard/PerformanceGraph'
import { useAuth } from '@/contexts/AuthContext'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'

interface DashboardPageProps {
  displayName?: string
}

const DashboardPage: React.FC<DashboardPageProps> = ({ displayName }) => {
  const { user } = useAuth()
  const { selectedCustomerId, selectedSiteId } = useCustomerSelection()

  const role = (user?.role || '').toLowerCase()
  const isAdmin = role === 'administrator'
  const isManager = role === 'manager'
  const isSecurityOfficer = role === 'security-officer'
  const isStoreUser = role === 'store'

  const scopeLabel = React.useMemo(() => {
    if (isAdmin && !selectedCustomerId) {
      return 'All customers'
    }
    if (isAdmin && selectedCustomerId) {
      return `Customer-scoped (Customer ID: ${selectedCustomerId})`
    }
    if ((isManager || isSecurityOfficer) && selectedCustomerId && selectedSiteId) {
      return `Assigned customers & stores (Customer ID: ${selectedCustomerId}, Store ID: ${selectedSiteId})`
    }
    if ((isManager || isSecurityOfficer) && selectedCustomerId) {
      return `Assigned customers (Customer ID: ${selectedCustomerId})`
    }
    if (isStoreUser && selectedCustomerId && selectedSiteId) {
      return `Store-scoped (Customer ID: ${selectedCustomerId}, Store ID: ${selectedSiteId})`
    }
    if (isStoreUser && selectedCustomerId) {
      return `Store-scoped (Customer ID: ${selectedCustomerId})`
    }
    return 'Role-scoped view'
  }, [isAdmin, isManager, isSecurityOfficer, isStoreUser, selectedCustomerId, selectedSiteId])

  const roleSummary = React.useMemo(() => {
    if (isAdmin) {
      return "You are viewing the overall system dashboard. Data reflects all customers unless you select a specific company."
    }
    if (isManager) {
      return "You are viewing incidents and activity for your assigned customers across all their stores."
    }
    if (isSecurityOfficer) {
      return "You are viewing incidents and activity for your assigned customers and the stores you are responsible for."
    }
    if (isStoreUser) {
      return "You are viewing incidents and activity for your assigned store only."
    }
    return "You are viewing a role-scoped dashboard based on your current access."
  }, [isAdmin, isManager, isSecurityOfficer, isStoreUser])

  return (
    <>
      {/* Header */}
      <header className="flex flex-col gap-1 mb-4 sm:mb-6 lg:mb-8">
        <h1 className="text-xl sm:text-2xl font-bold text-gray-900 dark:text-white">Dashboard</h1>
        <p className="text-sm text-gray-600 dark:text-gray-300">
          Welcome back{displayName ? `, ${displayName}` : ''}! {roleSummary}
        </p>
        <p className="text-xs sm:text-sm text-gray-500 dark:text-gray-400">
          Scope: {scopeLabel}
        </p>
      </header>

      {/* Metrics */}
      <section className="mb-4 sm:mb-6 lg:mb-8">
        <div className="grid grid-cols-1 xs:grid-cols-2 md:grid-cols-4 gap-3 sm:gap-4">
          <DashboardMetrics />
        </div>
      </section>

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-6 lg:gap-8 mb-4 sm:mb-6 lg:mb-8">
        {/* Left Column - Task Scheduler */}
        <section className="lg:col-span-8 space-y-4 sm:space-y-6">
          <TaskScheduler />
        </section>

        {/* Right Column - Recent Activity */}
        <aside className="lg:col-span-4 space-y-4 sm:space-y-6">
          <RecentActivity />
        </aside>
      </div>

      {/* Performance Graph */}
      <section className="mb-4">
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm">
          <div className="p-3 sm:p-4 md:p-6">
            <PerformanceGraph />
          </div>
        </div>
      </section>
    </>
  )
}

export default DashboardPage;