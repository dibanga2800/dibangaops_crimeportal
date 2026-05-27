# Unified Azure Front Door: same-origin SPA + API on www (e.g. www.dibangops.com/api/* → backend, /* → SWA).
# Enable with enable_unified_front_door = true and point www DNS CNAME to unified_front_door_endpoint_host.

resource "azurerm_cdn_frontdoor_profile" "unified" {
  count               = var.enable_unified_front_door ? 1 : 0
  name                = "${local.frontend_name_effective}-unified-fd"
  resource_group_name = azurerm_resource_group.rg.name
  sku_name            = var.front_door_sku_name
}

# WAF managed rule sets require Premium_AzureFrontDoor. Standard SKU uses profile + routes only (no WAF policy).

resource "azurerm_cdn_frontdoor_endpoint" "unified" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "${local.frontend_name_effective}-unified-ep"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id
}

resource "azurerm_cdn_frontdoor_origin_group" "unified_api" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "${local.backend_name_effective}-unified-api"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }

  health_probe {
    protocol            = "Https"
    interval_in_seconds = 30
    path                = "/health"
    request_type        = "GET"
  }
}

resource "azurerm_cdn_frontdoor_origin_group" "unified_spa" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "${local.frontend_name_effective}-unified-spa"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }

  health_probe {
    protocol            = "Https"
    interval_in_seconds = 60
    path                = "/"
    request_type        = "GET"
  }
}

resource "azurerm_cdn_frontdoor_origin" "unified_api" {
  count                          = var.enable_unified_front_door ? 1 : 0
  name                           = "${local.backend_name_effective}-unified-api-origin"
  cdn_frontdoor_origin_group_id  = azurerm_cdn_frontdoor_origin_group.unified_api[0].id
  enabled                        = true
  host_name                      = azurerm_container_app.backend.ingress[0].fqdn
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = azurerm_container_app.backend.ingress[0].fqdn
  priority                       = 1
  weight                         = 1000
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_origin" "unified_spa" {
  count                          = var.enable_unified_front_door ? 1 : 0
  name                           = "${local.frontend_name_effective}-unified-spa-origin"
  cdn_frontdoor_origin_group_id  = azurerm_cdn_frontdoor_origin_group.unified_spa[0].id
  enabled                        = true
  host_name                      = azurerm_static_web_app.frontend.default_host_name
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = azurerm_static_web_app.frontend.default_host_name
  priority                       = 1
  weight                         = 1000
  certificate_name_check_enabled = true
}

# Single catch-all route + rule set: /api* → API origin group, everything else → SWA (more reliable than two routes on Standard SKU).
resource "azurerm_cdn_frontdoor_rule_set" "unified_routing" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "unifiedrouting"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id
}

resource "azurerm_cdn_frontdoor_rule" "unified_api_override" {
  count                     = var.enable_unified_front_door ? 1 : 0
  name                      = "apiorigingroup"
  cdn_frontdoor_rule_set_id = azurerm_cdn_frontdoor_rule_set.unified_routing[0].id
  order                     = 1
  behavior_on_match         = "Stop"

  actions {
    route_configuration_override_action {
      cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.unified_api[0].id
      forwarding_protocol           = "HttpsOnly"
      cache_behavior                = "Disabled"
    }
  }

  conditions {
    url_path_condition {
      operator         = "BeginsWith"
      match_values     = ["/api"]
      negate_condition = false
    }
  }

  depends_on = [
    azurerm_cdn_frontdoor_origin.unified_api,
    azurerm_cdn_frontdoor_origin_group.unified_api,
  ]
}

resource "azurerm_cdn_frontdoor_route" "unified_spa" {
  count                         = var.enable_unified_front_door ? 1 : 0
  name                          = "${local.frontend_name_effective}-unified-spa-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.unified[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.unified_spa[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.unified_spa[0].id]
  cdn_frontdoor_rule_set_ids    = [azurerm_cdn_frontdoor_rule_set.unified_routing[0].id]
  enabled                       = true
  forwarding_protocol           = "HttpsOnly"
  https_redirect_enabled        = true
  patterns_to_match             = ["/*"]
  supported_protocols           = ["Http", "Https"]
  # When www custom domain is enabled, do not bind the default *.azurefd.net host on this route (avoids wrong edge cert on www).
  link_to_default_domain          = var.enable_unified_front_door_custom_domain && length(azurerm_cdn_frontdoor_custom_domain.unified) > 0 ? false : true
  cdn_frontdoor_custom_domain_ids = var.enable_unified_front_door_custom_domain && length(azurerm_cdn_frontdoor_custom_domain.unified) > 0 ? [azurerm_cdn_frontdoor_custom_domain.unified[0].id] : []

  depends_on = [azurerm_cdn_frontdoor_rule.unified_api_override]
}

resource "azurerm_cdn_frontdoor_custom_domain" "unified" {
  count                    = var.enable_unified_front_door && var.enable_unified_front_door_custom_domain && local.unified_front_door_hostname_effective != null && local.unified_front_door_hostname_effective != "" ? 1 : 0
  name                     = replace(replace(local.unified_front_door_hostname_effective, ".", "-"), "_", "-")
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id
  host_name                = local.unified_front_door_hostname_effective

  tls {
    certificate_type    = "ManagedCertificate"
    minimum_tls_version = "TLS12"
  }
}

