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
budget_start_date     = "2026-01-01T00:00:00Z"
budget_alert_emails   = ["crimeportalai@gmail.com"]

# Set via TF_VAR_budget_alert_emails in CI/CLI.
# Example:
# export TF_VAR_budget_alert_emails='["team@yourcompany.com","finance@yourcompany.com"]'

# SQL admin password you manage (never commit real values). GitHub Actions: repository secret TF_VAR_SQL_ADMIN_PASSWORD
# Local CLI: set TF_VAR_sql_admin_password before terraform apply/plan
