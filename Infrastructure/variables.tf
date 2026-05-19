variable "resource_group" {
  description = "Azure Resource Group name"
  type        = string
  default     = "crimeportal-rg"
}

variable "location" {
  description = "Primary Azure region"
  type        = string
  default     = "UK South"
}

variable "static_web_app_location" {
  description = "Region for Static Web App (must support Microsoft.Web/staticSites)"
  type        = string
  default     = "westeurope"
}

variable "frontend_name" {
  description = "Static Web App name"
  type        = string
  default     = "crimeportal-frontend"
}

variable "backend_name" {
  description = ".NET API Container App name"
  type        = string
  default     = "crimeportal-backend"
}

variable "ai_name" {
  description = "AI Container App name"
  type        = string
  default     = "crimeportal-ai"
}

variable "sql_server_name_prefix" {
  description = "Prefix for Azure SQL Server name (unique suffix added automatically)"
  type        = string
  default     = "crimeportalsql"
}

variable "sql_db_name" {
  description = "Azure SQL database name"
  type        = string
  default     = "COOP"
}

variable "blob_storage_name_prefix" {
  description = "Prefix for storage account name (lowercase and numbers only; unique suffix added automatically)"
  type        = string
  default     = "crimeportalstorage"
  validation {
    condition     = can(regex("^[a-z0-9]+$", var.blob_storage_name_prefix))
    error_message = "blob_storage_name_prefix must contain only lowercase letters and numbers."
  }
}

variable "keyvault_name_prefix" {
  description = "Prefix for Key Vault name (unique suffix added automatically)"
  type        = string
  default     = "crimeportal-kv"
}

variable "key_vault_soft_delete_retention_days" {
  description = "Key Vault soft-delete retention in days (7–90). Used when the vault is first created; Azure does not allow changing this on an existing vault, so Terraform ignores in-place updates (see lifecycle on azurerm_key_vault.kv)."
  type        = number
  default     = 90
  validation {
    condition     = var.key_vault_soft_delete_retention_days >= 7 && var.key_vault_soft_delete_retention_days <= 90
    error_message = "key_vault_soft_delete_retention_days must be between 7 and 90."
  }
}

variable "key_vault_purge_protection_enabled" {
  description = "Enable Key Vault purge protection (recommended for production; irreversible once enabled)"
  type        = bool
  default     = true
}

variable "terraform_kv_admin_principal_object_id" {
  description = "Object ID to assign Key Vault Administrator for Terraform runs (use CI service principal object ID)"
  type        = string
  default     = "474a9563-e96f-46d8-af37-21478bad7f5b"
}

variable "backend_image" {
  description = "Container image for .NET API"
  type        = string
}

variable "ai_image" {
  description = "Container image for AI service"
  type        = string
}

variable "backend_target_port" {
  description = "Backend container ingress target port. Set null to auto-detect based on image."
  type        = number
  default     = null
}

variable "ai_target_port" {
  description = "AI container ingress target port. Set null to auto-detect based on image."
  type        = number
  default     = null
}

variable "backend_allow_insecure_connections" {
  description = "Allow HTTP for backend ingress (keep false in production)"
  type        = bool
  default     = false
}

variable "ai_allow_insecure_connections" {
  description = "Allow HTTP for AI ingress (internal-only; keep false for consistency)"
  type        = bool
  default     = false
}

variable "sql_admin_username" {
  description = "Azure SQL administrator username"
  type        = string
  default     = "sqladminuser"
}

variable "sql_admin_password" {
  description = <<-EOT
    Azure SQL server administrator password. Set this (or TF_VAR_sql_admin_password) to a strong value you keep
    outside the repo (Key Vault will still store a copy for apps). Leave null/empty to auto-generate once via random_password.
    Changing this after deploy updates the SQL admin login and Key Vault secrets on the next apply.
  EOT
  type        = string
  default     = null
  sensitive   = true
  nullable    = true

  validation {
    condition     = var.sql_admin_password == null || var.sql_admin_password == "" || length(var.sql_admin_password) >= 8
    error_message = "sql_admin_password must be at least 8 characters (Azure SQL minimum) when set."
  }
}

variable "jwt_signing_key" {
  description = "Optional JWT HMAC signing key (long random string). Leave null to auto-generate; value is stored in Key Vault and injected into the backend Container App."
  type        = string
  default     = null
  nullable    = true
  sensitive   = true
}

variable "jwt_issuer" {
  description = "JWT issuer claim (must match backend configuration)"
  type        = string
  default     = "AIPBackend"
}

variable "jwt_audience" {
  description = "JWT audience claim (must match backend configuration)"
  type        = string
  default     = "AIPFrontend"
}

variable "jwt_access_token_expiration_minutes" {
  description = "JWT access token lifetime in minutes"
  type        = number
  default     = 60
}

variable "jwt_refresh_token_expiration_days" {
  description = "JWT refresh token lifetime in days"
  type        = number
  default     = 7
}

