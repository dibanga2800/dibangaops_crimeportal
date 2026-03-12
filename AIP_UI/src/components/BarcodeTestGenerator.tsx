import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Copy, Download, DownloadCloud, Grid3x3 } from 'lucide-react';

/**
 * Calculate EAN-13 check digit
 */
const calculateEAN13CheckDigit = (digits: string): number => {
	if (digits.length !== 12) return 0;
	
	let sum = 0;
	for (let i = 0; i < 12; i++) {
		const digit = parseInt(digits[i], 10);
		sum += (i % 2 === 0) ? digit : digit * 3;
	}
	
	const remainder = sum % 10;
	return remainder === 0 ? 0 : 10 - remainder;
};

/**
 * Generate valid EAN-13 barcode
 */
const generateValidEAN13 = (base: string = '400638133393'): string => {
	const baseDigits = base.padStart(12, '0').slice(0, 12);
	const checkDigit = calculateEAN13CheckDigit(baseDigits);
	return baseDigits + checkDigit;
};

/**
 * Fix check digit for database EANs (they may have incorrect check digits)
 */
const fixEANCheckDigit = (ean: string): string => {
	let baseDigits: string;
	if (ean.length === 13) {
		// Remove the last digit (check digit) and recalculate
		baseDigits = ean.slice(0, 12);
	} else if (ean.length < 12) {
		// Pad to 12 digits
		baseDigits = ean.padStart(12, '0');
	} else {
		// Already 12 digits
		baseDigits = ean;
	}
	const checkDigit = calculateEAN13CheckDigit(baseDigits);
	return baseDigits + checkDigit;
};

/**
 * Sample EANs from your database
 */
const SAMPLE_DB_EANS = [
	'0000000000004',
	'0000000000018',
	'0000000000024',
	'0000000000031',
	'0000000000048',
	'0000000000055',
	'0000000000062',
	'0000000000079',
	'0000000000086',
	'0000000000093',
	'0000000000109',
	'0000000000116',
	'0000000000123',
	'0000000000130',
	'0000000000147',
	'0000000000154',
	'0000000000161',
	'0000000000178',
	'0000000000185',
	'0000000000192',
	'0000000000208',
	'0000000000215',
	'0000000000222',
	'0000000000239',
	'0000000000246',
	'0000000000253',
	'0000000000260',
	'0000000000277',
	'0000000000284',
	'0000000000291',
	'0000000000307',
	'0000000000314',
	'0000000000321',
	'4444',
	'11024',
];

/**
 * Pre-defined test barcodes for quick generation
 * Corrected EAN codes from database
 */
const TEST_BARCODES = [
	'0000000000017',
	'0000000000024',
	'0000000000031',
	'0000000000154',
	'0000000000191',
	'0000000000200',
	'0000000000518',
	'0000000004497',
	'0000000007573',
	'0000000007582',
	'0000000007664',
	'0000000007766',
	'0000000007773',
];

interface BarcodeItem {
	ean: string;
	url: string;
	originalEan?: string;
}

