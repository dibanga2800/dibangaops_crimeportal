#!/usr/bin/env node

/**
 * Pre-build validation script
 * Runs checks before building for production
 */

import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

const rootDir = path.resolve(__dirname, '..')
const srcDir = path.join(rootDir, 'src')

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

// Check 1: Verify .env.example exists
function checkEnvExample() {
	info('Checking for .env.example file...')
	const envExamplePath = path.join(rootDir, '.env.example')
	
	if (!fs.existsSync(envExamplePath)) {
		error('.env.example file not found')
		return false
	}
	
	success('.env.example file exists')
	return true
}

// Check 2: Verify required dependencies are installed
function checkDependencies() {
	info('Checking dependencies...')
	const packageJsonPath = path.join(rootDir, 'package.json')
	const nodeModulesPath = path.join(rootDir, 'node_modules')
	
	if (!fs.existsSync(packageJsonPath)) {
		error('package.json not found')
		return false
	}
	
	if (!fs.existsSync(nodeModulesPath)) {
		error('node_modules not found. Run: npm install')
		return false
	}
	
	success('Dependencies are installed')
	return true
}

// Check 3: Look for common issues in code
function checkCodeQuality() {
	info('Checking code quality...')
	let issueCount = 0
	
	// Check for debugger statements
	const files = getAllFiles(srcDir, ['.tsx', '.ts'])
	
	files.forEach(file => {
		const content = fs.readFileSync(file, 'utf8')
		
		// Check for debugger statements
		if (content.includes('debugger')) {
			warning(`Found debugger statement in ${path.relative(rootDir, file)}`)
			issueCount++
		}
		
		// Check for TODO/FIXME comments (informational only)
		const todoMatches = content.match(/\/\/\s*(TODO|FIXME):/gi)
		if (todoMatches && todoMatches.length > 0) {
			// This is just informational, not a warning
			// info(`Found ${todoMatches.length} TODO/FIXME comment(s) in ${path.relative(rootDir, file)}`)
		}
	})
	
	if (issueCount === 0) {
		success('No code quality issues found')
	} else {
		warning(`Found ${issueCount} code quality issue(s)`)
	}
	
	return true
}

// Check 4: Verify TypeScript configuration
function checkTypeScript() {
	info('Checking TypeScript configuration...')
	const tsconfigPath = path.join(rootDir, 'tsconfig.json')
	
	if (!fs.existsSync(tsconfigPath)) {
		error('tsconfig.json not found')
		return false
	}
	
	success('TypeScript configuration exists')
	return true
}

// Check 5: Verify build configuration
function checkBuildConfig() {
	info('Checking build configuration...')
	const viteConfigPath = path.join(rootDir, 'vite.config.ts')
	
	if (!fs.existsSync(viteConfigPath)) {
		error('vite.config.ts not found')
		return false
	}
	
	const content = fs.readFileSync(viteConfigPath, 'utf8')
	
	// Verify important production settings
	if (!content.includes('minify')) {
		warning('Minification not configured in vite.config.ts')
	}
	
	success('Build configuration exists')
	return true
}

// Check 6: Verify essential files exist
function checkEssentialFiles() {
	info('Checking essential files...')
	const essentialFiles = [
		'index.html',
		'src/main.tsx',
		'src/App.tsx',
		'public',
	]
	
	let allExist = true
	
	essentialFiles.forEach(file => {
		const filePath = path.join(rootDir, file)
		if (!fs.existsSync(filePath)) {
			error(`Essential file/directory not found: ${file}`)
			allExist = false
		}
	})
	
	if (allExist) {
		success('All essential files exist')
	}
	
	return allExist
}

// Helper function to get all files with specific extensions
function getAllFiles(dirPath, extensions, arrayOfFiles = []) {
	const files = fs.readdirSync(dirPath)
	
	files.forEach(file => {
		const filePath = path.join(dirPath, file)
		
		if (fs.statSync(filePath).isDirectory()) {
			// Skip node_modules and dist
			if (!file.includes('node_modules') && !file.includes('dist')) {
				arrayOfFiles = getAllFiles(filePath, extensions, arrayOfFiles)
			}
		} else {
			const ext = path.extname(file)
			if (extensions.includes(ext)) {
				arrayOfFiles.push(filePath)
			}
		}
	})
	
	return arrayOfFiles
}

// Main execution
async function main() {
	log('\n🚀 Running pre-build checks...\n', 'blue')
	
	// Run all checks
	checkEnvExample()
	checkDependencies()
	checkCodeQuality()
	checkTypeScript()
	checkBuildConfig()
	checkEssentialFiles()
	
	// Summary
	log('\n' + '='.repeat(50), 'blue')
	
	if (hasErrors) {
		log('\n❌ Pre-build checks FAILED', 'red')
		log('Please fix the errors above before building for production.\n', 'red')
		process.exit(1)
	} else if (hasWarnings) {
		log('\n⚠️  Pre-build checks passed with warnings', 'yellow')
		log('Consider addressing the warnings above.\n', 'yellow')
		process.exit(0)
	} else {
		log('\n✅ All pre-build checks passed!', 'green')
		log('Ready to build for production.\n', 'green')
		process.exit(0)
	}
}

main().catch(err => {
	error(`Pre-build check failed: ${err.message}`)
	process.exit(1)
})
