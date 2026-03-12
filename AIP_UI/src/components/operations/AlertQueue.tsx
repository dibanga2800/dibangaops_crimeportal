import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Bell, CheckCircle, AlertTriangle, ArrowUpRight, Clock } from 'lucide-react'
import { alertInstancesApi } from '@/services/api/alertInstances'
import type { AlertInstance, AlertStatus } from '@/types/alertInstances'
import { cn } from '@/lib/utils'

interface AlertQueueProps {
	customerId?: number
	maxItems?: number
}

const severityColors: Record<string, string> = {
	'high': 'bg-red-100 text-red-800 border-red-200',
	'medium': 'bg-amber-100 text-amber-800 border-amber-200',
	'low': 'bg-blue-100 text-blue-800 border-blue-200'
}

const statusIcons: Record<AlertStatus, typeof Clock> = {
	'new': Bell,
	'acknowledged': CheckCircle,
	'escalated': ArrowUpRight,
	'resolved': CheckCircle
}

export const AlertQueue = ({ customerId, maxItems = 10 }: AlertQueueProps) => {
	const [alerts, setAlerts] = useState<AlertInstance[]>([])
	const [loading, setLoading] = useState(true)
	const [filter, setFilter] = useState<string>('active')

	const loadAlerts = async () => {
		try {
			setLoading(true)
			const status = filter === 'active' ? undefined : filter
			const response = await alertInstancesApi.getAlerts({
				status,
				customerId,
				page: 1,
				pageSize: maxItems
			})
			const filtered = filter === 'active'
				? response.data.filter(a => a.status !== 'resolved')
				: response.data
			setAlerts(filtered)
		} catch (err) {
			console.warn('Alert queue loading failed:', err)
		} finally {
			setLoading(false)
		}
	}

	useEffect(() => {
		loadAlerts()
	}, [filter, customerId])

	const handleAcknowledge = async (id: number) => {
		try {
			await alertInstancesApi.acknowledge(id)
			await loadAlerts()
			window.dispatchEvent(new Event('alert-updated'))
		} catch (err) {
			console.error('Failed to acknowledge alert:', err)
		}
	}

	const handleResolve = async (id: number) => {
		try {
			await alertInstancesApi.resolve(id, { resolutionNotes: 'Resolved from dashboard' })
			await loadAlerts()
			window.dispatchEvent(new Event('alert-resolved'))
		} catch (err) {
			console.error('Failed to resolve alert:', err)
		}
	}

	return (
		<Card>
			<CardHeader className="p-3 pb-2">
				<div className="flex items-center justify-between">
					<CardTitle className="text-sm flex items-center gap-1.5">
						<Bell className="h-4 w-4 text-amber-500" />
						Alert Queue
						{alerts.length > 0 && (
							<Badge variant="destructive" className="text-xs ml-1">{alerts.length}</Badge>
						)}
					</CardTitle>
					<div className="flex gap-1">
						{['active', 'new', 'escalated', 'resolved'].map(f => (
							<Button
								key={f}
								variant={filter === f ? 'default' : 'ghost'}
								size="sm"
								onClick={() => setFilter(f)}
								className="h-6 px-2 text-xs capitalize"
							>
								{f}
							</Button>
						))}
					</div>
				</div>
			</CardHeader>
			<CardContent className="p-0">
				{loading && (
					<div className="flex items-center gap-2 text-sm text-muted-foreground p-4">
						<div className="animate-spin h-4 w-4 border-2 border-amber-500 border-t-transparent rounded-full" />
						Loading alerts...
					</div>
				)}

				{!loading && alerts.length === 0 && (
					<div className="p-6 text-center text-sm text-muted-foreground">
						No alerts to display
					</div>
				)}

				<div className="divide-y max-h-[400px] overflow-y-auto">
					{alerts.map(alert => {
						const StatusIcon = statusIcons[alert.status] || Bell
						return (
							<div
								key={alert.alertInstanceId}
								className={cn(
									'p-3 hover:bg-muted/50 transition-colors',
									alert.severity === 'high' && alert.status === 'new' && 'bg-red-50/50'
								)}
							>
								<div className="flex items-start gap-2">
									<div className={cn(
										'mt-0.5 flex-shrink-0 flex items-center justify-center w-6 h-6 rounded-full',
										alert.severity === 'high' ? 'bg-red-500 text-white' :
										alert.severity === 'medium' ? 'bg-amber-500 text-white' :
										'bg-blue-500 text-white'
									)}>
										<StatusIcon className="h-3 w-3" />
									</div>
									<div className="flex-1 min-w-0">
										<div className="flex items-center gap-1.5 mb-0.5">
											<span className="text-sm font-medium truncate">
												{alert.alertRuleName || 'Alert'}
											</span>
											<Badge className={cn('text-xs', severityColors[alert.severity])}>
												{alert.severity}
											</Badge>
											<Badge variant="outline" className="text-xs capitalize">
												{alert.status}
											</Badge>
										</div>
										<p className="text-xs text-muted-foreground line-clamp-2">{alert.message}</p>
										<div className="flex items-center gap-2 mt-1.5">
											<span className="text-xs text-muted-foreground">
												{new Date(alert.createdAt).toLocaleString('en-GB', {
													day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit'
												})}
											</span>
											{alert.incidentId && (
												<span className="text-xs text-blue-600">
													Incident #{alert.incidentId}
												</span>
											)}
										</div>
										{alert.status === 'new' && (
											<div className="flex gap-1 mt-2">
												<Button
													size="sm"
													variant="outline"
													onClick={() => handleAcknowledge(alert.alertInstanceId)}
													className="h-6 px-2 text-xs"
												>
													Acknowledge
												</Button>
												<Button
													size="sm"
													variant="ghost"
													onClick={() => handleResolve(alert.alertInstanceId)}
													className="h-6 px-2 text-xs"
												>
													Resolve
												</Button>
											</div>
										)}
										{alert.status === 'acknowledged' && (
											<Button
												size="sm"
												variant="ghost"
												onClick={() => handleResolve(alert.alertInstanceId)}
												className="h-6 px-2 text-xs mt-2"
											>
												Mark Resolved
											</Button>
										)}
									</div>
								</div>
							</div>
						)
					})}
				</div>
			</CardContent>
		</Card>
	)
}
