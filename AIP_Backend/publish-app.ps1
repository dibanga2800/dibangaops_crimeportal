# Quick Publish Script
# This publishes the application to the .\publish directory

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Publishing AIP Backend Application" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "AIPBackend.csproj"
$publishPath = ".\publish"

# Check if project exists
if (-not (Test-Path $projectPath)) {
	Write-Host "ERROR: Project file not found: $projectPath" -ForegroundColor Red
	exit 1
}

Write-Host "Project: $projectPath" -ForegroundColor Green
Write-Host "Output: $publishPath" -ForegroundColor Green
Write-Host ""

# Remove existing publish directory
if (Test-Path $publishPath) {
	Write-Host "Removing existing publish directory..." -ForegroundColor Yellow
	Remove-Item $publishPath -Recurse -Force
}

# Publish
Write-Host "Publishing application..." -ForegroundColor Yellow
Write-Host "   This may take a few minutes..." -ForegroundColor Gray
Write-Host ""

dotnet publish $projectPath `
	-c Release `
	-o $publishPath `
	--self-contained false `
	/p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) {
	Write-Host ""
	Write-Host "ERROR: Publish failed!" -ForegroundColor Red
	exit 1
}

Write-Host ""
Write-Host "Publish successful!" -ForegroundColor Green
Write-Host ""

# Verify key files
Write-Host "Verifying published files..." -ForegroundColor Cyan

$requiredFiles = @(
	"AIPBackend.dll",
	"web.config",
	"appsettings.json",
	"appsettings.Production.json"
)

$allFound = $true
foreach ($file in $requiredFiles) {
	$filePath = Join-Path $publishPath $file
	if (Test-Path $filePath) {
		Write-Host "   [OK] $file" -ForegroundColor Green
	} else {
		Write-Host "   [MISSING] $file" -ForegroundColor Red
		$allFound = $false
	}
}

Write-Host ""
if ($allFound) {
	Write-Host "========================================" -ForegroundColor Green
	Write-Host "  PUBLISH COMPLETE!" -ForegroundColor Green
	Write-Host "========================================" -ForegroundColor Green
	Write-Host ""
	Write-Host "Published files are in: $((Resolve-Path $publishPath).Path)" -ForegroundColor Cyan
	Write-Host ""
	Write-Host "Next steps:" -ForegroundColor Yellow
	Write-Host "   1. Copy the entire 'publish' folder to your server" -ForegroundColor White
	Write-Host "   2. Replace the files in your IIS deployment directory" -ForegroundColor White
	Write-Host "   3. Restart IIS or the application pool" -ForegroundColor White
	Write-Host "   4. Test the application" -ForegroundColor White
	Write-Host ""
} else {
	Write-Host "WARNING: Some required files are missing!" -ForegroundColor Yellow
}
