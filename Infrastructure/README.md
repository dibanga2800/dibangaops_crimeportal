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

### Terraform state recovery (legacy RG deleted)

If **Deploy Full Stack** fails at `terraform init` with `ResourceGroupNotFound` on the state backend, the `TF_STATE_*` secrets still point at **deleted** infrastructure (often legacy `crimeportal-rg`, removed when the blue stack was cut over).

**Check in Azure Portal (subscription must be active):**

1. **Resource groups** — confirm `crimeportal-tfstate-rg` or legacy `crimeportal-rg` exists. Today prod-v2 lives in `crimeportal-prod-v2-rg` only; **Terraform state is stored separately** in a storage account, not in the prod-v2 RG.
2. **Storage accounts → Manage deleted accounts** — if the old tfstate account was soft-deleted with legacy `crimeportal-rg`, **recover it** and reuse the same `TF_STATE_*` values from before deletion.
3. If no tfstate storage exists, create a dedicated backend (do **not** reuse `crimeportalstorage*` app blob storage):

```powershell
.\scripts\setup-tfstate-backend.ps1
```

Then set GitHub secrets from the script output:

| Secret | Example |
|--------|---------|
| `TF_STATE_RESOURCE_GROUP` | `crimeportal-tfstate-rg` |
| `TF_STATE_STORAGE_ACCOUNT` | `crimeportaltfstateXXXXX` |
| `TF_STATE_CONTAINER` | `tfstate` |
| `TF_STATE_KEY_V2` | `crimeportal-prod-v2.tfstate` |

**Critical:** A **new empty** state blob is not the same as the old one. If `crimeportal-prod-v2.tfstate` was lost, **do not run `terraform apply`** until the blob is recovered or existing prod-v2 resources are imported into state — otherwise Terraform may plan duplicate resources with new random suffixes. Frontend/backend-only deploy workflows do not use this state file.

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
- `TF_STATE_KEY` — production state blob key (e.g. `crimeportal-prod.tfstate`)
- `AZURE_STATIC_WEB_APPS_API_TOKEN` (optional but required for frontend deploy job)
- `TF_VAR_BUDGET_ALERT_EMAILS` (JSON array string, e.g. `["team@yourcompany.com"]`)

### Deploy targets (`target_environment`)

`Deploy Full Stack` routes Terraform state, tfvars, and resource group by input:

| Input | Resource group | State secret | Tfvars secret | www verify |
|-------|----------------|--------------|---------------|------------|
| `prod` (default) | `crimeportal-rg` | `TF_STATE_KEY` | `TERRAFORM_PROD_TFVARS` | Yes (`www`) |
| `prod-v2` | `crimeportal-prod-v2-rg` | `TF_STATE_KEY_V2` | `TERRAFORM_PROD_V2_TFVARS` | No (smoke `*.azurefd.net` + CA FQDN) |
| `phase1-scratch` | `crimeportal-phase1-scratch-rg` | `TF_STATE_KEY_SCRATCH` | `TERRAFORM_PHASE1_SCRATCH_TFVARS` | No |

**Tfvars templates (committed):**

- [`terraform.prod.public.tfvars.example`](terraform.prod.public.tfvars.example) — Path A legacy (public SQL)
- [`terraform.prod.private.tfvars.example`](terraform.prod.private.tfvars.example) — Path B green (private SQL + NAT from first apply; **cost-optimised ~£90–100/mo** profile with Basic SQL)

**Cost-optimised prod-v2 (private SQL kept):** The private tfvars template targets ~£90–100/month at full-month run rate: `sql_sku_name = "Basic"`, single backend replica at 0.25 vCPU, AI scale-to-zero, 7-day logs, no App Insights. NAT (~£29/mo) and Front Door (~£30/mo) remain — they are required for the current architecture. Update GitHub secret `TERRAFORM_PROD_V2_TFVARS` to match, then `Deploy Full Stack` → `prod-v2`.

**Manual cleanup:** If Azure shows a Container App named `crimeportal-ui-prod-v2` (~£0/mo), it is not managed by Terraform (frontend is Static Web Apps). Delete it in Portal if unused.

