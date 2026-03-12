// Test the calculation
const calc = (base) => {
	let sum = 0;
	for (let i = 0; i < 12; i++) {
		const digit = parseInt(base[i], 10);
		sum += (i % 2 === 0) ? digit : digit * 3;
	}
	const remainder = sum % 10;
	return remainder === 0 ? 0 : 10 - remainder;
};

// Test a few manually
console.log('Test 1: 000000000001');
console.log('Expected: 7');
console.log('Got:', calc('000000000001'));
console.log('');

console.log('Test 2: 000000000015');
console.log('Expected: ?');
console.log('Got:', calc('000000000015'));
console.log('');

console.log('Test 3: 000000000020');
console.log('Expected: ?');
console.log('Got:', calc('000000000020'));
console.log('');

// Calculate all
const codes = [
	'0000000000011', '0000000000024', '0000000000025', '0000000000030',
	'0000000000154', '0000000000190', '0000000000201', '0000000000512',
	'0000000004493', '0000000004494', '0000000004495', '0000000004496',
	'0000000004497', '0000000007575', '0000000007576', '0000000007580',
	'0000000007588', '0000000007666', '0000000007760', '0000000007770',
];

console.log('\n=== ALL CORRECTIONS ===\n');
codes.forEach(code => {
	const base = code.slice(0, 12);
	const current = code[12];
	const correct = calc(base);
	const corrected = base + correct;
	const needsUpdate = current !== correct.toString();
	console.log(`${needsUpdate ? '❌' : '✅'} ${code} → ${corrected} (check: ${current}→${correct})`);
});
