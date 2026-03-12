# ========================================
# 1️⃣ IIS SETUP SCRIPT
# ========================================
# Run this script as Administrator
# This installs IIS and required features

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IIS INSTALLATION SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "❌ ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Running as Administrator" -ForegroundColor Green
Write-Host ""

# Install IIS
Write-Host "📦 Installing IIS Web Server..." -ForegroundColor Yellow
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

# Install required IIS features
Write-Host "📦 Installing IIS Features..." -ForegroundColor Yellow

$features = @(
    "Web-WebServer",
    "Web-Common-Http",
    "Web-Default-Doc",
    "Web-Dir-Browsing",
    "Web-Http-Errors",
    "Web-Static-Content",
    "Web-Http-Redirect",
    "Web-Health",
    "Web-Http-Logging",
    "Web-Performance",
    "Web-Stat-Compression",
    "Web-Dyn-Compression",
    "Web-Security",
    "Web-Filtering",
    "Web-Basic-Auth",
    "Web-Windows-Auth",
    "Web-App-Dev",
    "Web-Net-Ext45",
    "Web-Asp-Net45",
    "Web-ISAPI-Ext",
    "Web-ISAPI-Filter",
    "Web-Mgmt-Tools",
    "Web-Mgmt-Console"
)

foreach ($feature in $features) {
    try {
        Install-WindowsFeature -Name $feature -ErrorAction Stop
        Write-Host "  ✅ Installed: $feature" -ForegroundColor Green
    } catch {
        Write-Host "  ⚠️  Warning: Could not install $feature" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✅ IIS Installation Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Install .NET 8.0 Hosting Bundle from:" -ForegroundColor White
Write-Host "     https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
Write-Host "  2. Download the 'ASP.NET Core Runtime 8.0.x - Windows Hosting Bundle'" -ForegroundColor White
Write-Host "  3. Restart IIS after installing: 'net stop was /y' then 'net start w3svc'" -ForegroundColor White
Write-Host "  4. Run script 2-create-iis-site.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