const BarcodeTestGenerator: React.FC = () => {
	// Initialize with a valid EAN (0000000000000 has correct check digit)
	const [ean13, setEan13] = useState<string>('0000000000000');
	const [customBase, setCustomBase] = useState<string>('000000000000');
	const [barcodeUrl, setBarcodeUrl] = useState<string>('');
	const [selectedDbEan, setSelectedDbEan] = useState<string>('0000000000004');
	const [originalDbEan, setOriginalDbEan] = useState<string>('');
	const [multipleBarcodes, setMultipleBarcodes] = useState<BarcodeItem[]>([]);
	
	// Initialize with first DB EAN (corrected)
	React.useEffect(() => {
		if (selectedDbEan) {
			handleUseDbEan(selectedDbEan);
		}
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	const handleGenerate = () => {
		const validEAN = generateValidEAN13(customBase);
		setEan13(validEAN);
		const url = `https://barcode.tec-it.com/barcode.ashx?data=${validEAN}&code=EAN13&dpi=300&scale=3`;
		setBarcodeUrl(url);
	};

	const handleUseDbEan = (ean: string) => {
		setSelectedDbEan(ean);
		setOriginalDbEan(ean);
		
		// Fix the check digit for barcode generation
		const validEAN = fixEANCheckDigit(ean);
		setEan13(validEAN);
	};

	const handleCopy = () => {
		navigator.clipboard.writeText(ean13);
	};

	const handleDownload = () => {
		if (!barcodeUrl) return;
		const link = document.createElement('a');
		link.href = barcodeUrl;
		link.download = `barcode-${ean13}.png`;
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
	};

	const handleGenerateMultiple = () => {
		const barcodes: BarcodeItem[] = TEST_BARCODES.map((ean) => {
			const validEAN = fixEANCheckDigit(ean);
			const url = `https://barcode.tec-it.com/barcode.ashx?data=${validEAN}&code=EAN13&dpi=300&scale=3`;
			return {
				ean: validEAN,
				url,
				originalEan: ean !== validEAN ? ean : undefined,
			};
		});
		setMultipleBarcodes(barcodes);
	};

	const handleDownloadBarcode = (barcode: BarcodeItem) => {
		const link = document.createElement('a');
		link.href = barcode.url;
		link.download = `barcode-${barcode.ean}.png`;
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
	};

	const handleDownloadAll = async () => {
		for (const barcode of multipleBarcodes) {
			await new Promise((resolve) => {
				const link = document.createElement('a');
				link.href = barcode.url;
				link.download = `barcode-${barcode.ean}.png`;
				document.body.appendChild(link);
				link.click();
				document.body.removeChild(link);
				setTimeout(resolve, 100); // Small delay between downloads
			});
		}
	};

	const handleCopyBarcode = (ean: string) => {
		navigator.clipboard.writeText(ean);
	};

	React.useEffect(() => {
		if (ean13 && ean13.length === 13) {
			const url = `https://barcode.tec-it.com/barcode.ashx?data=${ean13}&code=EAN13&dpi=300&scale=3`;
			setBarcodeUrl(url);
		}
	}, [ean13]);

	return (
		<div className="w-full max-w-6xl mx-auto space-y-6">
			<Card>
				<CardHeader>
					<CardTitle>EAN-13 Barcode Test Generator</CardTitle>
					<CardDescription>
						Generate valid EAN-13 barcodes for testing the scanner
					</CardDescription>
				</CardHeader>
				<CardContent className="space-y-4">
				<div className="space-y-2">
					<Label htmlFor="ean13">EAN Code (corrected for barcode generation)</Label>
					<div className="flex gap-2">
						<Input
							id="ean13"
							value={ean13}
							readOnly
							className="font-mono"
						/>
						<Button variant="outline" size="icon" onClick={handleCopy}>
							<Copy className="h-4 w-4" />
						</Button>
					</div>
					{originalDbEan && originalDbEan !== ean13 && (
						<p className="text-xs text-amber-600 bg-amber-50 p-2 rounded">
							⚠️ Database EAN: <span className="font-mono">{originalDbEan}</span> → Corrected to: <span className="font-mono">{ean13}</span> (check digit fixed)
						</p>
					)}
					<p className="text-xs text-muted-foreground">
						This EAN has been corrected with a valid check digit for barcode generation. 
						{originalDbEan && originalDbEan !== ean13 && (
							<> <strong>Note:</strong> Your database stores <span className="font-mono">{originalDbEan}</span>, but the scanner will read <span className="font-mono">{ean13}</span>. You may need to update your database or handle both values in the lookup.</>
						)}
					</p>
				</div>

				<div className="space-y-2">
					<Label>Quick Select: EANs from Your Database</Label>
					<div className="flex flex-wrap gap-2 max-h-32 overflow-y-auto p-2 border rounded-lg">
						{SAMPLE_DB_EANS.map((ean) => (
							<Button
								key={ean}
								variant={selectedDbEan === ean ? 'default' : 'outline'}
								size="sm"
								onClick={() => handleUseDbEan(ean)}
								className="font-mono text-xs"
							>
								{ean}
							</Button>
						))}
					</div>
					<p className="text-xs text-muted-foreground">
						Click any EAN to generate its barcode. Shorter codes will be padded to 13 digits for EAN-13 format.
					</p>
				</div>

				<div className="space-y-2">
					<Label htmlFor="customBase">Custom Base (12 digits, check digit auto-calculated)</Label>
					<div className="flex gap-2">
						<Input
							id="customBase"
							value={customBase}
							onChange={(e) => {
								const value = e.target.value.replace(/\D/g, '').slice(0, 12);
								setCustomBase(value);
							}}
							placeholder="400638133393"
							className="font-mono"
						/>
						<Button onClick={handleGenerate}>Generate</Button>
					</div>
				</div>

				{barcodeUrl && (
					<div className="space-y-2">
						<Label>Barcode Image</Label>
						<div className="border rounded-lg p-4 bg-white flex flex-col items-center gap-4">
							<img
								src={barcodeUrl}
								alt={`EAN-13 Barcode: ${ean13}`}
								className="max-w-full h-auto"
								onError={(e) => {
									console.error('Failed to load barcode image');
									e.currentTarget.style.display = 'none';
								}}
							/>
							<div className="flex gap-2">
								<Button variant="outline" onClick={handleDownload}>
									<Download className="h-4 w-4 mr-2" />
									Download
								</Button>
								<Button variant="outline" onClick={() => window.open(barcodeUrl, '_blank')}>
									Open in New Tab
								</Button>
							</div>
						</div>
					</div>
				)}

					<div className="text-sm text-muted-foreground space-y-1">
						<p className="font-medium">Usage:</p>
						<ol className="list-decimal list-inside space-y-1 ml-2">
							<li>Generate or use the default EAN-13 code</li>
							<li>Download or open the barcode image</li>
							<li>Display it on another screen or print it</li>
							<li>Scan it with the barcode scanner in the incident form</li>
						</ol>
					</div>
				</CardContent>
			</Card>

			{/* Multiple Barcodes Generator */}
			<Card>
				<CardHeader>
					<CardTitle className="flex items-center gap-2">
						<Grid3x3 className="h-5 w-5" />
						Generate Multiple Test Barcodes
					</CardTitle>
					<CardDescription>
						Generate a set of test barcodes for comprehensive scanner testing
					</CardDescription>
				</CardHeader>
				<CardContent className="space-y-4">
					<div className="flex gap-2">
						<Button onClick={handleGenerateMultiple} className="flex items-center gap-2">
							<Grid3x3 className="h-4 w-4" />
							Generate {TEST_BARCODES.length} Test Barcodes
						</Button>
						{multipleBarcodes.length > 0 && (
							<Button variant="outline" onClick={handleDownloadAll} className="flex items-center gap-2">
								<DownloadCloud className="h-4 w-4" />
								Download All ({multipleBarcodes.length})
							</Button>
						)}
					</div>

					{multipleBarcodes.length > 0 && (
						<div className="space-y-4">
							<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
								{multipleBarcodes.map((barcode) => (
									<Card key={barcode.ean} className="overflow-hidden">
										<CardContent className="p-4 space-y-3">
											<div className="flex flex-col items-center gap-2">
												<div className="border rounded-lg p-3 bg-white w-full flex justify-center">
													<img
														src={barcode.url}
														alt={`EAN-13 Barcode: ${barcode.ean}`}
														className="max-w-full h-auto"
														onError={(e) => {
															console.error('Failed to load barcode image');
															e.currentTarget.style.display = 'none';
														}}
													/>
												</div>
												<div className="w-full space-y-2">
													<div className="flex items-center gap-2">
														<Input
															value={barcode.ean}
															readOnly
															className="font-mono text-xs flex-1"
														/>
														<Button
															variant="outline"
															size="icon"
															onClick={() => handleCopyBarcode(barcode.ean)}
															className="h-8 w-8"
														>
															<Copy className="h-3 w-3" />
														</Button>
													</div>
													{barcode.originalEan && (
														<p className="text-xs text-amber-600 bg-amber-50 p-1 rounded text-center">
															Original: <span className="font-mono">{barcode.originalEan}</span>
														</p>
													)}
													<Button
														variant="outline"
														size="sm"
														onClick={() => handleDownloadBarcode(barcode)}
														className="w-full"
													>
														<Download className="h-3 w-3 mr-2" />
														Download
													</Button>
												</div>
											</div>
										</CardContent>
									</Card>
								))}
							</div>
							<p className="text-xs text-muted-foreground text-center">
								Generated {multipleBarcodes.length} test barcodes. Download individual barcodes or all at once.
							</p>
						</div>
					)}
				</CardContent>
			</Card>
		</div>
	);
};

export default BarcodeTestGenerator;

