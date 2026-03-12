# ========================================
# 2️⃣ CREATE IIS WEBSITE SCRIPT
# ========================================
# Run this script as Administrator AFTER installing .NET Hosting Bundle

param(
    [string]$SiteName = "AIPBackend",
    [string]$PhysicalPath = "C:\inetpub\AIPBackend",
    [string]$Port = "80",
    [string]$HostName = ""
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREATE IIS WEBSITE SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "❌ ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Import IIS module
Import-Module WebAdministration

# Create physical directory if it doesn't exist
if (-not (Test-Path $PhysicalPath)) {
    Write-Host "📁 Creating directory: $PhysicalPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
    Write-Host "✅ Directory created" -ForegroundColor Green
} else {
    Write-Host "✅ Directory already exists: $PhysicalPath" -ForegroundColor Green
}

# Create logs directory
$logsPath = Join-Path $PhysicalPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Host "✅ Logs directory created" -ForegroundColor Green
}

# Set permissions for IIS_IUSRS
Write-Host "🔐 Setting permissions for IIS_IUSRS..." -ForegroundColor Yellow
$acl = Get-Acl $PhysicalPath
$permission = "IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $PhysicalPath $acl
Write-Host "✅ Permissions set" -ForegroundColor Green

# Check if site already exists
if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
    Write-Host "⚠️  Website '$SiteName' already exists!" -ForegroundColor Yellow
    $response = Read-Host "Do you want to remove and recreate it? (y/n)"
    if ($response -eq 'y') {
        Remove-Website -Name $SiteName
        Write-Host "✅ Existing website removed" -ForegroundColor Green
    } else {
        Write-Host "❌ Operation cancelled" -ForegroundColor Red
        exit 0
    }
}

# Create application pool
$appPoolName = "${SiteName}AppPool"
Write-Host "🏊 Creating Application Pool: $appPoolName" -ForegroundColor Yellow

if (Test-Path "IIS:\AppPools\$appPoolName") {
    Remove-WebAppPool -Name $appPoolName
}

New-WebAppPool -Name $appPoolName
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"
Write-Host "✅ Application Pool created" -ForegroundColor Green

# Create website
Write-Host "🌐 Creating Website: $SiteName" -ForegroundColor Yellow
$binding = "*:${Port}:"
if ($HostName) {
    $binding = "*:${Port}:${HostName}"
}

New-Website -Name $SiteName `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $appPoolName `
    -BindingInformation $binding

Write-Host "✅ Website created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Website Configuration:" -ForegroundColor Cyan
Write-Host "  Site Name: $SiteName" -ForegroundColor White
Write-Host "  Physical Path: $PhysicalPath" -ForegroundColor White
Write-Host "  Port: $Port" -ForegroundColor White
Write-Host "  Host Name: $(if ($HostName) { $HostName } else { '(none)' })" -ForegroundColor White
Write-Host "  Application Pool: $appPoolName" -ForegroundColor White
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Publish your .NET app to: $PhysicalPath" -ForegroundColor White
Write-Host "  2. Update appsettings.Production.json with your settings" -ForegroundColor White
Write-Host "  3. Run script 3-deploy-api.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
