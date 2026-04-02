# Production reliability profile
enable_application_insights  = true
log_analytics_retention_days = 30

backend_container_cpu    = 0.5
backend_container_memory = "1Gi"
backend_min_replicas     = 1
backend_max_replicas     = 3

ai_container_cpu    = 0.5
ai_container_memory = "1Gi"
ai_min_replicas     = 0
ai_max_replicas     = 2

sql_max_size_gb = 5

monthly_budget_amount = 75
enable_budget_alert   = true
budget_start_date     = "2026-04-01T00:00:00Z"
budget_alert_emails   = ["crimeportalai@gmail.com"]

# Set via TF_VAR_budget_alert_emails in CI/CLI.
# Example:
# export TF_VAR_budget_alert_emails='["team@yourcompany.com","finance@yourcompany.com"]'

# SQL admin password you manage (never commit real values). GitHub Actions: repository secret TF_VAR_SQL_ADMIN_PASSWORD
# Local CLI: set TF_VAR_sql_admin_password before terraform apply/plan

# Backend runtime configuration (non-sensitive)
smtp_host       = "smtp.gmail.com"
smtp_port       = 587
smtp_enable_ssl = true
smtp_username   = "crimeportalai@gmail.com"
smtp_from_email = "crimeportalai@gmail.com"
smtp_from_name  = "Crime Portal Notifications"

azure_openai_enabled  = false
azure_openai_endpoint = "https://crime-portal-ai.openai.azure.com/"

insightface_enabled            = true
insightface_base_url           = "https://crimeportal-ai.internal.bravemushroom-fae169c7.uksouth.azurecontainerapps.io"
insightface_timeout_seconds    = 30
insightface_min_similarity     = 0.85
insightface_max_search_results = 3

# Canonical app URL (apex redirects to www via Cloudflare)
frontend_url               = "https://www.dibangops.com"
frontend_custom_domain     = "dibangops.com"
frontend_www_custom_domain = "www.dibangops.com"

jwt_access_token_expiration_minutes = 60
jwt_refresh_token_expiration_days   = 7

# Still provide these outside the repo before applying:
# - TF_VAR_smtp_password
# - TF_VAR_azure_openai_api_key
# Optional:
# - insightface_base_url
# - frontend_url