**Blue/green cutover:** [`docs/blue-green-cutover-runbook.md`](../docs/blue-green-cutover-runbook.md)

### Green stack secrets (`prod-v2`)

Create **new** secrets (do not overwrite legacy until DNS cutover):

- `TF_STATE_KEY_V2` — e.g. `crimeportal-prod-v2.tfstate` (same storage account/container as `TF_STATE_KEY`)
- `TERRAFORM_PROD_V2_TFVARS` — full HCL from `terraform.prod.private.tfvars.example` with production values; first line must be a variable assignment
- `AZURE_STATIC_WEB_APPS_API_TOKEN_V2` — SWA deployment token from the green Static Web App (after first green apply)

The workflow sets `TF_VAR_resource_group=crimeportal-prod-v2-rg` and skips legacy-only steps (COOP DB import, www custom domain import, unified FD state sync, `www` edge verification).

**Parallel stack naming:** When `resource_group` is not `crimeportal-rg`, Terraform automatically suffixes globally unique resource names (Static Web App, Container Apps, unified Front Door endpoint/profile, etc.) with the stack slug derived from the RG (e.g. `crimeportal-prod-v2-rg` → `-prod-v2`). Legacy prod keeps unsuffixed names. No extra tfvars keys required.

**SWA custom domains on green:** `dibangops.com` / `www` stay on the legacy Static Web App until cutover. Prod-v2 still sets `frontend_custom_domain` / `frontend_www_custom_domain` in tfvars for backend CORS, but Terraform does not attach those domains to the green SWA (Azure allows one SWA per hostname). UAT uses `unified_front_door_endpoint_host` (`*.azurefd.net`).

### Scratch RG secrets (`phase1-scratch`)

Optional throwaway rehearsal — not a migration source. See [`docs/phase1-perimeter-runbook.md`](../docs/phase1-perimeter-runbook.md).

- `TF_STATE_KEY_SCRATCH` — e.g. `crimeportal-phase1-scratch.tfstate`
- `TERRAFORM_PHASE1_SCRATCH_TFVARS` — copy prod-v2 body; often `enable_unified_front_door = false` for CA-only smoke tests; lower `monthly_budget_amount` if desired

Bootstrap fails fast if scratch or prod-v2 secrets are missing when those targets are selected.

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

**Production runbook:** [docs/production-edge-runbook.md](../docs/production-edge-runbook.md) — frozen DNS/TLS flags, CI guardrails, and incident table.

```hcl
enable_unified_front_door               = true
enable_unified_front_door_custom_domain = true # required in production after cutover
unified_front_door_hostname             = "www.example.com"
frontend_www_custom_domain              = "www.example.com"
```

