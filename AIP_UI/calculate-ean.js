/**
 * Calculate EAN-13 check digit
 */
const calculateEAN13CheckDigit = (digits) => {
	if (digits.length !== 12) return 0;
	
	let sum = 0;
	for (let i = 0; i < 12; i++) {
		const digit = parseInt(digits[i], 10);
		sum += (i % 2 === 0) ? digit : digit * 3;
	}
	
	const remainder = sum % 10;
	return remainder === 0 ? 0 : 10 - remainder;
};

const codes = [
	'0000000000011',
	'0000000000024',
	'0000000000025',
	'0000000000030',
	'0000000000154',
	'0000000000190',
	'0000000000201',
	'0000000000512',
	'0000000004493',
	'0000000004494',
	'0000000004495',
	'0000000004496',
	'0000000004497',
	'0000000007575',
	'0000000007576',
	'0000000007580',
	'0000000007588',
	'0000000007666',
	'0000000007760',
	'0000000007770',
];

console.log('EAN Code (Current)    | Correct Check Digit | Corrected EAN-13');
console.log('----------------------|---------------------|------------------');

codes.forEach(code => {
	// Handle codes that might be 12 or 13 digits
	const base = code.length === 13 ? code.slice(0, 12) : code.padStart(12, '0');
	const currentCheck = code.length === 13 ? code[12] : 'N/A';
	const correctCheck = calculateEAN13CheckDigit(base);
	const corrected = base + correctCheck;
	
	console.log(`${code.padEnd(20)} | ${correctCheck.toString().padEnd(19)} | ${corrected}`);
});
