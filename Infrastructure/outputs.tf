output "frontend_url" {
  value = "https://${azurerm_static_web_app.frontend.default_host_name}"
}

output "backend_url" {
  value = local.effective_api_public_url
}

output "backend_container_app_url" {
  value = local.backend_container_fqdn
}

output "api_front_door_endpoint_host" {
  value = try(azurerm_cdn_frontdoor_endpoint.api[0].host_name, null)
}

output "api_front_door_url" {
  value = var.enable_api_front_door ? (
    var.api_custom_domain != null && var.api_custom_domain != "" ?
    "https://${var.api_custom_domain}" :
    "https://${azurerm_cdn_frontdoor_endpoint.api[0].host_name}"
  ) : null
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