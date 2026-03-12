# Demo Application Build Guide

This guide previously described how to build the application as a **demo/prototype** with mock data enabled using Mock Service Worker (MSW). MSW-based mocking has now been removed from the project and this guide is retained only for historical reference.

## Overview

The application now always talks directly to the configured backend API. There is no in-browser mock layer; any demo/prototype environment should be provided by a real (possibly non-production) backend.

## Quick Demo Build

To build a demo or production build now, simply configure `VITE_API_BASE_URL` to point at the backend you want to use (real or stub), then run your normal production build commands (for example `npm run build:production` followed by `npm run preview`).

## Deploy as Demo

### Vercel

1. Push your code to GitHub

2. Import project in Vercel

3. Configure environment variables in Vercel dashboard:
   ```
   VITE_APP_ENV=production
   VITE_API_BASE_URL=https://your-real-api.com/api
   VITE_APP_NAME=Central Co-op Interactive Portal
   ```

4. Deploy!

### Netlify

1. Push your code to GitHub

2. Import project in Netlify

3. Configure environment variables in Netlify dashboard:
   ```
   VITE_APP_ENV=production
   VITE_API_BASE_URL=https://your-real-api.com/api
   VITE_APP_NAME=Central Co-op Interactive Portal
   ```

4. Deploy!

## Notes on Demo vs Production

You can still use a non-production backend for demos (for example, a database snapshot or separate demo API), but all data now comes from whatever backend `VITE_API_BASE_URL` points to. There is no longer any in-browser mocking layer.

## Troubleshooting

### Backend Not Working
**Check:**
1. `VITE_API_BASE_URL` is correct in `.env.production` or your hosting environment.
2. The backend is reachable from your browser (no network or DNS errors).
3. CORS and authentication are configured correctly on the backend.

## Demo Presentation Tips

When presenting the demo:

1. **Highlight Real Features**: All UI/UX is production-ready
2. **Explain Mock Data**: Makes demo self-contained
3. **Show Responsiveness**: Works on all devices
4. **Demonstrate Speed**: Production build is fast
5. **Note Scalability**: Easy switch to real backend

## Transition to Production

When backend is ready, simply:

1. Update `.env.production`:
   ```env
   VITE_API_BASE_URL=https://your-real-api.com/api
   ```

2. Rebuild and redeploy

---

**This is a demo application with mock data enabled for demonstration purposes.**
