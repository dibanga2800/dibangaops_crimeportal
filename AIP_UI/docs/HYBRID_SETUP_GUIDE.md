# Hybrid Backend Setup Guide

This guide explains how to run the app with a **real backend** for most features while using **mock data** only for Analytics Hub.

## Configuration Overview

```
┌─────────────────────────────────────────┐
│         Your Application                 │
├─────────────────────────────────────────┤
│                                          │
│  ✅ Login             → Real Backend    │
│  ✅ Incidents         → Real Backend    │
│  ✅ Customers         → Real Backend    │
│  ✅ Users             → Real Backend    │
│  ✅ Alert Rules       → Real Backend    │
│  ✅ Action Calendar   → Real Backend    │
│  ✅ Stock Management  → Real Backend    │
│                                          │
│  📊 Analytics Hub     → Mock Data       │
│                                          │
└─────────────────────────────────────────┘
```

## Quick Setup

### Step 1: Create Production Environment File

Create `.env.production` with these settings:

```env
# Application Environment
VITE_APP_ENV=production

# Real Backend API
VITE_API_BASE_URL=https://your-api.com/api

# MSW Disabled (not needed for production)
VITE_ENABLE_MSW=false

# Analytics uses mock data (backend not ready)
VITE_ANALYTICS_USE_MOCK=true

# Optional
VITE_ENABLE_ANALYTICS=false
VITE_ENABLE_ERROR_TRACKING=false
VITE_APP_NAME=Central Co-op Interactive Portal
VITE_APP_VERSION=1.0.0
```

### Step 2: Build

```bash
npm run build:production
```

### Step 3: Deploy

Upload the `dist` folder or deploy to Vercel/Netlify with the environment variables above.

## How It Works

### Real Backend Endpoints

These use your actual backend API:

```typescript
// All these hit your real API at VITE_API_BASE_URL
GET    /api/incidents
POST   /api/incidents
GET    /api/customers
POST   /api/users
GET    /api/alertrules
GET    /api/actioncalendar
GET    /api/stock
// ... all other endpoints
```

### Mock Data (Analytics Hub Only)

The Analytics Hub service (`src/services/analyticsService.ts`) checks `VITE_ANALYTICS_USE_MOCK`:

```typescript
// When VITE_ANALYTICS_USE_MOCK=true
analyticsService.getAnalyticsHub() 
  → Returns local mock data from mockAnalyticsData.ts
  → No API call made
  → Instant response with demo data

// When VITE_ANALYTICS_USE_MOCK=false
analyticsService.getAnalyticsHub()
  → Makes real API call to /analytics/hub
  → Returns data from backend
```

## Environment Variables Reference

| Variable | Purpose | Value for Hybrid Setup |
|----------|---------|----------------------|
| `VITE_APP_ENV` | Application mode | `production` |
| `VITE_API_BASE_URL` | Backend API URL | Your real API URL |
| `VITE_ENABLE_MSW` | Enable MSW | `false` (not needed) |
| `VITE_ANALYTICS_USE_MOCK` | Analytics mock data | `true` (until backend ready) |

## Platform-Specific Setup

### Vercel

1. Go to Project Settings → Environment Variables
2. Add:
   ```
   VITE_APP_ENV=production
   VITE_API_BASE_URL=https://your-api.com/api
   VITE_ENABLE_MSW=false
   VITE_ANALYTICS_USE_MOCK=true
   ```
3. Redeploy

### Netlify

1. Go to Site Settings → Build & Deploy → Environment
2. Add the same variables as above
3. Trigger new deploy

### Manual Deployment

```bash
# Set environment variables in your terminal
export VITE_APP_ENV=production
export VITE_API_BASE_URL=https://your-api.com/api
export VITE_ENABLE_MSW=false
export VITE_ANALYTICS_USE_MOCK=true

# Build
npm run build:production

# Upload dist/ folder to your server
```

## Testing Locally

