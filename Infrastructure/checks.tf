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
