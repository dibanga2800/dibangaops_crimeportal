# Production Deployment Checklist

This checklist ensures your application is ready for production deployment.

## Pre-Deployment Checklist

### 1. Environment Configuration ✅

- [ ] Copy `.env.example` to `.env.production`
- [ ] Set `VITE_APP_ENV=production`
- [ ] Configure production API URL in `VITE_API_BASE_URL`
- [ ] Disable MSW: `VITE_ENABLE_MSW=false`
- [ ] Configure analytics if needed: `VITE_ENABLE_ANALYTICS=true`
- [ ] Add error tracking DSN if using Sentry: `VITE_SENTRY_DSN=...`
- [ ] Remove any development/test credentials
- [ ] Verify all required environment variables are set

### 2. Code Quality ✅

- [ ] Run linter: `npm run lint`
- [ ] Run type checking: `npm run type-check`
- [ ] Remove all `debugger` statements
- [ ] Remove unnecessary `console.log` statements (build script handles this)
- [ ] Review and address all TODOs/FIXMEs in code
- [ ] Ensure no sensitive data (API keys, passwords) in code

### 3. Testing ✅

- [ ] Run all unit tests (if configured)
- [ ] Test critical user flows manually
- [ ] Test on multiple browsers (Chrome, Firefox, Safari, Edge)
- [ ] Test responsive design on different screen sizes
- [ ] Test with slow network connection
- [ ] Test error scenarios and error boundaries
- [ ] Verify 404 page works correctly
- [ ] Test authentication flows
- [ ] Test protected routes

### 4. Performance ✅

- [ ] Run production build: `npm run build:production`
- [ ] Check bundle sizes in build output
- [ ] Verify code splitting is working
- [ ] Test with Lighthouse (aim for 90+ scores)
- [ ] Optimize images (compress, use WebP if possible)
- [ ] Verify lazy loading is implemented for routes
- [ ] Check for memory leaks (open DevTools Performance)
- [ ] Verify Web Vitals metrics are acceptable

### 5. Security ✅

- [ ] Enable HTTPS on hosting platform
- [ ] Configure proper CORS headers on backend
- [ ] Set up Content Security Policy (CSP) headers
- [ ] Enable security headers (X-Frame-Options, X-Content-Type-Options)
- [ ] Verify JWT token expiration is configured
- [ ] Review and secure sensitive routes
- [ ] Enable rate limiting on API if available
- [ ] Ensure passwords are never logged
- [ ] Verify input validation is working

### 6. Build & Assets ✅

- [ ] Run pre-build checks: `npm run pre-build`
- [ ] Build for production: `npm run build:production`
- [ ] Run post-build checks: `npm run post-build`
- [ ] Verify `dist` folder contains all necessary files
- [ ] Check `index.html` has correct meta tags
- [ ] Verify favicon is present
- [ ] Test production build locally: `npm run preview`
- [ ] Verify all static assets load correctly

### 7. Error Handling ✅

- [ ] Error boundary is implemented
- [ ] API errors are handled gracefully
- [ ] Network errors show user-friendly messages
- [ ] Failed API calls have retry logic
- [ ] Loading states are shown appropriately
- [ ] Empty states are designed and implemented
- [ ] Toast notifications work for success/error

### 8. Analytics & Monitoring ✅

- [ ] Configure error tracking (e.g., Sentry) if needed
- [ ] Set up analytics (Google Analytics, Mixpanel, etc.) if needed
- [ ] Web Vitals monitoring is enabled
- [ ] Performance monitoring is configured
- [ ] Verify logging in production (errors/warnings only)

### 9. Documentation ✅

- [ ] README.md is up to date
- [ ] Deployment instructions are documented
- [ ] Environment variables are documented in `.env.example`
- [ ] API endpoints are documented (if applicable)
- [ ] Known issues are documented
- [ ] Changelog is updated (if maintaining one)

### 10. Backend Integration ✅

