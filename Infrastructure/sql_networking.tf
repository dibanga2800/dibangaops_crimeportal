# Optional private connectivity for Azure SQL.
# When enable_sql_private_endpoint = true:
#   - Creates VNet + private endpoint + private DNS for privatelink.database.windows.net
#   - Disables public network access on the SQL server
#   - Container Apps environment must use infrastructure_subnet_id (set automatically below)
#
# WARNING: Enabling this on an existing stack may recreate the Container Apps environment.
# Plan carefully and run during a maintenance window.

resource "azurerm_virtual_network" "data" {
  count               = var.enable_sql_private_endpoint ? 1 : 0
  name                = "${var.resource_group}-vnet"
  address_space       = [var.vnet_address_space]
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_subnet" "private_endpoints" {
  count                = var.enable_sql_private_endpoint ? 1 : 0
  name                 = "snet-private-endpoints"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.data[0].name
  address_prefixes     = [var.private_endpoint_subnet_prefix]
}

resource "azurerm_subnet" "container_apps" {
  count                = var.enable_sql_private_endpoint ? 1 : 0
  name                 = "snet-container-apps"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.data[0].name
  address_prefixes     = [var.container_apps_subnet_prefix]

  delegation {
    name = "container-apps-delegation"
    service_delegation {
      name = "Microsoft.App/environments"
      actions = [
        "Microsoft.Network/virtualNetworks/subnets/join/action",
      ]
    }
  }
}

resource "azurerm_private_dns_zone" "sql" {
  count               = var.enable_sql_private_endpoint ? 1 : 0
  name                = "privatelink.database.windows.net"
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql" {
  count                 = var.enable_sql_private_endpoint ? 1 : 0
  name                  = "${local.sql_server_name}-sql-dns-link"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.sql[0].name
  virtual_network_id    = azurerm_virtual_network.data[0].id
}

resource "azurerm_private_endpoint" "sql" {
  count               = var.enable_sql_private_endpoint ? 1 : 0
  name                = "${local.sql_server_name}-pe"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  subnet_id           = azurerm_subnet.private_endpoints[0].id

  private_service_connection {
    name                           = "${local.sql_server_name}-psc"
    private_connection_resource_id = azurerm_mssql_server.sql_server.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "sql-dns-group"
    private_dns_zone_ids = [azurerm_private_dns_zone.sql[0].id]
  }
}

# NAT Gateway for VNet-integrated Container Apps outbound egress.
# Required because VNet-integrated Container Apps cannot reliably reach ACR,
# App Insights, Key Vault, SMTP relays, or external model CDNs through Azure's
# default outbound. Adds a static, deterministic egress IP so external service
# allow-lists can be configured (see output nat_gateway_egress_ip).
# Phase 1 v2: gated by enable_sql_private_endpoint && enable_nat_gateway_egress;
# the validation script and a Terraform `check` block in checks.tf both refuse
# to apply Phase 1 without it.
resource "azurerm_public_ip" "nat_egress" {
  count               = var.enable_sql_private_endpoint && var.enable_nat_gateway_egress ? 1 : 0
  name                = "${var.resource_group}-nat-egress-pip"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

resource "azurerm_nat_gateway" "egress" {
  count                   = var.enable_sql_private_endpoint && var.enable_nat_gateway_egress ? 1 : 0
  name                    = "${var.resource_group}-egress-natgw"
  location                = azurerm_resource_group.rg.location
  resource_group_name     = azurerm_resource_group.rg.name
  sku_name                = "Standard"
  idle_timeout_in_minutes = var.nat_gateway_idle_timeout_minutes
}

resource "azurerm_nat_gateway_public_ip_association" "egress" {
  count                = var.enable_sql_private_endpoint && var.enable_nat_gateway_egress ? 1 : 0
  nat_gateway_id       = azurerm_nat_gateway.egress[0].id
  public_ip_address_id = azurerm_public_ip.nat_egress[0].id
}

resource "azurerm_subnet_nat_gateway_association" "container_apps" {
  count          = var.enable_sql_private_endpoint && var.enable_nat_gateway_egress ? 1 : 0
  subnet_id      = azurerm_subnet.container_apps[0].id
  nat_gateway_id = azurerm_nat_gateway.egress[0].id
}
