/**
 * Client-side CSV preview for barcode catalog import.
 * Keep in sync with ProductBarcodeCsvParser — run both test suites when changing rules.
 */

export const MAX_BARCODE_CSV_FILE_BYTES = 5 * 1024 * 1024
export const MAX_BARCODE_CSV_DATA_ROWS = 10_000

export interface BarcodeCsvPreviewRow {
	lineNumber: number
	barcode: string
	department: string
	vmeCode: string
	productName: string
	retailPrice: string
	validationError: string | null
}

export interface BarcodeCsvPreviewResult {
	ok: boolean
	fatalError?: string
	retailPriceColumnPresent: boolean
	ignoredCostPriceColumnDetected: boolean
	/** @deprecated use ignoredCostPriceColumnDetected */
	ignoredPriceColumnsDetected: boolean
	rows: BarcodeCsvPreviewRow[]
}

export const parseCsvLine = (line: string): string[] => {
	const fields: string[] = []
	let current = ''
	let inQuotes = false
	for (let i = 0; i < line.length; i++) {
		const c = line[i]
		if (inQuotes) {
			if (c === '"') {
				if (i + 1 < line.length && line[i + 1] === '"') {
					current += '"'
					i++
				} else {
					inQuotes = false
				}
			} else {
				current += c
			}
		} else if (c === '"') {
			inQuotes = true
		} else if (c === ',') {
			fields.push(current)
			current = ''
		} else {
			current += c
		}
	}
	fields.push(current)
	return fields
}

const trimBom = (line: string): string => (line.length > 0 && line[0] === '\ufeff' ? line.replace(/^\ufeff/, '') : line)

export const sanitizeFormulaInjectionPrefix = (value: string): string => {
	const s = value.trimStart('\t', '\u0009')
	if (s.length === 0) return s
	const c0 = s[0]
	if (c0 === '=' || c0 === '+' || c0 === '-' || c0 === '@') {
		return `'${s}`
	}
	return s
}

export const normalizeField = (value: string | null | undefined): string | null => {
	if (value == null) return null
	const trimmed = value.trim()
	if (!trimmed) return null
	const sanitized = sanitizeFormulaInjectionPrefix(trimmed)
	return sanitized.length === 0 ? null : sanitized
}

/** Invariant-culture decimal parse (matches ProductBarcodeCsvParser.TryParseRetailPrice). */
export const tryParseRetailPrice = (raw: string): { ok: true; value: number } | { ok: false } => {
	const trimmed = raw.trim().replace(/[£$]/g, '').trim()
	if (!trimmed) return { ok: false }
	if (!/^-?\d+(\.\d+)?$/.test(trimmed)) return { ok: false }
	const value = Number(trimmed)
	if (!Number.isFinite(value) || value < 0) return { ok: false }
	return { ok: true, value }
}

const validateRow = (
	barcode: string | null,
	department: string | null,
	vmeCode: string | null,
	productName: string | null,
	retailPriceRaw: string | null
): string | null => {
	const b = normalizeField(barcode)
	const d = normalizeField(department)
	const v = normalizeField(vmeCode)
	const n = normalizeField(productName)
	if (!b) return 'barcode is required.'
	if (b.length > 50) return 'barcode exceeds 50 characters.'
	if (d != null && d.length > 100) return 'Department exceeds 100 characters.'
	if (!n) return 'ProductName is required.'
	if (n.length > 500) return 'ProductName exceeds 500 characters.'
	if (v != null && v.length > 500) return 'VMECode exceeds 500 characters.'

	if (retailPriceRaw?.trim()) {
		const parsed = tryParseRetailPrice(retailPriceRaw)
		if (!parsed.ok) return 'RetailPrice is not a valid decimal value.'
	}

	return null
}

