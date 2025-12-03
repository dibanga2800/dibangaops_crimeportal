# IMPORTANT: You need to be in the ROOT directory!

## Your current location: AIP_Backend (WRONG!)
## You need to be in: COOP_AIP (ROOT)

## Step 1: Navigate to ROOT directory

```powershell
cd ..
# Now you should be in C:\Users\David Ibanga\COOP_AIP

# Verify with:
pwd
```

## Step 2: Remove nested .git folders (if any)

```powershell
if (Test-Path ".\AIP_Backend\.git") { Remove-Item -Path ".\AIP_Backend\.git" -Recurse -Force; Write-Host "Removed Backend .git" }
if (Test-Path ".\AIP_UI\.git") { Remove-Item -Path ".\AIP_UI\.git" -Recurse -Force; Write-Host "Removed UI .git" }
```

## Step 3: Add Backend

```powershell
git add AIP_Backend/
git status
git commit -m "Add Backend: .NET Core + MSSQL API"
git push origin main
```

## Step 4: Add Frontend

```powershell
git add AIP_UI/
git status
git commit -m "Add Frontend: React + TypeScript"
git push origin main
```

## Step 5: Add Documentation

```powershell
git add .
git status
git commit -m "Add documentation files"
git push origin main
```

**START WITH: cd ..**
