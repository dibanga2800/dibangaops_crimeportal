# Check Publish Folder Structure
# This script identifies what's normal vs. what might be a mistake

Write-Host "=== Checking Publish Folder Structure ===" -ForegroundColor Cyan
Write-Host ""

$publishPath = ".\publish"

if (-not (Test-Path $publishPath)) {
    Write-Host "❌ Publish folder not found!" -ForegroundColor Red
    exit 1
}

# Check for nested publish folder (this would be a mistake)
$nestedPublish = Join-Path $publishPath "publish"
if (Test-Path $nestedPublish) {
    Write-Host "⚠️  WARNING: Found nested 'publish' folder inside publish!" -ForegroundColor Yellow
    Write-Host "   This is likely a mistake from a previous publish." -ForegroundColor Yellow
    Write-Host "   Location: $nestedPublish" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   You can safely delete this nested folder." -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "✅ No nested publish folder found (good!)" -ForegroundColor Green
    Write-Host ""
}

# List all subdirectories
Write-Host "=== Subdirectories in Publish Folder ===" -ForegroundColor Cyan
$subdirs = Get-ChildItem $publishPath -Directory

if ($subdirs.Count -eq 0) {
    Write-Host "   No subdirectories found" -ForegroundColor Gray
} else {
    foreach ($dir in $subdirs) {
        $fileCount = (Get-ChildItem $dir.FullName -Recurse -File).Count
        Write-Host "   📁 $($dir.Name) ($fileCount files)" -ForegroundColor White
        
        # Check if it looks like a duplicate publish
        if ($dir.Name -eq "publish") {
            Write-Host "      ⚠️  This is a nested publish folder - should be removed!" -ForegroundColor Yellow
        } elseif ($dir.Name -eq "runtimes") {
            Write-Host "      ✅ Normal: Contains platform-specific native libraries" -ForegroundColor Green
        } elseif ($dir.Name -like "*.dll" -or $dir.Name -like "*.exe") {
            Write-Host "      ⚠️  Unusual: Directory with DLL/EXE name" -ForegroundColor Yellow
        } else {
            Write-Host "      ℹ️  Check if this is expected for your project" -ForegroundColor Gray
        }
    }
}

Write-Host ""
Write-Host "=== Normal Publish Folder Contents ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Normal subdirectories:" -ForegroundColor Green
Write-Host "   - runtimes/          (Platform-specific native libraries)" -ForegroundColor White
Write-Host "   - Any custom folders you created" -ForegroundColor White
Write-Host ""
Write-Host "❌ NOT normal (mistakes):" -ForegroundColor Red
Write-Host "   - publish/           (Nested publish folder)" -ForegroundColor White
Write-Host "   - bin/               (Should not be in publish)" -ForegroundColor White
Write-Host "   - obj/               (Should not be in publish)" -ForegroundColor White
Write-Host ""

# Check for common mistakes
Write-Host "=== Checking for Common Mistakes ===" -ForegroundColor Cyan
$mistakes = @()

if (Test-Path (Join-Path $publishPath "bin")) {
    $mistakes += "bin/ folder found (should not be in publish)"
}

if (Test-Path (Join-Path $publishPath "obj")) {
    $mistakes += "obj/ folder found (should not be in publish)"
}

if (Test-Path $nestedPublish) {
    $mistakes += "publish/publish/ nested folder found"
}

if ($mistakes.Count -gt 0) {
    Write-Host "⚠️  Found potential issues:" -ForegroundColor Yellow
    foreach ($mistake in $mistakes) {
        Write-Host "   - $mistake" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "💡 Recommendation: Clean and republish:" -ForegroundColor Cyan
    Write-Host "   1. Delete the publish folder" -ForegroundColor White
    Write-Host "   2. Run: dotnet publish AIPBackend.csproj -c Release -o .\publish --self-contained false" -ForegroundColor White
} else {
    Write-Host "✅ No common mistakes found!" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "For deployment, you need:" -ForegroundColor Yellow
Write-Host "   ✅ All DLL files in the root of publish/" -ForegroundColor Green
Write-Host "   ✅ AIPBackend.dll (your main application)" -ForegroundColor Green
Write-Host "   ✅ web.config" -ForegroundColor Green
Write-Host "   ✅ appsettings.json files" -ForegroundColor Green
Write-Host "   ✅ runtimes/ folder (if present, contains native libraries)" -ForegroundColor Green
Write-Host ""
Write-Host "You do NOT need:" -ForegroundColor Yellow
Write-Host "   ❌ Nested publish/ folder" -ForegroundColor Red
Write-Host "   ❌ bin/ or obj/ folders" -ForegroundColor Red
Write-Host "   ❌ Source code files (.cs)" -ForegroundColor Red
