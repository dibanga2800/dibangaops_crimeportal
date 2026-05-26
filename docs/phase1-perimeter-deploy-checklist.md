# Phase 1 perimeter deploy checklist (Standard Front Door — no Premium)

Code for Phase 1 is in the repo, including **SQL private endpoint** (`enable_sql_private_endpoint = true` in [`terraform.prod.tfvars.example`](../Infrastructure/terraform.prod.tfvars.example)). CI validation **requires** SQL PE when the production resource group exists.

**Do not apply until** you complete this checklist in a maintenance window (30–60 minutes possible downtime; Container Apps environment may recreate).

## What Phase 1 changes

| Setting | Target value |
|---------|----------------|
| `enable_sql_private_endpoint` | `true` |
| `sql_public_network_access_enabled` | `false` |
| `sql_allow_azure_services_firewall_rule` | `false` |
| `sql_allowed_ip_ranges` | `[]` |
| `front_door_sku_name` | `Standard_AzureFrontDoor` (unchanged cost tier) |
| `backend_ingress_ip_restrictions_enabled` | `true` (after CIDR list populated) |
| `backend_ingress_allowed_cidrs` | From `AzureFrontDoor.Backend` service tag |

**Not included:** Premium Front Door / managed WAF (~$330/mo tier).

## Before apply

1. Announce maintenance window to users.
2. Download weekly [Azure Service Tags JSON](https://www.microsoft.com/en-us/download/details.aspx?id=56519).
3. Generate CIDR HCL (adjust `-Region` if your SQL/CA region differs):

   ```powershell
   .\scripts\extract-front-door-backend-cidrs.ps1 -ServiceTagsPath "C:\Downloads\ServiceTags_Public.json" -Region uksouth
   ```

4. Update GitHub secret **`TERRAFORM_PROD_TFVARS`** (full file) — merge hardened flags from [`Infrastructure/terraform.prod.tfvars.example`](../Infrastructure/terraform.prod.tfvars.example) plus the generated `backend_ingress_allowed_cidrs` block.
5. Keep `enable_unified_front_door = true` and `enable_unified_front_door_custom_domain = true` (CI guardrails require both when `crimeportal-rg` exists).
6. Optional local dry-run:

   ```bash
   cd Infrastructure
   terraform init
   terraform validate
   terraform plan -var-file=terraform.prod.tfvars
   ```

## Apply (when ready)

1. GitHub Actions → **Deploy Full Stack** → `workflow_dispatch`.
2. Wait for Terraform apply + frontend deploy + **Verify production edge**.
3. Confirm:
   - `curl -fsS https://www.dibangops.com/api/health` contains `"healthy"`
   - Login at `https://www.dibangops.com`
4. Confirm direct Container App URL is **blocked** or restricted (ingress allowlist): `*.azurecontainerapps.io` should not serve API traffic except from allowed CIDRs.

## Rollback

1. Revert `TERRAFORM_PROD_TFVARS` to previous snapshot (SQL public + no ingress restrictions).
2. Re-run **Deploy Full Stack** or targeted Terraform apply.
3. If SQL private endpoint blocks admin access, use Azure Portal Query Editor or temporary break-glass PE.

## After apply

Update [`docs/pentest-evidence-pack.md`](pentest-evidence-pack.md) production snapshot table to reflect private SQL and ingress restrictions.
