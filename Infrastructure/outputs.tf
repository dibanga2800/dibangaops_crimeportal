output "frontend_url" {
  value = "https://${azurerm_static_web_app.frontend.default_host_name}"
}

output "static_web_app_name" {
  description = "Static Web App resource name (includes stack suffix for prod-v2 / scratch)"
  value       = azurerm_static_web_app.frontend.name
}

output "public_app_url" {
  description = "Canonical public URL for the SPA (unified Front Door hostname when enabled, else SWA default)"
  value       = local.public_app_url
}

output "unified_front_door_endpoint_host" {
  description = "Azure Front Door endpoint hostname for unified SPA+API (CNAME www to this before cutover)"
  value       = try(azurerm_cdn_frontdoor_endpoint.unified[0].host_name, null)
}

output "unified_front_door_profile_name" {
  description = "Unified Front Door profile resource name (stack-suffixed for parallel blue/green stacks)"
  value       = try(azurerm_cdn_frontdoor_profile.unified[0].name, null)
}

output "unified_front_door_endpoint_name" {
  description = "Unified Front Door endpoint resource name (globally unique; stack-suffixed for prod-v2)"
  value       = try(azurerm_cdn_frontdoor_endpoint.unified[0].name, null)
}

output "unified_front_door_url" {
  description = "HTTPS URL for unified app when custom hostname is configured"
  value = var.enable_unified_front_door && local.unified_front_door_hostname_effective != null && local.unified_front_door_hostname_effective != "" ? (
    "https://${local.unified_front_door_hostname_effective}"
  ) : try("https://${azurerm_cdn_frontdoor_endpoint.unified[0].host_name}", null)
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

output "nat_gateway_egress_ip" {
  description = "Static public IP used for VNet-integrated Container App outbound traffic. Add to external service allow-lists (SMTP relay, etc.) when SQL private endpoint + NAT Gateway are enabled."
  value       = (var.enable_sql_private_endpoint && var.enable_nat_gateway_egress) ? azurerm_public_ip.nat_egress[0].ip_address : null
}