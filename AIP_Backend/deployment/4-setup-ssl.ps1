# ========================================
# 4️⃣ SSL SETUP SCRIPT (using win-acme)
# ========================================
# This script helps set up SSL certificate using win-acme (Let's Encrypt)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SSL SETUP GUIDE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📝 SSL Setup Instructions:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1️⃣  Download win-acme:" -ForegroundColor Cyan
Write-Host "   https://github.com/win-acme/win-acme/releases" -ForegroundColor White
Write-Host "   Download: win-acme.v2.x.x.zip (latest version)" -ForegroundColor White
Write-Host ""

Write-Host "2️⃣  Extract and Run:" -ForegroundColor Cyan
Write-Host "   - Extract to C:\win-acme" -ForegroundColor White
Write-Host "   - Run wacs.exe as Administrator" -ForegroundColor White
Write-Host ""

Write-Host "3️⃣  Certificate Setup:" -ForegroundColor Cyan
Write-Host "   - Select 'N' for New certificate" -ForegroundColor White
Write-Host "   - Choose '1' for Single binding of an IIS site" -ForegroundColor White
Write-Host "   - Select your AIPBackend site" -ForegroundColor White
Write-Host "   - Accept defaults for other options" -ForegroundColor White
Write-Host ""

Write-Host "4️⃣  Prerequisites:" -ForegroundColor Yellow
Write-Host "   ⚠️  BEFORE running win-acme, ensure:" -ForegroundColor Yellow
Write-Host "   ✓ Your domain DNS is pointing to your server's public IP" -ForegroundColor White
Write-Host "   ✓ Port 80 is accessible from the internet" -ForegroundColor White
Write-Host "   ✓ Port 443 is forwarded in your router" -ForegroundColor White
Write-Host "   ✓ Your IIS site is running and accessible via HTTP" -ForegroundColor White
Write-Host ""

Write-Host "5️⃣  After Certificate Installation:" -ForegroundColor Cyan
Write-Host "   - win-acme will automatically:" -ForegroundColor White
Write-Host "     • Install the certificate in IIS" -ForegroundColor White
Write-Host "     • Add HTTPS binding (port 443)" -ForegroundColor White
Write-Host "     • Set up automatic renewal" -ForegroundColor White
Write-Host ""

Write-Host "6️⃣  Update IIS Binding:" -ForegroundColor Cyan
Write-Host "   If you need to manually update the binding:" -ForegroundColor White
Write-Host ""

$updateBinding = @"
Import-Module WebAdministration

# Add HTTPS binding
New-WebBinding -Name "AIPBackend" ``
    -Protocol https ``
    -Port 443 ``
    -HostHeader "api.yourdomain.com" ``
    -SslFlags 1

# Get the certificate
`$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { `$_.Subject -like "*api.yourdomain.com*" }

# Bind certificate to IIS
`$binding = Get-WebBinding -Name "AIPBackend" -Protocol https
`$binding.AddSslCertificate(`$cert.Thumbprint, "My")

Write-Host "✅ SSL Certificate bound to IIS site" -ForegroundColor Green
"@

Write-Host $updateBinding -ForegroundColor Gray
Write-Host ""

Write-Host "7️⃣  Verify SSL:" -ForegroundColor Cyan
Write-Host "   Test your SSL installation at:" -ForegroundColor White
Write-Host "   https://www.ssllabs.com/ssltest/" -ForegroundColor Yellow
Write-Host ""

Write-Host "8️⃣  Automatic Renewal:" -ForegroundColor Cyan
Write-Host "   win-acme automatically creates a scheduled task for renewal" -ForegroundColor White
Write-Host "   Check: Task Scheduler → Task Scheduler Library → win-acme" -ForegroundColor White
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
