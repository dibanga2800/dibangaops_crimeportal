# Non-production cost-conscious profile
enable_application_insights  = false
log_analytics_retention_days = 7

backend_container_cpu    = 0.5
backend_container_memory = "1Gi"
backend_min_replicas     = 0
backend_max_replicas     = 1

ai_container_cpu    = 0.5
ai_container_memory = "1Gi"
ai_min_replicas     = 0
ai_max_replicas     = 1

sql_max_size_gb = 2

monthly_budget_amount = 25
enable_budget_alert   = true
budget_start_date     = "2026-01-01T00:00:00Z"
budget_alert_emails   = ["crimeportalai@gmail.com"]

# Set via TF_VAR_budget_alert_emails in CI/CLI.
# Example:
# export TF_VAR_budget_alert_emails='["team@yourcompany.com"]'