**Important:** Do not set `enable_unified_front_door_custom_domain = true` until **www** DNS CNAME points at `unified_front_door_endpoint_host`. A custom domain stuck in **Pending** validation blocks edge deployment and returns 404 (“We weren't able to find your Azure Front Door Service”) on the `*.azurefd.net` hostname too.

**CI:** Full stack and frontend deploys run `scripts/verify-production-edge.sh` (TLS CN + `/api/health`). When `crimeportal-rg` exists, `scripts/validate-terraform-prod-tfvars.sh` blocks Terraform apply if unified custom domain flags are turned off.

**Analytics KPIs:** Crime Intelligence (`GET /api/incidents/insights`) and Incident Graph (`GET /api/incidents/graph-analytics`) aggregate the **full** filtered dataset server-side. Do not use paginated `GET /api/incidents` for dashboard totals (max 100 rows per page).

**DNS cutover (do in order):**

1. `terraform apply` with `enable_unified_front_door = true` and `enable_unified_front_door_custom_domain = false` (www can still point at SWA).
2. Note `terraform output unified_front_door_endpoint_host`.
3. Validate via Front Door default hostname (before custom DNS):
   - `curl -sS "https://<unified-fd-host>/api/health"` → `{"status":"healthy"}`
   - Open `https://<unified-fd-host>/` → SPA loads.
4. Change **www** DNS CNAME from Static Web App to `unified_front_door_endpoint_host` (DNS only / grey cloud in Cloudflare).
5. `terraform apply` with `enable_unified_front_door_custom_domain = true`.
6. Add the **TXT** record Azure shows for domain validation (Portal → Front Door → custom domain → Validation). In Cloudflare: type `TXT`, name `_dnsauth.www`, content = the validation token, **DNS only**. Without this TXT, validation stays **Pending** and `https://www` returns 404 or certificate errors.
7. In Azure Portal → custom domain → wait until **Domain validation** = Approved and **Managed certificate** = Deployed (often 5–30 minutes after TXT propagates).
8. Redeploy frontend with `VITE_API_BASE_URL=/api` (Deploy Frontend workflow does this when unified FD is enabled).
9. Confirm cookies appear under `www.example.com` in DevTools → Application → Cookies (not under `azurecontainerapps.io`).
10. Smoke-test login on iOS Safari (non-private).

**Rollback:** Point www CNAME back to SWA; redeploy frontend with previous `VITE_API_BASE_URL` if needed.

CI: set `public_app_url` / use relative `/api` — see `.github/workflows/deploy-frontend.yml`.

**Note:** Unified Front Door on `Standard_AzureFrontDoor` does not attach WAF managed rule sets (Premium SKU only). Use Azure SWA + Container App hardening and optional `enable_api_front_door` with Premium if you need WAF managed rules.

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

### Phase 1 perimeter (Standard FD, no Premium WAF)

Terraform supports:

- `enable_sql_private_endpoint` provisions VNet + private endpoint + private DNS for `privatelink.database.windows.net` and disables SQL public network access.
- `enable_nat_gateway_egress` (default `true`) attaches a Standard SKU NAT Gateway to the Container Apps subnet so VNet-integrated apps have deterministic outbound to ACR, App Insights, Key Vault, SMTP, and the InsightFace model CDNs. **Required** when `enable_sql_private_endpoint = true`; the `check "phase1_requires_nat_gateway"` assertion in `checks.tf` plus the validation script will refuse to apply otherwise. Cost ≈ £30/mo (NAT hours + Standard public IP) plus £0.045/GB processed.
- `backend_ingress_ip_restrictions_enabled` + `backend_ingress_allowed_cidrs` on the backend Container App ingress (Allow-only CIDR list).
- `lifecycle.ignore_changes` on `azurerm_container_app_environment.env` for `infrastructure_resource_group_name` and `workload_profile` so Azure-populated drift does not destroy-recreate the environment on every apply.
- Terraform checks enforce SQL private endpoint + non-empty CIDR list when restrictions are enabled, plus the NAT requirement and the /23 minimum on the Container Apps subnet.

**Deploy steps:** [`docs/phase1-perimeter-deploy-checklist.md`](../docs/phase1-perimeter-deploy-checklist.md)  
**Operator runbook:** [`docs/phase1-perimeter-runbook.md`](../docs/phase1-perimeter-runbook.md)  
**Pre-prod validation:** scratch RG dry-run via `target_environment = phase1-scratch` (see [`docs/phase1-perimeter-runbook.md`](../docs/phase1-perimeter-runbook.md)).  
**Generate CIDRs:** `scripts/extract-front-door-backend-cidrs.ps1` from weekly Azure Service Tags (`AzureFrontDoor.Backend`).

When SQL PE + NAT Gateway are enabled, the `nat_gateway_egress_ip` Terraform output exposes the static public IP for downstream allow-lists (SMTP relay, third-party APIs).

Stay on `front_door_sku_name = "Standard_AzureFrontDoor"` unless budget allows Premium (~$330/mo base vs ~$35/mo).

## Notes

- `terraform.tfvars`, `terraform.prod.tfvars`, and `terraform.dev.tfvars` are **gitignored**; keep real values in local files or CI secrets (`TERRAFORM_PROD_TFVARS`). Commit only the `*.tfvars.example` templates.
- `.terraform` and local state files are intentionally ignored.
