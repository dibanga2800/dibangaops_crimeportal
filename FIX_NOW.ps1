# Force remove nested .git folders and add files
Write-Host "=== Fixing Git Repository ===" -ForegroundColor Cyan

# Navigate to root
Set-Location "c:\Users\David Ibanga\COOP_AIP"

# Force remove nested .git folders
Write-Host "`nRemoving nested .git folders..." -ForegroundColor Yellow
$backendGit = ".\AIP_Backend\.git"
$uiGit = ".\AIP_UI\.git"

if (Test-Path $backendGit) {
    Remove-Item -Path $backendGit -Recurse -Force
    Write-Host "✓ Removed AIP_Backend\.git" -ForegroundColor Green
} else {
    Write-Host "✓ AIP_Backend\.git not found (good)" -ForegroundColor Green
}

if (Test-Path $uiGit) {
    Remove-Item -Path $uiGit -Recurse -Force
    Write-Host "✓ Removed AIP_UI\.git" -ForegroundColor Green
} else {
    Write-Host "✓ AIP_UI\.git not found (good)" -ForegroundColor Green
}

# Clean git cache
Write-Host "`nCleaning git cache..." -ForegroundColor Yellow
git rm -r --cached AIP_Backend 2>$null
git rm -r --cached AIP_UI 2>$null

# Add folders again
Write-Host "`nAdding Backend..." -ForegroundColor Yellow
git add AIP_Backend/

Write-Host "Adding Frontend..." -ForegroundColor Yellow
git add AIP_UI/

# Show what will be committed
Write-Host "`nFiles to be committed:" -ForegroundColor Cyan
git status --short

Write-Host "`n=== Ready! ===" -ForegroundColor Green
Write-Host "Run these commands:" -ForegroundColor Yellow
Write-Host "git commit -m 'Add complete COOP application'" -ForegroundColor White
Write-Host "git push origin main" -ForegroundColor White
