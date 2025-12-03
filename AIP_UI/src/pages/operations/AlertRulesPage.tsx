import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Bell, Store, Shield, Plus, Trash2, Edit2, CheckCircle2, XCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { StoreAlertRuleForm } from '@/components/operations/StoreAlertRuleForm'
import { LPMAlertRuleForm } from '@/components/operations/LPMAlertRuleForm'
import { StoreAlertRule, LPMAlertRule } from '@/types/alertRules'
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog'
import {
	AlertDialog,
	AlertDialogAction,
	AlertDialogCancel,
	AlertDialogContent,
	AlertDialogDescription,
	AlertDialogFooter,
	AlertDialogHeader,
	AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useToast } from '@/hooks/use-toast'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { alertRuleService } from '@/services/alertRuleService'

export default function AlertRulesPage() {
	const [activeTab, setActiveTab] = useState<'store' | 'lpm'>('store')
	const [storeRules, setStoreRules] = useState<StoreAlertRule[]>([])
	const [lpmRules, setLpmRules] = useState<LPMAlertRule[]>([])
	const [isStoreDialogOpen, setIsStoreDialogOpen] = useState(false)
	const [isLPMDialogOpen, setIsLPMDialogOpen] = useState(false)
	const [editingStoreRule, setEditingStoreRule] = useState<StoreAlertRule | null>(null)
	const [editingLPMRule, setEditingLPMRule] = useState<LPMAlertRule | null>(null)
	const [deleteStoreDialogOpen, setDeleteStoreDialogOpen] = useState(false)
	const [deleteLPMDialogOpen, setDeleteLPMDialogOpen] = useState(false)
	const [storeRuleToDelete, setStoreRuleToDelete] = useState<StoreAlertRule | null>(null)
	const [lpmRuleToDelete, setLpmRuleToDelete] = useState<LPMAlertRule | null>(null)
	const [loading, setLoading] = useState(false)
	const { toast } = useToast()

	// Load alert rules from API
	useEffect(() => {
		loadAlertRules()
	}, [])

	const loadAlertRules = async () => {
		setLoading(true)
		try {
			// Load store rules
			const storeResponse = await alertRuleService.getAlertRules({
				ruleType: 'Store',
				page: 1,
				pageSize: 100
			})
			if (storeResponse.success && storeResponse.data?.data) {
				setStoreRules(storeResponse.data.data)
				console.log('✅ Loaded store rules:', storeResponse.data.data.length)
			}

			// Load LPM rules
			const lpmResponse = await alertRuleService.getAlertRules({
				ruleType: 'LPM',
				page: 1,
				pageSize: 100
			})
			if (lpmResponse.success && lpmResponse.data?.data) {
				setLpmRules(lpmResponse.data.data)
				console.log('✅ Loaded LPM rules:', lpmResponse.data.data.length)
			}
		} catch (error) {
			console.error('Failed to load alert rules:', error)
			toast({
				title: 'Error',
				description: 'Failed to load alert rules. Please try again.',
				variant: 'destructive'
			})
		} finally {
			setLoading(false)
		}
	}

	const handleStoreRuleSubmit = async (rule: StoreAlertRule) => {
		try {
			// Check if we're editing (either editingStoreRule has ID or rule has ID)
			const isEditing = editingStoreRule?.id || rule.id
			
			if (isEditing) {
				// Update existing rule
				const ruleId = typeof (editingStoreRule?.id || rule.id) === 'string' 
					? parseInt((editingStoreRule?.id || rule.id)!.toString().replace('store-', '')) 
					: (editingStoreRule?.id || rule.id)
				
				console.log('🔄 Updating store alert rule:', { ruleId, ruleName: rule.name })
				await alertRuleService.updateAlertRule(ruleId as number, rule)
				toast({
					title: 'Success',
					description: 'Store alert rule updated successfully',
				})
			} else {
				// Create new rule
				console.log('➕ Creating new store alert rule:', rule.name)
				await alertRuleService.createStoreAlertRule(rule)
				toast({
					title: 'Success',
					description: 'Store alert rule created successfully',
				})
			}
			setIsStoreDialogOpen(false)
			setEditingStoreRule(null)
			loadAlertRules() // Reload rules
		} catch (error) {
			console.error('Error saving store alert rule:', error)
			toast({
				title: 'Error',
				description: 'Failed to save store alert rule',
				variant: 'destructive'
			})
		}
	}

	const handleLPMRuleSubmit = async (rule: LPMAlertRule) => {
		try {
			// Check if we're editing (either editingLPMRule has ID or rule has ID)
			const isEditing = editingLPMRule?.id || rule.id
			
			if (isEditing) {
				// Update existing rule
				const ruleId = typeof (editingLPMRule?.id || rule.id) === 'string' 
					? parseInt((editingLPMRule?.id || rule.id)!.toString().replace('lpm-', '')) 
					: (editingLPMRule?.id || rule.id)
				
				console.log('🔄 Updating LPM alert rule:', { ruleId, ruleName: rule.name })
				await alertRuleService.updateAlertRule(ruleId as number, rule)
				toast({
					title: 'Success',
					description: 'LPM alert rule updated successfully',
				})
			} else {
				// Create new rule
				console.log('➕ Creating new LPM alert rule:', rule.name)
				await alertRuleService.createLPMAlertRule(rule)
				toast({
					title: 'Success',
					description: 'LPM alert rule created successfully',
				})
			}
			setIsLPMDialogOpen(false)
			setEditingLPMRule(null)
			loadAlertRules() // Reload rules
		} catch (error) {
			console.error('Error saving LPM alert rule:', error)
			toast({
				title: 'Error',
				description: 'Failed to save LPM alert rule',
				variant: 'destructive'
			})
		}
	}

	const handleEditStoreRule = (rule: StoreAlertRule) => {
		setEditingStoreRule(rule)
		setIsStoreDialogOpen(true)
	}

	const handleEditLPMRule = (rule: LPMAlertRule) => {
		setEditingLPMRule(rule)
		setIsLPMDialogOpen(true)
	}

	const handleDeleteStoreRuleClick = (rule: StoreAlertRule) => {
		console.log('🗑️ Delete store rule clicked:', rule)
		setStoreRuleToDelete(rule)
		setDeleteStoreDialogOpen(true)
	}

	const handleDeleteStoreRule = async () => {
		if (!storeRuleToDelete?.id) return

		try {
			console.log('🗑️ Confirming delete store rule:', { id: storeRuleToDelete.id, name: storeRuleToDelete.name })
			
			// Handle different ID formats
			let ruleId: number
			if (typeof storeRuleToDelete.id === 'string') {
				// Remove any prefix and parse to number
				ruleId = parseInt(storeRuleToDelete.id.replace('store-', '').replace('lpm-', ''))
			} else {
				ruleId = storeRuleToDelete.id
			}
			
			console.log('🗑️ Parsed rule ID:', ruleId)
			
			if (isNaN(ruleId)) {
				throw new Error(`Invalid rule ID: ${storeRuleToDelete.id}`)
			}
			
			await alertRuleService.deleteAlertRule(ruleId)
			toast({
				title: 'Success',
				description: 'Store alert rule deleted successfully',
			})
			setDeleteStoreDialogOpen(false)
			setStoreRuleToDelete(null)
			loadAlertRules() // Reload rules
		} catch (error) {
			console.error('❌ Error deleting store alert rule:', error)
			toast({
				title: 'Error',
				description: error instanceof Error ? error.message : 'Failed to delete store alert rule',
				variant: 'destructive'
			})
		}
	}

	const handleDeleteLPMRuleClick = (rule: LPMAlertRule) => {
		console.log('🗑️ Delete LPM rule clicked:', rule)
		setLpmRuleToDelete(rule)
		setDeleteLPMDialogOpen(true)
	}

	const handleDeleteLPMRule = async () => {
		if (!lpmRuleToDelete?.id) return

		try {
			console.log('🗑️ Confirming delete LPM rule:', { id: lpmRuleToDelete.id, name: lpmRuleToDelete.name })
			
			// Handle different ID formats
			let ruleId: number
			if (typeof lpmRuleToDelete.id === 'string') {
				// Remove any prefix and parse to number
				ruleId = parseInt(lpmRuleToDelete.id.replace('store-', '').replace('lpm-', ''))
			} else {
				ruleId = lpmRuleToDelete.id
			}
			
			console.log('🗑️ Parsed rule ID:', ruleId)
			
			if (isNaN(ruleId)) {
				throw new Error(`Invalid rule ID: ${lpmRuleToDelete.id}`)
			}
			
			await alertRuleService.deleteAlertRule(ruleId)
			toast({
				title: 'Success',
				description: 'LPM alert rule deleted successfully',
			})
			setDeleteLPMDialogOpen(false)
			setLpmRuleToDelete(null)
			loadAlertRules() // Reload rules
		} catch (error) {
			console.error('❌ Error deleting LPM alert rule:', error)
			toast({
				title: 'Error',
				description: error instanceof Error ? error.message : 'Failed to delete LPM alert rule',
				variant: 'destructive'
			})
		}
	}

	const handleToggleStoreRuleStatus = async (id: string | number) => {
		try {
			console.log('🔄 Toggle store rule clicked:', { id, type: typeof id })
			
			const rule = storeRules.find(r => String(r.id) === String(id))
			if (!rule) {
				console.error('❌ Rule not found:', id)
				return
			}

			// Handle different ID formats
			let ruleId: number
			if (typeof id === 'string') {
				ruleId = parseInt(id.replace('store-', '').replace('lpm-', ''))
			} else {
				ruleId = id
			}
			
			console.log('🔄 Parsed rule ID:', ruleId, 'New status:', !rule.isActive)
			
			if (isNaN(ruleId)) {
				throw new Error(`Invalid rule ID: ${id}`)
			}
			
			await alertRuleService.toggleAlertRule(ruleId, !rule.isActive)
			toast({
				title: 'Success',
				description: 'Store alert rule status updated',
			})
			loadAlertRules() // Reload rules
		} catch (error) {
			console.error('❌ Error toggling store alert rule:', error)
			toast({
				title: 'Error',
				description: error instanceof Error ? error.message : 'Failed to update store alert rule status',
				variant: 'destructive'
			})
		}
	}

	const handleToggleLPMRuleStatus = async (id: string | number) => {
		try {
			console.log('🔄 Toggle LPM rule clicked:', { id, type: typeof id })
			
			const rule = lpmRules.find(r => String(r.id) === String(id))
			if (!rule) {
				console.error('❌ Rule not found:', id)
				return
			}

			// Handle different ID formats
			let ruleId: number
			if (typeof id === 'string') {
				ruleId = parseInt(id.replace('store-', '').replace('lpm-', ''))
			} else {
				ruleId = id
			}
			
			console.log('🔄 Parsed rule ID:', ruleId, 'New status:', !rule.isActive)
			
			if (isNaN(ruleId)) {
				throw new Error(`Invalid rule ID: ${id}`)
			}
			
			await alertRuleService.toggleAlertRule(ruleId, !rule.isActive)
			toast({
				title: 'Success',
				description: 'LPM alert rule status updated',
			})
			loadAlertRules() // Reload rules
		} catch (error) {
			console.error('❌ Error toggling LPM alert rule:', error)
			toast({
				title: 'Error',
				description: error instanceof Error ? error.message : 'Failed to update LPM alert rule status',
				variant: 'destructive'
			})
		}
	}

	return (
		<div className="min-h-screen w-full max-w-[100vw] overflow-x-hidden bg-gradient-to-br from-blue-50 via-slate-50 to-blue-50">
			<div className="container mx-auto px-3 sm:px-4 md:px-6 py-3 sm:py-4 md:py-6 space-y-4 sm:space-y-6 max-w-screen-2xl">
				<div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 sm:gap-4">
					<div>
						<h1 className="text-lg sm:text-xl md:text-2xl lg:text-3xl font-bold">Alert Rules</h1>
						<p className="text-xs sm:text-sm md:text-base text-muted-foreground mt-1">
							Manage alert rules for stores and Loss Prevention Managers
						</p>
					</div>
				</div>

			{loading && (
				<Card>
					<CardContent className="p-4 sm:p-6">
						<div className="flex items-center justify-center gap-3">
							<div className="animate-spin rounded-full h-5 w-5 border-b-2 border-primary"></div>
							<p className="text-xs sm:text-sm text-muted-foreground">Loading alert rules...</p>
						</div>
					</CardContent>
				</Card>
			)}

			<Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as 'store' | 'lpm')} className="w-full">
				<TabsList className="grid w-full grid-cols-2 mb-4 sm:mb-6 h-auto">
					<TabsTrigger value="store" className="flex items-center gap-2 text-xs sm:text-sm py-2">
						<Store className="h-4 w-4" />
						<span className="hidden sm:inline">Store Alert Rules</span>
						<span className="sm:hidden">Store</span>
						<Badge variant="secondary" className="ml-1 sm:ml-2 text-xs">{storeRules.length}</Badge>
					</TabsTrigger>
					<TabsTrigger value="lpm" className="flex items-center gap-2 text-xs sm:text-sm py-2">
						<Shield className="h-4 w-4" />
						<span className="hidden sm:inline">LPM Alert Rules</span>
						<span className="sm:hidden">LPM</span>
						<Badge variant="secondary" className="ml-1 sm:ml-2 text-xs">{lpmRules.length}</Badge>
					</TabsTrigger>
				</TabsList>

				{/* Store Alert Rules Tab */}
				<TabsContent value="store" className="space-y-4 sm:space-y-6">
					<Card className="overflow-hidden">
						<CardHeader className="p-4 sm:p-6">
							<div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 sm:gap-4">
								<div className="min-w-0 flex-1">
									<CardTitle className="flex items-center gap-2 text-base sm:text-lg">
										<Store className="h-5 w-5 flex-shrink-0" />
										<span>Store Alert Rules</span>
									</CardTitle>
									<CardDescription className="mt-1 text-xs sm:text-sm">
										Create rules that trigger alerts for stores based on keywords, incident types, and conditions
									</CardDescription>
								</div>
								<Button
									onClick={() => {
										setEditingStoreRule(null)
										setIsStoreDialogOpen(true)
									}}
									className="w-full sm:w-auto text-xs sm:text-sm"
								>
									<Plus className="h-4 w-4 mr-2" />
									Create Store Rule
								</Button>
							</div>
						</CardHeader>
						<CardContent className="p-4 sm:p-6 pt-0">
							{storeRules.length === 0 ? (
								<div className="text-center py-8 text-muted-foreground">
									<Bell className="h-10 w-10 sm:h-12 sm:w-12 mx-auto mb-4 opacity-50" />
									<p className="text-sm sm:text-base">No store alert rules created yet</p>
									<p className="text-xs sm:text-sm mt-2">Click "Create Store Rule" to get started</p>
								</div>
							) : (
								<>
									{/* Mobile Card Layout */}
									<div className="block md:hidden space-y-3">
										{storeRules.map(rule => (
											<div key={rule.id} className="rounded-lg border bg-white shadow-sm p-4 space-y-3">
												{/* Header with name and status */}
												<div className="flex items-start justify-between gap-2">
													<div className="flex-1 min-w-0">
														<div className="font-semibold text-sm truncate">{rule.name}</div>
													</div>
													<Button
														variant="ghost"
														size="sm"
														onClick={() => rule.id && handleToggleStoreRuleStatus(rule.id)}
														className="h-8 w-8 p-0 flex-shrink-0"
													>
														{rule.isActive ? (
															<CheckCircle2 className="h-4 w-4 text-green-600" />
														) : (
															<XCircle className="h-4 w-4 text-gray-400" />
														)}
													</Button>
												</div>

												{/* Details */}
												<div className="space-y-2 text-xs pt-2 border-t">
													<div>
														<span className="text-gray-500 block mb-1">Keywords</span>
														<div className="flex flex-wrap gap-1">
															{rule.keywords.slice(0, 3).map((keyword, idx) => (
																<Badge key={idx} variant="secondary" className="text-xs">
																	{keyword}
																</Badge>
															))}
															{rule.keywords.length > 3 && (
																<Badge variant="secondary" className="text-xs">
																	+{rule.keywords.length - 3}
																</Badge>
															)}
														</div>
													</div>
													<div>
														<span className="text-gray-500 block mb-1">Incident Types</span>
														<div className="flex flex-wrap gap-1">
															{rule.incidentTypes.slice(0, 3).map((type, idx) => (
																<Badge key={idx} variant="outline" className="text-xs">
																	{type}
																</Badge>
															))}
															{rule.incidentTypes.length > 3 && (
																<Badge variant="outline" className="text-xs">
																	+{rule.incidentTypes.length - 3}
																</Badge>
															)}
														</div>
													</div>
													<div className="grid grid-cols-2 gap-2">
														<div>
															<span className="text-gray-500 block mb-0.5">Radius</span>
															<div className="font-medium">{rule.storeRadius} miles</div>
														</div>
														<div>
															<span className="text-gray-500 block mb-0.5">Channels</span>
															<div className="flex flex-wrap gap-1">
																{rule.channels.map((channel, idx) => (
																	<Badge key={idx} variant="default" className="text-xs">
																		{channel}
																	</Badge>
																))}
															</div>
														</div>
													</div>
												</div>

												{/* Actions */}
												<div className="flex gap-2 pt-2 border-t">
													<Button
														variant="outline"
														size="sm"
														onClick={() => handleEditStoreRule(rule)}
														className="flex-1 h-9 text-xs"
													>
														<Edit2 className="h-3.5 w-3.5 mr-1.5" />
														Edit
													</Button>
													<Button
														variant="outline"
														size="sm"
														onClick={() => handleDeleteStoreRuleClick(rule)}
														className="h-9 px-3 text-xs text-red-600 hover:text-red-700 hover:bg-red-50"
													>
														<Trash2 className="h-3.5 w-3.5" />
													</Button>
												</div>
											</div>
										))}
									</div>

									{/* Desktop Table Layout */}
									<div className="hidden md:block overflow-x-auto">
										<Table>
											<TableHeader>
												<TableRow>
													<TableHead className="text-sm">Name</TableHead>
													<TableHead className="text-sm">Keywords</TableHead>
													<TableHead className="text-sm">Incident Types</TableHead>
													<TableHead className="text-sm">Radius</TableHead>
													<TableHead className="text-sm">Channels</TableHead>
													<TableHead className="text-sm">Status</TableHead>
													<TableHead className="text-right text-sm">Actions</TableHead>
												</TableRow>
											</TableHeader>
											<TableBody>
												{storeRules.map(rule => (
													<TableRow key={rule.id}>
														<TableCell className="font-medium">{rule.name}</TableCell>
														<TableCell>
															<div className="flex flex-wrap gap-1">
																{rule.keywords.slice(0, 2).map((keyword, idx) => (
																	<Badge key={idx} variant="secondary" className="text-xs">
																		{keyword}
																	</Badge>
																))}
																{rule.keywords.length > 2 && (
																	<Badge variant="secondary" className="text-xs">
																		+{rule.keywords.length - 2}
																	</Badge>
																)}
															</div>
														</TableCell>
														<TableCell>
															<div className="flex flex-wrap gap-1">
																{rule.incidentTypes.slice(0, 2).map((type, idx) => (
																	<Badge key={idx} variant="outline" className="text-xs">
																		{type}
																	</Badge>
																))}
																{rule.incidentTypes.length > 2 && (
																	<Badge variant="outline" className="text-xs">
																		+{rule.incidentTypes.length - 2}
																	</Badge>
																)}
															</div>
														</TableCell>
														<TableCell>{rule.storeRadius} miles</TableCell>
														<TableCell>
															<div className="flex flex-wrap gap-1">
																{rule.channels.map((channel, idx) => (
																	<Badge key={idx} variant="default" className="text-xs">
																		{channel}
																	</Badge>
																))}
															</div>
														</TableCell>
														<TableCell>
															<Button
																variant="ghost"
																size="sm"
																onClick={() => rule.id && handleToggleStoreRuleStatus(rule.id)}
																className="h-6"
															>
																{rule.isActive ? (
																	<CheckCircle2 className="h-4 w-4 text-green-600" />
																) : (
																	<XCircle className="h-4 w-4 text-gray-400" />
																)}
															</Button>
														</TableCell>
														<TableCell className="text-right">
															<div className="flex justify-end gap-2">
																<Button
																	variant="ghost"
																	size="sm"
																	onClick={() => handleEditStoreRule(rule)}
																>
																	<Edit2 className="h-4 w-4" />
																</Button>
																<Button
																	variant="ghost"
																	size="sm"
																	onClick={() => handleDeleteStoreRuleClick(rule)}
																>
																	<Trash2 className="h-4 w-4 text-destructive" />
																</Button>
															</div>
														</TableCell>
													</TableRow>
												))}
											</TableBody>
										</Table>
									</div>
								</>
							)}
						</CardContent>
					</Card>
				</TabsContent>

				{/* LPM Alert Rules Tab */}
				<TabsContent value="lpm" className="space-y-4 sm:space-y-6">
					<Card className="overflow-hidden">
						<CardHeader className="p-4 sm:p-6">
							<div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 sm:gap-4">
								<div className="min-w-0 flex-1">
									<CardTitle className="flex items-center gap-2 text-base sm:text-lg">
										<Shield className="h-5 w-5 flex-shrink-0" />
										<span>LPM Alert Rules</span>
									</CardTitle>
									<CardDescription className="mt-1 text-xs sm:text-sm">
										Create rules that trigger alerts for Loss Prevention Managers in specific regions
									</CardDescription>
								</div>
								<Button
									onClick={() => {
										setEditingLPMRule(null)
										setIsLPMDialogOpen(true)
									}}
									className="w-full sm:w-auto text-xs sm:text-sm"
								>
									<Plus className="h-4 w-4 mr-2" />
									Create LPM Rule
								</Button>
							</div>
						</CardHeader>
						<CardContent className="p-4 sm:p-6 pt-0">
							{lpmRules.length === 0 ? (
								<div className="text-center py-8 text-muted-foreground">
									<Shield className="h-10 w-10 sm:h-12 sm:w-12 mx-auto mb-4 opacity-50" />
									<p className="text-sm sm:text-base">No LPM alert rules created yet</p>
									<p className="text-xs sm:text-sm mt-2">Click "Create LPM Rule" to get started</p>
								</div>
							) : (
								<>
									{/* Mobile Card Layout */}
									<div className="block md:hidden space-y-3">
										{lpmRules.map(rule => (
											<div key={rule.id} className="rounded-lg border bg-white shadow-sm p-4 space-y-3">
												{/* Header with name and status */}
												<div className="flex items-start justify-between gap-2">
													<div className="flex-1 min-w-0">
														<div className="font-semibold text-sm truncate">{rule.name}</div>
													</div>
													<Button
														variant="ghost"
														size="sm"
														onClick={() => rule.id && handleToggleLPMRuleStatus(rule.id)}
														className="h-8 w-8 p-0 flex-shrink-0"
													>
														{rule.isActive ? (
															<CheckCircle2 className="h-4 w-4 text-green-600" />
														) : (
															<XCircle className="h-4 w-4 text-gray-400" />
														)}
													</Button>
												</div>

												{/* Details */}
												<div className="space-y-2 text-xs pt-2 border-t">
													<div>
														<span className="text-gray-500 block mb-1">Keywords</span>
														<div className="flex flex-wrap gap-1">
															{rule.keywords.slice(0, 3).map((keyword, idx) => (
																<Badge key={idx} variant="secondary" className="text-xs">
																	{keyword}
																</Badge>
															))}
															{rule.keywords.length > 3 && (
																<Badge variant="secondary" className="text-xs">
																	+{rule.keywords.length - 3}
																</Badge>
															)}
														</div>
													</div>
													<div>
														<span className="text-gray-500 block mb-1">Incident Types</span>
														<div className="flex flex-wrap gap-1">
															{rule.incidentTypes.slice(0, 3).map((type, idx) => (
																<Badge key={idx} variant="outline" className="text-xs">
																	{type}
																</Badge>
															))}
															{rule.incidentTypes.length > 3 && (
																<Badge variant="outline" className="text-xs">
																	+{rule.incidentTypes.length - 3}
																</Badge>
															)}
														</div>
													</div>
													<div>
														<span className="text-gray-500 block mb-0.5">LPM Region</span>
														<div className="font-medium">{rule.lpmRegion}</div>
													</div>
													<div>
														<span className="text-gray-500 block mb-0.5">Channels</span>
														<div className="flex flex-wrap gap-1">
															{rule.channels.map((channel, idx) => (
																<Badge key={idx} variant="default" className="text-xs">
																	{channel}
																</Badge>
															))}
														</div>
													</div>
												</div>

												{/* Actions */}
												<div className="flex gap-2 pt-2 border-t">
													<Button
														variant="outline"
														size="sm"
														onClick={() => handleEditLPMRule(rule)}
														className="flex-1 h-9 text-xs"
													>
														<Edit2 className="h-3.5 w-3.5 mr-1.5" />
														Edit
													</Button>
													<Button
														variant="outline"
														size="sm"
														onClick={() => handleDeleteLPMRuleClick(rule)}
														className="h-9 px-3 text-xs text-red-600 hover:text-red-700 hover:bg-red-50"
													>
														<Trash2 className="h-3.5 w-3.5" />
													</Button>
												</div>
											</div>
										))}
									</div>

									{/* Desktop Table Layout */}
									<div className="hidden md:block overflow-x-auto">
										<Table>
											<TableHeader>
												<TableRow>
													<TableHead className="text-sm">Name</TableHead>
													<TableHead className="text-sm">Keywords</TableHead>
													<TableHead className="text-sm">Incident Types</TableHead>
													<TableHead className="text-sm">LPM Region</TableHead>
													<TableHead className="text-sm">Channels</TableHead>
													<TableHead className="text-sm">Status</TableHead>
													<TableHead className="text-right text-sm">Actions</TableHead>
												</TableRow>
											</TableHeader>
											<TableBody>
												{lpmRules.map(rule => (
													<TableRow key={rule.id}>
														<TableCell className="font-medium">{rule.name}</TableCell>
														<TableCell>
															<div className="flex flex-wrap gap-1">
																{rule.keywords.slice(0, 2).map((keyword, idx) => (
																	<Badge key={idx} variant="secondary" className="text-xs">
																		{keyword}
																	</Badge>
																))}
																{rule.keywords.length > 2 && (
																	<Badge variant="secondary" className="text-xs">
																		+{rule.keywords.length - 2}
																	</Badge>
																)}
															</div>
														</TableCell>
														<TableCell>
															<div className="flex flex-wrap gap-1">
																{rule.incidentTypes.slice(0, 2).map((type, idx) => (
																	<Badge key={idx} variant="outline" className="text-xs">
																		{type}
																	</Badge>
																))}
																{rule.incidentTypes.length > 2 && (
																	<Badge variant="outline" className="text-xs">
																		+{rule.incidentTypes.length - 2}
																	</Badge>
																)}
															</div>
														</TableCell>
														<TableCell>{rule.lpmRegion}</TableCell>
														<TableCell>
															<div className="flex flex-wrap gap-1">
																{rule.channels.map((channel, idx) => (
																	<Badge key={idx} variant="default" className="text-xs">
																		{channel}
																	</Badge>
																))}
															</div>
														</TableCell>
														<TableCell>
															<Button
																variant="ghost"
																size="sm"
																onClick={() => rule.id && handleToggleLPMRuleStatus(rule.id)}
																className="h-6"
															>
																{rule.isActive ? (
																	<CheckCircle2 className="h-4 w-4 text-green-600" />
																) : (
																	<XCircle className="h-4 w-4 text-gray-400" />
																)}
															</Button>
														</TableCell>
														<TableCell className="text-right">
															<div className="flex justify-end gap-2">
																<Button
																	variant="ghost"
																	size="sm"
																	onClick={() => handleEditLPMRule(rule)}
																>
																	<Edit2 className="h-4 w-4" />
																</Button>
																<Button
																	variant="ghost"
																	size="sm"
																	onClick={() => handleDeleteLPMRuleClick(rule)}
																>
																	<Trash2 className="h-4 w-4 text-destructive" />
																</Button>
															</div>
														</TableCell>
													</TableRow>
												))}
											</TableBody>
										</Table>
									</div>
								</>
							)}
						</CardContent>
					</Card>
				</TabsContent>
			</Tabs>

			{/* Store Alert Rule Dialog */}
			<Dialog open={isStoreDialogOpen} onOpenChange={setIsStoreDialogOpen}>
				<DialogContent className="max-w-[95vw] sm:max-w-2xl max-h-[90vh] overflow-y-auto p-4 sm:p-6">
					<DialogHeader>
						<DialogTitle className="text-base sm:text-lg">
							{editingStoreRule ? 'Edit Store Alert Rule' : 'Create Store Alert Rule'}
						</DialogTitle>
						<DialogDescription className="text-xs sm:text-sm">
							Configure alert rules that will notify stores when incidents match the specified criteria
						</DialogDescription>
					</DialogHeader>
					<StoreAlertRuleForm
						initialData={editingStoreRule}
						onSubmit={handleStoreRuleSubmit}
						onCancel={() => {
							setIsStoreDialogOpen(false)
							setEditingStoreRule(null)
						}}
					/>
				</DialogContent>
			</Dialog>

			{/* LPM Alert Rule Dialog */}
			<Dialog open={isLPMDialogOpen} onOpenChange={setIsLPMDialogOpen}>
				<DialogContent className="max-w-[95vw] sm:max-w-2xl max-h-[90vh] overflow-y-auto p-4 sm:p-6">
					<DialogHeader>
						<DialogTitle className="text-base sm:text-lg">
							{editingLPMRule ? 'Edit LPM Alert Rule' : 'Create LPM Alert Rule'}
						</DialogTitle>
						<DialogDescription className="text-xs sm:text-sm">
							Configure alert rules that will notify Loss Prevention Managers in specific regions
						</DialogDescription>
					</DialogHeader>
					<LPMAlertRuleForm
						initialData={editingLPMRule}
						onSubmit={handleLPMRuleSubmit}
						onCancel={() => {
							setIsLPMDialogOpen(false)
							setEditingLPMRule(null)
						}}
					/>
				</DialogContent>
			</Dialog>

			{/* Delete Store Rule Confirmation Dialog */}
			<AlertDialog open={deleteStoreDialogOpen} onOpenChange={setDeleteStoreDialogOpen}>
				<AlertDialogContent className="max-w-[95vw] sm:max-w-md">
					<AlertDialogHeader>
						<AlertDialogTitle className="text-base sm:text-lg">Delete Store Alert Rule</AlertDialogTitle>
						<AlertDialogDescription className="text-xs sm:text-sm">
							Are you sure you want to delete the rule <strong>"{storeRuleToDelete?.name}"</strong>? 
							This action cannot be undone and the rule will stop triggering alerts.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter className="gap-2 sm:gap-0">
						<AlertDialogCancel onClick={() => setStoreRuleToDelete(null)} className="text-sm">
							Cancel
						</AlertDialogCancel>
						<AlertDialogAction
							onClick={handleDeleteStoreRule}
							className="bg-red-600 hover:bg-red-700 text-sm"
						>
							Delete Rule
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>

			{/* Delete LPM Rule Confirmation Dialog */}
			<AlertDialog open={deleteLPMDialogOpen} onOpenChange={setDeleteLPMDialogOpen}>
				<AlertDialogContent className="max-w-[95vw] sm:max-w-md">
					<AlertDialogHeader>
						<AlertDialogTitle className="text-base sm:text-lg">Delete LPM Alert Rule</AlertDialogTitle>
						<AlertDialogDescription className="text-xs sm:text-sm">
							Are you sure you want to delete the rule <strong>"{lpmRuleToDelete?.name}"</strong>? 
							This action cannot be undone and the rule will stop triggering alerts.
						</AlertDialogDescription>
					</AlertDialogHeader>
					<AlertDialogFooter className="gap-2 sm:gap-0">
						<AlertDialogCancel onClick={() => setLpmRuleToDelete(null)} className="text-sm">
							Cancel
						</AlertDialogCancel>
						<AlertDialogAction
							onClick={handleDeleteLPMRule}
							className="bg-red-600 hover:bg-red-700 text-sm"
						>
							Delete Rule
						</AlertDialogAction>
					</AlertDialogFooter>
				</AlertDialogContent>
			</AlertDialog>
			</div>
		</div>
	)
}

