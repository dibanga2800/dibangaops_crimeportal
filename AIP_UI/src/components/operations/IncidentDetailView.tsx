import { format } from 'date-fns'
import { Incident } from '@/types/incidents'

interface IncidentDetailViewProps {
	incident: Incident
}

export const IncidentDetailView = ({ incident }: IncidentDetailViewProps) => {
	const totalStolenValue =
		typeof incident.totalStolenValue === 'number' && !isNaN(incident.totalStolenValue)
			? incident.totalStolenValue
			: (incident.stolenItems ?? []).reduce((sum, item) => sum + (item.totalAmount || 0), 0)
	const totalRecoveredValue =
		typeof incident.totalRecoveredValue === 'number' && !isNaN(incident.totalRecoveredValue)
			? incident.totalRecoveredValue
			: typeof incident.totalValueRecovered === 'number' && !isNaN(incident.totalValueRecovered)
				? incident.totalValueRecovered
				: (incident.stolenItems ?? []).reduce((sum, item) => sum + (item.recoveredAmount || 0), 0)
	const totalLostValue =
		typeof incident.totalLostValue === 'number' && !isNaN(incident.totalLostValue)
			? incident.totalLostValue
			: Math.max(totalStolenValue - totalRecoveredValue, 0)

	return (
		<div className="bg-[#F8F3F1]">
			<div className="w-full max-w-[98%] mx-auto px-4 py-4">
				{/* Basic Information */}
				<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
					<div className="flex items-center gap-2 mb-4">
						<div className="h-6 w-6 text-blue-600">📋</div>
						<h2 className="text-lg font-medium text-gray-900">Basic Information</h2>
					</div>
					<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
						<div>
							<label className="text-sm font-medium text-gray-500">Company Name</label>
							<p className="mt-1 text-sm text-gray-900">{incident.customerName || 'N/A'}</p>
						</div>
						<div>
						<label className="text-sm font-medium text-gray-500">Store Name</label>
						<p className="mt-1 text-sm text-gray-900">{incident.siteName || 'N/A'}</p>
					</div>
					<div>
						<label className="text-sm font-medium text-gray-500">Staff Member Name</label>
						<p className="mt-1 text-sm text-gray-900">{incident.officerName || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Assigned To</label>
							<p className="mt-1 text-sm text-gray-900">{incident.assignedTo || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Status</label>
							<p className="mt-1 text-sm text-gray-900">{incident.status || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Date</label>
							<p className="mt-1 text-sm text-gray-900">
								{incident.dateOfIncident
									? format(new Date(incident.dateOfIncident), 'dd MMM yyyy')
									: 'N/A'}
							</p>
						</div>
					</div>
				</div>

				{/* Incident Details */}
				<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
					<div className="flex items-center gap-2 mb-4">
						<div className="h-6 w-6 text-blue-600">🕒</div>
						<h2 className="text-lg font-medium text-gray-900">Incident Details</h2>
					</div>
					<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
						<div>
							<label className="text-sm font-medium text-gray-500">Date of Incident</label>
							<p className="mt-1 text-sm text-gray-900">
								{incident.dateOfIncident
									? format(new Date(incident.dateOfIncident), 'dd MMM yyyy')
									: 'N/A'}
							</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Time</label>
							<p className="mt-1 text-sm text-gray-900">{incident.timeOfIncident || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Priority</label>
							<p className="mt-1 text-sm text-gray-900">{incident.priority || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Incident Type</label>
							<p className="mt-1 text-sm text-gray-900">{incident.incidentType || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Total Value Stolen</label>
							<p className="mt-1 text-sm text-gray-900">£{totalStolenValue.toFixed(2)}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Value Saved</label>
							<p className="mt-1 text-sm text-emerald-700">£{totalRecoveredValue.toFixed(2)}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-gray-500">Value Lost</label>
							<p className="mt-1 text-sm text-rose-700">£{totalLostValue.toFixed(2)}</p>
						</div>
						{incident.regionName && (
							<div>
								<label className="text-sm font-medium text-gray-500">Region</label>
								<p className="mt-1 text-sm text-gray-900">{incident.regionName}</p>
							</div>
						)}
					</div>
				</div>

				{/* Description */}
				<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
					<div className="flex items-center gap-2 mb-4">
						<div className="h-6 w-6 text-blue-600">📝</div>
						<h2 className="text-lg font-medium text-gray-900">Description</h2>
					</div>
					<div className="space-y-4">
						<div>
							<label className="text-sm font-medium text-gray-500">Incident Details</label>
							<p className="mt-1 text-sm text-gray-900 whitespace-pre-wrap">
								{incident.description || incident.incidentDetails || 'N/A'}
							</p>
						</div>
						{incident.storeComments && (
							<div>
								<label className="text-sm font-medium text-gray-500">Store Comments</label>
								<p className="mt-1 text-sm text-gray-900 whitespace-pre-wrap">
									{incident.storeComments}
								</p>
							</div>
						)}
						{incident.actionTaken && (
							<div>
								<label className="text-sm font-medium text-gray-500">Action Taken</label>
								<p className="mt-1 text-sm text-gray-900 whitespace-pre-wrap">
									{incident.actionTaken}
								</p>
							</div>
						)}
					</div>
				</div>

				{/* Police Involvement */}
				<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
					<div className="flex items-center gap-2 mb-4">
						<div className="h-6 w-6 text-blue-600">👮</div>
						<h2 className="text-lg font-medium text-gray-900">Police Involvement</h2>
					</div>
					<div className="space-y-4">
						<div>
							<label className="text-sm font-medium text-gray-500">Was Police Involved?</label>
							<p className="mt-1 text-sm text-gray-900">
								{incident.policeInvolvement ? 'Yes' : 'No'}
							</p>
						</div>
						{incident.policeInvolvement && (
							<div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
								{incident.urnNumber && (
									<div>
										<label className="text-sm font-medium text-gray-500">URN Number</label>
										<p className="mt-1 text-sm text-gray-900">{incident.urnNumber}</p>
									</div>
								)}
								{incident.crimeRefNumber && (
									<div>
										<label className="text-sm font-medium text-gray-500">
											Crime Reference Number
										</label>
										<p className="mt-1 text-sm text-gray-900">{incident.crimeRefNumber}</p>
									</div>
								)}
								{incident.policeID && (
									<div>
										<label className="text-sm font-medium text-gray-500">Police ID</label>
										<p className="mt-1 text-sm text-gray-900">{incident.policeID}</p>
									</div>
								)}
							</div>
						)}
					</div>
				</div>

				{/* Offender Details */}
				{incident.offenderName && (
					<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
						<div className="flex items-center gap-2 mb-4">
							<div className="h-6 w-6 text-blue-600">👤</div>
							<h2 className="text-lg font-medium text-gray-900">Offender Details</h2>
						</div>
						<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
							<div>
								<label className="text-sm font-medium text-gray-500">Name</label>
								<p className="mt-1 text-sm text-gray-900">{incident.offenderName}</p>
							</div>
							<div>
								<label className="text-sm font-medium text-gray-500">Sex/Gender</label>
								<p className="mt-1 text-sm text-gray-900">
									{incident.offenderSex || incident.gender || 'N/A'}
								</p>
							</div>
							{incident.offenderDOB && (
								<div>
									<label className="text-sm font-medium text-gray-500">Date of Birth</label>
									<p className="mt-1 text-sm text-gray-900">
										{format(new Date(incident.offenderDOB), 'dd MMM yyyy')}
									</p>
								</div>
							)}
							{incident.offenderPlaceOfBirth && (
								<div>
									<label className="text-sm font-medium text-gray-500">Place of Birth</label>
									<p className="mt-1 text-sm text-gray-900">{incident.offenderPlaceOfBirth}</p>
								</div>
							)}
							{incident.offenderMarks && (
								<div>
									<label className="text-sm font-medium text-gray-500">Distinguishing Marks</label>
									<p className="mt-1 text-sm text-gray-900">{incident.offenderMarks}</p>
								</div>
							)}
							{incident.offenderAddress && (
								<>
									<div>
										<label className="text-sm font-medium text-gray-500">Address</label>
										<p className="mt-1 text-sm text-gray-900">
											{incident.offenderAddress.numberAndStreet || 'N/A'}
										</p>
									</div>
									<div>
										<label className="text-sm font-medium text-gray-500">Town</label>
										<p className="mt-1 text-sm text-gray-900">
											{incident.offenderAddress.town || 'N/A'}
										</p>
									</div>
									<div>
										<label className="text-sm font-medium text-gray-500">Post Code</label>
										<p className="mt-1 text-sm text-gray-900">
											{incident.offenderAddress.postCode || 'N/A'}
										</p>
									</div>
								</>
							)}
						</div>
					</div>
				)}

				{/* Incident Categories */}
				{incident.incidentInvolved && incident.incidentInvolved.length > 0 && (
					<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
						<div className="flex items-center gap-2 mb-4">
							<div className="h-6 w-6 text-blue-600">🏷️</div>
							<h2 className="text-lg font-medium text-gray-900">Incident Categories</h2>
						</div>
						<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
							{incident.incidentInvolved.map((type, index) => (
								<div key={index} className="flex items-center gap-2">
									<div className="h-2 w-2 rounded-full bg-blue-600"></div>
									<p className="text-sm text-gray-900">{type}</p>
								</div>
							))}
						</div>
					</div>
				)}

				{/* Stolen Items */}
				{incident.stolenItems && incident.stolenItems.length > 0 && (
					<div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
						<div className="flex items-center gap-2 mb-4">
							<div className="h-6 w-6 text-blue-600">💰</div>
							<h2 className="text-lg font-medium text-gray-900">Stolen Items</h2>
						</div>
						<div className="overflow-x-auto">
							<table className="w-full">
								<thead>
									<tr className="border-b">
										<th className="text-left py-2 text-sm font-medium text-gray-500">Category</th>
										<th className="text-left py-2 text-sm font-medium text-gray-500">Product Name</th>
										<th className="text-left py-2 text-sm font-medium text-gray-500">Description</th>
										<th className="text-right py-2 text-sm font-medium text-gray-500">Cost</th>
										<th className="text-right py-2 text-sm font-medium text-gray-500">Qty</th>
										<th className="text-right py-2 text-sm font-medium text-gray-500">Total</th>
										<th className="text-center py-2 text-sm font-medium text-gray-500">Recovered</th>
										<th className="text-right py-2 text-sm font-medium text-gray-500">Recovered Qty</th>
										<th className="text-right py-2 text-sm font-medium text-gray-500">Saved</th>
										<th className="text-right py-2 text-sm font-medium text-gray-500">Lost</th>
									</tr>
								</thead>
								<tbody>
									{incident.stolenItems.map((item, index) => {
										const cost = typeof item.cost === 'number' && !isNaN(item.cost) ? item.cost : 0
										const quantity =
											typeof item.quantity === 'number' && !isNaN(item.quantity) ? item.quantity : 0
										const totalAmount =
											typeof item.totalAmount === 'number' && !isNaN(item.totalAmount)
												? item.totalAmount
												: cost * quantity
										const recoveredQuantity =
											typeof item.recoveredQuantity === 'number' && !isNaN(item.recoveredQuantity)
												? item.recoveredQuantity
												: 0
										const recoveredAmount =
											typeof item.recoveredAmount === 'number' && !isNaN(item.recoveredAmount)
												? item.recoveredAmount
												: cost * recoveredQuantity
										const lostAmount =
											typeof item.lostAmount === 'number' && !isNaN(item.lostAmount)
												? item.lostAmount
												: totalAmount - recoveredAmount
										return (
											<tr key={index} className="border-b">
												<td className="py-2 text-sm text-gray-900">{item.category || 'N/A'}</td>
												<td className="py-2 text-sm text-gray-900">{item.productName || 'N/A'}</td>
												<td className="py-2 text-sm text-gray-900">{item.description || 'N/A'}</td>
												<td className="py-2 text-sm text-gray-900 text-right">£{cost.toFixed(2)}</td>
												<td className="py-2 text-sm text-gray-900 text-right">{quantity}</td>
												<td className="py-2 text-sm text-gray-900 text-right">
													£{totalAmount.toFixed(2)}
												</td>
												<td className="py-2 text-sm text-gray-900 text-center">
													{item.wasRecovered ? 'Yes' : 'No'}
												</td>
												<td className="py-2 text-sm text-gray-900 text-right">{recoveredQuantity}</td>
												<td className="py-2 text-sm text-emerald-700 text-right">
													£{recoveredAmount.toFixed(2)}
												</td>
												<td className="py-2 text-sm text-rose-700 text-right">
													£{lostAmount.toFixed(2)}
												</td>
											</tr>
										)
									})}
									<tr className="bg-gray-50">
										<td colSpan={6} className="py-2 text-sm font-medium text-gray-900">
											Totals
										</td>
										<td className="py-2 text-sm font-medium text-gray-900 text-right">
											£{totalStolenValue.toFixed(2)}
										</td>
										<td className="py-2 text-sm font-medium text-gray-900 text-center">-</td>
										<td className="py-2 text-sm font-medium text-gray-900 text-right">
											{incident.totalRecoveredQuantity ?? 0}
										</td>
										<td className="py-2 text-sm font-medium text-emerald-700 text-right">
											£{totalRecoveredValue.toFixed(2)}
										</td>
										<td className="py-2 text-sm font-medium text-rose-700 text-right">
											£{totalLostValue.toFixed(2)}
										</td>
									</tr>
								</tbody>
							</table>
						</div>
					</div>
				)}
			</div>
		</div>
	)
}

