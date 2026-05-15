import { useCallback, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorBoundary, type FallbackProps } from 'react-error-boundary'
import { ScanBarcode, Upload, CheckCircle2, Loader2, Download, ExternalLink } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableHeader,
	TableRow,
} from '@/components/ui/table'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
	buildBarcodeCsvPreview,
	MAX_BARCODE_CSV_FILE_BYTES,
	type BarcodeCsvPreviewResult,
} from '@/lib/barcode-csv/barcode-csv-preview'
import { productBarcodeCsvImportService, type BarcodeCsvImportResult } from '@/services/productBarcodeCsvImportService'
import { useToast } from '@/hooks/use-toast'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { downloadRowErrorsCsv, getImportOutcome, getRowErrorsSummary } from '@/lib/barcode-csv/import-result-status'

const PREVIEW_ROW_CAP = 500

const BarcodeCatalogImportPageInner = () => {
	const { toast } = useToast()
	const [file, setFile] = useState<File | null>(null)
	const [preview, setPreview] = useState<BarcodeCsvPreviewResult | null>(null)
	const [submitting, setSubmitting] = useState(false)
	const [lastResult, setLastResult] = useState<BarcodeCsvImportResult | null>(null)

	const handlePickFile = useCallback(async (picked: File | null) => {
		setLastResult(null)
		setFile(null)
		setPreview(null)
		if (!picked) return
		if (!picked.name.toLowerCase().endsWith('.csv')) {
			toast({ title: 'Invalid file', description: 'Please choose a .csv file.', variant: 'destructive' })
			return
		}
		if (picked.size > MAX_BARCODE_CSV_FILE_BYTES) {
			toast({ title: 'File too large', description: 'Maximum size is 5MB.', variant: 'destructive' })
			return
		}
		const text = await picked.text()
		const built = buildBarcodeCsvPreview(text)
		setFile(picked)
		setPreview(built)
		if (!built.ok && built.fatalError) {
			toast({ title: 'CSV preview failed', description: built.fatalError, variant: 'destructive' })
		}
	}, [toast])

	const stats = useMemo(() => {
		if (!preview?.ok) {
			return { total: 0, valid: 0, invalid: 0, deduplicatedInFile: 0, creates: 0, updates: 0 }
		}
		const lastByBarcode = new Map<string, (typeof preview.rows)[number]>()
		let invalid = 0
		for (const row of preview.rows) {
			if (row.validationError) {
				invalid++
				continue
			}
			const key = row.barcode.trim().toLowerCase()
			if (!key) {
				invalid++
				continue
			}
			lastByBarcode.set(key, row)
		}
		const deduplicatedInFile = preview.rows.length - invalid - lastByBarcode.size
		return {
			total: preview.rows.length,
			valid: lastByBarcode.size,
			invalid,
			deduplicatedInFile,
			creates: lastResult?.createdCount ?? 0,
			updates: lastResult?.updatedCount ?? 0,
		}
	}, [preview, lastResult])

	const canSubmit = Boolean(file) && preview?.ok === true && stats.valid > 0 && !submitting

	const handleSubmit = async () => {
		if (!file) return
		setSubmitting(true)
		setLastResult(null)
		try {
			const result = await productBarcodeCsvImportService.importCsv(file)
			setLastResult(result)
			const outcome = getImportOutcome(result)
			const deduped = result.deduplicatedInFileCount ?? 0
			toast({
				title: outcome?.label ?? 'Import completed',
				description: `${result.createdCount} created, ${result.updatedCount} updated${
					deduped > 0 ? `, ${deduped} duplicate row(s) merged (last row wins)` : ''
				}${result.invalidRows > 0 ? `, ${result.invalidRows} invalid` : ''}${
					result.importCompleted === false ? '. Import stopped before all rows were saved.' : '.'
				}`,
				variant: outcome?.status === 'failed' ? 'destructive' : undefined,
			})
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Import failed'
			toast({ title: 'Import failed', description: message, variant: 'destructive' })
		} finally {
			setSubmitting(false)
		}
	}

	const previewRows = preview?.ok ? preview.rows.slice(0, PREVIEW_ROW_CAP) : []
	const importOutcome = getImportOutcome(lastResult)

	const handleDownloadErrors = () => {
		if (!lastResult?.rowErrors.length) return
		downloadRowErrorsCsv(lastResult.rowErrors, `import-errors-${lastResult.fileName || 'export'}.csv`)
	}

	return (
		<div className="min-h-screen w-full max-w-[100vw] overflow-x-hidden bg-gradient-to-br from-background via-muted/30 to-background">
			<div className="container mx-auto max-w-screen-2xl space-y-4 px-3 py-4 sm:px-4 md:space-y-6 md:px-6 lg:px-8">
				<div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
					<div className="flex items-center gap-2">
						<ScanBarcode className="h-7 w-7 shrink-0 text-primary" aria-hidden />
						<h1 className="text-xl font-bold tracking-tight text-foreground sm:text-2xl md:text-3xl">
							Barcode catalog import
						</h1>
					</div>
					<Button variant="outline" size="sm" asChild>
						<Link to="/administration/product-catalog">
							<ExternalLink className="mr-2 h-4 w-4" aria-hidden />
							Browse catalog
						</Link>
					</Button>
				</div>

				<Card>
					<CardHeader>
						<CardTitle>Upload CSV</CardTitle>
						<CardDescription>
							Required columns (match your export): <code className="text-xs">Barcode</code> (saved as EAN),{' '}
							<code className="text-xs">Department</code>, <code className="text-xs">VMECode</code>,{' '}
							<code className="text-xs">ProductName</code>, <code className="text-xs">RetailPrice</code>. Empty{' '}
							<code className="text-xs">Department</code> or <code className="text-xs">RetailPrice</code> cells on
							update preserve existing values. <code className="text-xs">CostPrice</code> is ignored. Max 10,000 rows
							per file.
						</CardDescription>
					</CardHeader>
					<CardContent className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="csv-file">CSV file</Label>
							<div className="flex flex-wrap items-center gap-2">
								<Input
									id="csv-file"
									type="file"
									accept=".csv,text/csv"
									className="max-w-md cursor-pointer"
									onChange={(e) => {
										const f = e.target.files?.[0] ?? null
										void handlePickFile(f)
									}}
									aria-label="Choose CSV file to import"
								/>
								<Upload className="h-4 w-4 text-muted-foreground" aria-hidden />
							</div>
							<p className="text-xs text-muted-foreground">Max 5MB. Up to 10,000 data rows.</p>
						</div>

						{preview?.ok && preview.ignoredCostPriceColumnDetected && (
							<Alert>
								<AlertTitle>CostPrice column detected</AlertTitle>
								<AlertDescription>CostPrice is present and will be ignored by the import.</AlertDescription>
							</Alert>
						)}

						<div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
							<Card className="border-muted">
								<CardHeader className="py-3">
									<CardTitle className="text-sm font-medium text-muted-foreground">Total rows</CardTitle>
								</CardHeader>
								<CardContent className="pb-3 pt-0 text-2xl font-semibold">{stats.total}</CardContent>
							</Card>
							<Card className="border-muted">
								<CardHeader className="py-3">
									<CardTitle className="text-sm font-medium text-muted-foreground">Valid (unique)</CardTitle>
								</CardHeader>
								<CardContent className="pb-3 pt-0 text-2xl font-semibold text-emerald-600 dark:text-emerald-400">
									{stats.valid}
								</CardContent>
							</Card>
							<Card className="border-muted">
								<CardHeader className="py-3">
									<CardTitle className="text-sm font-medium text-muted-foreground">Invalid</CardTitle>
								</CardHeader>
								<CardContent className="pb-3 pt-0 text-2xl font-semibold text-destructive">{stats.invalid}</CardContent>
							</Card>
							<Card className="border-muted">
								<CardHeader className="py-3">
									<CardTitle className="text-sm font-medium text-muted-foreground">Merged duplicates</CardTitle>
								</CardHeader>
								<CardContent className="pb-3 pt-0 text-2xl font-semibold text-amber-600 dark:text-amber-400">
									{lastResult?.deduplicatedInFileCount ?? stats.deduplicatedInFile}
								</CardContent>
							</Card>
							<Card className="border-muted">
								<CardHeader className="py-3">
									<CardTitle className="text-sm font-medium text-muted-foreground">Last import</CardTitle>
								</CardHeader>
								<CardContent className="pb-3 pt-0 text-sm leading-snug text-muted-foreground">
									{lastResult ? (
										<span>
											{lastResult.createdCount} created, {lastResult.updatedCount} updated
										</span>
									) : (
										<span>—</span>
									)}
								</CardContent>
							</Card>
						</div>

						<div className="flex flex-wrap gap-2">
							<Button
								type="button"
								onClick={() => void handleSubmit()}
								disabled={!canSubmit}
								className="min-w-[140px]"
								aria-busy={submitting}
							>
								{submitting ? (
									<>
										<Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden />
										Importing…
									</>
								) : (
									<>
										<CheckCircle2 className="mr-2 h-4 w-4" aria-hidden />
										Run import
									</>
								)}
							</Button>
							<Button
								type="button"
								variant="outline"
								onClick={() => {
									setFile(null)
									setPreview(null)
									setLastResult(null)
								}}
								disabled={submitting}
							>
								Clear
							</Button>
							{lastResult && lastResult.rowErrors.length > 0 && (
								<Button type="button" variant="outline" onClick={handleDownloadErrors}>
									<Download className="mr-2 h-4 w-4" aria-hidden />
									Download row errors
								</Button>
							)}
						</div>

						{importOutcome && (
							<Alert
								className={
									importOutcome.status === 'full'
										? 'border-emerald-500/50 bg-emerald-50 text-emerald-950 dark:bg-emerald-950/30 dark:text-emerald-100'
										: importOutcome.status === 'partial'
											? 'border-amber-500/50 bg-amber-50 text-amber-950 dark:bg-amber-950/30 dark:text-amber-100'
											: undefined
								}
								variant={importOutcome.status === 'failed' ? 'destructive' : 'default'}
							>
								<AlertTitle>{importOutcome.label}</AlertTitle>
								<AlertDescription className="space-y-1">
									<p>{importOutcome.description}</p>
									{lastResult && (
										<p className="text-xs opacity-90">
											Processed {importOutcome.processed} of {lastResult.validRows} unique barcodes
											{!importOutcome.reconciled
												? ' (counts do not reconcile — contact support if this persists)'
												: ''}
											.
										</p>
									)}
								</AlertDescription>
							</Alert>
						)}

						{stats.deduplicatedInFile > 0 && !lastResult && (
							<Alert>
								<AlertTitle>Duplicate barcodes in file</AlertTitle>
								<AlertDescription>
									{stats.deduplicatedInFile} row(s) share a barcode with an earlier row. Import uses the{' '}
									<strong>last</strong> row for each barcode (same as updating an existing product).
								</AlertDescription>
							</Alert>
						)}

						{lastResult && (lastResult.deduplicatedInFileCount ?? 0) > 0 && (
							<Alert>
								<AlertTitle>Duplicates merged</AlertTitle>
								<AlertDescription>
									{lastResult.deduplicatedInFileCount} duplicate row(s) in the file were merged; the last row per
									barcode was applied.
								</AlertDescription>
							</Alert>
						)}

						{lastResult && (lastResult.ignoredExtraHeaders?.length ?? 0) > 0 && (
							<Alert>
								<AlertTitle>Extra columns ignored</AlertTitle>
								<AlertDescription>
									These header columns were not imported: {lastResult.ignoredExtraHeaders?.join(', ')}.
								</AlertDescription>
							</Alert>
						)}

						{lastResult && lastResult.rowErrors.length > 0 && (
							<Alert variant="destructive">
								<AlertTitle>
									Row issues
									{lastResult.invalidRows > lastResult.rowErrors.length
										? ` (showing ${lastResult.rowErrors.length} of ${lastResult.invalidRows})`
										: ` (${lastResult.rowErrors.length})`}
								</AlertTitle>
								<AlertDescription>
									{getRowErrorsSummary(lastResult) && (
										<p className="mb-2 text-sm">{getRowErrorsSummary(lastResult)}</p>
									)}
									<ul className="mt-2 max-h-40 list-disc space-y-1 overflow-y-auto pl-4 text-sm">
										{lastResult.rowErrors.slice(0, 50).map((r) => (
											<li key={`${r.lineNumber}-${r.message}`}>
												Line {r.lineNumber}: {r.message}
											</li>
										))}
									</ul>
								</AlertDescription>
							</Alert>
						)}

						{preview?.ok && (
							<div className="space-y-2">
								<h2 className="text-sm font-semibold text-foreground">Preview</h2>
								<p className="text-xs text-muted-foreground">
									Showing first {Math.min(preview.rows.length, PREVIEW_ROW_CAP)} rows.
								</p>
								<ScrollArea className="h-[min(420px,50vh)] w-full rounded-md border">
									<Table>
										<TableHeader>
											<TableRow>
												<TableHead className="w-14">Line</TableHead>
												<TableHead>Barcode</TableHead>
												<TableHead>Department</TableHead>
												<TableHead>VMECode</TableHead>
												<TableHead>ProductName</TableHead>
												<TableHead>RetailPrice</TableHead>
												<TableHead>Status</TableHead>
											</TableRow>
										</TableHeader>
										<TableBody>
											{previewRows.map((row) => (
												<TableRow key={row.lineNumber}>
													<TableCell className="font-mono text-xs">{row.lineNumber}</TableCell>
													<TableCell className="max-w-[120px] truncate text-xs">{row.barcode}</TableCell>
													<TableCell className="max-w-[120px] truncate text-xs">{row.department || '—'}</TableCell>
													<TableCell className="max-w-[100px] truncate text-xs">{row.vmeCode}</TableCell>
													<TableCell className="max-w-[180px] truncate text-xs">{row.productName}</TableCell>
													<TableCell className="max-w-[80px] truncate text-xs">{row.retailPrice || '—'}</TableCell>
													<TableCell className="text-xs">
														{row.validationError ? (
															<span className="text-destructive">{row.validationError}</span>
														) : (
															<span className="text-emerald-600 dark:text-emerald-400">OK</span>
														)}
													</TableCell>
												</TableRow>
											))}
										</TableBody>
									</Table>
								</ScrollArea>
							</div>
						)}
					</CardContent>
				</Card>
			</div>
		</div>
	)
}

const BarcodeCatalogImportFallback = ({ error, resetErrorBoundary }: FallbackProps) => (
	<div className="container mx-auto max-w-screen-lg px-4 py-8">
		<Alert variant="destructive">
			<AlertTitle>Something went wrong</AlertTitle>
			<AlertDescription className="space-y-2">
				<p>{error.message}</p>
				<Button type="button" variant="outline" size="sm" onClick={resetErrorBoundary}>
					Try again
				</Button>
			</AlertDescription>
		</Alert>
	</div>
)

export const BarcodeCatalogImportPage = () => (
	<ErrorBoundary FallbackComponent={BarcodeCatalogImportFallback}>
		<BarcodeCatalogImportPageInner />
	</ErrorBoundary>
)

export default BarcodeCatalogImportPage
