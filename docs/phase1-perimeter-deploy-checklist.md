# Phase 1 perimeter deploy checklist (Standard Front Door — no Premium)

> **Phase 1 v2 (2026-05-27):** This checklist incorporates the NAT Gateway and
> `lifecycle.ignore_changes` fixes added after the v1 failure on 2026-05-26.
> The operator-focused runbook is in [`docs/phase1-perimeter-runbook.md`](phase1-perimeter-runbook.md).

Code for Phase 1 is in the repo, including:

- SQL private endpoint (`enable_sql_private_endpoint = true`)
- NAT Gateway for VNet-integrated Container App egress (`enable_nat_gateway_egress = true`, default)
- `lifecycle.ignore_changes` on the Container Apps Environment for Azure-populated drift
- Backend ingress IP restrictions (`backend_ingress_ip_restrictions_enabled` + `backend_ingress_allowed_cidrs`)

**Do not apply to production until** you complete this checklist, including the **scratch-RG pre-flight (Step 2)**, in a maintenance window (~25 minutes; one Container Apps Environment replacement on first cutover).

## What Phase 1 v2 changes

| Setting | Target value |
|---------|----------------|
| `enable_sql_private_endpoint` | `true` |
| `enable_nat_gateway_egress` | `true` (default) |
| `nat_gateway_idle_timeout_minutes` | `10` (default; 4–120) |
| `sql_public_network_access_enabled` | `false` |
| `sql_allow_azure_services_firewall_rule` | `false` |
| `sql_allowed_ip_ranges` | `[]` |
| `private_endpoint_subnet_prefix` | `10.40.2.0/24` (default; must not overlap CA subnet) |
| `container_apps_subnet_prefix` | `10.40.0.0/23` (default; /23 required by Container Apps) |
| `front_door_sku_name` | `Standard_AzureFrontDoor` (unchanged cost tier) |
| `backend_ingress_ip_restrictions_enabled` | `true` (after CIDR list populated) |
| `backend_ingress_allowed_cidrs` | From `AzureFrontDoor.Backend` service tag |

**Not included:** Premium Front Door / managed WAF (~$330/mo tier), ACR / Storage / Key Vault private endpoints (Phase 1 v3).

**Cost delta:** ≈ £30/month (NAT Gateway base hours + Standard public IP) plus £0.045/GB processed. Within the existing £85/mo budget.

## Step 1 — Pre-prod readiness