variable "container_app_environment_name" {
  description = "Container Apps environment name"
  type        = string
  default     = "crimeportal-env"
}

variable "frontend_url" {
  description = "Allowed frontend origin(s) for backend CORS (comma-separated for apex + www, e.g. https://www.example.com,https://example.com)"
  type        = string
  default     = null
  nullable    = true
}

variable "frontend_custom_domain" {
  description = "Apex custom hostname for Azure Static Web App (e.g. dibangops.com)"
  type        = string
  default     = null
  nullable    = true
}

variable "frontend_www_custom_domain" {
  description = "WWW custom hostname for Azure Static Web App (e.g. www.dibangops.com)"
  type        = string
  default     = null
  nullable    = true
}

variable "smtp_host" {
  description = "SMTP server host"
  type        = string
  default     = null
  nullable    = true
}

variable "smtp_port" {
  description = "SMTP server port"
  type        = number
  default     = 587
}

variable "smtp_enable_ssl" {
  description = "Whether SMTP should use SSL/TLS"
  type        = bool
  default     = true
}

variable "smtp_username" {
  description = "SMTP username"
  type        = string
  default     = null
  nullable    = true
}

variable "smtp_password" {
  description = "SMTP password or app password"
  type        = string
  default     = null
  nullable    = true
  sensitive   = true
}

variable "smtp_from_email" {
  description = "Default From email for backend notifications"
  type        = string
  default     = null
  nullable    = true
}

variable "smtp_from_name" {
  description = "Default From display name for backend notifications"
  type        = string
  default     = "Crime Portal Notifications"
}

variable "azure_openai_endpoint" {
  description = "Azure OpenAI endpoint URL"
  type        = string
  default     = null
  nullable    = true
}

variable "azure_openai_api_key" {
  description = "Azure OpenAI API key"
  type        = string
  default     = null
  nullable    = true
  sensitive   = true
}

variable "azure_openai_deployment" {
  description = "Azure OpenAI deployment name"
  type        = string
  default     = null
  nullable    = true
}

variable "azure_openai_enabled" {
  description = "Whether Azure OpenAI-backed classification is enabled"
  type        = bool
  default     = false
}

variable "insightface_enabled" {
  description = "Whether the backend should use InsightFace for offender recognition"
  type        = bool
  default     = true
}

variable "insightface_base_url" {
  description = "InsightFace service base URL; leave null to use the internal Container App URL"
  type        = string
  default     = null
  nullable    = true
}

variable "insightface_timeout_seconds" {
  description = "InsightFace request timeout in seconds"
  type        = number
  default     = 30
}

variable "insightface_min_similarity" {
  description = "Minimum InsightFace similarity threshold"
  type        = number
  default     = 0.85
}

variable "insightface_max_search_results" {
  description = "Maximum InsightFace search matches to return"
  type        = number
  default     = 3
}

variable "enable_application_insights" {
  description = "Whether to deploy Application Insights"
  type        = bool
  default     = true
}

variable "acr_name_prefix" {
  description = "Prefix for Azure Container Registry name (lowercase and numbers only; unique suffix added automatically)"
  type        = string
  default     = "crimeportalacr"
  validation {
    condition     = can(regex("^[a-z0-9]+$", var.acr_name_prefix))
    error_message = "acr_name_prefix must contain only lowercase letters and numbers."
  }
}

variable "log_analytics_retention_days" {
  description = "Log Analytics data retention in days"
  type        = number
  default     = 30
  validation {
    condition     = var.log_analytics_retention_days >= 7 && var.log_analytics_retention_days <= 730
    error_message = "log_analytics_retention_days must be between 7 and 730."
  }
}

variable "backend_container_cpu" {
  description = "CPU cores for backend container app"
  type        = number
  default     = 0.5
}

variable "backend_container_memory" {
  description = "Memory for backend container app"
  type        = string
  default     = "1Gi"
}

variable "backend_min_replicas" {
  description = "Minimum replicas for backend container app"
  type        = number
  default     = 1
}

variable "backend_max_replicas" {
  description = "Maximum replicas for backend container app"
  type        = number
  default     = 3
}

variable "ai_container_cpu" {
  description = "CPU cores for AI container app (InsightFace buffalo_l + ONNX needs ~1 CPU for stable activation)"
  type        = number
  default     = 1.0
}

variable "ai_container_memory" {
  description = "Memory for AI container app (buffalo_l models exceed 1Gi at load; use 2Gi+)"
  type        = string
  default     = "2Gi"
}

variable "ai_min_replicas" {
  description = "Minimum replicas for AI container app"
  type        = number
  default     = 0
}

variable "ai_max_replicas" {
  description = "Maximum replicas for AI container app"
  type        = number
  default     = 2
}

variable "sql_max_size_gb" {
  description = "Maximum SQL database size in GB"
  type        = number
  default     = 5
}

