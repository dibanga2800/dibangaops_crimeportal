# Production edge runbook (www + unified Front Door)

This document is the **source of truth** for keeping `https://www.dibangops.com` stable. Routine app deploys must not change DNS, Front Door flags, or Cloudflare proxy mode without following this runbook.

## Architecture (frozen in production)

| Layer | Value |
|--------|--------|
| Public URL | `https://www.dibangops.com` |
| DNS | Cloudflare CNAME `www` → `crimeportal-frontend-unified-ep-….z02.azurefd.net` (**DNS only**, grey cloud) |
| Domain validation | TXT `_dnsauth.www` → Azure validation token (**DNS only**) |
| Edge | Unified Azure Front Door: `/*` → Static Web App, `/api*` → Container App (rule set `unifiedrouting`) |
| SPA API base | `VITE_API_BASE_URL=/api` (same-origin cookies) |

## Do not change in production (without maintenance window)

1. **`enable_unified_front_door`** — must stay `true` in `TERRAFORM_PROD_TFVARS`.
2. **`enable_unified_front_door_custom_domain`** — must stay `true` after cutover. Setting `false` and running `terraform apply` unlinks www from the route and causes `NET::ERR_CERT_COMMON_NAME_INVALID`.
3. **Cloudflare proxy** — never enable orange cloud on `www` or `_dnsauth.www`.
4. **www CNAME target** — must remain the unified Front Door endpoint hostname from `terraform output unified_front_door_endpoint_host`.
5. **TXT `_dnsauth.www`** — required for managed certificate renewal; do not delete.
6. **Second Front Door route** — only `crimeportal-frontend-unified-spa-route` is managed. Do not recreate `crimeportal-backend-unified-api-route` (legacy).

CI enforces items 1–2 when `crimeportal-rg` already exists (`scripts/validate-terraform-prod-tfvars.sh`).

## Symptoms and causes

| Symptom | Likely cause | Fix |
|---------|----------------|-----|
| `NET::ERR_CERT_COMMON_NAME_INVALID` on www | Route not linked to custom domain, or edge cert still propagating | Confirm route `customDomains` includes `www-dibangops-com`; wait up to 60 min; re-run Deploy Full Stack or `scripts/verify-production-edge.sh` |
| 404 “We weren't able to find your Azure Front Door Service” | Custom domain stuck **Pending** (missing/wrong TXT or CNAME) | Fix DNS; wait for **Approved** before enabling custom domain in Terraform |
| Login works on desktop but not mobile | SPA calling cross-origin API (wrong `VITE_API_BASE_URL`) | Rebuild with `/api`; confirm CSP `connect-src 'self'` |
| `/api/health` returns HTML | Traffic hitting SWA instead of API | Confirm rule set `unifiedrouting` on the single `/*` route |

## Initial cutover (one-time only)

Follow `Infrastructure/README.md` → **Unified Front Door** in order:

1. `enable_unified_front_door = true`, `enable_unified_front_door_custom_domain = false` → apply → validate `*.azurefd.net`.
2. CNAME `www` to unified endpoint (DNS only).
3. `enable_unified_front_door_custom_domain = true` → apply → add TXT `_dnsauth.www` → wait **Approved** + cert deployed.
4. Deploy frontend with `VITE_API_BASE_URL=/api`.

Do not repeat phase 1 after www already points at Front Door.

## Routine deploys (safe)

| Workflow | Touches edge? |
|----------|----------------|
| **Deploy Frontend** | No — SWA static files only; post-deploy smoke test on www |
| **Deploy Backend** | No — container image only |
| **Deploy Full Stack** | Yes — Terraform may update Front Door; runs validation + smoke tests |

## Manual verification

```bash
# From repo root (Git Bash / WSL / Linux agent)
./scripts/verify-production-edge.sh www.dibangops.com

# Azure binding (requires az login)
./scripts/verify-unified-front-door-azure.sh
```

## Rollback

1. Point www CNAME back to Static Web App default hostname.
2. Set `enable_unified_front_door_custom_domain = false` only if you accept losing unified TLS until cutover is redone.
3. Redeploy frontend with previous API URL if not using `/api`.

## Monitoring (recommended)

Configure an external uptime check every 5 minutes:

- `GET https://www.dibangops.com/api/health` → body contains `"healthy"`
- Optional: TLS certificate expiry alert on `www.dibangops.com`

## Related files

- `Infrastructure/frontdoor-unified.tf` — unified profile, route, custom domain
- `Infrastructure/checks.tf` — Terraform assert blocks
- `scripts/validate-terraform-prod-tfvars.sh` — CI guardrails
- `scripts/verify-production-edge.sh` — TLS + HTTP smoke test
- `.github/actions/verify-production-edge/` — composite CI action
