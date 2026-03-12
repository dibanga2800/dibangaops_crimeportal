#!/usr/bin/env node

/**
 * Post-build validation script
 * Verifies build output and checks bundle sizes
 */

import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

const rootDir = path.resolve(__dirname, '..')
const distDir = path.join(rootDir, 'dist')

let hasErrors = false
let hasWarnings = false

// Color codes for console output
const colors = {
	reset: '\x1b[0m',
	red: '\x1b[31m',
	yellow: '\x1b[33m',
	green: '\x1b[32m',
	blue: '\x1b[34m',
}

function log(message, color = 'reset') {
	console.log(`${colors[color]}${message}${colors.reset}`)
}

function error(message) {
	hasErrors = true
	log(`❌ ERROR: ${message}`, 'red')
}

function warning(message) {
	hasWarnings = true
	log(`⚠️  WARNING: ${message}`, 'yellow')
}

function success(message) {
	log(`✅ ${message}`, 'green')
}

function info(message) {
	log(`ℹ️  ${message}`, 'blue')
}

// Get file size in human-readable format
function formatBytes(bytes) {
	if (bytes === 0) return '0 Bytes'
	const k = 1024
	const sizes = ['Bytes', 'KB', 'MB', 'GB']
	const i = Math.floor(Math.log(bytes) / Math.log(k))
	return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
}

// Get all files recursively
function getAllFiles(dirPath, arrayOfFiles = []) {
	const files = fs.readdirSync(dirPath)
	
	files.forEach(file => {
		const filePath = path.join(dirPath, file)
		if (fs.statSync(filePath).isDirectory()) {
			arrayOfFiles = getAllFiles(filePath, arrayOfFiles)
		} else {
			arrayOfFiles.push(filePath)
		}
	})
	
	return arrayOfFiles
}

// Check 1: Verify dist directory exists
function checkDistExists() {
	info('Checking if dist directory exists...')
	
	if (!fs.existsSync(distDir)) {
		error('dist directory not found. Build may have failed.')
		return false
	}
	
	success('dist directory exists')
	return true
}

// Check 2: Verify essential files in dist
function checkEssentialFiles() {
	info('Checking essential files in dist...')
	
	const essentialFiles = [
		'index.html',
	]
	
	let allExist = true
	
	essentialFiles.forEach(file => {
		const filePath = path.join(distDir, file)
		if (!fs.existsSync(filePath)) {
			error(`Essential file not found in dist: ${file}`)
			allExist = false
		}
	})
	
	if (allExist) {
		success('All essential files exist in dist')
	}
	
	return allExist
}

// Check 3: Analyze bundle sizes
function analyzeBundleSizes() {
	info('Analyzing bundle sizes...')
	
	const jsFiles = getAllFiles(distDir).filter(file => file.endsWith('.js'))
	const cssFiles = getAllFiles(distDir).filter(file => file.endsWith('.css'))
	
	let totalJsSize = 0
	let totalCssSize = 0
	let largeFiles = []
	
	const maxRecommendedSize = 500 * 1024 // 500KB
	
	// Analyze JS files
	jsFiles.forEach(file => {
		const stats = fs.statSync(file)
		totalJsSize += stats.size
		
		if (stats.size > maxRecommendedSize) {
			largeFiles.push({
				path: path.relative(distDir, file),
				size: stats.size,
				type: 'JavaScript'
			})
		}
	})
	
	// Analyze CSS files
	cssFiles.forEach(file => {
		const stats = fs.statSync(file)
		totalCssSize += stats.size
		
		if (stats.size > maxRecommendedSize) {
			largeFiles.push({
				path: path.relative(distDir, file),
				size: stats.size,
				type: 'CSS'
			})
		}
	})
	
	info(`Total JavaScript: ${formatBytes(totalJsSize)}`)
	info(`Total CSS: ${formatBytes(totalCssSize)}`)
	info(`Total Assets: ${formatBytes(totalJsSize + totalCssSize)}`)
	
	if (largeFiles.length > 0) {
		warning('Found files larger than recommended size (500KB):')
		largeFiles.forEach(file => {
			warning(`  ${file.type}: ${file.path} - ${formatBytes(file.size)}`)
		})
	} else {
		success('All bundle sizes are within recommended limits')
	}
	
	return true
}

// Check 4: Verify index.html has proper structure
function checkIndexHtml() {
	info('Checking index.html...')
	
	const indexPath = path.join(distDir, 'index.html')
	
	if (!fs.existsSync(indexPath)) {
		error('index.html not found in dist')
		return false
	}
	
	const content = fs.readFileSync(indexPath, 'utf8')
	
	// Check for script tags
	if (!content.includes('<script')) {
		warning('No script tags found in index.html')
	}
	
	// Check for module type
	if (!content.includes('type="module"')) {
		warning('No module script found in index.html')
	}
	
	success('index.html structure looks good')
	return true
}

// Check 5: Calculate total build size
function calculateTotalSize() {
	info('Calculating total build size...')
	
	const allFiles = getAllFiles(distDir)
	let totalSize = 0
	
	allFiles.forEach(file => {
		const stats = fs.statSync(file)
		totalSize += stats.size
	})
	
	info(`Total build size: ${formatBytes(totalSize)}`)
	
	// Warn if build is too large (>10MB)
	if (totalSize > 10 * 1024 * 1024) {
		warning(`Build size is large (${formatBytes(totalSize)}). Consider optimization.`)
	} else {
		success('Build size is reasonable')
	}
	
	return true
}

// Main execution
async function main() {
	log('\n📦 Running post-build checks...\n', 'blue')
	
	// Run all checks
	const distExists = checkDistExists()
	
	if (distExists) {
		checkEssentialFiles()
		checkIndexHtml()
		analyzeBundleSizes()
		calculateTotalSize()
	}
	
	// Summary
	log('\n' + '='.repeat(50), 'blue')
	
	if (hasErrors) {
		log('\n❌ Post-build checks FAILED', 'red')
		log('Please review the errors above.\n', 'red')
		process.exit(1)
	} else if (hasWarnings) {
		log('\n⚠️  Post-build checks passed with warnings', 'yellow')
		log('Consider addressing the warnings above.\n', 'yellow')
		process.exit(0)
	} else {
		log('\n✅ All post-build checks passed!', 'green')
		log('Build is ready for deployment.\n', 'green')
		process.exit(0)
	}
}

main().catch(err => {
	error(`Post-build check failed: ${err.message}`)
	process.exit(1)
})
