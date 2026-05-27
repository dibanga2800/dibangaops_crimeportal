# Blue/green cutover runbook (SQL private endpoint)

Build the **green** stack in `crimeportal-prod-v2-rg` while **legacy** `crimeportal-rg` keeps serving `www`. Cut over DNS in a short window; keep legacy alive 7–14 days for rollback.

**Terraform:** one codebase, two profiles — see `Infrastructure/terraform.prod.public.tfvars.example` (Path A) and `Infrastructure/terraform.prod.private.tfvars.example` (Path B).

| Profile | RG | State secret | Tfvars secret | Workflow target |
|---------|-----|--------------|---------------|-----------------|
| Legacy | `crimeportal-rg` | `TF_STATE_KEY` | `TERRAFORM_PROD_TFVARS` | `prod` |
| Green | `crimeportal-prod-v2-rg` | `TF_STATE_KEY_V2` | `TERRAFORM_PROD_V2_TFVARS` | `prod-v2` |

---

## Phase 0 — Prerequisites

1. Merge Phase 1 v2 + blue/green workflow to `main`.
2. Create GitHub secrets (do **not** overwrite legacy until cutover):
   - `TF_STATE_KEY_V2` = `crimeportal-prod-v2.tfstate`
   - `TERRAFORM_PROD_V2_TFVARS` — body from `terraform.prod.private.tfvars.example` with real values
   - `AZURE_STATIC_WEB_APPS_API_TOKEN_V2` — deployment token from the **green** Static Web App (after first apply)
3. Optional rehearsal: `target_environment = phase1-scratch` (throwaway RG; not a migration source).

---

## Phase 1 — Build green (zero www impact)

1. **Deploy Full Stack** → `target_environment = prod-v2`.
2. Expect greenfield create (~30–45 min): VNet, NAT, SQL PE, new SQL + empty `COOP`, KV, ACR, VNet CAE, apps, SWA, Front Door.
3. CI smoke tests (no `www` verify):
   - `terraform output backend_container_app_url` → `/api/health` = 200
   - `terraform output unified_front_door_endpoint_host` → `https://<host>/api/health` = 200
4. Legacy `www` unchanged.

Record rollback target **before** cutover:

```bash
./scripts/export-legacy-front-door-host.sh
```

---

## Phase 2 — Migrate data and config (still no www cutover)

### SQL `COOP` (BACPAC)

**Legacy export** (adjust names from your subscription):

```bash
LEGACY_RG=crimeportal-rg
LEGACY_SQL=crimeportalsql0gf8g
STORAGE=crimeportalstorageXXXX   # staging account in legacy RG
CONTAINER=bacpac-migration

az sql db export \
  --resource-group "${LEGACY_RG}" \
  --server "${LEGACY_SQL}" \
  --name COOP \
  --admin-user "<sql-admin>" \
  --admin-password "<password>" \
  --storage-key-type StorageAccessKey \
  --storage-key "$(az storage account keys list -g "${LEGACY_RG}" -n "${STORAGE}" --query '[0].value' -o tsv)" \
  --storage-uri "https://${STORAGE}.blob.core.windows.net/${CONTAINER}/coop-$(date +%Y%m%d).bacpac"
```

**Green import** (after green SQL server exists from Terraform):

```bash
GREEN_RG=crimeportal-prod-v2-rg
GREEN_SQL=<green-sql-server-name>   # from Azure Portal or terraform state

az sql db import \
  --resource-group "${GREEN_RG}" \
  --server "${GREEN_SQL}" \
  --name COOP \
  --admin-user "<sql-admin>" \
  --admin-password "<password-from-TF_VAR>" \
  --storage-key-type StorageAccessKey \
  --storage-key "<key>" \
  --storage-uri "https://${STORAGE}.blob.core.windows.net/${CONTAINER}/coop-YYYYMMDD.bacpac"
```

Plan 30–60 min for ~5 GB. For final sync at cutover, stop legacy writes briefly and re-export/import or use a delta strategy you accept.

### Blob storage (`images`, `evidence`)

```bash
./scripts/azcopy-legacy-to-green.sh
```

Or manually:

```bash
azcopy copy \
  'https://<legacy-storage>.blob.core.windows.net/images/*' \
  'https://<green-storage>.blob.core.windows.net/images/*' \
  --recursive

azcopy copy \
  'https://<legacy-storage>.blob.core.windows.net/evidence/*' \
  'https://<green-storage>.blob.core.windows.net/evidence/*' \
  --recursive
```

### Key Vault

After SQL import:

1. Copy non-connection secrets from legacy KV to green KV (Portal or `az keyvault secret` show/set).
2. Regenerate `sql-connection-string` in green (Terraform apply with correct `TF_VAR_sql_admin_password`, or set manually to green SQL FQDN).
3. JWT secrets: copy for seamless sessions, or regenerate (forces re-login).

### Container images

- Push same tags to green ACR from CI (default), **or**
- Temporarily grant green managed identity `AcrPull` on legacy ACR.

### Frontend

**Deploy Full Stack** with `prod-v2` deploys SWA when `AZURE_STATIC_WEB_APPS_API_TOKEN_V2` is set. `VITE_API_BASE_URL=/api` uses unified Front Door on the green `*.azurefd.net` host for UAT.

---

## Phase 3 — Cutover window (plan 30–90 min, buffer 2 h)

**48 h before:** Lower `www` DNS TTL to 300s (Cloudflare: DNS only).

**Ordered steps:**

1. Maintenance banner on legacy (optional).
2. **Final data sync:** brief legacy freeze → BACPAC delta or full re-import + AzCopy delta.
3. In `TERRAFORM_PROD_V2_TFVARS`: `enable_unified_front_door_custom_domain = true`.
4. **Deploy Full Stack** → `prod-v2` (TXT validation for `www` if not pre-staged).
5. **DNS:** CNAME `www` from legacy Front Door host → green `terraform output unified_front_door_endpoint_host`.
6. Add `_dnsauth.www` TXT if Azure prompts (DNS only).
7. Verify (up to ~30 min propagation + managed cert):
   - `curl -fsS https://www.example.com/api/health`
   - Login and critical flows on iOS Safari
8. Apex `example.com` if used.

**Do not** delete `crimeportal-rg` in this window.

---

## Phase 4 — Rollback

1. Revert `www` CNAME to legacy host (from `export-legacy-front-door-host.sh` output or saved notes).
2. Legacy Path A tfvars unchanged → **Deploy Full Stack** → `prod` only if legacy was modified.
3. Rollback time = DNS TTL (minutes to ~1 h), not hours of Terraform surgery.

---

## Phase 5 — Decommission legacy (after 7–14 day bake)

1. Final backup: legacy SQL BACPAC + storage containers.
2. `az group delete --name crimeportal-rg --yes --no-wait`
3. Archive legacy state blob (`TF_STATE_KEY`); do not delete immediately.
4. When confident, retire `TERRAFORM_PROD_TFVARS` / Path A secrets or repoint defaults to v2-only.

---

## Ingress allow-list (follow-up)

After green is stable on private SQL, schedule a separate window:

1. Run `scripts/extract-front-door-backend-cidrs.ps1`.
2. Set `backend_ingress_ip_restrictions_enabled = true` and populate `backend_ingress_allowed_cidrs` in green tfvars.
3. Apply `prod-v2` (~10 min if CAE is unchanged).

---

## Related docs

- [phase1-perimeter-runbook.md](./phase1-perimeter-runbook.md) — scratch dry-run
- [production-edge-runbook.md](./production-edge-runbook.md) — unified Front Door operations
- [Infrastructure/README.md](../Infrastructure/README.md) — secrets matrix
