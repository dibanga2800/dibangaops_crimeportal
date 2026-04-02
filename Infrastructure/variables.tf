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
  description = "Allowed frontend origin for backend CORS"
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
  description = "CPU cores for AI container app"
  type        = number
  default     = 0.5
}

variable "ai_container_memory" {
  description = "Memory for AI container app"
  type        = string
  default     = "1Gi"
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