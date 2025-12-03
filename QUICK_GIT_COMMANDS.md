# Quick Git Commands Reference

## Run the automated setup script:
```powershell
cd "c:\Users\David Ibanga\COOP_AIP"
.\fix-git-setup.ps1
```

## Or run commands manually:

### 1. Clean up nested repos
```powershell
cd "c:\Users\David Ibanga\COOP_AIP"
Remove-Item -Path ".\AIP_Backend\.git" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\AIP_UI\.git" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\.git" -Recurse -Force -ErrorAction SilentlyContinue
```

### 2. Initialize new repo
```powershell
git init
git remote add origin https://github.com/advantageonesecurity/COOP_AIP
```

### 3. Create commit
```powershell
git add .
git commit -m "Initial commit: COOP Security Management Application"
```

### 4. Push to main branch
```powershell
git branch -M main
git push -u origin main
```

## Check if GitHub repo exists:
Visit: https://github.com/advantageonesecurity/COOP_AIP

## If repo doesn't exist:
1. Go to https://github.com/new
2. Repository name: `COOP_AIP`
3. Set as Public or Private
4. Do NOT initialize with README (we already have code)
5. Click "Create repository"
6. Then run: `git push -u origin main`

## Verify everything:
```powershell
git status
git remote -v
git log --oneline
```
