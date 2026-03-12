import { useState, useEffect } from 'react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Brain, ChevronDown, ChevronUp, AlertTriangle, CheckCircle, Info } from 'lucide-react'
import { classificationApi } from '@/services/api/classification'
import type { IncidentClassificationResult } from '@/types/classification'
import { cn } from '@/lib/utils'

interface IncidentClassificationBadgeProps {
	incidentId: number
	compact?: boolean
}

export const IncidentClassificationBadge = ({ incidentId, compact = false }: IncidentClassificationBadgeProps) => {
	const [classification, setClassification] = useState<IncidentClassificationResult | null>(null)
	const [loading, setLoading] = useState(false)
	const [expanded, setExpanded] = useState(false)
	const [error, setError] = useState<string | null>(null)

	const handleClassify = async () => {
		setLoading(true)
		setError(null)
		try {
			const result = await classificationApi.classifyExistingIncident(incidentId)
			setClassification(result)
		} catch (err) {
			setError('Classification unavailable')
			console.warn('Classification failed:', err)
		} finally {
			setLoading(false)
		}
	}

	const riskIcon = classification?.riskLevel === 'high'
		? <AlertTriangle className="h-3 w-3" />
		: classification?.riskLevel === 'medium'
			? <Info className="h-3 w-3" />
			: <CheckCircle className="h-3 w-3" />

	const riskColor = classification?.riskLevel === 'high'
		? 'bg-red-100 text-red-800 border-red-200'
		: classification?.riskLevel === 'medium'
			? 'bg-amber-100 text-amber-800 border-amber-200'
			: 'bg-emerald-100 text-emerald-800 border-emerald-200'

	if (compact) {
		return (
			<div className="inline-flex items-center gap-1">
				{!classification && !loading && (
					<Button
						variant="ghost"
						size="sm"
						onClick={handleClassify}
						className="h-6 px-2 text-xs"
					>
						<Brain className="h-3 w-3 mr-1" />
						AI Classify
					</Button>
				)}
				{loading && (
					<Badge variant="secondary" className="text-xs">
						<div className="animate-spin h-3 w-3 border border-current border-t-transparent rounded-full mr-1" />
						Analysing...
					</Badge>
				)}
				{classification && (
					<Badge className={cn('text-xs gap-1', riskColor)}>
						{riskIcon}
						{classification.riskLevel.toUpperCase()} risk
					</Badge>
				)}
			</div>
		)
	}

	return (
		<Card className="border-dashed">
			<CardHeader className="p-3 pb-2">
				<div className="flex items-center justify-between">
					<CardTitle className="text-sm flex items-center gap-1.5">
						<Brain className="h-4 w-4 text-violet-500" />
						AI Classification
					</CardTitle>
					{!classification && !loading && (
						<Button variant="outline" size="sm" onClick={handleClassify} className="h-7 text-xs">
							Classify
						</Button>
					)}
				</div>
			</CardHeader>
			{loading && (
				<CardContent className="p-3 pt-0">
					<div className="flex items-center gap-2 text-sm text-muted-foreground">
						<div className="animate-spin h-4 w-4 border-2 border-violet-500 border-t-transparent rounded-full" />
						Analysing incident...
					</div>
				</CardContent>
			)}
			{error && (
				<CardContent className="p-3 pt-0">
					<p className="text-xs text-muted-foreground">{error}</p>
				</CardContent>
			)}
			{classification && (
				<CardContent className="p-3 pt-0 space-y-2">
					<div className="flex items-center gap-2 flex-wrap">
						<Badge className={cn('gap-1', riskColor)}>
							{riskIcon}
							{classification.riskLevel.toUpperCase()} Risk ({Math.round(classification.riskScore * 100)}%)
						</Badge>
						<Badge variant="outline" className="text-xs">
							{classification.suggestedCategory}
						</Badge>
						<span className="text-xs text-muted-foreground">
							{Math.round(classification.confidence * 100)}% confidence
						</span>
					</div>

					{classification.tags.length > 0 && (
						<div className="flex flex-wrap gap-1">
							{classification.tags.map(tag => (
								<Badge key={tag} variant="secondary" className="text-xs">{tag}</Badge>
							))}
						</div>
					)}

					<button
						onClick={() => setExpanded(!expanded)}
						className="flex items-center gap-1 text-xs text-violet-600 hover:text-violet-800"
					>
						{expanded ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
						{expanded ? 'Hide' : 'Show'} suggested actions
					</button>

					{expanded && classification.suggestedActions.length > 0 && (
						<ul className="text-xs space-y-1 text-muted-foreground pl-4">
							{classification.suggestedActions.map((action, i) => (
								<li key={i} className="list-disc">{action}</li>
							))}
						</ul>
					)}
				</CardContent>
			)}
		</Card>
	)
}
