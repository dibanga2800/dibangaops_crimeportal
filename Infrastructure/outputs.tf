output "frontend_url" {
  value = "https://${azurerm_static_web_app.frontend.default_host_name}"
}

output "backend_url" {
  value = "https://${azurerm_container_app.backend.latest_revision_fqdn}"
}

output "ai_url" {
  value = "https://${azurerm_container_app.ai.latest_revision_fqdn}"
}

output "sql_server_name" {
  value = azurerm_mssql_server.sql_server.name
}

output "sql_database_name" {
  value = azurerm_mssql_database.sql_db.name
}

output "storage_account_name" {
  value = azurerm_storage_account.storage.name
}

output "key_vault_name" {
  value = azurerm_key_vault.kv.name
}

output "acr_name" {
  value = azurerm_container_registry.acr.name
}

output "acr_login_server" {
  value = azurerm_container_registry.acr.login_server
}

output "application_insights_connection_string" {
  value     = try(azurerm_application_insights.appinsights[0].connection_string, null)
  sensitive = true
}