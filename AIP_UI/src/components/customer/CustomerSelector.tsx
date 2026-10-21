import { useState, useEffect, useRef } from 'react'
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue
} from '@/components/ui/select'
import { useCustomerSelection } from '@/contexts/CustomerSelectionContext'
import { getAvailableCustomers } from '@/hooks/useAvailableCustomers'

export const CustomerSelector: React.FC = () => {
	const { selectedCustomerId, setSelectedCustomerId, isAdmin } = useCustomerSelection()
	const [customers, setCustomers] = useState<Array<{ id: number; name: string }>>([])
	const [isLoading, setIsLoading] = useState(true)
	const hasLoadedCustomers = useRef(false)

	// Load customers once - persist across remounts
	useEffect(() => {
		if (!isAdmin) return
		
		// Only load if we haven't loaded yet
		if (hasLoadedCustomers.current && customers.length > 0) {
			return
		}

		let mounted = true

		const loadCustomers = async () => {
			try {
				setIsLoading(true)
				const availableCustomers = await getAvailableCustomers()
				
				if (!mounted) return
				
				setCustomers(availableCustomers)
				hasLoadedCustomers.current = true

				// Auto-select first customer if none is selected
				if (!selectedCustomerId && availableCustomers.length > 0) {
					setSelectedCustomerId(availableCustomers[0].id)
				}
			} catch (error) {
				console.error('Failed to load customers:', error)
				if (mounted) {
					setCustomers([])
				}
			} finally {
				if (mounted) {
					setIsLoading(false)
				}
			}
		}

		loadCustomers()

		return () => {
			mounted = false
		}
		 
	}, [isAdmin])

	if (!isAdmin) {
		return null
	}

	const selectedCustomer = customers.find(c => c.id === selectedCustomerId)
	const selectValue = selectedCustomerId?.toString() || ''

	const handleValueChange = (value: string) => {
		if (!value) {
			setSelectedCustomerId(null)
			return
		}
		const customerId = parseInt(value, 10)
		if (!isNaN(customerId)) {
			setSelectedCustomerId(customerId)
		}
	}

	return (
		<div className="px-3 py-2">
			<label className="mb-1.5 block text-xs font-medium text-sidebar-foreground/70">
				Company
			</label>
			<Select
				value={selectValue}
				onValueChange={handleValueChange}
				disabled={isLoading || customers.length === 0}
			>
				<SelectTrigger className="h-9 w-full border-sidebar-border bg-sidebar-accent text-sm text-white shadow-none hover:border-white/25 focus:border-signal focus:ring-signal/25 data-[placeholder]:text-sidebar-foreground [&>svg]:text-sidebar-foreground">
					<SelectValue placeholder={isLoading ? 'Loading companies...' : 'Select a company'} />
				</SelectTrigger>
				<SelectContent className="max-h-[300px]">
					{customers.map((customer) => (
						<SelectItem key={customer.id} value={customer.id.toString()}>
							{customer.name}
						</SelectItem>
					))}
				</SelectContent>
			</Select>
			{selectedCustomer && (
				<p className="mt-1.5 text-xs text-sidebar-foreground/70">
					Viewing <span className="font-medium text-sidebar-foreground">{selectedCustomer.name}</span>
				</p>
			)}
		</div>
	)
}
