import { useMemo } from 'react'
import type { OffenderNetworkData } from '@/types/analytics'

interface OffenderNetworkGraphProps {
	data: OffenderNetworkData
}

const VIEW_SIZE = 600
const OFFENDER_COLOR = '#3b82f6'
const STORE_COLOR = '#22c55e'
const LINK_COLOR = '#94a3b8'

export const OffenderNetworkGraph = ({ data }: OffenderNetworkGraphProps) => {
	const { nodes, links } = data

	const nodeById = useMemo(
		() => new Map(nodes.map((node) => [node.id, node])),
		[nodes],
	)

	if (nodes.length === 0) {
		return (
			<p className="flex min-h-[320px] items-center justify-center rounded-lg border bg-gray-50 text-sm text-gray-500">
				No repeat offenders with store links in this period.
			</p>
		)
	}

	return (
		<div className="w-full overflow-x-auto">
			<svg
				viewBox={`0 0 ${VIEW_SIZE} ${VIEW_SIZE}`}
				className="mx-auto h-auto w-full max-w-3xl rounded-lg border bg-white"
				role="img"
				aria-label="Offender and store network graph"
			>
				{links.map((link) => {
					const source = nodeById.get(link.source)
					const target = nodeById.get(link.target)
					if (!source || !target) return null

					const strokeWidth = 1 + link.strength * 4

					return (
						<line
							key={`${link.source}-${link.target}`}
							x1={source.x}
							y1={source.y}
							x2={target.x}
							y2={target.y}
							stroke={LINK_COLOR}
							strokeWidth={strokeWidth}
							strokeOpacity={0.55}
						/>
					)
				})}

				{nodes.map((node) => {
					const isOffender = node.type === 'offender'
					const radius = isOffender ? 14 : 10
					const fill = isOffender ? OFFENDER_COLOR : STORE_COLOR

					return (
						<g key={node.id}>
							<circle cx={node.x} cy={node.y} r={radius} fill={fill} />
							<text
								x={node.x}
								y={node.y + radius + 12}
								textAnchor="middle"
								className="fill-gray-700 text-[10px]"
							>
								{node.name.length > 22 ? `${node.name.slice(0, 20)}…` : node.name}
							</text>
							<title>
								{node.name} ({isOffender ? 'offender' : 'store'})
							</title>
						</g>
					)
				})}
			</svg>
		</div>
	)
}
