# Phase 1 perimeter operator runbook

Operator-focused reference for executing the Phase 1 perimeter cutover (or its rollback) on production. Pair with [`docs/phase1-perimeter-deploy-checklist.md`](phase1-perimeter-deploy-checklist.md) — the checklist drives the sequence; this runbook tells you what "healthy" looks like at each stage and how to react to failure modes.

> **Audience:** the on-call engineer running the maintenance window.
> **Pre-reqs:** the v2 PR (`feat/phase1-perimeter-v2`) has been merged to `main`,
> the scratch-RG dry-run (Step 2 of the checklist) has run twice cleanly,
> and you have `az` + `gh` CLI authenticated.

---

## At-a-glance timing

| Phase | Step | Expected duration |
|---|---|---|
| Bootstrap | Workflow setup, OIDC login, validation, state lease break, init, ACR import | 2–3 min |
| Bootstrap | Container image build + push (backend + AI) | 3–4 min |
| Release | First-time CAE replacement (Phase 1 cutover only) | 7–9 min destroy + 3 min create |
| Release | NAT Gateway provisioning (first apply only) | 1–2 min |
| Release | Container app revisions (backend + AI) | 5–10 min each |
| Release | Front Door + SWA custom domain reconciliation | 1–3 min |
| Verify | `verify-production-edge` action | 30–60 s |
| **Total** | **Cutover (with one CAE replacement)** | **~25 min** |
| **Total** | **Subsequent applies (lifecycle.ignore_changes effective)** | **~10 min** |

---

## Pre-cutover smoke check (production current state)

Run these against production **before** triggering the cutover so you have a baseline and can compare after.

```bash
# Resource group exists, location, tags
az group show --name crimeportal-rg --query '{name:name, location:location}' -o table

# Container apps are running; provisioningState should be Succeeded
az containerapp list -g crimeportal-rg --query '[].{name:name, state:properties.provisioningState, fqdn:properties.configuration.ingress.fqdn}' -o table

# SQL public access is currently true (pre-Phase 1) and the home IP rule exists
az sql server show -g crimeportal-rg -n crimeportalsql0gf8g --query '{name:name, publicNetworkAccess:publicNetworkAccess}' -o jsonc
az sql server firewall-rule list -g crimeportal-rg -s crimeportalsql0gf8g --query '[].{name:name, start:startIpAddress, end:endIpAddress}' -o table

# Public smoke test
curl -fsS https://www.dibangops.com/api/health
```

Expected before Phase 1 cutover: `publicNetworkAccess = Enabled`, container apps `Succeeded`, `/api/health` returns `{"status":"healthy"}`.

---

## Healthy state during cutover

### After bootstrap job

```bash
gh -R dibanga2800/dibangaops_crimeportal run watch <RUN_ID>
```

Expected log lines in **Terraform Apply (Bootstrap)** job:

- `validate-terraform-prod-tfvars: resource_group=crimeportal-rg exists=true strict_prod_edge=true unified_fd=true custom_domain=true sql_pe=true nat=true`
- `Production edge tfvars validation passed.`
- The bootstrap apply step is skipped (`steps.infra-check.outputs.resource_group_exists != 'true'`) because prod RG already exists.

### During release job — Terraform Apply (Release Images)

Phase 1 v2 first-time cutover plan should report something like:

```
Plan: 6 to add, 0 to change, 2 to destroy.
```

Where the additions are NAT Gateway + public IP + two associations + the recreated CAE, and the destroys are the old CAE plus any stale resources. **If you see `2 to add, 0 to change, 0 to destroy`** the CAE may already be in the desired shape — that is fine.

Expected long pauses:

- `azurerm_container_app_environment.env: Destroying...` → up to 9 min.
- `azurerm_container_app_environment.env: Creating...` → ~3 min.
- `azurerm_container_app.backend: Creating...` and `azurerm_container_app.ai: Creating...` → 5–10 min each (image pull, container start, ingress probe).

Apply complete looks like:

