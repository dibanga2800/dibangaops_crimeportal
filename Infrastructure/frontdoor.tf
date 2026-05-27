# Azure Front Door (Standard) + WAF in front of the public API Container App.
# Enable with enable_api_front_door = true and set api_custom_domain (e.g. api.dibangops.com).

resource "azurerm_cdn_frontdoor_profile" "api" {
  count               = var.enable_api_front_door ? 1 : 0
  name                = "${local.backend_name_effective}-fd"
  resource_group_name = azurerm_resource_group.rg.name
  sku_name            = var.front_door_sku_name
}

resource "azurerm_cdn_frontdoor_firewall_policy" "api" {
  count               = var.enable_api_front_door ? 1 : 0
  name                = "${replace(local.backend_name_effective, "-", "")}waf"
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

resource "azurerm_cdn_frontdoor_endpoint" "api" {
  count                    = var.enable_api_front_door ? 1 : 0
  name                     = "${local.backend_name_effective}-ep"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.api[0].id
}

resource "azurerm_cdn_frontdoor_origin_group" "api" {
  count                    = var.enable_api_front_door ? 1 : 0
  name                     = "${local.backend_name_effective}-origins"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.api[0].id

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

resource "azurerm_cdn_frontdoor_origin" "api" {
  count                          = var.enable_api_front_door ? 1 : 0
  name                           = "${local.backend_name_effective}-containerapp"
  cdn_frontdoor_origin_group_id  = azurerm_cdn_frontdoor_origin_group.api[0].id
  enabled                        = true
  host_name                      = azurerm_container_app.backend.latest_revision_fqdn
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = azurerm_container_app.backend.latest_revision_fqdn
  priority                       = 1
  weight                         = 1000
  certificate_name_check_enabled = true
}

resource "azurerm_cdn_frontdoor_route" "api" {
  count                         = var.enable_api_front_door ? 1 : 0
  name                          = "${local.backend_name_effective}-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.api[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.api[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.api[0].id]
  enabled                       = true
  forwarding_protocol           = "HttpsOnly"
  https_redirect_enabled        = true
  patterns_to_match             = ["/*"]
  supported_protocols           = ["Http", "Https"]
  link_to_default_domain        = true
}

resource "azurerm_cdn_frontdoor_custom_domain" "api" {
  count                    = var.enable_api_front_door && var.api_custom_domain != null && var.api_custom_domain != "" ? 1 : 0
  name                     = replace(replace(var.api_custom_domain, ".", "-"), "_", "-")
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.api[0].id
  host_name                = var.api_custom_domain

  tls {
    certificate_type    = "ManagedCertificate"
    minimum_tls_version = "TLS12"
  }
}

resource "azurerm_cdn_frontdoor_security_policy" "api" {
  count                    = var.enable_api_front_door ? 1 : 0
  name                     = "${local.backend_name_effective}-sec"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.api[0].id

  security_policies {
    firewall {
      cdn_frontdoor_firewall_policy_id = azurerm_cdn_frontdoor_firewall_policy.api[0].id

      association {
        domain {
          cdn_frontdoor_domain_id = var.api_custom_domain != null && var.api_custom_domain != "" ? azurerm_cdn_frontdoor_custom_domain.api[0].id : azurerm_cdn_frontdoor_endpoint.api[0].id
        }
        patterns_to_match = ["/*"]
      }
    }
  }
}
