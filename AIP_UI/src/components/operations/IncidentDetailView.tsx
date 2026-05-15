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

	const showOffenderBlock =
		Boolean(incident.offenderName?.trim()) ||
		Boolean(incident.offenderId?.trim()) ||
		(incident.modusOperandi?.length ?? 0) > 0

	return (
		<div className="bg-muted/30 dark:bg-slate-950/40">
			<div className="mx-auto w-full max-w-[98%] px-4 py-4">
				{/* Basic Information */}
				<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
					<div className="mb-4 flex items-center gap-2">
						<div className="h-6 w-6 text-blue-600">📋</div>
						<h2 className="text-lg font-medium text-card-foreground">Basic Information</h2>
					</div>
					<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
						<div>
							<label className="text-sm font-medium text-muted-foreground">Company Name</label>
							<p className="mt-1 text-sm text-card-foreground">{incident.customerName || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Store Name</label>
							<p className="mt-1 text-sm text-card-foreground">{incident.siteName || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Staff Member Name</label>
							<p className="mt-1 text-sm text-card-foreground">{incident.officerName || 'N/A'}</p>
						</div>
						{incident.officerRole && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Officer Role</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.officerRole}</p>
							</div>
						)}
						{incident.dutyManagerName && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Duty Manager</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.dutyManagerName}</p>
							</div>
						)}
						{incident.reportNumber && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Report Number</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.reportNumber}</p>
							</div>
						)}
						{incident.location && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Location / Area</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.location}</p>
							</div>
						)}
						<div>
							<label className="text-sm font-medium text-muted-foreground">Date</label>
							<p className="mt-1 text-sm text-card-foreground">
								{incident.dateOfIncident
									? format(new Date(incident.dateOfIncident), 'dd MMM yyyy')
									: incident.date
										? format(new Date(incident.date), 'dd MMM yyyy')
										: 'N/A'}
							</p>
						</div>
					</div>
				</div>

				{/* Incident Details */}
				<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
					<div className="mb-4 flex items-center gap-2">
						<div className="h-6 w-6 text-blue-600">🕒</div>
						<h2 className="text-lg font-medium text-card-foreground">Incident Details</h2>
					</div>
					<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
						<div>
							<label className="text-sm font-medium text-muted-foreground">Date of Incident</label>
							<p className="mt-1 text-sm text-card-foreground">
								{incident.dateOfIncident
									? format(new Date(incident.dateOfIncident), 'dd MMM yyyy')
									: incident.date
										? format(new Date(incident.date), 'dd MMM yyyy')
										: 'N/A'}
							</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Time</label>
							<p className="mt-1 text-sm text-card-foreground">{incident.timeOfIncident || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Priority</label>
							<p className="mt-1 text-sm text-card-foreground">{incident.priority || 'N/A'}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Incident Type</label>
							<p className="mt-1 text-sm text-card-foreground">{incident.incidentType || incident.type || 'N/A'}</p>
						</div>
						{incident.actionCode && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Action Code</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.actionCode}</p>
							</div>
						)}
						<div>
							<label className="text-sm font-medium text-muted-foreground">Total Value Stolen</label>
							<p className="mt-1 text-sm text-card-foreground">£{totalStolenValue.toFixed(2)}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Value Saved</label>
							<p className="mt-1 text-sm text-emerald-700 dark:text-emerald-400">£{totalRecoveredValue.toFixed(2)}</p>
						</div>
						<div>
							<label className="text-sm font-medium text-muted-foreground">Value Lost</label>
							<p className="mt-1 text-sm text-rose-700 dark:text-rose-400">£{totalLostValue.toFixed(2)}</p>
						</div>
						{incident.regionName && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Region</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.regionName}</p>
							</div>
						)}
					</div>
				</div>

				{/* Description */}
				<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
					<div className="mb-4 flex items-center gap-2">
						<div className="h-6 w-6 text-blue-600">📝</div>
						<h2 className="text-lg font-medium text-card-foreground">Description</h2>
					</div>
					<div className="space-y-4">
						<div>
							<label className="text-sm font-medium text-muted-foreground">Incident Details</label>
							<p className="mt-1 whitespace-pre-wrap text-sm text-card-foreground">
								{incident.description || incident.incidentDetails || 'N/A'}
							</p>
						</div>
						{incident.storeComments && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Store Comments</label>
								<p className="mt-1 whitespace-pre-wrap text-sm text-card-foreground">
									{incident.storeComments}
								</p>
							</div>
						)}
						{incident.actionTaken && (
							<div>
								<label className="text-sm font-medium text-muted-foreground">Action Taken</label>
								<p className="mt-1 whitespace-pre-wrap text-sm text-card-foreground">
									{incident.actionTaken}
								</p>
							</div>
						)}
					</div>
				</div>

				{incident.arrestSaveComment && (
					<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
						<div className="mb-4 flex items-center gap-2">
							<div className="h-6 w-6 text-blue-600">⚖️</div>
							<h2 className="text-lg font-medium text-card-foreground">Arrest / Save</h2>
						</div>
						<p className="whitespace-pre-wrap text-sm text-card-foreground">{incident.arrestSaveComment}</p>
					</div>
				)}

				{(incident.offenderDetailsVerified != null ||
					incident.verificationMethod ||
					incident.verificationEvidenceImage) && (
					<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
						<div className="mb-4 flex items-center gap-2">
							<div className="h-6 w-6 text-blue-600">✅</div>
							<h2 className="text-lg font-medium text-card-foreground">Offender verification</h2>
						</div>
						<div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
							<div>
								<label className="text-sm font-medium text-muted-foreground">Details verified</label>
								<p className="mt-1 text-sm text-card-foreground">
									{incident.offenderDetailsVerified ? 'Yes' : incident.offenderDetailsVerified === false ? 'No' : 'N/A'}
								</p>
							</div>
							{incident.verificationMethod && (
								<div>
									<label className="text-sm font-medium text-muted-foreground">Verification method</label>
									<p className="mt-1 text-sm text-card-foreground">{incident.verificationMethod}</p>
								</div>
							)}
							{incident.verificationEvidenceImage && (
								<div className="sm:col-span-2">
									<label className="text-sm font-medium text-muted-foreground">Verification evidence</label>
									<p className="mt-1 truncate text-sm text-card-foreground" title={incident.verificationEvidenceImage}>
										{incident.verificationEvidenceImage}
									</p>
								</div>
							)}
						</div>
					</div>
				)}

				{/* Police Involvement */}
				<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
					<div className="mb-4 flex items-center gap-2">
						<div className="h-6 w-6 text-blue-600">👮</div>
						<h2 className="text-lg font-medium text-card-foreground">Police Involvement</h2>
					</div>
					<div className="space-y-4">
						<div>
							<label className="text-sm font-medium text-muted-foreground">Was Police Involved?</label>
							<p className="mt-1 text-sm text-card-foreground">
								{incident.policeInvolvement ? 'Yes' : 'No'}
							</p>
						</div>
						{incident.policeInvolvement && (
							<div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
								{incident.urnNumber && (
									<div>
										<label className="text-sm font-medium text-muted-foreground">URN Number</label>
										<p className="mt-1 text-sm text-card-foreground">{incident.urnNumber}</p>
									</div>
								)}
								{incident.crimeRefNumber && (
									<div>
										<label className="text-sm font-medium text-muted-foreground">
											Crime Reference Number
										</label>
										<p className="mt-1 text-sm text-card-foreground">{incident.crimeRefNumber}</p>
									</div>
								)}
								{incident.policeID && (
									<div>
										<label className="text-sm font-medium text-muted-foreground">Police ID</label>
										<p className="mt-1 text-sm text-card-foreground">{incident.policeID}</p>
									</div>
								)}
							</div>
						)}
					</div>
				</div>

				{/* Offender Details */}
				{showOffenderBlock && (
					<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
						<div className="mb-4 flex items-center gap-2">
							<div className="h-6 w-6 text-blue-600">👤</div>
							<h2 className="text-lg font-medium text-card-foreground">Offender Details</h2>
						</div>
						<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
							<div>
								<label className="text-sm font-medium text-muted-foreground">Name</label>
								<p className="mt-1 text-sm text-card-foreground">{incident.offenderName?.trim() || 'N/A'}</p>
							</div>
							{incident.offenderId && (
								<div>
									<label className="text-sm font-medium text-muted-foreground">Offender ID</label>
									<p className="mt-1 text-sm text-card-foreground">{incident.offenderId}</p>
								</div>
							)}
							<div>
								<label className="text-sm font-medium text-muted-foreground">Sex/Gender</label>
								<p className="mt-1 text-sm text-card-foreground">
									{incident.offenderSex || incident.gender || 'N/A'}
								</p>
							</div>
							{incident.offenderDOB && (
								<div>
									<label className="text-sm font-medium text-muted-foreground">Date of Birth</label>
									<p className="mt-1 text-sm text-card-foreground">
										{format(new Date(incident.offenderDOB), 'dd MMM yyyy')}
									</p>
								</div>
							)}
							{incident.offenderPlaceOfBirth && (
								<div>
									<label className="text-sm font-medium text-muted-foreground">Place of Birth</label>
									<p className="mt-1 text-sm text-card-foreground">{incident.offenderPlaceOfBirth}</p>
								</div>
							)}
							{incident.offenderMarks && (
								<div>
									<label className="text-sm font-medium text-muted-foreground">Distinguishing Marks</label>
									<p className="mt-1 text-sm text-card-foreground">{incident.offenderMarks}</p>
								</div>
							)}
							{incident.offenderAddress && (
								<>
									<div>
										<label className="text-sm font-medium text-muted-foreground">Address</label>
										<p className="mt-1 text-sm text-card-foreground">
											{incident.offenderAddress.numberAndStreet || 'N/A'}
										</p>
									</div>
									<div>
										<label className="text-sm font-medium text-muted-foreground">Town</label>
										<p className="mt-1 text-sm text-card-foreground">
											{incident.offenderAddress.town || 'N/A'}
										</p>
									</div>
									<div>
										<label className="text-sm font-medium text-muted-foreground">Post Code</label>
										<p className="mt-1 text-sm text-card-foreground">
											{incident.offenderAddress.postCode || 'N/A'}
										</p>
									</div>
								</>
							)}
							{incident.modusOperandi && incident.modusOperandi.length > 0 && (
								<div className="sm:col-span-2 lg:col-span-3">
									<label className="text-sm font-medium text-muted-foreground">Modus operandi</label>
									<ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-card-foreground">
										{incident.modusOperandi.map((m, i) => (
											<li key={i}>{m}</li>
										))}
									</ul>
								</div>
							)}
						</div>
					</div>
				)}

				{/* Incident Categories */}
				{incident.incidentInvolved && incident.incidentInvolved.length > 0 && (
					<div className="mb-4 rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
						<div className="mb-4 flex items-center gap-2">
							<div className="h-6 w-6 text-blue-600">🏷️</div>
							<h2 className="text-lg font-medium text-card-foreground">Incident Categories</h2>
						</div>
						<div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
							{incident.incidentInvolved.map((type, index) => (
								<div key={index} className="flex items-center gap-2">
									<div className="h-2 w-2 rounded-full bg-blue-600"></div>
									<p className="text-sm text-card-foreground">{type}</p>
								</div>
							))}
						</div>
					</div>
				)}

				{/* Stolen Items */}
				{incident.stolenItems && incident.stolenItems.length > 0 && (
					<div className="rounded-lg border border-gray-200 bg-card p-4 shadow-sm dark:border-slate-700">
						<div className="mb-4 flex items-center gap-2">
							<div className="h-6 w-6 text-blue-600">💰</div>
							<h2 className="text-lg font-medium text-card-foreground">Stolen Items</h2>
						</div>
						<div className="overflow-x-auto">
							<table className="w-full">
								<thead>
									<tr className="border-b border-border">
										<th className="py-2 text-left text-sm font-medium text-muted-foreground">
											Category
										</th>
										<th className="py-2 text-left text-sm font-medium text-muted-foreground">
											Product Name
										</th>
										<th className="py-2 text-left text-sm font-medium text-muted-foreground">
											Description
										</th>
										<th className="py-2 text-right text-sm font-medium text-muted-foreground">Cost</th>
										<th className="py-2 text-right text-sm font-medium text-muted-foreground">Qty</th>
										<th className="py-2 text-right text-sm font-medium text-muted-foreground">
											Total
										</th>
										<th className="py-2 text-center text-sm font-medium text-muted-foreground">
											Recovered
										</th>
										<th className="py-2 text-right text-sm font-medium text-muted-foreground">
											Recovered Qty
										</th>
										<th className="py-2 text-right text-sm font-medium text-muted-foreground">Saved</th>
										<th className="py-2 text-right text-sm font-medium text-muted-foreground">Lost</th>
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
											<tr key={index} className="border-b border-border">
												<td className="py-2 text-sm text-card-foreground">{item.category || 'N/A'}</td>
												<td className="py-2 text-sm text-card-foreground">
													{item.productName || 'N/A'}
												</td>
												<td className="py-2 text-sm text-card-foreground">
													{item.description || 'N/A'}
												</td>
												<td className="py-2 text-right text-sm text-card-foreground">
													£{cost.toFixed(2)}
												</td>
												<td className="py-2 text-right text-sm text-card-foreground">{quantity}</td>
												<td className="py-2 text-right text-sm text-card-foreground">
													£{totalAmount.toFixed(2)}
												</td>
												<td className="py-2 text-center text-sm text-card-foreground">
													{item.wasRecovered ? 'Yes' : 'No'}
												</td>
												<td className="py-2 text-right text-sm text-card-foreground">
													{recoveredQuantity}
												</td>
												<td className="py-2 text-right text-sm text-emerald-700 dark:text-emerald-400">
													£{recoveredAmount.toFixed(2)}
												</td>
												<td className="py-2 text-right text-sm text-rose-700 dark:text-rose-400">
													£{lostAmount.toFixed(2)}
												</td>
											</tr>
										)
									})}
									<tr className="bg-muted/50 dark:bg-slate-800/50">
										<td colSpan={5} className="py-2 text-sm font-medium text-card-foreground">
											Totals
										</td>
										<td className="py-2 text-right text-sm font-medium text-card-foreground">
											£{totalStolenValue.toFixed(2)}
										</td>
										<td className="py-2 text-center text-sm font-medium text-card-foreground">—</td>
										<td className="py-2 text-right text-sm font-medium text-card-foreground">
											{incident.totalRecoveredQuantity ?? '—'}
										</td>
										<td className="py-2 text-right text-sm font-medium text-emerald-700 dark:text-emerald-400">
											£{totalRecoveredValue.toFixed(2)}
										</td>
										<td className="py-2 text-right text-sm font-medium text-rose-700 dark:text-rose-400">
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

