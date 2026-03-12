# Test Email Sending Script
# This script tests the SMTP configuration by sending a test email

param(
    [Parameter(Mandatory=$true)]
    [string]$ToEmail,
    
    [string]$SmtpHost = "mail.advantage1.co.uk",
    [int]$SmtpPort = 587,
    [bool]$EnableSsl = $true,
    [string]$SmtpUsername = "noreply@advantage1.co.uk",
    [string]$SmtpPassword = "Advantage29!",
    [string]$FromEmail = "noreply@advantage1.co.uk",
    [string]$FromName = "AIP Test Email"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EMAIL TEST SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "SMTP Configuration:" -ForegroundColor Yellow
Write-Host "  Host: $SmtpHost" -ForegroundColor White
Write-Host "  Port: $SmtpPort" -ForegroundColor White
Write-Host "  SSL: $EnableSsl" -ForegroundColor White
Write-Host "  Username: $SmtpUsername" -ForegroundColor White
Write-Host "  From: $FromEmail" -ForegroundColor White
Write-Host "  To: $ToEmail" -ForegroundColor White
Write-Host ""

# Load required assemblies
Add-Type -AssemblyName System.Net
Add-Type -AssemblyName System.Net.Mail

# Handle SSL certificate validation
if ($EnableSsl) {
    # For testing: accept all certificates (similar to what might be needed in production)
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {
        param($sender, $certificate, $chain, $sslPolicyErrors)
        return $true
    }
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13
}

try {
    Write-Host "Creating SMTP client..." -ForegroundColor Yellow
    
    $smtpClient = New-Object System.Net.Mail.SmtpClient
    $smtpClient.Host = $SmtpHost
    $smtpClient.Port = $SmtpPort
    $smtpClient.EnableSsl = $EnableSsl
    $smtpClient.Credentials = New-Object System.Net.NetworkCredential($SmtpUsername, $SmtpPassword)
    $smtpClient.Timeout = 30000
    
    Write-Host "Creating test email message..." -ForegroundColor Yellow
    
    $mailMessage = New-Object System.Net.Mail.MailMessage
    $mailMessage.From = New-Object System.Net.Mail.MailAddress($FromEmail, $FromName)
    $mailMessage.To.Add($ToEmail)
    $mailMessage.Subject = "Test Email - AIP Backend SMTP Test"
    $mailMessage.Body = @"
<html>
<body>
    <h2>Test Email from AIP Backend</h2>
    <p>This is a test email to verify SMTP configuration.</p>
    <p><strong>Configuration Used:</strong></p>
    <ul>
        <li>Host: $SmtpHost</li>
        <li>Port: $SmtpPort</li>
        <li>SSL Enabled: $EnableSsl</li>
        <li>From: $FromEmail</li>
    </ul>
    <p>If you received this email, the SMTP configuration is working correctly!</p>
    <p><em>Sent at: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")</em></p>
</body>
</html>
"@
    $mailMessage.IsBodyHtml = $true
    
    Write-Host "Sending test email..." -ForegroundColor Yellow
    Write-Host ""
    
    $smtpClient.Send($mailMessage)
    
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  EMAIL SENT SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test email sent to: $ToEmail" -ForegroundColor White
    Write-Host "Please check the recipient's inbox (and spam folder)." -ForegroundColor Yellow
    Write-Host ""
    
    $mailMessage.Dispose()
    $smtpClient.Dispose()
    
    exit 0
}
catch [System.Net.Mail.SmtpException] {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  SMTP ERROR" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Type: SMTP Exception" -ForegroundColor Red
    Write-Host "StatusCode: $($_.Exception.StatusCode)" -ForegroundColor Yellow
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Cyan
    Write-Host "  - Incorrect username/password" -ForegroundColor White
    Write-Host "  - Port blocked by firewall" -ForegroundColor White
    Write-Host "  - SSL/TLS certificate issues" -ForegroundColor White
    Write-Host "  - Mail server requires authentication" -ForegroundColor White
    Write-Host ""
    exit 1
}
catch {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ERROR" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Type: $($_.Exception.GetType().Name)" -ForegroundColor Red
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.Exception.StackTrace -ForegroundColor Gray
    Write-Host ""
    exit 1
}