1. Confirm latest `main` includes the Phase 1 v2 PR (`feat/phase1-perimeter-v2`).
2. Confirm CI is green on `main` (Terraform validate, lint, CodeQL).
3. Make sure the GitHub secrets in [`Infrastructure/README.md`](../Infrastructure/README.md#cicd-secrets-required) are populated, including the scratch-only secrets:
   - `TF_STATE_KEY_SCRATCH` (e.g. `crimeportal-phase1-scratch.tfstate`)
   - `TERRAFORM_PHASE1_SCRATCH_TFVARS` (see Step 2 below for body shape)
4. Download the weekly [Azure Service Tags JSON](https://www.microsoft.com/en-us/download/details.aspx?id=56519) and generate the Front Door backend CIDRs you will use for `backend_ingress_allowed_cidrs`:

   ```powershell
   .\scripts\extract-front-door-backend-cidrs.ps1 -ServiceTagsPath "C:\Downloads\ServiceTags_Public.json" -Region uksouth
   ```

## Step 2 — Scratch-RG pre-flight (REQUIRED before any prod cutover)

Phase 1 v2 mandates a dry-run in a parallel resource group before touching production. This catches surprises that only show up on a real Azure apply (NAT Gateway provisioning, CAE recreation, image pull behavior). The full procedure lives in the runbook; high-level steps below.

1. Prepare a scratch tfvars body (`TERRAFORM_PHASE1_SCRATCH_TFVARS`) that mirrors the planned prod tfvars but:
   - Disables unified Front Door + custom domain (no DNS to wire up).
   - Drops budget to a low cap (e.g. `monthly_budget_amount = 20`).
   - Omits `insightface_base_url` so Terraform derives it from the scratch CAE.
   - Keeps Phase 1 toggles (`enable_sql_private_endpoint = true`, `enable_nat_gateway_egress = true`).
2. GitHub Actions → **Deploy Full Stack** → `workflow_dispatch` → set `target_environment = phase1-scratch`. Confirm:
   - The bootstrap "Guard scratch target requires scratch-only secrets" step passes.
   - Bootstrap apply creates `crimeportal-phase1-scratch-rg`, ACR, VNet (with NAT Gateway), CAE.
   - Backend + AI container apps provision successfully on the VNet-integrated env.
   - "Verify scratch Container App ingress" step returns HTTP 200 on `/api/health`.
3. **Re-run the same scratch workflow** with no changes to validate `lifecycle.ignore_changes`. Expected: Terraform plan reports `No changes. Your infrastructure matches the configuration.` and the apply completes in under a minute. If the CAE shows as in-place / destroy-recreate on the second apply, the lifecycle block needs revisiting before going to prod.
4. Optional: hit the scratch backend's `*.azurecontainerapps.io` FQDN directly to sanity-check it responds.
5. **Tear the scratch RG down** to control cost:

   ```bash
   az group delete --name crimeportal-phase1-scratch-rg --yes --no-wait
   ```

   The state blob in `TF_STATE_KEY_SCRATCH` survives the RG deletion. Either keep it (next dry-run reuses it after re-applying) or delete it explicitly via `az storage blob delete`.

## Step 3 — Production apply (maintenance window)

1. Announce maintenance window to users (~25 min expected; allow 45 min buffer).
2. Update GitHub secret `TERRAFORM_PROD_TFVARS` to re-enable Phase 1 (keep AI sizing at `1.0` CPU / `2Gi`, `enable_nat_gateway_egress = true`).
3. Optional local dry-run:

   ```bash
   cd Infrastructure
   terraform init
   terraform validate
   terraform plan -var-file=terraform.prod.tfvars
   ```

4. GitHub Actions → **Deploy Full Stack** → `workflow_dispatch` → leave `target_environment = prod` (the default).
5. Watch for the **one** CAE replacement (because Azure state still carries drift from v1 attempts). After this apply, `lifecycle.ignore_changes` suppresses future drift, so subsequent applies will be in-place.
6. Wait for Terraform apply + frontend deploy + **Verify production edge**.
7. Confirm:
   - `curl -fsS https://www.dibangops.com/api/health` contains `"healthy"`.
   - Login at `https://www.dibangops.com` succeeds.
   - Direct Container App URL is blocked or restricted: `*.azurecontainerapps.io` should not serve API traffic except from allowed CIDRs (when `backend_ingress_ip_restrictions_enabled = true`).
   - `terraform output nat_gateway_egress_ip` returns a static IPv4; add to external service allow-lists (SMTP relay etc.) if needed.

## Rollback (Path A)

If Step 3 fails or production becomes unhealthy after cutover, roll back via `TERRAFORM_PROD_TFVARS` and re-trigger `Deploy Full Stack`. The exact rollback diff (Path A) is:

```diff
- enable_sql_private_endpoint            = true
- sql_public_network_access_enabled      = false
- sql_allow_azure_services_firewall_rule = false
- sql_allowed_ip_ranges                  = []
+ enable_sql_private_endpoint            = false
+ sql_public_network_access_enabled      = true
+ sql_allow_azure_services_firewall_rule = true
+ sql_allowed_ip_ranges = [
+   { name = "home", start_ip = "<your home IP>", end_ip = "<your home IP>" }
+ ]
```

Keep `enable_nat_gateway_egress` as-is — the NAT Gateway block is gated on `enable_sql_private_endpoint`, so it disappears automatically when Phase 1 is disabled.

The validation script emits a `::warning::` (not an error) when `enable_sql_private_endpoint != true` so an informed rollback can land without bypassing CI guardrails. Other internal-consistency checks (SQL public access vs PE, NAT vs PE, ingress restriction CIDRs) remain hard fails.

If SQL private endpoint blocks admin access during the rollback transition, use the Azure Portal Query Editor or a temporary public-access exception (apply via `az sql server firewall-rule create`) as a break-glass.

## After production apply

1. Update [`docs/pentest-evidence-pack.md`](pentest-evidence-pack.md) production snapshot table to reflect private SQL, NAT Gateway egress, and ingress restrictions.
2. Add the `nat_gateway_egress_ip` to your SMTP / external API allow-lists.
3. Confirm Application Insights is receiving telemetry from the new container app revisions.

## Reference

- [`docs/phase1-perimeter-runbook.md`](phase1-perimeter-runbook.md) — operator runbook with healthy-state outputs, long-running step durations, and failure-mode decision tree.
- [`Infrastructure/README.md`](../Infrastructure/README.md#phase-1-perimeter-standard-fd-no-premium-waf) — Terraform variable reference.
- [`scripts/validate-terraform-prod-tfvars.sh`](../scripts/validate-terraform-prod-tfvars.sh) — pre-apply guardrails (Phase 1 perimeter checks always run; production-edge checks gated by RG existence and `STRICT_PROD_EDGE`).
