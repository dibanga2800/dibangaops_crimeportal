data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
}

resource "random_password" "sql_admin" {
  count = var.sql_admin_password == null || var.sql_admin_password == "" ? 1 : 0

  length           = 24
  special          = true
  override_special = "!@#$%^&*()_-+=?"
  min_upper        = 2
  min_lower        = 2
  min_numeric      = 2
}

# HS256 signing key for ASP.NET JWT (must match Program.cs Jwt:Key configuration binding).
resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}

locals {
  suffix                         = random_string.suffix.result
  sql_server_name                = substr("${var.sql_server_name_prefix}${local.suffix}", 0, 63)
  storage_account_name           = substr("${var.blob_storage_name_prefix}${local.suffix}", 0, 24)
  acr_name                       = substr("${var.acr_name_prefix}${local.suffix}", 0, 50)
  key_vault_name                 = substr("${var.keyvault_name_prefix}-${local.suffix}", 0, 24)
  sql_admin_password             = var.sql_admin_password != null && var.sql_admin_password != "" ? var.sql_admin_password : random_password.sql_admin[0].result
  backend_target_port            = var.backend_target_port == null ? (strcontains(var.backend_image, "azuredocs/containerapps-helloworld") ? 80 : 8080) : var.backend_target_port
  ai_target_port                 = var.ai_target_port == null ? (strcontains(var.ai_image, "azuredocs/containerapps-helloworld") ? 80 : 8000) : var.ai_target_port
  backend_uses_acr               = strcontains(var.backend_image, ".azurecr.io/")
  ai_uses_acr                    = strcontains(var.ai_image, ".azurecr.io/")
  jwt_signing_key_value          = var.jwt_signing_key != null && var.jwt_signing_key != "" ? var.jwt_signing_key : random_password.jwt_signing_key.result
  backend_container_fqdn         = "https://${azurerm_container_app.backend.latest_revision_fqdn}"
  ai_container_fqdn              = "https://${var.ai_name}.internal.${azurerm_container_app_environment.env.default_domain}"
  effective_frontend_url         = var.frontend_url != null && var.frontend_url != "" ? var.frontend_url : "https://${azurerm_static_web_app.frontend.default_host_name}"
  effective_insightface_base_url = var.insightface_base_url != null && var.insightface_base_url != "" ? var.insightface_base_url : local.ai_container_fqdn
}

resource "azurerm_resource_group" "rg" {
  name     = var.resource_group
  location = var.location
}

resource "null_resource" "register_microsoft_app" {
  triggers = {
    subscription_id = data.azurerm_client_config.current.subscription_id
  }

  provisioner "local-exec" {
    command = "az provider register -n Microsoft.App --wait"
  }
}

resource "azurerm_log_analytics_workspace" "logs" {
  name                = "crimeportal-logs-${local.suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_analytics_retention_days
}

resource "azurerm_application_insights" "appinsights" {
  count               = var.enable_application_insights ? 1 : 0
  name                = "crimeportal-ai-${local.suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  workspace_id        = azurerm_log_analytics_workspace.logs.id
  application_type    = "web"
}

# ---------------- KEY VAULT & SECRETS ----------------

resource "azurerm_key_vault" "kv" {
  name                          = local.key_vault_name
  location                      = azurerm_resource_group.rg.location
  resource_group_name           = azurerm_resource_group.rg.name
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  sku_name                      = "standard"
  soft_delete_retention_days    = 7
  purge_protection_enabled      = false
  rbac_authorization_enabled    = true
  public_network_access_enabled = true
}

resource "azurerm_role_assignment" "terraform_kv_admin" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = var.terraform_kv_admin_principal_object_id
}

# ---------------- SQL ----------------

