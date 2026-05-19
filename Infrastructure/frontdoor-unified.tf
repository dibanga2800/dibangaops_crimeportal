# Unified Azure Front Door: same-origin SPA + API on www (e.g. www.dibangops.com/api/* → backend, /* → SWA).
# Enable with enable_unified_front_door = true and point www DNS CNAME to unified_front_door_endpoint_host.

resource "azurerm_cdn_frontdoor_profile" "unified" {
  count               = var.enable_unified_front_door ? 1 : 0
  name                = "${var.frontend_name}-unified-fd"
  resource_group_name = azurerm_resource_group.rg.name
  sku_name            = var.front_door_sku_name
}

resource "azurerm_cdn_frontdoor_firewall_policy" "unified" {
  count               = var.enable_unified_front_door ? 1 : 0
  name                = "${replace(var.frontend_name, "-", "")}unifiedwaf"
  resource_group_name = azurerm_resource_group.rg.name
  sku_name            = var.front_door_waf_sku_name
  enabled             = true
  mode                = var.front_door_waf_mode

  managed_rule {
    type    = "Microsoft_DefaultRuleSet"
    version = "2.1"
    action  = "Block"
  }

  managed_rule {
    type    = "Microsoft_BotManagerRuleSet"
    version = "1.0"
    action  = "Block"
  }
}

resource "azurerm_cdn_frontdoor_endpoint" "unified" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "${var.frontend_name}-unified-ep"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id
}

resource "azurerm_cdn_frontdoor_origin_group" "unified_api" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "${var.backend_name}-unified-api"
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
  name                     = "${var.frontend_name}-unified-spa"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }

  health_probe {
    protocol            = "Https"
    interval_in_seconds = 60
    path                = "/"
    request_type        = "HEAD"
  }
}

resource "azurerm_cdn_frontdoor_origin" "unified_api" {
  count                         = var.enable_unified_front_door ? 1 : 0
  name                          = "${var.backend_name}-unified-api-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.unified_api[0].id
  enabled                       = true
  host_name                     = azurerm_container_app.backend.ingress[0].fqdn
  http_port                     = 80
  https_port                    = 443
  origin_host_header            = azurerm_container_app.backend.ingress[0].fqdn
  priority                      = 1
  weight                        = 1000
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_origin" "unified_spa" {
  count                         = var.enable_unified_front_door ? 1 : 0
  name                          = "${var.frontend_name}-unified-spa-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.unified_spa[0].id
  enabled                       = true
  host_name                     = azurerm_static_web_app.frontend.default_host_name
  http_port                     = 80
  https_port                    = 443
  origin_host_header            = azurerm_static_web_app.frontend.default_host_name
  priority                      = 1
  weight                        = 1000
  certificate_name_check_enabled = true
}

# API route must be registered before catch-all SPA route.
resource "azurerm_cdn_frontdoor_route" "unified_api" {
  count                         = var.enable_unified_front_door ? 1 : 0
  name                          = "${var.backend_name}-unified-api-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.unified[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.unified_api[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.unified_api[0].id]
  enabled                       = true
  forwarding_protocol           = "HttpsOnly"
  https_redirect_enabled        = true
  patterns_to_match             = ["/api/*"]
  supported_protocols           = ["Http", "Https"]
  link_to_default_domain        = true
}

resource "azurerm_cdn_frontdoor_route" "unified_spa" {
  count                         = var.enable_unified_front_door ? 1 : 0
  name                          = "${var.frontend_name}-unified-spa-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.unified[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.unified_spa[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.unified_spa[0].id]
  enabled                       = true
  forwarding_protocol           = "HttpsOnly"
  https_redirect_enabled        = true
  patterns_to_match             = ["/*"]
  supported_protocols           = ["Http", "Https"]
  link_to_default_domain        = true

  depends_on = [azurerm_cdn_frontdoor_route.unified_api]
}

resource "azurerm_cdn_frontdoor_custom_domain" "unified" {
  count                    = var.enable_unified_front_door && local.unified_front_door_hostname_effective != null && local.unified_front_door_hostname_effective != "" ? 1 : 0
  name                     = replace(replace(local.unified_front_door_hostname_effective, ".", "-"), "_", "-")
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id
  host_name                = local.unified_front_door_hostname_effective

  tls {
    certificate_type    = "ManagedCertificate"
    minimum_tls_version = "TLS12"
  }
}

resource "azurerm_cdn_frontdoor_security_policy" "unified" {
  count                    = var.enable_unified_front_door ? 1 : 0
  name                     = "${var.frontend_name}-unified-sec"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.unified[0].id

  security_policies {
    firewall {
      cdn_frontdoor_firewall_policy_id = azurerm_cdn_frontdoor_firewall_policy.unified[0].id

      association {
        domain {
          cdn_frontdoor_domain_id = local.unified_front_door_hostname_effective != null && local.unified_front_door_hostname_effective != "" ? azurerm_cdn_frontdoor_custom_domain.unified[0].id : azurerm_cdn_frontdoor_endpoint.unified[0].id
        }
        patterns_to_match = ["/*"]
      }
    }
  }
}