const mapHeaders = (
	headerFields: string[]
):
	| {
			ok: true
			barcodeIdx: number
			departmentIdx: number
			vmeIdx: number
			nameIdx: number
			retailPriceIdx: number
			retailPriceColumnPresent: boolean
			ignoredCostPriceColumnDetected: boolean
	  }
	| { ok: false; error: string; retailPriceColumnPresent: boolean; ignoredCostPriceColumnDetected: boolean } => {
	let barcodeIdx = -1
	let departmentIdx = -1
	let vmeIdx = -1
	let nameIdx = -1
	let retailPriceIdx = -1
	let retailPriceColumnPresent = false
	let ignoredCostPriceColumnDetected = false

	if (headerFields.length === 0) {
		return { ok: false, error: 'CSV header row is empty.', retailPriceColumnPresent: false, ignoredCostPriceColumnDetected: false }
	}

	for (let i = 0; i < headerFields.length; i++) {
		const raw = headerFields[i]?.trim() ?? ''
		if (!raw) continue

		const lower = raw.toLowerCase()
		if (lower === 'barcode') {
			if (barcodeIdx >= 0) return { ok: false, error: "Duplicate 'barcode' column in header.", retailPriceColumnPresent, ignoredCostPriceColumnDetected }
			barcodeIdx = i
		} else if (lower === 'department') {
			if (departmentIdx >= 0) return { ok: false, error: "Duplicate 'Department' column in header.", retailPriceColumnPresent, ignoredCostPriceColumnDetected }
			departmentIdx = i
		} else if (lower === 'vmecode') {
			if (vmeIdx >= 0) return { ok: false, error: "Duplicate 'VMECode' column in header.", retailPriceColumnPresent, ignoredCostPriceColumnDetected }
			vmeIdx = i
		} else if (lower === 'productname') {
			if (nameIdx >= 0) return { ok: false, error: "Duplicate 'ProductName' column in header.", retailPriceColumnPresent, ignoredCostPriceColumnDetected }
			nameIdx = i
		} else if (lower === 'retailprice') {
			if (retailPriceIdx >= 0) return { ok: false, error: "Duplicate 'RetailPrice' column in header.", retailPriceColumnPresent, ignoredCostPriceColumnDetected }
			retailPriceIdx = i
			retailPriceColumnPresent = true
		} else if (lower === 'costprice') {
			ignoredCostPriceColumnDetected = true
		}
	}

	if (barcodeIdx < 0 || departmentIdx < 0 || vmeIdx < 0 || nameIdx < 0 || retailPriceIdx < 0) {
		return {
			ok: false,
			error: 'CSV must include headers: barcode, Department, VMECode, ProductName, RetailPrice.',
			retailPriceColumnPresent,
			ignoredCostPriceColumnDetected,
		}
	}

	return { ok: true, barcodeIdx, departmentIdx, vmeIdx, nameIdx, retailPriceIdx, retailPriceColumnPresent, ignoredCostPriceColumnDetected }
}

const getCell = (cells: string[], index: number): string | null => {
	if (index < 0 || index >= cells.length) return null
	return cells[index]
}

export const buildBarcodeCsvPreview = (csvText: string): BarcodeCsvPreviewResult => {
	const lines = csvText.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n')
	const headerLineRaw = lines[0]
	if (headerLineRaw == null || headerLineRaw === '') {
		return {
			ok: false,
			fatalError: 'CSV is empty.',
			retailPriceColumnPresent: false,
			ignoredCostPriceColumnDetected: false,
			ignoredPriceColumnsDetected: false,
			rows: [],
		}
	}

	const headerLine = trimBom(headerLineRaw)
	const headerFields = parseCsvLine(headerLine)
	const mapped = mapHeaders(headerFields)
	if (!mapped.ok) {
		return {
			ok: false,
			fatalError: mapped.error,
			retailPriceColumnPresent: mapped.retailPriceColumnPresent,
			ignoredCostPriceColumnDetected: mapped.ignoredCostPriceColumnDetected,
			ignoredPriceColumnsDetected: mapped.ignoredCostPriceColumnDetected,
			rows: [],
		}
	}

	const { barcodeIdx, departmentIdx, vmeIdx, nameIdx, retailPriceIdx, retailPriceColumnPresent, ignoredCostPriceColumnDetected } = mapped
	const rows: BarcodeCsvPreviewRow[] = []

	for (let li = 1; li < lines.length; li++) {
		const line = lines[li]
		if (line == null || line.trim() === '') continue
		if (rows.length >= MAX_BARCODE_CSV_DATA_ROWS) {
			return {
				ok: false,
				fatalError: `CSV exceeds maximum of ${MAX_BARCODE_CSV_DATA_ROWS} data rows.`,
				retailPriceColumnPresent,
				ignoredCostPriceColumnDetected,
				ignoredPriceColumnsDetected: ignoredCostPriceColumnDetected,
				rows: [],
			}
		}

		const lineNumber = li + 1
		const cells = parseCsvLine(line)
		const barcode = getCell(cells, barcodeIdx)
		const department = getCell(cells, departmentIdx)
		const vmeCode = getCell(cells, vmeIdx)
		const productName = getCell(cells, nameIdx)
		const retailPriceRaw = getCell(cells, retailPriceIdx)
		const validationError = validateRow(barcode, department, vmeCode, productName, retailPriceRaw)

		rows.push({
			lineNumber,
			barcode: barcode?.trim() ?? '',
			department: department?.trim() ?? '',
			vmeCode: vmeCode?.trim() ?? '',
			productName: productName?.trim() ?? '',
			retailPrice: retailPriceRaw?.trim() ?? '',
			validationError,
		})
	}

	return {
		ok: true,
		retailPriceColumnPresent,
		ignoredCostPriceColumnDetected,
		ignoredPriceColumnsDetected: ignoredCostPriceColumnDetected,
		rows,
	}
}
