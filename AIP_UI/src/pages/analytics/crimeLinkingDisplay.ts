import type { IncidentCluster } from '@/types/analytics'
import { formatOffenderDisplayName } from './offenderDisplay'

export const getClusterTitle = (cluster: IncidentCluster): string => {
	if (cluster.title?.trim()) {
		return cluster.title.trim()
	}

	if (cluster.suspectedOffender?.name) {
		return `${formatOffenderDisplayName(
			cluster.suspectedOffender.name,
			cluster.suspectedOffender.id,
		)} · ${cluster.incidents.length} incidents`
	}

	if (cluster.reason?.trim()) {
		const short = cluster.reason.split('.')[0]?.trim()
		if (short) return short
	}

	return `Incident cluster · ${cluster.incidents.length} incidents`
}

export const formatConfidencePercent = (confidence: number): string => {
	const pct = confidence <= 1 ? confidence * 100 : confidence
	return `${Math.round(pct)}%`
}
