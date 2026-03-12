const calc = (base) => {
	let sum = 0;
	for (let i = 0; i < 12; i++) {
		const digit = parseInt(base[i], 10);
		sum += (i % 2 === 0) ? digit : digit * 3;
	}
	const remainder = sum % 10;
	return remainder === 0 ? 0 : 10 - remainder;
};

const codes = [
	'0000000000011', '0000000000024', '0000000000025', '0000000000030',
	'0000000000154', '0000000000190', '0000000000201', '0000000000512',
	'0000000004493', '0000000004494', '0000000004495', '0000000004496',
	'0000000004497', '0000000007575', '0000000007576', '0000000007580',
	'0000000007588', '0000000007666', '0000000007760', '0000000007770',
];

const fs = require('fs');
let output = 'EAN CORRECTIONS\n';
output += '===============\n\n';

codes.forEach(code => {
	const base = code.slice(0, 12);
	const current = code[12];
	const correct = calc(base);
	const corrected = base + correct;
	const needsUpdate = current !== correct.toString();
	output += `${needsUpdate ? 'UPDATE' : 'OK     '} ${code} → ${corrected} (${current}→${correct})\n`;
});

fs.writeFileSync('ean-output.txt', output);
console.log('Results written to ean-output.txt');
