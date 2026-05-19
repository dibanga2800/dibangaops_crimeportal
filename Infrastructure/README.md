# Infrastructure Deployment Guide

This folder provisions Azure infrastructure for the Crime Portal platform using Terraform.

## What it deploys

- Resource Group
- Log Analytics Workspace
- Optional Application Insights
- Key Vault (RBAC mode)
- Azure SQL Server + serverless SQL Database
- Storage Account + private `images` and `evidence` containers
- Azure Container Registry (ACR)
- Container Apps environment
- Backend Container App (external ingress, port `8080`)
- AI Container App (internal ingress, port `8000`)
- Static Web App frontend
- Subscription budget alert (optional, email based)

## Cost defaults (practical and production-safe)

Defaults are now tuned for reliable runtime behavior with sensible spend:

- Application Insights enabled for operational visibility
- Log Analytics retention set to 30 days
- Backend Container App keeps `min_replicas = 1` for responsiveness
- Backend and AI container resources set to `0.5 CPU` / `1Gi`
- SQL max size set to 5 GB (serverless auto-pause still applies)
- Monthly budget alert enabled when `budget_alert_emails` is provided

For lower non-production costs, use environment-specific overrides (`terraform.tfvars`) instead of changing shared defaults.

## Prerequisites

- Terraform `>= 1.3.0`
- Azure CLI authenticated to target subscription
- Remote Terraform backend in Azure Storage

## State backend

Use a remote backend. Do not use local `.tfstate` in source control.

1. Copy `backend.hcl.example` to a local secure file (for example `backend.hcl`)
2. Set your real backend values
3. Initialize:

```bash
terraform init -backend-config=backend.hcl
```

## Local plan/apply example

```bash
cp terraform.dev.tfvars.example terraform.dev.tfvars   # optional: edit for your dev profile

terraform plan \
  -var-file="terraform.dev.tfvars" \
  -var="backend_image=mcr.microsoft.com/azuredocs/containerapps-helloworld:latest" \
  -var="ai_image=mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
```

```bash
cp terraform.prod.tfvars.example terraform.prod.tfvars   # then edit with real values (file is gitignored)

terraform apply \
  -var-file="terraform.prod.tfvars" \
  -var="backend_image=<acr-login-server>/crimeportal-backend:<tag>" \
  -var="ai_image=<acr-login-server>/crimeportal-ai:<tag>"
```

## Environment profiles

- `terraform.dev.tfvars.example` → copy to **`terraform.dev.tfvars`** (gitignored) for local dev plans
- `terraform.prod.tfvars.example` → copy to **`terraform.prod.tfvars`** (gitignored) for production applies

Use `TF_VAR_budget_alert_emails` for alert recipients, for example:

```bash
export TF_VAR_budget_alert_emails='["team@yourcompany.com"]'
```

## CI/CD secrets required

For GitHub Actions:

- `TERRAFORM_PROD_TFVARS` — **full multiline contents** of your production `terraform.prod.tfvars` (same keys as `terraform.prod.tfvars.example`). Required for deploy workflows; never commit the real file.
- `TERRAFORM_PLAN_TFVARS` — **optional**; if set, used by pull-request `terraform plan` so planned changes match what is deployed (paste the same body you use for real plans, often identical to prod tfvars). If unset, CI uses `terraform.dev.tfvars.example` only (validate still runs; plan may be noisy or misleading).
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID` (must match an **active** subscription the app can access)
- `PRODUCTION_API_BASE_URL` (optional) — full `https://` API base **without** `/api` suffix; when set, **Deploy Frontend** skips Azure OIDC and uses this URL for `VITE_API_BASE_URL`

