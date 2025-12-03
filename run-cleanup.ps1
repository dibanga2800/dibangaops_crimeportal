# PowerShell script to clean up non-existent pages from the database
# This script runs the cleanup-pages.sql file

Write-Host "🧹 Starting PageAccess Cleanup..." -ForegroundColor Cyan
Write-Host ""

$server = "DAVIDS-YOGA"
$database = "AIP_Security"
$sqlFile = "cleanup-pages.sql"

# Check if SQL file exists
if (-not (Test-Path $sqlFile)) {
    Write-Host "❌ Error: $sqlFile not found in current directory!" -ForegroundColor Red
    Write-Host "   Please run this script from: c:\Users\David Ibanga\COOP_AIP\" -ForegroundColor Yellow
    exit 1
}

Write-Host "📋 SQL Script: $sqlFile" -ForegroundColor White
Write-Host "🗄️  Database: $server.$database" -ForegroundColor White
Write-Host ""

# Confirm before running
Write-Host "⚠️  This will DELETE non-existent pages from the database:" -ForegroundColor Yellow
Write-Host "   - Bank Holiday" -ForegroundColor Yellow
Write-Host "   - Holiday Requests" -ForegroundColor Yellow
Write-Host "   - Incident List" -ForegroundColor Yellow
Write-Host "   - All Customer/* pages" -ForegroundColor Yellow
Write-Host "   - All Management/* pages" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Do you want to continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "❌ Cleanup cancelled by user" -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "🚀 Running cleanup script..." -ForegroundColor Green

try {
    # Run the SQL script
    sqlcmd -S $server -d $database -E -i $sqlFile -W -s "," -m 1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Cleanup completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📊 Next steps:" -ForegroundColor Cyan
        Write-Host "   1. Restart the backend server (Ctrl+C then 'dotnet run')" -ForegroundColor White
        Write-Host "   2. Refresh your browser (F5)" -ForegroundColor White
        Write-Host "   3. Go to Settings page - you should now see only 12 active pages!" -ForegroundColor White
    } else {
        Write-Host ""
        Write-Host "⚠️  Script completed with warnings (Exit Code: $LASTEXITCODE)" -ForegroundColor Yellow
        Write-Host "   Check the output above for details" -ForegroundColor Yellow
    }
} catch {
    Write-Host ""
    Write-Host "❌ Error running cleanup script:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
