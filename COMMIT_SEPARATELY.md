# Commit Frontend and Backend Separately

Better approach: Commit each part separately for cleaner git history

## Step 1: Check for nested .git folders and remove them

```powershell
cd "c:\Users\David Ibanga\COOP_AIP"

# Check for nested .git in AIP_Backend
if (Test-Path ".\AIP_Backend\.git") {
    Remove-Item -Path ".\AIP_Backend\.git" -Recurse -Force
    Write-Host "Removed AIP_Backend/.git"
}

# Check for nested .git in AIP_UI
if (Test-Path ".\AIP_UI\.git") {
    Remove-Item -Path ".\AIP_UI\.git" -Recurse -Force
    Write-Host "Removed AIP_UI/.git"
}
```

## Step 2: Commit Backend (.NET)

```powershell
# Add only backend files
git add AIP_Backend/

# Check what will be committed
git status

# Commit backend
git commit -m "Add Backend: .NET Core + MSSQL API

- ASP.NET Core Web API
- Entity Framework Core
- Controllers: Employee, Incident, Alert Rules, Analytics
- Authentication & Authorization
- MSSQL Database integration
- Repository pattern implementation"

# Push backend
git push origin main
```

## Step 3: Commit Frontend (React)

```powershell
# Add only frontend files
git add AIP_UI/

# Check what will be committed
git status

# Commit frontend
git commit -m "Add Frontend: React + TypeScript + Vite

- React 18 with TypeScript
- Vite build tool
- TailwindCSS + Shadcn UI
- Pages: Analytics, Customer Intelligence, Employee Management
- Alert Rules management
- MSW for API mocking
- Responsive design for all screen sizes"

# Push frontend
git push origin main
```

## Step 4: Commit remaining files (docs, scripts, etc.)

```powershell
# Add everything else
git add .

# Check what's left
git status

# Commit remaining files
git commit -m "Add documentation and configuration files

- Database scripts
- Documentation files
- Configuration files
- Setup scripts"

# Push
git push origin main
```

This gives you 3 clean commits:
1. Backend code
2. Frontend code
3. Documentation/config

Much better for git history!