- [ ] Backend API is deployed and accessible
- [ ] CORS is configured correctly on backend
- [ ] API endpoints match frontend expectations
- [ ] Authentication/authorization is working
- [ ] Database migrations are applied
- [ ] Backend health check endpoint exists
- [ ] API rate limiting is configured

## Deployment Steps

### Option 1: Vercel Deployment

1. Install Vercel CLI (if not already):
   ```bash
   npm install -g vercel
   ```

2. Login to Vercel:
   ```bash
   vercel login
   ```

3. Deploy:
   ```bash
   vercel --prod
   ```

4. Configure environment variables in Vercel dashboard

5. Set up custom domain (if needed)

### Option 2: Netlify Deployment

1. Install Netlify CLI (if not already):
   ```bash
   npm install -g netlify-cli
   ```

2. Login to Netlify:
   ```bash
   netlify login
   ```

3. Deploy:
   ```bash
   netlify deploy --prod
   ```

4. Configure environment variables in Netlify dashboard

5. Set up custom domain (if needed)

### Option 3: Manual Deployment

1. Build the application:
   ```bash
   npm run build:production
   ```

2. Upload `dist` folder to your hosting provider

3. Configure web server:
   - For SPA routing, redirect all routes to `index.html`
   - Example Nginx config:
     ```nginx
     location / {
       try_files $uri $uri/ /index.html;
     }
     ```

4. Configure environment variables on server

5. Set up SSL certificate (Let's Encrypt recommended)

## Post-Deployment Checklist

### 1. Smoke Testing ✅

- [ ] Visit production URL
- [ ] Test login functionality
- [ ] Navigate through main pages
- [ ] Test critical user flows
- [ ] Verify API calls are working
- [ ] Check browser console for errors
- [ ] Test on mobile devices
- [ ] Test in incognito/private mode

### 2. Monitoring ✅

- [ ] Check error tracking dashboard (if configured)
- [ ] Monitor server logs for errors
- [ ] Check performance metrics
- [ ] Verify analytics is receiving data
- [ ] Set up alerts for critical errors

### 3. DNS & Domain ✅

- [ ] Update DNS records (if needed)
- [ ] Verify SSL certificate is valid
- [ ] Test domain propagation
- [ ] Configure www redirect (if applicable)
- [ ] Set up HSTS header

## Rollback Plan

If issues are discovered after deployment:

1. **Immediate Rollback**:
   - Revert to previous deployment in hosting dashboard
   - Or redeploy previous stable version

2. **Fix Issues**:
   - Identify and fix the issue locally
   - Test thoroughly
   - Deploy again following this checklist

3. **Communication**:
   - Notify users if there's downtime
   - Document what went wrong
   - Update team on status

## Performance Targets

Aim for these metrics in production:

- **Lighthouse Score**: 90+ across all categories
- **First Contentful Paint (FCP)**: < 1.8s
- **Largest Contentful Paint (LCP)**: < 2.5s
- **First Input Delay (FID)**: < 100ms
- **Cumulative Layout Shift (CLS)**: < 0.1
- **Time to Interactive (TTI)**: < 3.8s
- **Total Bundle Size**: < 500KB (gzipped)

## Support & Maintenance

### Regular Maintenance Tasks

- Monitor error rates weekly
- Review performance metrics monthly
- Update dependencies monthly
- Review and optimize bundle sizes quarterly
- Backup database regularly (backend)
- Review and rotate API keys/secrets quarterly
- Update SSL certificates before expiration

### Emergency Contacts

- DevOps: [Contact Information]
- Backend Team: [Contact Information]
- Database Admin: [Contact Information]
- Hosting Support: [Provider Support Link]

## Additional Resources

- [Vite Production Build Guide](https://vitejs.dev/guide/build.html)
- [React Performance Optimization](https://react.dev/learn/render-and-commit)
- [Web Vitals](https://web.dev/vitals/)
- [OWASP Security Guidelines](https://owasp.org/www-project-top-ten/)

---

**Last Updated**: December 2025
**Maintained By**: Development Team