```bash
# 1. Create .env.production
cat > .env.production << EOF
VITE_APP_ENV=production
VITE_API_BASE_URL=http://localhost:5128/api
VITE_ENABLE_MSW=false
VITE_ANALYTICS_USE_MOCK=true
EOF

# 2. Build
npm run build:production

# 3. Preview
npm run preview

# 4. Test in browser
# - Login → Should use real backend
# - Create incident → Should save to database
# - View Analytics Hub → Should show mock data
```

## Verification Checklist

After deployment, verify:

- [ ] ✅ Login works (real authentication)
- [ ] ✅ Incidents CRUD works (saves to database)
- [ ] ✅ Customers load from database
- [ ] ✅ Users management works
- [ ] ✅ Alert rules functional
- [ ] 📊 Analytics Hub shows data (mock)
- [ ] No MSW console messages
- [ ] No CORS errors
- [ ] API calls go to correct backend

## Transitioning Analytics to Real Backend

When your backend team completes the Analytics API:

### Step 1: Update Environment Variable

```env
# Change from true to false
VITE_ANALYTICS_USE_MOCK=false
```

### Step 2: Update Analytics Service (Optional)

Uncomment the real API call in `src/services/analyticsService.ts`:

```typescript
async getAnalyticsHub(params?: AnalyticsQueryParams): Promise<AnalyticsHubData> {
  if (!this.useMockData) {
    const response = await api.get(`${this.baseUrl}/hub`, { params })
    return response.data
  }
  // ... mock data code
}
```

### Step 3: Rebuild and Redeploy

```bash
npm run build:production
# Deploy
```

That's it! No other code changes needed.

## Backend Requirements

For Analytics Hub to work with real backend, implement these endpoints:

### Required Endpoint

```
GET /api/analytics/hub
```

**Query Parameters:**
- `customerId` (optional): Filter by customer
- `startDate` (optional): Date range start
- `endDate` (optional): Date range end  
- `storeIds` (optional): Filter by stores
- `regionIds` (optional): Filter by regions

**Response Format:**
```json
{
  "success": true,
  "data": {
    "summary": {
      "totalIncidents": 1245,
      "violentCrimes": 156,
      "propertyCrimes": 789,
      "otherIncidents": 300,
      "trendPercentage": 12.5
    },
    "crimeTrends": [...],
    "hotProducts": [...],
    "repeatOffenders": [...],
    "deploymentRecommendations": [...],
    "crimeLinking": [...]
  }
}
```

See `src/types/analytics.ts` for complete type definitions.

## Troubleshooting

### Issue: All endpoints returning mock data
**Problem**: `VITE_ENABLE_MSW=true` is still set
**Solution**: Set `VITE_ENABLE_MSW=false` and rebuild

### Issue: Analytics Hub not loading
**Problem**: Backend endpoint called but not ready
**Solution**: Set `VITE_ANALYTICS_USE_MOCK=true`

### Issue: CORS errors on real backend
**Problem**: Backend CORS not configured
**Solution**: Add frontend domain to backend CORS whitelist

### Issue: Authentication not working
**Problem**: Wrong API URL
**Solution**: Verify `VITE_API_BASE_URL` points to correct backend

## Current Status

✅ **Working with Real Backend:**
- User authentication
- Incident management
- Customer management
- User management
- Alert rules
- Action calendar
- Stock management
- All CRUD operations

📊 **Using Mock Data (Temporary):**
- Analytics Hub
- Crime trend analysis
- Hot products analysis
- Repeat offender tracking
- Deployment recommendations

## Summary

This hybrid setup allows you to:
1. ✅ Use your real backend for all implemented features
2. 📊 Show Analytics Hub with demo data
3. 🚀 Transition smoothly when backend is ready
4. 🎯 No code changes needed - just flip environment variable

Perfect for phased backend development! 🎉

---

**Note**: Keep `VITE_ANALYTICS_USE_MOCK=true` until your backend team implements `/api/analytics/hub` endpoint.