```
Apply complete! Resources: N added, 0 changed, M destroyed.
```

### After release apply

```bash
# CAE is VNet-integrated
az containerapp env show -g crimeportal-rg -n crimeportal-env \
  --query '{state:properties.provisioningState, subnet:properties.vnetConfiguration.infrastructureSubnetId}' -o jsonc

# NAT Gateway exists and is attached to the CA subnet
az network nat gateway show -g crimeportal-rg -n crimeportal-rg-egress-natgw \
  --query '{state:provisioningState, sku:sku.name, idleTimeout:idleTimeoutInMinutes}' -o jsonc
az network nat gateway show -g crimeportal-rg -n crimeportal-rg-egress-natgw \
  --query 'subnets[].id' -o tsv

# SQL public access is now disabled and the firewall rules are gone
az sql server show -g crimeportal-rg -n crimeportalsql0gf8g \
  --query '{publicNetworkAccess:publicNetworkAccess}' -o jsonc
az sql server firewall-rule list -g crimeportal-rg -s crimeportalsql0gf8g -o table   # expected empty

# Container apps healthy
az containerapp list -g crimeportal-rg \
  --query '[].{name:name, state:properties.provisioningState, runningStatus:properties.runningStatus}' -o table

# Static egress IP (note this and add to external allow-lists)
terraform -chdir=Infrastructure output nat_gateway_egress_ip
```

Expected:

- `publicNetworkAccess: Disabled`
- Firewall rule list is empty (`AllowAzureServices` and `home` rules removed)
- Both container apps `provisioningState: Succeeded` and `runningStatus: Running`
- NAT Gateway `provisioningState: Succeeded`, attached to `snet-container-apps`
- `nat_gateway_egress_ip` returns a public IPv4 (this is the new SNAT source for the VNet)

### After verify-production-edge

```bash
curl -fsS https://www.dibangops.com/api/health    # contains "healthy"
```

Open `https://www.dibangops.com` in a browser, sign in. Cookies should be on `www.dibangops.com` (not on `*.azurecontainerapps.io`).

---

## Failure-mode decision tree

```mermaid
flowchart TD
    Start[Apply fails or production unhealthy]
    Start --> Q1{Failed in bootstrap job?}
    Q1 -- Yes --> B1[Validation error] --> B1Action[Fix tfvars secret, re-run]
    Q1 -- Yes --> B2[OIDC login failed] --> B2Action[Verify federated credential subject matches branch + event]
    Q1 -- No --> Q2{Failed in build-and-push job?}
    Q2 -- Yes --> P1[Docker build error] --> P1Action[Investigate Dockerfile change; revert offending commit]
    Q2 -- No --> Q3{Failed in release apply?}
    Q3 -- Yes --> R1{CAE 'Operation expired'?}
    R1 -- Yes --> RA[Bug 3: NAT Gateway missing or unhealthy]
    RA --> RAAction[Check NAT GW state with az; if missing, ensure enable_nat_gateway_egress=true and re-apply]
    R1 -- No --> R2{CIDR overlap error?}
    R2 -- Yes --> RB[Bug 1 regression: subnet prefixes overlap]
    RB --> RBAction[Use defaults 10.40.0.0/23 CA and 10.40.2.0/24 PE; verify in tfvars]
    R2 -- No --> R3{Container app 'NoActiveReplicas' or crashloop?}
    R3 -- Yes --> RC[Image pull failure or app crash]
    RC --> RCAction[Inspect container app logs via az containerapp logs show]
    Q3 -- No --> Q4{verify-production-edge failed?}
    Q4 -- Yes --> V1[/api/health did not return 200] --> V1Action[Check backend logs, SQL connectivity from container]
    Q4 -- No --> Q5[Application failure post-deploy]
    Q5 --> Q5Action[Hit /api/health, check App Insights, consider Path A rollback]
```

---

## Path A rollback procedure (production restore from a broken Phase 1 state)

If production is down or seriously degraded after cutover:

