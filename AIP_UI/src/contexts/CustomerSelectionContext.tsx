import React, { createContext, useContext, useState, useEffect, useCallback, useMemo, ReactNode } from 'react'
import { useAuth } from './AuthContext'

interface Customer {
	id: number
	name: string
}

interface CustomerSelectionContextType {
	selectedCustomerId: number | null
	selectedCustomer: Customer | null
	setSelectedCustomerId: (customerId: number | null) => void
	isAdmin: boolean
	isManager: boolean
	needsCustomerSelection: boolean
	/** For managers: list of assigned customers to show in dropdowns (id + name from user) */
	assignedCustomers: Customer[]
	selectedSiteId: string | null
	setSelectedSiteId: (siteId: string | null) => void
	selectedRegionId: string | null
	setSelectedRegionId: (regionId: string | null) => void
}

const CustomerSelectionContext = createContext<CustomerSelectionContextType | undefined>(undefined)

interface CustomerSelectionProviderProps {
	children: ReactNode
}

export const CustomerSelectionProvider: React.FC<CustomerSelectionProviderProps> = ({ children }) => {
	const { user } = useAuth()
	const [selectedCustomerId, setSelectedCustomerIdState] = useState<number | null>(null)
	const [selectedSiteId, setSelectedSiteIdState] = useState<string | null>(null)
	const [selectedRegionId, setSelectedRegionIdState] = useState<string | null>(null)

	const isAdmin = user?.role === 'administrator'
	const isManager = user?.role === 'manager'
	const assignedIds = (user as any)?.assignedCustomerIds ?? (user as any)?.AssignedCustomerIds
	const assignedNames = (user as any)?.assignedCustomerNames ?? (user as any)?.AssignedCustomerNames
	const assignedCustomers: Customer[] = useMemo(() => {
		if (!Array.isArray(assignedIds) || assignedIds.length === 0) return []
		const names = Array.isArray(assignedNames) ? assignedNames : []
		return assignedIds.map((id: number, i: number) => ({
			id: typeof id === 'string' ? parseInt(id, 10) : id,
			name: names[i] ?? `Customer ${id}`,
		}))
	}, [assignedIds, assignedNames])
	const needsCustomerSelection = isAdmin || (isManager && assignedCustomers.length > 1)

	useEffect(() => {
		if (isAdmin) {
			setSelectedCustomerIdState(null)
			setSelectedSiteIdState(null)
			setSelectedRegionIdState(null)
		} else if (isManager && assignedCustomers.length > 0) {
			// Manager: default to first assigned customer so they always have a selection
			const firstId = assignedCustomers[0].id
			setSelectedCustomerIdState((prev) => (prev !== null && assignedCustomers.some((c) => c.id === prev) ? prev : firstId))
		} else {
			const customerId = user && 'customerId' in user ? (user as any).customerId : null
			if (customerId) {
				setSelectedCustomerIdState(typeof customerId === 'string' ? parseInt(customerId, 10) : customerId)
			}
			// For store users, also fix the site from primarySiteId if available
			const primarySiteId = user && 'primarySiteId' in user ? (user as any).primarySiteId : null
			if (primarySiteId) {
				setSelectedSiteIdState(String(primarySiteId))
			}
		}
	}, [isAdmin, isManager, user, assignedCustomers])

	// URL sync is now handled by CustomerSelectionUrlSync component inside Router context

	// Store selected customer in context state
	const setSelectedCustomerId = useCallback((customerId: number | null) => {
		setSelectedCustomerIdState(customerId)
	}, [])

	const setSelectedSiteId = useCallback((siteId: string | null) => {
		setSelectedSiteIdState(siteId)
	}, [])

	const setSelectedRegionId = useCallback((regionId: string | null) => {
		setSelectedRegionIdState(regionId)
	}, [])

	// For now, we'll fetch customer details when needed
	// In a real scenario, you might want to cache this
	const selectedCustomer: Customer | null = selectedCustomerId
		? (assignedCustomers.find((c) => c.id === selectedCustomerId) ?? { id: selectedCustomerId, name: '' })
		: null

	return (
		<CustomerSelectionContext.Provider
			value={{
				selectedCustomerId,
				selectedCustomer,
				setSelectedCustomerId,
				isAdmin,
				isManager,
				needsCustomerSelection,
				assignedCustomers,
				selectedSiteId,
				setSelectedSiteId,
				selectedRegionId,
				setSelectedRegionId,
			}}
		>
			{children}
		</CustomerSelectionContext.Provider>
	)
}

export const useCustomerSelection = (): CustomerSelectionContextType => {
	const context = useContext(CustomerSelectionContext)
	if (context === undefined) {
		throw new Error('useCustomerSelection must be used within a CustomerSelectionProvider')
	}
	return context
}
