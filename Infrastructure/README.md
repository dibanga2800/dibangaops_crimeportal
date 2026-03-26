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
terraform plan \
  -var-file="terraform.dev.tfvars" \
  -var="backend_image=mcr.microsoft.com/azuredocs/containerapps-helloworld:latest" \
  -var="ai_image=mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
```

```bash
terraform apply \
  -var-file="terraform.prod.tfvars" \
  -var="backend_image=<acr-login-server>/crimeportal-backend:<tag>" \
  -var="ai_image=<acr-login-server>/crimeportal-ai:<tag>"
```

## Environment profiles

- `terraform.dev.tfvars`: lower-cost non-production tuning
- `terraform.prod.tfvars`: production reliability tuning

Use `TF_VAR_budget_alert_emails` for alert recipients, for example:

```bash
export TF_VAR_budget_alert_emails='["team@yourcompany.com"]'
```

## CI/CD secrets required

For GitHub Actions:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
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

## Notes

- `terraform.tfvars` is intentionally gitignored; use CI vars/secrets instead.
- `.terraform` and local state files are intentionally ignored.