1. **Cancel** any still-running deploy workflow runs:

   ```bash
   gh run list --workflow deploy-main.yml --limit 5
   gh run cancel <RUN_ID>
   ```

2. **Update `TERRAFORM_PROD_TFVARS`** with the rollback diff. Either edit via the GitHub UI or push a verified local file:

   ```bash
   # Edit Infrastructure/terraform.prod.rollback.tfvars (gitignored) with the Path A body, then:
   gh secret set TERRAFORM_PROD_TFVARS < Infrastructure/terraform.prod.rollback.tfvars
   ```

   The Path A body sets:

   ```hcl
   enable_sql_private_endpoint            = false
   sql_public_network_access_enabled      = true
   sql_allow_azure_services_firewall_rule = true
   sql_allowed_ip_ranges = [
     { name = "home", start_ip = "<your home IP>", end_ip = "<your home IP>" }
   ]
   ```

   Leave AI sizing at `1.0` / `2Gi` (no regression). Do **not** set `enable_nat_gateway_egress = false` explicitly — it is gated on `enable_sql_private_endpoint`, so the NAT block disappears automatically with PE off.

   The validation script emits a `::warning::` (not error) when Phase 1 is disabled, so this rollback passes CI guardrails.

3. **Trigger `Deploy Full Stack`** (target_environment = prod, the default). Terraform will:

   - Set `azurerm_mssql_server.sql_server.public_network_access_enabled = true`.
   - Recreate the `AllowAzureServices` and `home` firewall rules.
   - Destroy NAT Gateway, public IP, both subnets, private DNS zone, vnet link, private endpoint, VNet.
   - Replace the Container Apps Environment one last time (`infrastructure_subnet_id` goes from set to null).
   - Recreate both container apps on the non-VNet env.

4. **Verify**:

   ```bash
   curl -fsS https://www.dibangops.com/api/health
   az containerapp list -g crimeportal-rg --query '[].{name:name, state:properties.provisioningState}' -o table
   az sql server show -g crimeportal-rg -n crimeportalsql0gf8g --query 'publicNetworkAccess' -o tsv
   ```

   Expected: `/api/health` returns 200, both container apps `Succeeded`, SQL `publicNetworkAccess: Enabled`.

Estimated duration: ~25 min.

---

## Pen-test finding closure mapping

| Finding | Before Phase 1 | After Phase 1 v2 |
|---------|----------------|-------------------|
| SQL Server publicly reachable | Open (Medium) | Closed — `publicNetworkAccess = Disabled`, no firewall rules. SQL is reachable only via private endpoint from the VNet. |
| `AllowAzureServices` SQL firewall rule | Open (Medium) | Closed — rule not provisioned (`sql_allow_azure_services_firewall_rule = false`). |
| Container App FQDN bypasses Front Door / WAF | Open (Medium) | Closed when `backend_ingress_ip_restrictions_enabled = true` is also set; only AzureFrontDoor.Backend CIDRs reach the backend ingress. |
| TLS / WAF / JWT / secrets / audit | Already closed | Unchanged. |
| ACR public network access | Out of scope | Out of scope for v2; Phase 1 v3 could add ACR private endpoint. |
| Storage public access | Out of scope | Out of scope for v2; Phase 1 v3 could add storage private endpoint. |

---

## Future-self notes

- The `lifecycle.ignore_changes` block on the Container Apps Environment masks `infrastructure_resource_group_name` and `workload_profile`. **Re-evaluate this block whenever you upgrade the `hashicorp/azurerm` provider major version** — the drift behavior is provider-specific and could change.
- NAT Gateway is a Standard SKU regional resource. It is highly available within the region but **not** zone-redundant. If you ever move to zone redundancy, add `zones = ["1", "2", "3"]` to the resource and validate cost impact.
- The `nat_gateway_egress_ip` is allocated as `Static` so it persists across CAE replacements as long as the resource itself is not destroyed. Adding more apps to the same VNet automatically shares this egress IP.
