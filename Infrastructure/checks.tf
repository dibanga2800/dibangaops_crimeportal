# Production guardrails for unified Front Door (prevent www TLS / auth regressions).

check "unified_front_door_custom_domain_requires_unified" {
  assert {
    condition     = !var.enable_unified_front_door_custom_domain || var.enable_unified_front_door
    error_message = "enable_unified_front_door_custom_domain requires enable_unified_front_door = true."
  }
}

check "unified_front_door_custom_domain_requires_hostname" {
  assert {
    condition = !var.enable_unified_front_door_custom_domain || (
      coalesce(var.unified_front_door_hostname, var.frontend_www_custom_domain, "") != ""
    )
    error_message = "Set unified_front_door_hostname or frontend_www_custom_domain before enabling enable_unified_front_door_custom_domain."
  }
}

check "sql_private_endpoint_disables_public_sql" {
  assert {
    condition     = !var.enable_sql_private_endpoint || !var.sql_public_network_access_enabled
    error_message = "When enable_sql_private_endpoint is true, set sql_public_network_access_enabled = false."
  }
}

check "sql_private_endpoint_skips_azure_services_firewall" {
  assert {
    condition     = !var.enable_sql_private_endpoint || !var.sql_allow_azure_services_firewall_rule
    error_message = "When enable_sql_private_endpoint is true, set sql_allow_azure_services_firewall_rule = false."
  }
}

check "backend_ingress_ip_restrictions_require_cidrs" {
  assert {
    condition     = !var.backend_ingress_ip_restrictions_enabled || length(var.backend_ingress_allowed_cidrs) > 0
    error_message = "backend_ingress_ip_restrictions_enabled requires at least one entry in backend_ingress_allowed_cidrs. Run scripts/extract-front-door-backend-cidrs.ps1."
  }
}

check "sql_basic_max_size" {
  assert {
    condition     = var.sql_sku_name != "Basic" || var.sql_max_size_gb <= 2
    error_message = "Basic tier supports max_size_gb <= 2."
  }
}

check "phase1_requires_nat_gateway" {
  assert {
    condition     = !var.enable_sql_private_endpoint || var.enable_nat_gateway_egress
    error_message = "When enable_sql_private_endpoint = true, enable_nat_gateway_egress must also be true. VNet-integrated Container Apps cannot reliably reach ACR without a NAT Gateway."
  }
}

check "container_apps_subnet_minimum_size" {
  assert {
    condition     = !var.enable_sql_private_endpoint || tonumber(split("/", var.container_apps_subnet_prefix)[1]) <= 23
    error_message = "container_apps_subnet_prefix must be /23 or larger when enable_sql_private_endpoint = true (Azure Container Apps requirement)."
  }
}