**Production auth (pentest):** Keep `Auth__Cookies__ExposeTokensInResponse=false`. Prefer **unified Front Door** (`enable_unified_front_door = true`) so the SPA calls `/api` on the same origin as `www` — first-party `SameSite=Lax` cookies without `auth_cookie_domain`. Do not expose JWTs in login JSON responses in production.
- `TF_STATE_RESOURCE_GROUP`
- `TF_STATE_STORAGE_ACCOUNT`
- `TF_STATE_CONTAINER`
- `TF_STATE_KEY`
- `AZURE_STATIC_WEB_APPS_API_TOKEN` (optional but required for frontend deploy job)
- `TF_VAR_BUDGET_ALERT_EMAILS` (JSON array string, e.g. `["team@yourcompany.com"]`)

## Budget alert configuration

Budget alert is created only when:

- `enable_budget_alert = true`
- `budget_alert_emails` has at least one email

Default alert thresholds:

- 80% actual spend
- 100% forecasted spend

## Security hardening (optional Terraform flags)

### HttpOnly JWT cookies (application)

The API sets `aip_access`, `aip_refresh`, and `aip_csrf` cookies. Configure the backend Container App via:

- **`enable_unified_front_door = true`** (recommended) — cookies are host-only on `www` with `SameSite=Lax`; no `auth_cookie_domain` required
- **`auth_cookie_domain`** — only when using separate API hostname (e.g. `.example.com` for `www` + `api` subdomains)
- **`auth_cookie_same_site`** — `Lax` for unified or shared-domain setups; cross-origin SWA → Container Apps FQDN uses `None` automatically when unified FD is disabled and `auth_cookie_domain` is unset

### Unified Front Door (same-origin SPA + API)

```hcl
enable_unified_front_door  = true
unified_front_door_hostname = "www.example.com"
frontend_www_custom_domain  = "www.example.com"
```

**DNS cutover (do in order):**

1. `terraform apply` with unified Front Door enabled (www can still point at SWA).
2. Note `terraform output unified_front_door_endpoint_host`.
3. Validate via Front Door default hostname (before custom DNS):
   - `curl -sS "https://<unified-fd-host>/api/health"` → `{"status":"healthy"}`
   - Open `https://<unified-fd-host>/` → SPA loads.
4. In Azure Portal → Front Door → custom domain `www.example.com` → wait for **Managed certificate** = Deployed.
5. Change **www** DNS CNAME from Static Web App to `unified_front_door_endpoint_host` (TTL 300s or lower for rollback).
6. Redeploy frontend with `VITE_API_BASE_URL=/api` (Deploy Frontend workflow does this when unified FD is enabled).
7. Confirm cookies appear under `www.example.com` in DevTools → Application → Cookies (not under `azurecontainerapps.io`).
8. Smoke-test login on iOS Safari (non-private).

**Rollback:** Point www CNAME back to SWA; redeploy frontend with previous `VITE_API_BASE_URL` if needed.

CI: set `public_app_url` / use relative `/api` — see `.github/workflows/deploy-frontend.yml`.

### Azure Front Door + WAF (API-only, optional)

```hcl
enable_api_front_door = true
api_custom_domain     = "api.example.com"
```

Point DNS for `api.example.com` to the Front Door endpoint (`terraform output api_front_door_endpoint_host`). Use for Swagger/tools when the SPA uses unified Front Door on `www`.

**Pentest note — direct API bypass:** The backend Container App FQDN (`terraform output backend_container_app_url`) remains reachable when `external_enabled` is true. WAF rules on Front Door do not apply to direct Container App requests. For pentest scope, either document this as an accepted finding, add Container App ingress IP restrictions, or use private ingress with Front Door as the only public entry.

### SQL private endpoint

```hcl
enable_sql_private_endpoint = true
sql_allowed_ip_ranges     = []
```

This disables public SQL access, provisions a VNet + private endpoint, and integrates the Container Apps environment with the VNet. **Plan during a maintenance window** — it may recreate the Container Apps environment.

## Notes

- `terraform.tfvars`, `terraform.prod.tfvars`, and `terraform.dev.tfvars` are **gitignored**; keep real values in local files or CI secrets (`TERRAFORM_PROD_TFVARS`). Commit only the `*.tfvars.example` templates.
- `.terraform` and local state files are intentionally ignored.
