# Verify Domain and Port Forwarding Setup
# Run this on your server

Write-Host "=== Verifying Domain Setup ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check DNS Resolution
Write-Host "1. Checking DNS Resolution..." -ForegroundColor Green
try {
    $dns = Resolve-DnsName coopaip.advantage1.co.uk -ErrorAction Stop
    Write-Host "   ✅ DNS resolves to:" -ForegroundColor Green
    $dns | Where-Object { $_.Type -eq "A" } | ForEach-Object {
        Write-Host "      $($_.IPAddress)" -ForegroundColor Yellow
    }
    
    $dnsIP = ($dns | Where-Object { $_.Type -eq "A" }).IPAddress
    if ($dnsIP -eq "81.130.181.29") {
        Write-Host "   ✅ DNS points to correct public IP: 81.130.181.29" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  DNS points to: $dnsIP (expected: 81.130.181.29)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ DNS lookup failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This might be DNS propagation delay. Wait a few minutes and try again." -ForegroundColor Yellow
}

# 2. Check Server IP Addresses
Write-Host "`n2. Checking Server IP Addresses..." -ForegroundColor Green
$serverIPs = Get-NetIPAddress | Where-Object { 
    $_.AddressFamily -eq "IPv4" -and 
    $_.IPAddress -notlike "169.254.*" -and 
    $_.IPAddress -notlike "127.*" 
} | Select-Object IPAddress, InterfaceAlias

Write-Host "   Server has these IPs:" -ForegroundColor Cyan
$serverIPs | Format-Table IPAddress, InterfaceAlias

$has192_168_20_7 = $serverIPs | Where-Object { $_.IPAddress -eq "192.168.20.7" }
if ($has192_168_20_7) {
    Write-Host "   ✅ Server has IP 192.168.20.7" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Server does NOT have IP 192.168.20.7" -ForegroundColor Yellow
    Write-Host "   Available IPs: $($serverIPs.IPAddress -join ', ')" -ForegroundColor Yellow
    Write-Host "   You may need to assign 192.168.20.7 to a network adapter" -ForegroundColor Yellow
}

# 3. Check IIS Bindings
Write-Host "`n3. Checking IIS Bindings..." -ForegroundColor Green
Import-Module WebAdministration
$site = Get-Website -Name "AIP_API"
$bindings = Get-WebBinding -Name $site.Name

Write-Host "   Current bindings:" -ForegroundColor Cyan
$bindings | Format-Table Protocol, BindingInformation

$httpsBinding = $bindings | Where-Object { $_.Protocol -eq "https" }
if ($httpsBinding) {
    $bindingInfo = $httpsBinding.BindingInformation
    Write-Host "   HTTPS Binding: $bindingInfo" -ForegroundColor Yellow
    
    if ($bindingInfo -like "*192.168.20.7*") {
        Write-Host "   ✅ HTTPS is bound to 192.168.20.7" -ForegroundColor Green
    } elseif ($bindingInfo -like "*:443:*") {
        Write-Host "   ⚠️  HTTPS is bound to all IPs (*:443:)" -ForegroundColor Yellow
        Write-Host "   This should work, but binding to 192.168.20.7 specifically is better" -ForegroundColor Yellow
    } else {
        Write-Host "   ⚠️  HTTPS binding doesn't match expected configuration" -ForegroundColor Yellow
    }
}

# 4. Check SSL Certificate
Write-Host "`n4. Checking SSL Certificate..." -ForegroundColor Green
try {
    $sslBindings = Get-ChildItem IIS:\SslBindings
    if ($sslBindings) {
        Write-Host "   SSL Certificate bindings:" -ForegroundColor Cyan
        $sslBindings | Format-Table IPAddress, Port, CertificateHash, CertificateStoreName
        
        $httpsCert = $sslBindings | Where-Object { 
            ($_.IPAddress -eq "192.168.20.7" -or $_.IPAddress -eq "0.0.0.0") -and 
            $_.Port -eq 443 
        }
        
        if ($httpsCert) {
            Write-Host "   ✅ SSL certificate is bound to port 443" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  No SSL certificate found for port 443" -ForegroundColor Yellow
            Write-Host "   HTTPS won't work without a valid SSL certificate" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ❌ No SSL certificate bindings found" -ForegroundColor Red
        Write-Host "   You need to install and bind an SSL certificate for HTTPS to work" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Could not check SSL certificates: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 5. Check Firewall
Write-Host "`n5. Checking Firewall Rules..." -ForegroundColor Green
$firewallRules = Get-NetFirewallRule | Where-Object { 
    ($_.DisplayName -like "*443*" -or $_.DisplayName -like "*IIS*" -or $_.DisplayName -like "*HTTPS*") -and 
    $_.Enabled -eq $true -and
    $_.Direction -eq "Inbound"
}

if ($firewallRules) {
    Write-Host "   ✅ Firewall rules for HTTPS:" -ForegroundColor Green
    $firewallRules | Format-Table DisplayName, Enabled, Action
} else {
    Write-Host "   ⚠️  No firewall rules found for port 443" -ForegroundColor Yellow
    Write-Host "   Creating firewall rule..." -ForegroundColor Yellow
    try {
        New-NetFirewallRule -DisplayName "IIS HTTPS (Port 443)" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow -Profile Any
        Write-Host "   ✅ Firewall rule created" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Failed to create firewall rule: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 6. Test Port Locally
Write-Host "`n6. Testing Port 443 Locally..." -ForegroundColor Green
$portTest = Test-NetConnection -ComputerName localhost -Port 443 -WarningAction SilentlyContinue
if ($portTest.TcpTestSucceeded) {
    Write-Host "   ✅ Port 443 is accessible locally" -ForegroundColor Green
} else {
    Write-Host "   ❌ Port 443 is NOT accessible locally" -ForegroundColor Red
}

# 7. Test HTTPS Binding
Write-Host "`n7. Testing HTTPS Binding..." -ForegroundColor Green
try {
    # Test using the domain (if DNS works)
    $response = Invoke-WebRequest -Uri "https://coopaip.advantage1.co.uk/swagger" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ✅ HTTPS is working! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    if ($_.Exception.Message -like "*DNS*" -or $_.Exception.Message -like "*resolve*") {
        Write-Host "   ⚠️  DNS issue: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "   Testing with IP directly..." -ForegroundColor Yellow
        
        # Test with IP if DNS doesn't work
        try {
            $response = Invoke-WebRequest -Uri "https://192.168.20.7/swagger" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10 -ErrorAction Stop
            Write-Host "   ✅ HTTPS works with IP 192.168.20.7" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ HTTPS test failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ⚠️  HTTPS test failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# 8. Port Forwarding Verification
Write-Host "`n8. Port Forwarding Setup (Manual Check Required)..." -ForegroundColor Green
Write-Host "   ⚠️  Port forwarding must be configured on your router:" -ForegroundColor Yellow
Write-Host "   External Port: 443 (HTTPS)" -ForegroundColor Cyan
Write-Host "   Internal IP: 192.168.20.7" -ForegroundColor Cyan
Write-Host "   Internal Port: 443" -ForegroundColor Cyan
Write-Host "   Protocol: TCP" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Verify this is configured correctly on your router/firewall" -ForegroundColor Yellow

# 9. Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "DNS Record: coopaip.advantage1.co.uk → 81.130.181.29" -ForegroundColor $(if ($dnsIP -eq "81.130.181.29") { "Green" } else { "Yellow" })
Write-Host "Server IP: 192.168.20.7 (for HTTPS)" -ForegroundColor $(if ($has192_168_20_7) { "Green" } else { "Yellow" })
Write-Host "Port Forwarding: External 443 → Internal 192.168.20.7:443" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Ensure 192.168.20.7 is assigned to a network adapter" -ForegroundColor White
Write-Host "2. Verify IIS binding uses 192.168.20.7:443" -ForegroundColor White
Write-Host "3. Verify port forwarding on router" -ForegroundColor White
Write-Host "4. Wait for DNS propagation (can take up to 48 hours)" -ForegroundColor White
Write-Host "5. Test from external network: https://coopaip.advantage1.co.uk/swagger" -ForegroundColor White
