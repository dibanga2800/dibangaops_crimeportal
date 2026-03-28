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
  default     = "crimeportal-db"
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
  description = "Optional Azure SQL administrator password. Leave null to auto-generate."
  type        = string
  default     = null
  sensitive   = true
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

variable "container_app_environment_name" {
  description = "Container Apps environment name"
  type        = string
  default     = "crimeportal-env"
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