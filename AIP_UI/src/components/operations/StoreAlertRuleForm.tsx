import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { StoreAlertRule, TriggerCondition, AlertChannel } from '@/types/alertRules'
import { X, Loader2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { api, REGION_ENDPOINTS } from '@/config/api'
import { lookupTableService } from '@/services/lookupTableService'
import { sessionStore } from '@/state/sessionStore'

interface Region {
	regionID: number
	regionName: string
	fkCustomerID: number
}

interface StoreAlertRuleFormProps {
	initialData?: StoreAlertRule | null
	onSubmit: (rule: StoreAlertRule) => void
	onCancel: () => void
}

export const StoreAlertRuleForm = ({ initialData, onSubmit, onCancel }: StoreAlertRuleFormProps) => {
	const [name, setName] = useState(initialData?.name || '')
	const [keywordInput, setKeywordInput] = useState('')
	const [keywords, setKeywords] = useState<string[]>(initialData?.keywords || [])
	const [selectedIncidentTypes, setSelectedIncidentTypes] = useState<string[]>(
		initialData?.incidentTypes || []
	)
	const [storeRegion, setStoreRegion] = useState<string>(initialData?.lpmRegion || '')
	const [triggerCondition, setTriggerCondition] = useState<TriggerCondition>(
		initialData?.triggerCondition || 'any'
	)
	const [channels, setChannels] = useState<AlertChannel[]>(
		initialData?.channels || ['in-app']
	)
	const [emailInput, setEmailInput] = useState('')
	const [emailRecipients, setEmailRecipients] = useState<string[]>(
		initialData?.emailRecipients || []
	)
	const [isActive, setIsActive] = useState(initialData?.isActive ?? true)
	
	// State for regions and incident types
	const [regions, setRegions] = useState<Region[]>([])
	const [loadingRegions, setLoadingRegions] = useState(true)
	const [incidentTypes, setIncidentTypes] = useState<string[]>([])

	useEffect(() => {
		const user = sessionStore.getUser()
		const customerId = (user as any)?.customerId || (user as any)?.CustomerId

		const fetchData = async () => {
			setLoadingRegions(true)
			try {
				const [regionsResponse, lookupItems] = await Promise.all([
					api.get(
						customerId
							? `${REGION_ENDPOINTS.LIST}?pageSize=100&customerId=${customerId}`
							: `${REGION_ENDPOINTS.LIST}?pageSize=100`
					),
					lookupTableService.getByCategory('IncidentType').catch(() => []),
				])
				const regionData = regionsResponse.data?.data ?? regionsResponse.data ?? []
				setRegions(Array.isArray(regionData) ? regionData : [])
				if (lookupItems.length > 0) {
					setIncidentTypes(lookupItems.map(item => item.value))
				}
			} catch {
				setRegions([])
			} finally {
				setLoadingRegions(false)
			}
		}

		fetchData()
	}, [])

	const handleAddKeyword = () => {
		const trimmed = keywordInput.trim()
		if (trimmed && !keywords.includes(trimmed)) {
			const newKeywords = [...keywords, trimmed]
			setKeywords(newKeywords)
			setKeywordInput('')
			console.log('✅ Keyword added:', trimmed, '| Total keywords:', newKeywords.length)
		} else if (trimmed && keywords.includes(trimmed)) {
			alert('This keyword has already been added')
		}
	}

	const handleRemoveKeyword = (keyword: string) => {
		setKeywords(keywords.filter(k => k !== keyword))
	}

	const handleToggleIncidentType = (type: string) => {
		setSelectedIncidentTypes(prev =>
			prev.includes(type)
				? prev.filter(t => t !== type)
				: [...prev, type]
		)
	}

	const handleToggleChannel = (channel: AlertChannel) => {
		setChannels(prev =>
			prev.includes(channel)
				? prev.filter(c => c !== channel)
				: [...prev, channel]
		)
	}

	const handleAddEmail = () => {
		const trimmed = emailInput.trim()
		const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
		if (trimmed && emailRegex.test(trimmed) && !emailRecipients.includes(trimmed)) {
			setEmailRecipients([...emailRecipients, trimmed])
			setEmailInput('')
		} else if (trimmed && !emailRegex.test(trimmed)) {
			alert('Please enter a valid email address')
		}
	}

	const handleRemoveEmail = (email: string) => {
		setEmailRecipients(emailRecipients.filter(e => e !== email))
	}

	const handleSubmit = (e: React.FormEvent) => {
		e.preventDefault()

		console.log('🔍 Store Alert Form Validation:', {
			name: name.trim(),
			keywordsCount: keywords.length,
			keywords,
			incidentTypesCount: selectedIncidentTypes.length,
			storeRegion,
			channelsCount: channels.length,
			emailRecipientsCount: emailRecipients.length
		})

		if (!name.trim()) {
			alert('Please enter a rule name')
			return
		}

		if (keywords.length === 0) {
			console.error('❌ Validation failed: No keywords added')
			alert('Please add at least one keyword\n\nTip: Type a keyword and click "Add" button or press Enter')
			return
		}

		if (selectedIncidentTypes.length === 0) {
			alert('Please select at least one incident type')
			return
		}

		if (!storeRegion) {
			alert('Please select a region')
			return
		}

		if (channels.length === 0) {
			alert('Please select at least one channel')
			return
		}

		if (channels.includes('email') && emailRecipients.length === 0) {
			alert('Please add at least one email recipient when email channel is selected')
			return
		}

		const rule: StoreAlertRule = {
			...(initialData?.id && { id: initialData.id }), // Include ID if editing
			name: name.trim(),
			keywords,
			incidentTypes: selectedIncidentTypes,
			lpmRegion: storeRegion,
			storeRadius: undefined, // No longer used
			triggerCondition,
			channels,
			emailRecipients: channels.includes('email') ? emailRecipients : [],
			isActive,
		}

		console.log('✅ Store Alert Rule validated successfully:', { 
			isEdit: !!initialData?.id, 
			id: initialData?.id,
			rule 
		})
		onSubmit(rule)
	}

	return (
		<form onSubmit={handleSubmit} className="space-y-6">
			<div className="space-y-2">
				<Label htmlFor="name">Rule Name *</Label>
				<Input
					id="name"
					value={name}
					onChange={e => setName(e.target.value)}
					placeholder="e.g., Bulk Theft Alert"
					required
				/>
			</div>

			<div className="space-y-2">
				<div className="flex items-center justify-between">
					<Label>Keywords *</Label>
					{keywords.length > 0 && (
						<Badge variant="outline" className="text-xs">
							{keywords.length} keyword{keywords.length !== 1 ? 's' : ''} added
						</Badge>
					)}
				</div>
				<div className="flex gap-2">
					<Input
						value={keywordInput}
						onChange={e => setKeywordInput(e.target.value)}
						onKeyDown={e => {
							if (e.key === 'Enter') {
								e.preventDefault()
								handleAddKeyword()
							}
						}}
						placeholder="Enter keyword and press Enter"
					/>
					<Button type="button" onClick={handleAddKeyword} variant="outline">
						Add
					</Button>
				</div>
				{keywords.length > 0 && (
					<div className="flex flex-wrap gap-2 mt-2">
						{keywords.map((keyword, idx) => (
							<Badge key={idx} variant="secondary" className="flex items-center gap-1">
								{keyword}
								<button
									type="button"
									onClick={() => handleRemoveKeyword(keyword)}
									className="ml-1 hover:text-destructive"
								>
									<X className="h-3 w-3" />
								</button>
							</Badge>
						))}
					</div>
				)}
				<p className="text-sm text-muted-foreground">
					Add keywords that trigger this alert (e.g., "bulk theft", "threat", "meat", "razor")
				</p>
			</div>

			<div className="space-y-2">
				<Label>Incident Types *</Label>
				<div className="border rounded-md p-4 space-y-2 max-h-48 overflow-y-auto">
					{incidentTypes.map(type => (
						<div key={type} className="flex items-center space-x-2">
							<Checkbox
								id={`incident-${type}`}
								checked={selectedIncidentTypes.includes(type)}
								onCheckedChange={() => handleToggleIncidentType(type)}
							/>
							<Label
								htmlFor={`incident-${type}`}
								className="font-normal cursor-pointer flex-1"
							>
								{type}
							</Label>
						</div>
					))}
				</div>
			</div>

			<div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
				<div className="space-y-2">
					<Label htmlFor="region">Stores in Region *</Label>
					<Select value={storeRegion} onValueChange={setStoreRegion} required disabled={loadingRegions}>
						<SelectTrigger id="region">
							{loadingRegions ? (
								<div className="flex items-center gap-2">
									<Loader2 className="h-4 w-4 animate-spin" />
									<span>Loading regions...</span>
								</div>
							) : (
								<SelectValue placeholder="Select region" />
							)}
						</SelectTrigger>
						<SelectContent>
							{regions.length > 0 ? (
								regions.map(region => (
									<SelectItem key={region.regionID} value={region.regionName}>
										{region.regionName}
									</SelectItem>
								))
							) : (
								<SelectItem value="no-regions" disabled>
									No regions available
								</SelectItem>
							)}
						</SelectContent>
					</Select>
					<p className="text-sm text-muted-foreground">
						Alert stores within this region
					</p>
				</div>

				<div className="space-y-2">
					<Label htmlFor="trigger">Trigger Condition *</Label>
					<Select
						value={triggerCondition}
						onValueChange={(value: TriggerCondition) => setTriggerCondition(value)}
					>
						<SelectTrigger id="trigger">
							<SelectValue />
						</SelectTrigger>
						<SelectContent>
							<SelectItem value="any">Any keyword matches</SelectItem>
							<SelectItem value="all">All keywords match</SelectItem>
							<SelectItem value="exact-match">Exact match</SelectItem>
						</SelectContent>
					</Select>
				</div>
			</div>

			<div className="space-y-2">
				<Label>Channels *</Label>
				<div className="space-y-2">
					<div className="flex items-center space-x-2">
						<Checkbox
							id="channel-in-app"
							checked={channels.includes('in-app')}
							onCheckedChange={() => handleToggleChannel('in-app')}
						/>
						<Label htmlFor="channel-in-app" className="font-normal cursor-pointer">
							In-app notification
						</Label>
					</div>
					<div className="flex items-center space-x-2">
						<Checkbox
							id="channel-email"
							checked={channels.includes('email')}
							onCheckedChange={() => handleToggleChannel('email')}
						/>
						<Label htmlFor="channel-email" className="font-normal cursor-pointer">
							Email
						</Label>
					</div>
				</div>
			</div>

			{channels.includes('email') && (
				<div className="space-y-2">
					<Label>Email Recipients *</Label>
					<div className="flex gap-2">
						<Input
							type="email"
							value={emailInput}
							onChange={e => setEmailInput(e.target.value)}
							onKeyDown={e => {
								if (e.key === 'Enter') {
									e.preventDefault()
									handleAddEmail()
								}
							}}
							placeholder="Enter email and press Enter"
						/>
						<Button type="button" onClick={handleAddEmail} variant="outline">
							Add
						</Button>
					</div>
					{emailRecipients.length > 0 && (
						<div className="flex flex-wrap gap-2 mt-2">
							{emailRecipients.map((email, idx) => (
								<Badge key={idx} variant="secondary" className="flex items-center gap-1">
									{email}
									<button
										type="button"
										onClick={() => handleRemoveEmail(email)}
										className="ml-1 hover:text-destructive"
									>
										<X className="h-3 w-3" />
									</button>
								</Badge>
							))}
						</div>
					)}
					<p className="text-sm text-muted-foreground">
						Add email addresses for store managers who should receive alerts
					</p>
				</div>
			)}

			<div className="flex items-center space-x-2">
				<Checkbox
					id="isActive"
					checked={isActive}
					onCheckedChange={checked => setIsActive(checked === true)}
				/>
				<Label htmlFor="isActive" className="font-normal cursor-pointer">
					Active (enable this rule)
				</Label>
			</div>

			<div className="flex justify-end gap-2 pt-4 border-t">
				<Button type="button" variant="outline" onClick={onCancel}>
					Cancel
				</Button>
				<Button type="submit">
					{initialData ? 'Update Rule' : 'Create Rule'}
				</Button>
			</div>
		</form>
	)
}

