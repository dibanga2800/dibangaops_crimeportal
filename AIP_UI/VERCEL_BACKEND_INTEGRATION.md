# Azure Static Web App + IIS Backend Integration Guide

## Overview

Your frontend is hosted on **Azure Static Web Apps** and your backend is hosted on **IIS Server**. This guide explains how to connect them.

## Backend Configuration

### ✅ CORS is Already Configured

The backend (`Program.cs`) already has CORS configured to allow requests from:
- `https://www.dibangops.com` (Production)
- `https://dibangops.com` (Apex redirect domain)
- Any additional domains configured in backend `FrontendUrl`

### Backend URLs

Your backend is accessible at:
- **HTTPS (Production)**: `https://coopaip.advantage1.co.uk`
- **HTTP (Local/Testing)**: `http://localhost:5000` (won't work from public HTTPS frontend)

**For production frontend hosting, you MUST use the HTTPS URL**: `https://coopaip.advantage1.co.uk`

## Frontend Configuration

### Step 1: Set Environment Variable in Frontend Hosting

1. Go to your frontend hosting dashboard
2. Navigate to your project → **Configuration** → **Environment Variables**
3. Add the following variable:

   **Variable Name:** `VITE_API_BASE_URL`
   
   **Value:** `https://coopaip.advantage1.co.uk/api`
   
   **Environment:** Select **Production** (and optionally **Staging** and **Development**)

   ⚠️ **Important Notes:**
   - The URL must include `/api` at the end
   - Use `https://` (not `http://`)
   - Use your actual domain: `coopaip.advantage1.co.uk`

### Step 2: Verify Environment Variable Format

The frontend expects the API URL in this format:
```
https://coopaip.advantage1.co.uk/api
```

**NOT:**
- ❌ `https://coopaip.advantage1.co.uk` (missing `/api`)
- ❌ `http://coopaip.advantage1.co.uk/api` (must be HTTPS)
- ❌ `https://localhost:5000/api` (won't work from a hosted public frontend)

### Step 3: Redeploy Frontend

After adding the environment variable:
1. Go to your hosting provider's **Deployments** tab
2. Click **Redeploy** on your latest deployment
3. Or push a new commit to trigger a new deployment

## How It Works

### Request Flow

```
User Browser (Azure Static Web App Frontend)
    ↓
HTTPS Request to: https://coopaip.advantage1.co.uk/api/Auth/login
    ↓
IIS Server (Backend)
    ↓
CORS Check: Is origin "https://www.dibangops.com" allowed? ✅ YES
    ↓
Process Request & Return Response
    ↓
Response sent back to Frontend
```

### CORS Headers

The backend will automatically add these headers to responses:
- `Access-Control-Allow-Origin: https://www.dibangops.com`
- `Access-Control-Allow-Credentials: true`
- `Access-Control-Allow-Methods: *`
- `Access-Control-Allow-Headers: *`

## Testing the Connection

### 1. Check Environment Variable

After deployment, open your frontend app and check the browser console:
```javascript
// Should show your API URL
console.log(import.meta.env.VITE_API_BASE_URL)
// Expected: "https://coopaip.advantage1.co.uk/api"
```

### 2. Test Login

Try logging in through your frontend. If it works, the connection is successful.

### 3. Check Network Tab

Open browser DevTools → Network tab:
- Look for requests to `https://coopaip.advantage1.co.uk/api/...`
- Check if they return `200 OK` or `401 Unauthorized` (not CORS errors)

## Troubleshooting

### Issue: CORS Error

**Error:** `Access to fetch at 'https://coopaip.advantage1.co.uk/api/...' from origin 'https://www.dibangops.com' has been blocked by CORS policy`

**Solution:**
1. Verify your frontend domain matches exactly: `https://www.dibangops.com`
2. Check backend CORS configuration in `Program.cs`
3. Ensure backend is using HTTPS

### Issue: Network Error / Failed to Fetch

**Error:** `Network Error` or `Failed to fetch`

**Possible Causes:**
1. Backend is down or unreachable
2. SSL certificate issue
3. Firewall blocking the connection
4. Wrong API URL in environment variable

**Solution:**
1. Verify backend is running: `https://coopaip.advantage1.co.uk/swagger`
2. Check SSL certificate is valid
3. Verify environment variable is set correctly in frontend hosting

### Issue: 404 Not Found

**Error:** `404 Not Found` on API requests

**Solution:**
- Ensure API URL includes `/api` at the end
- Check the endpoint path is correct (e.g., `/api/Auth/login` not `/api/auth/login`)

### Issue: 401 Unauthorized

**Error:** `401 Unauthorized`

**This is normal!** It means:
- ✅ CORS is working
- ✅ Backend is reachable
- ⚠️ You need to authenticate first (login)

## Environment Variables Summary

### For Production:

```env
VITE_API_BASE_URL=https://coopaip.advantage1.co.uk/api
VITE_APP_ENV=production
```

### For Staging:

```env
VITE_API_BASE_URL=https://coopaip.advantage1.co.uk/api
VITE_APP_ENV=staging
```

### For Local Development:

```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_ENV=development
```

## Security Considerations

1. **HTTPS Only**: Always use HTTPS for production API calls
2. **CORS**: Backend only allows specific origins (already configured)
3. **JWT Tokens**: Tokens are stored securely in the frontend
4. **Environment Variables**: Never commit `.env` files with secrets

## Next Steps

1. ✅ Set `VITE_API_BASE_URL` in frontend hosting dashboard
2. ✅ Redeploy your frontend
3. ✅ Test login functionality
4. ✅ Verify all API endpoints work correctly

---

**Status:** Ready to connect! Set the environment variable in your hosting dashboard and redeploy.