variable "sql_allow_azure_services_firewall_rule" {
  description = "Whether to keep the 0.0.0.0 SQL firewall rule that allows Azure service connectivity"
  type        = bool
  default     = true
}

variable "sql_public_network_access_enabled" {
  description = "Allow public internet access to Azure SQL (disable when using private endpoint only)"
  type        = bool
  default     = true
}

variable "enable_sql_private_endpoint" {
  description = "Provision VNet, private endpoint, and disable SQL public access. Requires Container Apps VNet integration (may recreate the environment)."
  type        = bool
  default     = false
}

variable "vnet_address_space" {
  description = "Address space for the optional SQL private networking VNet"
  type        = string
  default     = "10.40.0.0/16"
}

variable "private_endpoint_subnet_prefix" {
  description = "Subnet for SQL private endpoint"
  type        = string
  default     = "10.40.1.0/24"
}

variable "container_apps_subnet_prefix" {
  description = "Delegated subnet for Container Apps environment (SQL private networking)"
  type        = string
  default     = "10.40.0.0/23"
}

variable "enable_unified_front_door" {
  description = "Single Front Door for SPA + API on one hostname (www): /api/* → backend, /* → Static Web App. Enables first-party auth cookies."
  type        = bool
  default     = false
}

variable "unified_front_door_hostname" {
  description = "Public hostname for unified Front Door (e.g. www.dibangops.com). Defaults to frontend_www_custom_domain when null."
  type        = string
  default     = null
  nullable    = true
}

variable "enable_unified_front_door_custom_domain" {
  description = "Attach TLS custom domain to unified Front Door routes. Enable only after www DNS CNAME targets unified_front_door_endpoint_host; a pending custom domain blocks edge deployment (404 on all hostnames)."
  type        = bool
  default     = false
}

variable "enable_api_front_door" {
  description = "Place Azure Front Door + WAF in front of the public API Container App"
  type        = bool
  default     = false
}

variable "api_custom_domain" {
  description = "Custom API hostname for Front Door (e.g. api.dibangops.com). Point DNS CNAME to the Front Door endpoint."
  type        = string
  default     = null
  nullable    = true
}

variable "front_door_sku_name" {
  description = "Azure Front Door profile SKU"
  type        = string
  default     = "Standard_AzureFrontDoor"
}

variable "front_door_waf_sku_name" {
  description = "Azure Front Door WAF policy SKU"
  type        = string
  default     = "Standard_AzureFrontDoor"
}

variable "front_door_waf_mode" {
  description = "WAF mode: Detection or Prevention"
  type        = string
  default     = "Prevention"
}

variable "auth_cookie_domain" {
  description = "Shared cookie domain for HttpOnly JWT cookies (e.g. .dibangops.com when SPA and API share the registrable domain)"
  type        = string
  default     = null
  nullable    = true
}

variable "auth_cookie_same_site" {
  description = "SameSite attribute for auth cookies (Lax recommended for api. + www. subdomains)"
  type        = string
  default     = "Lax"
}

variable "auth_cookie_secure" {
  description = "Secure flag for auth cookies (must be true in production)"
  type        = bool
  default     = true
}

variable "sql_allowed_ip_ranges" {
  description = "Additional SQL firewall rules for explicit ingress allow-list entries"
  type = list(object({
    name     = string
    start_ip = string
    end_ip   = string
  }))
  default = []
}

variable "storage_blob_delete_retention_days" {
  description = "Retention days for soft-deleted blobs"
  type        = number
  default     = 30
  validation {
    condition     = var.storage_blob_delete_retention_days >= 1 && var.storage_blob_delete_retention_days <= 365
    error_message = "storage_blob_delete_retention_days must be between 1 and 365."
  }
}

variable "storage_container_delete_retention_days" {
  description = "Retention days for soft-deleted containers"
  type        = number
  default     = 7
  validation {
    condition     = var.storage_container_delete_retention_days >= 1 && var.storage_container_delete_retention_days <= 365
    error_message = "storage_container_delete_retention_days must be between 1 and 365."
  }
}

variable "storage_blob_versioning_enabled" {
  description = "Enable blob versioning for recovery and forensic support"
  type        = bool
  default     = true
}

variable "storage_change_feed_enabled" {
  description = "Enable storage account blob change feed for auditing and incident review"
  type        = bool
  default     = true
}

variable "monthly_budget_amount" {
  description = "Monthly Azure budget amount in account currency"
  type        = number
  default     = 40
}

variable "budget_alert_emails" {
  description = "Email addresses to notify for cost budget thresholds"
  type        = list(string)
  default     = []
}

variable "enable_budget_alert" {
  description = "Whether to create Azure monthly budget and alerts"
  type        = bool
  default     = true
}

variable "budget_start_date" {
  description = "Budget start date in RFC3339 format"
  type        = string
  default     = "2026-01-01T00:00:00Z"
}