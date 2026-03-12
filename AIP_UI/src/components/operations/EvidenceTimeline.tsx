import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Package, ScanBarcode, Clock, MapPin, User, Plus } from 'lucide-react'
import { evidenceApi } from '@/services/api/evidence'
import type { EvidenceItem, RegisterEvidencePayload } from '@/types/evidence'
import { cn } from '@/lib/utils'

interface EvidenceTimelineProps {
	incidentId: number
	readonly?: boolean
}

const statusColors: Record<string, string> = {
	'registered': 'bg-blue-100 text-blue-800',
	'in-transit': 'bg-amber-100 text-amber-800',
	'in-storage': 'bg-emerald-100 text-emerald-800',
	'released': 'bg-violet-100 text-violet-800',
	'disposed': 'bg-gray-100 text-gray-800',
	'returned': 'bg-teal-100 text-teal-800'
}

const eventIcons: Record<string, typeof Clock> = {
	'registered': Package,
	'transferred': MapPin,
	'received': Package,
	'scanned': ScanBarcode,
	'released': User,
	'disposed': Package,
	'returned': User
}

export const EvidenceTimeline = ({ incidentId, readonly = false }: EvidenceTimelineProps) => {
	const [evidence, setEvidence] = useState<EvidenceItem[]>([])
	const [loading, setLoading] = useState(true)
	const [showRegister, setShowRegister] = useState(false)
	const [newBarcode, setNewBarcode] = useState('')
	const [newType, setNewType] = useState('')
	const [newDescription, setNewDescription] = useState('')
	const [registering, setRegistering] = useState(false)

	const loadEvidence = async () => {
		try {
			setLoading(true)
			const response = await evidenceApi.getByIncident(incidentId)
			setEvidence(response.data)
		} catch (err) {
			console.warn('Evidence loading failed:', err)
		} finally {
			setLoading(false)
		}
	}

	useEffect(() => {
		loadEvidence()
	}, [incidentId])

	const handleRegister = async () => {
		if (!newBarcode || !newType) return
		setRegistering(true)
		try {
			const payload: RegisterEvidencePayload = {
				barcode: newBarcode,
				evidenceType: newType,
				description: newDescription || undefined
			}
			await evidenceApi.registerEvidence(incidentId, payload)
			setNewBarcode('')
			setNewType('')
			setNewDescription('')
			setShowRegister(false)
			await loadEvidence()
		} catch (err) {
			console.error('Evidence registration failed:', err)
		} finally {
			setRegistering(false)
		}
	}

	return (
		<Card>
			<CardHeader className="p-3 pb-2">
				<div className="flex items-center justify-between">
					<CardTitle className="text-sm flex items-center gap-1.5">
						<ScanBarcode className="h-4 w-4 text-blue-500" />
						Evidence Chain
						{evidence.length > 0 && (
							<Badge variant="secondary" className="text-xs ml-1">{evidence.length}</Badge>
						)}
					</CardTitle>
					{!readonly && (
						<Button
							variant="outline"
							size="sm"
							onClick={() => setShowRegister(!showRegister)}
							className="h-7 text-xs"
						>
							<Plus className="h-3 w-3 mr-1" />
							Register
						</Button>
					)}
				</div>
			</CardHeader>
			<CardContent className="p-3 pt-0 space-y-3">
				{showRegister && (
					<div className="space-y-2 p-3 bg-muted/50 rounded-md">
						<Input
							placeholder="Barcode (e.g. EAN-13)"
							value={newBarcode}
							onChange={e => setNewBarcode(e.target.value)}
							className="h-8 text-sm"
						/>
						<Input
							placeholder="Evidence type (e.g. CCTV, Physical, Document)"
							value={newType}
							onChange={e => setNewType(e.target.value)}
							className="h-8 text-sm"
						/>
						<Input
							placeholder="Description (optional)"
							value={newDescription}
							onChange={e => setNewDescription(e.target.value)}
							className="h-8 text-sm"
						/>
						<div className="flex gap-2">
							<Button
								size="sm"
								onClick={handleRegister}
								disabled={!newBarcode || !newType || registering}
								className="h-7 text-xs"
							>
								{registering ? 'Registering...' : 'Register Evidence'}
							</Button>
							<Button
								size="sm"
								variant="ghost"
								onClick={() => setShowRegister(false)}
								className="h-7 text-xs"
							>
								Cancel
							</Button>
						</div>
					</div>
				)}

				{loading && (
					<div className="flex items-center gap-2 text-sm text-muted-foreground py-2">
						<div className="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full" />
						Loading evidence...
					</div>
				)}

				{!loading && evidence.length === 0 && (
					<p className="text-xs text-muted-foreground py-2">
						No evidence registered for this incident.
					</p>
				)}

				{evidence.map(item => (
					<div key={item.evidenceItemId} className="border rounded-md p-2 space-y-2">
						<div className="flex items-center justify-between">
							<div className="flex items-center gap-2">
								<ScanBarcode className="h-4 w-4 text-muted-foreground" />
								<span className="text-sm font-mono font-medium">{item.barcode}</span>
							</div>
							<Badge className={cn('text-xs', statusColors[item.status] || 'bg-gray-100')}>
								{item.status}
							</Badge>
						</div>
						<div className="text-xs text-muted-foreground">
							{item.evidenceType} {item.description ? `— ${item.description}` : ''}
						</div>

						{item.custodyEvents.length > 0 && (
							<div className="relative pl-4 space-y-1.5 border-l border-gray-200 ml-1">
								{item.custodyEvents.map(event => {
									const Icon = eventIcons[event.eventType] || Clock
									return (
										<div key={event.custodyEventId} className="flex items-start gap-2">
											<div className="absolute -left-[7px] w-3.5 h-3.5 rounded-full bg-white border-2 border-blue-400 flex items-center justify-center">
												<Icon className="h-2 w-2 text-blue-500" />
											</div>
											<div className="flex-1 ml-2">
												<div className="flex items-center gap-1.5">
													<span className="text-xs font-medium capitalize">{event.eventType}</span>
													<span className="text-xs text-muted-foreground">
														{new Date(event.eventTimestamp).toLocaleString('en-GB', {
															day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit'
														})}
													</span>
												</div>
												{event.notes && <p className="text-xs text-muted-foreground">{event.notes}</p>}
											</div>
										</div>
									)
								})}
							</div>
						)}
					</div>
				))}
			</CardContent>
		</Card>
	)
}