resource "azurerm_mssql_server" "sql_server" {
  name                         = local.sql_server_name
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = local.sql_admin_password
  minimum_tls_version          = "1.2"
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "sql_db" {
  name                        = var.sql_db_name
  server_id                   = azurerm_mssql_server.sql_server.id
  sku_name                    = "GP_S_Gen5_1"
  max_size_gb                 = var.sql_max_size_gb
  auto_pause_delay_in_minutes = 60
  min_capacity                = 0.5
}

resource "azurerm_key_vault_secret" "sql_admin_username" {
  name         = "sql-admin-username"
  value        = var.sql_admin_username
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "sql_admin_password" {
  name         = "sql-admin-password"
  value        = local.sql_admin_password
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "sql-connection-string"
  value        = "Server=tcp:${azurerm_mssql_server.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.sql_db.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${local.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "storage_connection_string" {
  name         = "storage-connection-string"
  value        = azurerm_storage_account.storage.primary_connection_string
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "jwt_signing_key" {
  name         = "jwt-signing-key"
  value        = local.jwt_signing_key_value
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "smtp_password" {
  count        = var.smtp_password != null && var.smtp_password != "" ? 1 : 0
  name         = "smtp-password"
  value        = var.smtp_password
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "azure_openai_api_key" {
  count        = var.azure_openai_api_key != null && var.azure_openai_api_key != "" ? 1 : 0
  name         = "azure-openai-api-key"
  value        = var.azure_openai_api_key
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_role_assignment.terraform_kv_admin]
}

# ---------------- STORAGE ----------------

resource "azurerm_storage_account" "storage" {
  name                            = local.storage_account_name
  resource_group_name             = azurerm_resource_group.rg.name
  location                        = azurerm_resource_group.rg.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = true
  shared_access_key_enabled       = true
}

resource "azurerm_storage_container" "images" {
  name                  = "images"
  storage_account_id    = azurerm_storage_account.storage.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "evidence" {
  name                  = "evidence"
  storage_account_id    = azurerm_storage_account.storage.id
  container_access_type = "private"
}

# ---------------- CONTAINER REGISTRY ----------------

resource "azurerm_container_registry" "acr" {
  name                = local.acr_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = false
}

# ---------------- CONTAINER APPS ----------------

resource "azurerm_container_app_environment" "env" {
  name                       = var.container_app_environment_name
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id

  depends_on = [null_resource.register_microsoft_app]
}

resource "azurerm_container_app" "backend" {
  name                         = var.backend_name
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  secret {
    name                = "sql-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.sql_connection_string.versionless_id
    identity            = "System"
  }

  secret {
    name                = "storage-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.storage_connection_string.versionless_id
    identity            = "System"
  }

  secret {
    name                = "jwt-signing-key"
    key_vault_secret_id = azurerm_key_vault_secret.jwt_signing_key.versionless_id
    identity            = "System"
  }

  dynamic "secret" {
    for_each = var.smtp_password != null && var.smtp_password != "" ? [1] : []
    content {
      name                = "smtp-password"
      key_vault_secret_id = azurerm_key_vault_secret.smtp_password[0].versionless_id
      identity            = "System"
    }
  }

  dynamic "secret" {
    for_each = var.azure_openai_api_key != null && var.azure_openai_api_key != "" ? [1] : []
    content {
      name                = "azure-openai-api-key"
      key_vault_secret_id = azurerm_key_vault_secret.azure_openai_api_key[0].versionless_id
      identity            = "System"
    }
  }

  template {
    container {
      name   = "backend"
      image  = var.backend_image
      cpu    = var.backend_container_cpu
      memory = var.backend_container_memory

      # Keep env order aligned with what is already deployed so Terraform does not match by index
      # and rewrite unrelated variables (see azurerm_container_app env block ordering).
      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "sql-connection-string"
      }

      env {
        name        = "ConnectionStrings__StorageAccount"
        secret_name = "storage-connection-string"
      }

      env {
        name  = "InsightFace__BaseUrl"
        value = local.effective_insightface_base_url
      }

      env {
        name  = "FrontendUrl"
        value = local.effective_frontend_url
      }

      env {
        name  = "IncidentImageStorage__Mode"
        value = "both"
      }

      env {
        name  = "IncidentImageStorage__ContainerName"
        value = "images"
      }

      env {
        name  = "IncidentImageStorage__BlobPathPrefix"
        value = "verification"
      }

      env {
        name        = "Jwt__Key"
        secret_name = "jwt-signing-key"
      }

      env {
        name  = "Jwt__Issuer"
        value = var.jwt_issuer
      }

      env {
        name  = "Jwt__Audience"
        value = var.jwt_audience
      }

      env {
        name  = "Jwt__AccessTokenExpirationMinutes"
        value = tostring(var.jwt_access_token_expiration_minutes)
      }

      env {
        name  = "Jwt__RefreshTokenExpirationDays"
        value = tostring(var.jwt_refresh_token_expiration_days)
      }

      dynamic "env" {
        for_each = var.smtp_host != null && var.smtp_host != "" ? [1] : []
        content {
          name  = "Smtp__Host"
          value = var.smtp_host
        }
      }

      dynamic "env" {
        for_each = var.smtp_port > 0 ? [1] : []
        content {
          name  = "Smtp__Port"
          value = tostring(var.smtp_port)
        }
      }

      env {
        name  = "Smtp__EnableSsl"
        value = tostring(var.smtp_enable_ssl)
      }

      dynamic "env" {
        for_each = var.smtp_username != null && var.smtp_username != "" ? [1] : []
        content {
          name  = "Smtp__Username"
          value = var.smtp_username
        }
      }

      dynamic "env" {
        for_each = var.smtp_password != null && var.smtp_password != "" ? [1] : []
        content {
          name        = "Smtp__Password"
          secret_name = "smtp-password"
        }
      }

      dynamic "env" {
        for_each = var.smtp_from_email != null && var.smtp_from_email != "" ? [1] : []
        content {
          name  = "Smtp__FromEmail"
          value = var.smtp_from_email
        }
      }

      env {
        name  = "Smtp__FromName"
        value = var.smtp_from_name
      }

      env {
        name  = "AzureOpenAI__Enabled"
        value = tostring(var.azure_openai_enabled)
      }

      dynamic "env" {
        for_each = var.azure_openai_endpoint != null && var.azure_openai_endpoint != "" ? [1] : []
        content {
          name  = "AzureOpenAI__Endpoint"
          value = var.azure_openai_endpoint
        }
      }

      dynamic "env" {
        for_each = var.azure_openai_api_key != null && var.azure_openai_api_key != "" ? [1] : []
        content {
          name        = "AzureOpenAI__ApiKey"
          secret_name = "azure-openai-api-key"
        }
      }

      dynamic "env" {
        for_each = var.azure_openai_deployment != null && var.azure_openai_deployment != "" ? [1] : []
        content {
          name  = "AzureOpenAI__Deployment"
          value = var.azure_openai_deployment
        }
      }

      env {
        name  = "InsightFace__Enabled"
        value = tostring(var.insightface_enabled)
      }

      env {
        name  = "InsightFace__TimeoutSeconds"
        value = tostring(var.insightface_timeout_seconds)
      }

      env {
        name  = "InsightFace__MinSimilarity"
        value = tostring(var.insightface_min_similarity)
      }

      env {
        name  = "InsightFace__MaxSearchResults"
        value = tostring(var.insightface_max_search_results)
      }
    }

    min_replicas = var.backend_min_replicas
    max_replicas = var.backend_max_replicas
  }

  ingress {
    external_enabled = true
    target_port      = local.backend_target_port
    transport        = "auto"
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  dynamic "registry" {
    for_each = local.backend_uses_acr ? [1] : []
    content {
      server   = azurerm_container_registry.acr.login_server
      identity = "System"
    }
  }
}

resource "azurerm_container_app" "ai" {
  name                         = var.ai_name
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    container {
      name   = "ai"
      image  = var.ai_image
      cpu    = var.ai_container_cpu
      memory = var.ai_container_memory
    }

    min_replicas = var.ai_min_replicas
    max_replicas = var.ai_max_replicas
  }

  ingress {
    external_enabled = false
    target_port      = local.ai_target_port
    transport        = "auto"
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  dynamic "registry" {
    for_each = local.ai_uses_acr ? [1] : []
    content {
      server   = azurerm_container_registry.acr.login_server
      identity = "System"
    }
  }
}

resource "azurerm_role_assignment" "backend_keyvault_secrets_user" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "ai_keyvault_secrets_user" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.ai.identity[0].principal_id
}

resource "azurerm_role_assignment" "backend_acr_pull" {
  count                = local.backend_uses_acr ? 1 : 0
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "ai_acr_pull" {
  count                = local.ai_uses_acr ? 1 : 0
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_container_app.ai.identity[0].principal_id
}

# ---------------- COST CONTROL ----------------

resource "azurerm_consumption_budget_subscription" "monthly" {
  count           = var.enable_budget_alert && length(var.budget_alert_emails) > 0 ? 1 : 0
  name            = "${var.resource_group}-monthly-budget"
  subscription_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}"

  amount     = var.monthly_budget_amount
  time_grain = "Monthly"

  time_period {
    start_date = var.budget_start_date
  }

  notification {
    enabled        = true
    threshold      = 80
    operator       = "GreaterThan"
    threshold_type = "Actual"
    contact_emails = var.budget_alert_emails
  }

  notification {
    enabled        = true
    threshold      = 100
    operator       = "GreaterThan"
    threshold_type = "Forecasted"
    contact_emails = var.budget_alert_emails
  }
}

# ---------------- STATIC WEB APP ----------------

resource "azurerm_static_web_app" "frontend" {
  name                = var.frontend_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.static_web_app_location
  sku_tier            = "Free"
  sku_size            = "Free"
}

resource "azurerm_static_web_app_custom_domain" "frontend_custom_domain" {
  count             = var.frontend_custom_domain != null && var.frontend_custom_domain != "" ? 1 : 0
  static_web_app_id = azurerm_static_web_app.frontend.id
  domain_name       = var.frontend_custom_domain
  validation_type   = "dns-txt-token"
}

resource "azurerm_static_web_app_custom_domain" "frontend_www_custom_domain" {
  count             = var.frontend_www_custom_domain != null && var.frontend_www_custom_domain != "" ? 1 : 0
  static_web_app_id = azurerm_static_web_app.frontend.id
  domain_name       = var.frontend_www_custom_domain
  validation_type   = "dns-txt-token"
}