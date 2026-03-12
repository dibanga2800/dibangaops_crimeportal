# Build Instructions

## Environment Configuration

The application uses different environment files for different environments:

### Development Build (Default)
Uses `.env` or `.env.example` values:
```bash
npm run dev
```

### Production Build

**Option 1: Using .env.production file (Recommended)**

1. Create `.env.production`:
   ```bash
   # For Windows
   echo VITE_APP_ENV=production > .env.production
   echo VITE_API_BASE_URL=https://your-api.com/api >> .env.production
   echo VITE_ENABLE_MSW=false >> .env.production
   echo VITE_ANALYTICS_USE_MOCK=true >> .env.production
   ```

2. Configuration guide:
   ```env
   VITE_APP_ENV=production
   VITE_API_BASE_URL=https://your-production-api.com/api
   
   # MSW: false for production with real backend
   VITE_ENABLE_MSW=false
   
   # Analytics: true if backend not ready, false if ready
   VITE_ANALYTICS_USE_MOCK=true
   ```
   
   > **Hybrid Setup**: Set `VITE_ENABLE_MSW=false` to use real backend for everything,
   > and `VITE_ANALYTICS_USE_MOCK=true` to use mock data only for Analytics Hub.

3. Build:
   ```bash
   npm run build:production
   ```

**Option 2: Using environment variables**

Set environment variables directly:
```bash
# Windows (PowerShell)
$env:VITE_APP_ENV="production"; $env:VITE_API_BASE_URL="https://api.yourdomain.com/api"; $env:VITE_ENABLE_MSW="false"; npm run build

# Windows (CMD)
set VITE_APP_ENV=production && set VITE_API_BASE_URL=https://api.yourdomain.com/api && set VITE_ENABLE_MSW=false && npm run build

# Linux/Mac
VITE_APP_ENV=production VITE_API_BASE_URL=https://api.yourdomain.com/api VITE_ENABLE_MSW=false npm run build
```

**Option 3: Using Vite modes**

Vite automatically loads `.env.production` when building:
```bash
vite build --mode production
```

## How Environment Variables Work

1. **Priority Order** (highest to lowest):
   - Actual environment variables (set in terminal/CI)
   - `.env.[mode].local` (e.g., `.env.production.local`)
   - `.env.[mode]` (e.g., `.env.production`)
   - `.env.local`
   - `.env`

2. **Important Notes**:
   - `.env.example` is just a template (not loaded by Vite)
   - Only variables starting with `VITE_` are exposed to the app
   - `.env.local` and `.env.*.local` are ignored by git

## Verification

After building, verify your configuration:

1. Check the build output for environment info
2. Run the built app locally:
   ```bash
   npm run preview
   ```
3. Open browser console and check:
   ```javascript
   console.log(import.meta.env.VITE_APP_ENV) // Should be "production"
   console.log(import.meta.env.VITE_API_BASE_URL) // Should be your production API
   ```

## Common Issues

### Issue: Still seeing development mode
**Solution**: Make sure you have `.env.production` file with `VITE_APP_ENV=production`

### Issue: Environment variables not updating
**Solution**: 
1. Delete `node_modules/.vite` cache
2. Rebuild: `npm run build:production`

### Issue: MSW still running in production
**Solution**: Set `VITE_ENABLE_MSW=false` in `.env.production`

## CI/CD Configuration

### GitHub Actions Example
```yaml
- name: Build
  env:
    VITE_APP_ENV: production
    VITE_API_BASE_URL: ${{ secrets.API_URL }}
    VITE_ENABLE_MSW: false
  run: npm run build
```

### Vercel/Netlify
Configure environment variables in the dashboard:
- `VITE_APP_ENV` = `production`
- `VITE_API_BASE_URL` = Your production API URL
- `VITE_ENABLE_MSW` = `false`

## Quick Start

**For Demo/Prototype Production Build (with mock data):**

```bash
# 1. Create production env file with MSW enabled
echo VITE_APP_ENV=production > .env.production
echo VITE_API_BASE_URL=https://your-api.com/api >> .env.production
echo VITE_ENABLE_MSW=true >> .env.production

# 2. Build
npm run build:production

# 3. Test locally
npm run preview
```

**For Real Production Build (with live backend):**

```bash
# 1. Create production env file with MSW disabled
echo VITE_APP_ENV=production > .env.production
echo VITE_API_BASE_URL=https://your-production-api.com/api >> .env.production
echo VITE_ENABLE_MSW=false >> .env.production

# 2. Build
npm run build:production

# 3. Test locally
npm run preview
```

---

**Remember**: Never commit `.env.production` or any file containing real credentials to version control!
