# Build Instructions

## Environment Configuration

The application uses different environment files for different environments. All runtime data comes from the configured backend API (`VITE_API_BASE_URL`); there is no in-browser mock layer.

### Development Build (Default)

Uses `.env` or `.env.example` values:

```bash
npm run dev
```

Ensure the .NET API is running and `VITE_API_BASE_URL` points at it (the default `/api` uses the Vite dev proxy).

### Production Build

**Option 1: Using .env.production file (Recommended)**

1. Create `.env.production`:

   ```env
   VITE_APP_ENV=production
   VITE_API_BASE_URL=https://your-production-api.com/api
   VITE_APP_NAME=Crime Portal
   VITE_APP_VERSION=1.0.0
   VITE_IDLE_TIMEOUT_MINUTES=60
   ```

2. Build:

   ```bash
   npm run build:production
   ```

**Option 2: Using environment variables**

```bash
# Windows (PowerShell)
$env:VITE_APP_ENV="production"; $env:VITE_API_BASE_URL="https://api.yourdomain.com/api"; npm run build:production

# Windows (CMD)
set VITE_APP_ENV=production && set VITE_API_BASE_URL=https://api.yourdomain.com/api && npm run build:production

# Linux/Mac
VITE_APP_ENV=production VITE_API_BASE_URL=https://api.yourdomain.com/api npm run build:production
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

### Issue: API calls fail in production

**Solution**: Confirm `VITE_API_BASE_URL` includes the `/api` path prefix and that CORS is configured on the backend.

## CI/CD Configuration

### GitHub Actions Example

```yaml
- name: Build
  env:
    VITE_APP_ENV: production
    VITE_API_BASE_URL: ${{ secrets.API_URL }}
  run: npm run build:production
```

### Vercel/Netlify

Configure environment variables in the dashboard:

- `VITE_APP_ENV` = `production`
- `VITE_API_BASE_URL` = Your production API URL

## Quick Start

```bash
# 1. Create production env file
echo VITE_APP_ENV=production > .env.production
echo VITE_API_BASE_URL=https://your-production-api.com/api >> .env.production

# 2. Build
npm run build:production

# 3. Test locally
npm run preview
```

---

**Remember**: Never commit `.env.production` or any file containing real credentials to version control!
