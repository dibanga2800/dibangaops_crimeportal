# PowerShell script to diagnose and fix port binding issues for AIP Backend
# Run this script as Administrator

Write-Host "=== AIP Backend Port Binding Diagnostic & Fix Script ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
	Write-Host "WARNING: This script should be run as Administrator for full functionality" -ForegroundColor Yellow
	Write-Host ""
}

# Step 1: Check what's using port 5000
Write-Host "1. Checking what's using port 5000..." -ForegroundColor Green
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($port5000) {
	Write-Host "   Port 5000 is in use by:" -ForegroundColor Yellow
	$port5000 | ForEach-Object {
		$process = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
		if ($process) {
			Write-Host "   - PID $($_.OwningProcess): $($process.ProcessName) ($($process.Path))" -ForegroundColor Yellow
		}
	}
} else {
	Write-Host "   Port 5000 is not currently in use" -ForegroundColor Green
}

# Step 2: Check Windows reserved port ranges
Write-Host ""
Write-Host "2. Checking Windows reserved port ranges..." -ForegroundColor Green
$reservedPorts = netsh interface ipv4 show excludedportrange protocol=tcp
Write-Host "   Reserved port ranges:" -ForegroundColor Cyan
$reservedPorts | Select-String "5000" | ForEach-Object {
	Write-Host "   $_" -ForegroundColor Yellow
}

# Step 3: Check if port 5000 is in a reserved range
Write-Host ""
Write-Host "3. Checking if port 5000 is reserved..." -ForegroundColor Green
$isReserved = $false
$reservedPorts | ForEach-Object {
	if ($_ -match '(\d+)\s+-\s+(\d+)') {
		$start = [int]$matches[1]
		$end = [int]$matches[2]
		if (5000 -ge $start -and 5000 -le $end) {
			Write-Host "   WARNING: Port 5000 is in reserved range $start-$end" -ForegroundColor Red
			$isReserved = $true
		}
	}
}
if (-not $isReserved) {
	Write-Host "   Port 5000 is not in a reserved range" -ForegroundColor Green
}

# Step 4: Check Hyper-V (common cause of port reservations)
Write-Host ""
Write-Host "4. Checking Hyper-V status..." -ForegroundColor Green
$hyperV = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All -ErrorAction SilentlyContinue
if ($hyperV -and $hyperV.State -eq "Enabled") {
	Write-Host "   Hyper-V is enabled (this often reserves port ranges)" -ForegroundColor Yellow
	Write-Host "   If port 5000 is reserved, you may need to:" -ForegroundColor Yellow
	Write-Host "   - Disable Hyper-V (if not needed)" -ForegroundColor Yellow
	Write-Host "   - Or exclude port 5000 from Hyper-V reservations" -ForegroundColor Yellow
} else {
	Write-Host "   Hyper-V is not enabled" -ForegroundColor Green
}

# Step 5: Check IIS binding configuration
Write-Host ""
Write-Host "5. Checking IIS site bindings..." -ForegroundColor Green
try {
	Import-Module WebAdministration -ErrorAction SilentlyContinue
	$sites = Get-Website
	$found = $false
	foreach ($site in $sites) {
		$bindings = Get-WebBinding -Name $site.Name
		foreach ($binding in $bindings) {
			if ($binding.bindingInformation -like "*:5000:*") {
				Write-Host "   Found IIS site '$($site.Name)' binding to port 5000" -ForegroundColor Yellow
				Write-Host "   Binding: $($binding.bindingInformation)" -ForegroundColor Yellow
				$found = $true
			}
		}
	}
	if (-not $found) {
		Write-Host "   No IIS sites are binding to port 5000" -ForegroundColor Green
	}
} catch {
	Write-Host "   Could not check IIS bindings: $_" -ForegroundColor Yellow
}

# Step 6: Recommendations
Write-Host ""
Write-Host "=== RECOMMENDATIONS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "For IIS deployment, the application should NOT bind to port 5000 directly." -ForegroundColor White
Write-Host "IIS will handle the port binding through the ASP.NET Core Module." -ForegroundColor White
Write-Host ""
Write-Host "Solutions:" -ForegroundColor Yellow
Write-Host "1. The Program.cs has been updated to detect IIS and not bind to ports" -ForegroundColor Green
Write-Host "2. If port 5000 is reserved by Windows/Hyper-V, you can:" -ForegroundColor White
Write-Host "   a) Change the application to use a different port (not recommended for IIS)" -ForegroundColor White
Write-Host "   b) Exclude port 5000 from Windows reserved ranges (requires admin)" -ForegroundColor White
Write-Host "   c) Use IIS binding instead (recommended)" -ForegroundColor White
Write-Host ""
Write-Host "3. For IIS deployment, ensure:" -ForegroundColor White
Write-Host "   - ASP.NET Core Runtime 8.0 is installed" -ForegroundColor White
Write-Host "   - ASP.NET Core Module V2 is installed (already confirmed)" -ForegroundColor White
Write-Host "   - IIS site is configured with proper bindings" -ForegroundColor White
Write-Host "   - Application pool is running" -ForegroundColor White
Write-Host ""

# Step 7: Option to exclude port from reservations (if admin)
if ($isAdmin -and $isReserved) {
	Write-Host "Would you like to try excluding port 5000 from reserved ranges? (Y/N)" -ForegroundColor Cyan
	$response = Read-Host
	if ($response -eq "Y" -or $response -eq "y") {
		Write-Host "Attempting to exclude port 5000..." -ForegroundColor Green
		# Note: This is complex and may require disabling Hyper-V or using netsh
		Write-Host "This requires manual intervention. See:" -ForegroundColor Yellow
		Write-Host "https://docs.microsoft.com/en-us/troubleshoot/windows-client/networking/tcpip-port-exclusion-range" -ForegroundColor Cyan
	}
}

Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
Write-Host "Press Enter to exit..."
Read-Host
