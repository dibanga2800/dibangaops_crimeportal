import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react'
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
	needsCustomerSelection: boolean
	selectedSiteId: string | null
	setSelectedSiteId: (siteId: string | null) => void
}

const CustomerSelectionContext = createContext<CustomerSelectionContextType | undefined>(undefined)

interface CustomerSelectionProviderProps {
	children: ReactNode
}

export const CustomerSelectionProvider: React.FC<CustomerSelectionProviderProps> = ({ children }) => {
	const { user } = useAuth()
	const [selectedCustomerId, setSelectedCustomerIdState] = useState<number | null>(null)
	const [selectedSiteId, setSelectedSiteIdState] = useState<string | null>(null)

	const isAdmin = user?.role === 'administrator'
	const needsCustomerSelection = isAdmin

	useEffect(() => {
		if (isAdmin) {
			setSelectedCustomerIdState(null)
			setSelectedSiteIdState(null)
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
	}, [isAdmin, user])

	// URL sync is now handled by CustomerSelectionUrlSync component inside Router context

	// Store selected customer in context state
	const setSelectedCustomerId = useCallback((customerId: number | null) => {
		setSelectedCustomerIdState(customerId)
	}, [])

	const setSelectedSiteId = useCallback((siteId: string | null) => {
		setSelectedSiteIdState(siteId)
	}, [])

	// For now, we'll fetch customer details when needed
	// In a real scenario, you might want to cache this
	const selectedCustomer: Customer | null = selectedCustomerId
		? { id: selectedCustomerId, name: '' } // Name will be fetched when needed
		: null

	return (
		<CustomerSelectionContext.Provider
			value={{
				selectedCustomerId,
				selectedCustomer,
				setSelectedCustomerId,
				isAdmin,
				needsCustomerSelection,
				selectedSiteId,
				setSelectedSiteId
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
