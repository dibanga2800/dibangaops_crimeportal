@echo off
cd /d "c:\Users\David Ibanga\COOP_AIP"

echo ========================================
echo Git Repository Setup
echo ========================================
echo.

echo [1/5] Removing nested .git folders...
if exist "AIP_Backend\.git" (
    echo   - Removing AIP_Backend\.git
    rmdir /s /q "AIP_Backend\.git"
)
if exist "AIP_UI\.git" (
    echo   - Removing AIP_UI\.git
    rmdir /s /q "AIP_UI\.git"
)
if exist ".git" (
    echo   - Removing root .git
    rmdir /s /q ".git"
)
echo   Done!
echo.

echo [2/5] Initializing Git repository...
git init
echo   Done!
echo.

echo [3/5] Adding remote origin...
git remote add origin https://github.com/advantageonesecurity/COOP_AIP
echo   Done!
echo.

echo [4/5] Staging files and creating commit...
git add .
git commit -m "Initial commit: COOP Security Management Application"
echo   Done!
echo.

echo [5/5] Setting up main branch...
git branch -M main
echo   Done!
echo.

echo ========================================
echo Setup Complete!
echo ========================================
echo.

echo Current status:
git status
echo.

echo Remote configuration:
git remote -v
echo.

echo ========================================
echo Next: Push to GitHub
echo ========================================
echo Run: git push -u origin main
echo.

pause
