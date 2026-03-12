# ========================================
# 3️⃣ DEPLOY API SCRIPT
# ========================================
# This script publishes and deploys your .NET API to IIS

param(
    [string]$ProjectPath = "..\AIPBackend.csproj",
    [string]$TargetPath = "C:\inetpub\AIPBackend",
    [string]$SiteName = "AIPBackend"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DEPLOY API SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "❌ ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Check if project file exists
$projectFullPath = Resolve-Path $ProjectPath -ErrorAction SilentlyContinue
if (-not $projectFullPath) {
    Write-Host "❌ ERROR: Project file not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Project file found: $projectFullPath" -ForegroundColor Green
Write-Host ""

# Stop IIS site
Write-Host "🛑 Stopping IIS site..." -ForegroundColor Yellow
Import-Module WebAdministration
if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
    Stop-Website -Name $SiteName
    Write-Host "✅ Site stopped" -ForegroundColor Green
} else {
    Write-Host "⚠️  Site not found: $SiteName" -ForegroundColor Yellow
}

Start-Sleep -Seconds 2

# Publish the application
Write-Host "📦 Publishing application..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Gray
Write-Host ""

$publishPath = Join-Path $env:TEMP "AIPBackend_publish"
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

dotnet publish $projectFullPath `
    -c Release `
    -o $publishPath `
    --self-contained false `
    /p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Publish failed!" -ForegroundColor Red
    Write-Host "Starting site again..." -ForegroundColor Yellow
    Start-Website -Name $SiteName
    exit 1
}

Write-Host "✅ Publish successful" -ForegroundColor Green
Write-Host ""

# Backup existing deployment (optional)
if (Test-Path $TargetPath) {
    $backupPath = "${TargetPath}_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "💾 Creating backup: $backupPath" -ForegroundColor Yellow
    
    try {
        # Only backup if there are files
        if ((Get-ChildItem $TargetPath -ErrorAction SilentlyContinue).Count -gt 0) {
            Copy-Item -Path $TargetPath -Destination $backupPath -Recurse -Force
            Write-Host "✅ Backup created" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️  Could not create backup: $_" -ForegroundColor Yellow
    }
}

# Deploy files
Write-Host "📂 Deploying files to: $TargetPath" -ForegroundColor Yellow

# Create target directory if it doesn't exist
if (-not (Test-Path $TargetPath)) {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}

# Copy files
Write-Host "Copying files..." -ForegroundColor Gray
Copy-Item -Path "$publishPath\*" -Destination $TargetPath -Recurse -Force

Write-Host "✅ Files deployed" -ForegroundColor Green
Write-Host ""

# Ensure appsettings.Production.json exists
$productionSettings = Join-Path $TargetPath "appsettings.Production.json"
if (-not (Test-Path $productionSettings)) {
    Write-Host "⚠️  WARNING: appsettings.Production.json not found!" -ForegroundColor Yellow
    Write-Host "Make sure to create this file with your production settings." -ForegroundColor Yellow
}

# Start IIS site
Write-Host "▶️  Starting IIS site..." -ForegroundColor Yellow
Start-Website -Name $SiteName
Start-Sleep -Seconds 2

$siteState = (Get-Website -Name $SiteName).State
if ($siteState -eq "Started") {
    Write-Host "✅ Site started successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Site state: $siteState" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Post-Deployment Checklist:" -ForegroundColor Cyan
Write-Host "  ✓ Files deployed to: $TargetPath" -ForegroundColor White
Write-Host "  ✓ IIS site started" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  Important:" -ForegroundColor Yellow
Write-Host "  1. Verify appsettings.Production.json has correct values" -ForegroundColor White
Write-Host "  2. Update connection strings (SQL Server, Azure Storage)" -ForegroundColor White
Write-Host "  3. Update JWT secret key" -ForegroundColor White
Write-Host "  4. Update SMTP credentials" -ForegroundColor White
Write-Host "  5. Test API: http://localhost" -ForegroundColor White
Write-Host "  6. Check logs if errors occur: $TargetPath\logs" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
