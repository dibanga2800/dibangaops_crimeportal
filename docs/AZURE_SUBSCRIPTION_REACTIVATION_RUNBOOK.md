# Azure Subscription Reactivation Runbook

## Purpose

Use this runbook when Azure services stop working after moving from Free Trial to Pay-As-You-Go, especially when apps appear deployed but are unavailable at runtime.

## Incident Signature

Common signs:

- Azure Portal subscription page shows `Reactivation in progress` or recently moved to `Active`
- Frontend static site loads, but API calls fail
- Azure Container Apps endpoint shows:
  - `Error 404 - This Container App is stopped or does not exist.`
- Azure CLI write operations fail with:
  - `ReadOnlyDisabledSubscription`

## Immediate Verification Commands

Run in order:

```powershell
az account show
az account list --all --query "[].{name:name,id:id,state:state,isDefault:isDefault}" -o table
```

Check app availability:

```powershell
curl.exe -I https://<static-web-app-hostname>
curl.exe -i https://<backend-container-app-fqdn>/swagger
az containerapp revision list -n <backend-app-name> -g <resource-group> -o table
az containerapp logs show -n <backend-app-name> -g <resource-group> --tail 50
```

Optional raw subscription check:

```powershell
az rest --method get --url "https://management.azure.com/subscriptions/<subscription-id>?api-version=2020-01-01"
```

## Diagnosis Matrix

- **Frontend 200, backend unavailable, write operations denied with `ReadOnlyDisabledSubscription`**
  - Root cause: subscription/provider reactivation not fully propagated
- **Backend responds (200/301) and revisions are healthy**
  - Reactivation likely complete; proceed to app-level checks (CORS, auth, DB)

## Recovery Steps

1. Confirm subscription status in Azure Portal:
   - `Subscriptions` -> target subscription -> status is `Active`
2. Wait for provider propagation:
   - Typical: 15 minutes to 4 hours
   - Can take up to 24 hours in some billing transitions
3. If still blocked after 4-6 hours:
   - Open Microsoft support ticket (Billing + Container Apps)
   - Include:
     - Subscription ID
     - Error `ReadOnlyDisabledSubscription`
     - Impact statement: Container Apps return unavailable 404 page
4. Once write operations succeed, refresh Container App runtime:

```powershell
az containerapp update -n <backend-app-name> -g <resource-group> --image <acr>/<image>:<tag>
az containerapp revision list -n <backend-app-name> -g <resource-group> -o table
az containerapp logs show -n <backend-app-name> -g <resource-group> --tail 100
```

## Post-Recovery Validation

Confirm backend reachable:

```powershell
curl.exe -i https://<backend-container-app-fqdn>/swagger/index.html
```

Confirm frontend reachable:

```powershell
curl.exe -I https://<static-web-app-hostname>
```

Validate API from frontend origin:

```powershell
curl.exe -i -X OPTIONS `
  -H "Origin: https://<frontend-origin>" `
  -H "Access-Control-Request-Method: GET" `
  https://<backend-container-app-fqdn>/api/<health-or-light-endpoint>
```

## Hardening Checklist

- Keep backend allowed origins aligned with all production frontend domains
- Add uptime checks for:
  - static web app host
  - backend health/swagger endpoint
- Add alerting for 5xx spikes and endpoint availability drops
- Keep a known-good image tag for fast rollback/redeploy

## Notes for This Project

- Frontend deployment workflow path:
  - `.github/workflows/deploy-frontend.yml`
- Runtime issue previously observed:
  - backend unavailable during subscription reactivation
  - resolved after subscription became active and backend revision recovered
