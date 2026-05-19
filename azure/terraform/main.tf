resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
  tags = merge(var.tags, {
    environment    = var.environment_type
    managedBy      = "azd"
    "azd-env-name" = var.environment_type
  })
}

resource "random_id" "kv" {
  byte_length = 6
  keepers = {
    environment = var.environment_type
    suffix      = var.name_suffix
  }
}

locals {
  suffix = lower(var.name_suffix)

  merged_tags = merge(var.tags, {
    environment    = var.environment_type
    managedBy      = "azd"
    "azd-env-name" = var.environment_type
  })

  app_service_plan_name = "asp-${var.environment_type}-${local.suffix}"
  app_insights_name     = "appi-${var.environment_type}-${local.suffix}"
  service_bus_name      = "sb-${var.environment_type}-${local.suffix}"
  redis_name            = "redis-${var.environment_type}-${local.suffix}"
  sql_server_name       = "sql-${var.environment_type}-${local.suffix}"
  postgres_server_name  = "pg-${var.environment_type}-${local.suffix}"
  dashboard_name        = "app-aspire-dashboard-${var.environment_type}-${local.suffix}"
  postgres_admin_password_effective = coalesce(var.postgres_admin_password, var.sql_admin_password)

  key_vault_name = substr("kv${var.environment_type}${local.suffix}${random_id.kv.hex}", 0, 24)
}

resource "azapi_resource" "app_insights" {
  type      = "Microsoft.Insights/components@2020-02-02"
  parent_id = azurerm_resource_group.rg.id
  name      = local.app_insights_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    kind = "web"
    properties = {
      Application_Type = "web"
      Flow_Type        = "Bluefield"
      Request_Source   = "rest"
    }
  }

  response_export_values = [
    "id",
    "name",
    "properties.ConnectionString",
    "properties.InstrumentationKey"
  ]
}

locals {
  app_insights_output = azapi_resource.app_insights.output
}

resource "azapi_resource" "key_vault" {
  type      = "Microsoft.KeyVault/vaults@2023-07-01"
  parent_id = azurerm_resource_group.rg.id
  name      = local.key_vault_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    properties = {
      sku = {
        family = "A"
        name   = "standard"
      }
      tenantId                     = data.azurerm_client_config.current.tenant_id
      enableRbacAuthorization      = true
      enabledForDeployment         = true
      enabledForTemplateDeployment = true
      enabledForDiskEncryption     = false
      publicNetworkAccess          = "Enabled"
      networkAcls = {
        bypass        = "AzureServices"
        defaultAction = var.key_vault_firewall_client_ip == "" ? "Allow" : "Deny"
        ipRules = var.key_vault_firewall_client_ip == "" ? [] : [
          {
            value = var.key_vault_firewall_client_ip
          }
        ]
        virtualNetworkRules = []
      }
    }
  }

  response_export_values = ["id", "name", "properties.vaultUri"]
}

resource "azurerm_role_assignment" "key_vault_admin" {
  scope                = azapi_resource.key_vault.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "time_sleep" "wait_for_key_vault_rbac" {
  depends_on      = [azurerm_role_assignment.key_vault_admin]
  create_duration = "20s"
}

resource "azapi_resource" "app_service_plan" {
  type      = "Microsoft.Web/serverfarms@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = local.app_service_plan_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    kind = "linux"
    sku = {
      name     = var.app_service_plan_sku_name
      tier     = var.app_service_plan_sku_tier
      capacity = var.app_service_plan_capacity
    }
    properties = {
      reserved = true
    }
  }
}

resource "azapi_resource" "service_bus" {
  type      = "Microsoft.ServiceBus/namespaces@2023-01-01-preview"
  parent_id = azurerm_resource_group.rg.id
  name      = local.service_bus_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    sku = {
      name = var.service_bus_sku_name
      tier = var.service_bus_sku_name
    }
    properties = {
      publicNetworkAccess = "Enabled"
      minimumTlsVersion   = "1.2"
    }
  }
}

resource "azapi_resource" "service_bus_topic" {
  type      = "Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview"
  parent_id = azapi_resource.service_bus.id
  name      = "contoso"

  body = {
    properties = {
      defaultMessageTimeToLive            = "P14D"
      requiresDuplicateDetection          = true
      duplicateDetectionHistoryTimeWindow = "PT10M"
    }
  }
}

resource "azapi_resource" "service_bus_subscription_products" {
  type      = "Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview"
  parent_id = azapi_resource.service_bus_topic.id
  name      = "products"

  body = {
    properties = {
      requiresSession                  = true
      maxDeliveryCount                 = 10
      lockDuration                     = "PT5M"
      deadLetteringOnMessageExpiration = true
    }
  }
}

resource "azapi_resource" "service_bus_subscription_shopping" {
  type      = "Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview"
  parent_id = azapi_resource.service_bus_topic.id
  name      = "shopping"

  body = {
    properties = {
      requiresSession                  = true
      maxDeliveryCount                 = 10
      lockDuration                     = "PT5M"
      deadLetteringOnMessageExpiration = true
    }
  }
}

resource "azapi_resource" "service_bus_auth_rule" {
  type      = "Microsoft.ServiceBus/namespaces/AuthorizationRules@2023-01-01-preview"
  parent_id = azapi_resource.service_bus.id
  name      = "app"

  body = {
    properties = {
      rights = ["Listen", "Send", "Manage"]
    }
  }
}

resource "azapi_resource_action" "service_bus_keys" {
  type        = "Microsoft.ServiceBus/namespaces/AuthorizationRules@2023-01-01-preview"
  resource_id = azapi_resource.service_bus_auth_rule.id
  action      = "listKeys"
  method      = "POST"

  response_export_values = ["primaryConnectionString"]
}

locals {
  service_bus_keys_output = azapi_resource_action.service_bus_keys.output
}

resource "azapi_resource" "redis" {
  type      = "Microsoft.Cache/redisEnterprise@2025-07-01"
  parent_id = azurerm_resource_group.rg.id
  name      = local.redis_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    sku = {
      name = var.redis_sku_name
    }
    properties = {
      highAvailability    = var.redis_high_availability
      minimumTlsVersion   = "1.2"
      publicNetworkAccess = "Enabled"
      encryption          = {}
    }
  }

  response_export_values = ["properties.hostName"]
}

resource "azapi_resource" "redis_default_db" {
  type      = "Microsoft.Cache/redisEnterprise/databases@2025-04-01"
  parent_id = azapi_resource.redis.id
  name      = "default"

  body = {
    properties = {
      clientProtocol   = "Encrypted"
      clusteringPolicy = "OSSCluster"
      evictionPolicy   = "VolatileLRU"
      modules          = []
      port             = 10000
    }
  }
}

resource "azapi_resource_action" "redis_keys" {
  type        = "Microsoft.Cache/redisEnterprise/databases@2025-04-01"
  resource_id = azapi_resource.redis_default_db.id
  action      = "listKeys"
  method      = "POST"

  response_export_values = ["primaryKey"]
}

locals {
  redis_output      = azapi_resource.redis.output
  redis_keys_output = azapi_resource_action.redis_keys.output

  redis_connection_string = "${local.redis_output.properties.hostName}:10000,password=${local.redis_keys_output.primaryKey},ssl=True,abortConnect=False"
}

resource "azapi_resource" "sql_server" {
  type      = "Microsoft.Sql/servers@2023-08-01-preview"
  parent_id = azurerm_resource_group.rg.id
  name      = local.sql_server_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    properties = {
      administratorLogin         = var.sql_admin_login
      administratorLoginPassword = var.sql_admin_password
      version                    = "12.0"
      publicNetworkAccess        = "Enabled"
      minimalTlsVersion          = "1.2"
    }
  }
}

resource "azapi_resource" "sql_firewall_azure" {
  type      = "Microsoft.Sql/servers/firewallRules@2023-08-01-preview"
  parent_id = azapi_resource.sql_server.id
  name      = "AllowAzureServices"

  body = {
    properties = {
      startIpAddress = "0.0.0.0"
      endIpAddress   = "0.0.0.0"
    }
  }
}

resource "azapi_resource" "sql_firewall_client" {
  count     = var.sql_firewall_client_ip == "" ? 0 : 1
  type      = "Microsoft.Sql/servers/firewallRules@2023-08-01-preview"
  parent_id = azapi_resource.sql_server.id
  name      = "AllowCurrentRunner-${replace(var.sql_firewall_client_ip, ".", "-")}"

  body = {
    properties = {
      startIpAddress = var.sql_firewall_client_ip
      endIpAddress   = var.sql_firewall_client_ip
    }
  }
}

resource "azapi_resource" "sql_database" {
  type      = "Microsoft.Sql/servers/databases@2023-08-01-preview"
  parent_id = azapi_resource.sql_server.id
  name      = var.sql_database_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    sku = {
      name = var.sql_sku_name
      tier = var.sql_sku_tier
    }
    properties = {
      collation      = "SQL_Latin1_General_CP1_CI_AS"
      minCapacity    = var.sql_min_capacity
      autoPauseDelay = var.sql_auto_pause_delay
    }
  }
}

locals {
  sql_fqdn              = "${local.sql_server_name}.database.windows.net"
  sql_connection_string = "Data Source=tcp:${local.sql_fqdn},1433;Initial Catalog=${var.sql_database_name};User id=${var.sql_admin_login};Password=${var.sql_admin_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  postgres_fqdn         = "${local.postgres_server_name}.postgres.database.azure.com"
  postgres_connection_string = "Host=${local.postgres_fqdn};Port=5432;Database=${var.postgres_database_name};Username=${var.postgres_admin_login};Password=${local.postgres_admin_password_effective};Ssl Mode=Require;Trust Server Certificate=true;"
}

resource "azapi_resource" "postgres_server" {
  type      = "Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview"
  parent_id = azurerm_resource_group.rg.id
  name      = local.postgres_server_name
  location  = var.location
  tags      = local.merged_tags

  body = {
    sku = {
      name = var.postgres_sku_name
      tier = var.postgres_sku_tier
    }
    properties = {
      administratorLogin         = var.postgres_admin_login
      administratorLoginPassword = local.postgres_admin_password_effective
      version                    = var.postgres_version
      publicNetworkAccess        = "Enabled"
      storage = {
        storageSizeGB = var.postgres_storage_mb / 1024
      }
      highAvailability = {
        mode = "Disabled"
      }
      backup = {
        backupRetentionDays = 7
        geoRedundantBackup  = "Disabled"
      }
    }
  }
}

resource "azapi_resource" "postgres_firewall_azure" {
  type      = "Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview"
  parent_id = azapi_resource.postgres_server.id
  name      = "AllowAzureServices"

  body = {
    properties = {
      startIpAddress = "0.0.0.0"
      endIpAddress   = "0.0.0.0"
    }
  }
}

resource "azapi_resource" "postgres_firewall_client" {
  count     = var.postgres_firewall_client_ip == "" ? 0 : 1
  type      = "Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview"
  parent_id = azapi_resource.postgres_server.id
  name      = "AllowCurrentRunner-${replace(var.postgres_firewall_client_ip, ".", "-")}"

  body = {
    properties = {
      startIpAddress = var.postgres_firewall_client_ip
      endIpAddress   = var.postgres_firewall_client_ip
    }
  }
}

resource "azapi_resource" "postgres_database" {
  type      = "Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview"
  parent_id = azapi_resource.postgres_server.id
  name      = var.postgres_database_name

  body = {
    properties = {
      charset   = "UTF8"
      collation = "en_US.utf8"
    }
  }
}

resource "azurerm_key_vault_secret" "sql_admin_password" {
  name         = "sql-admin-password"
  value        = var.sql_admin_password
  key_vault_id = azapi_resource.key_vault.id

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}

resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "sql-connection-string"
  value        = local.sql_connection_string
  key_vault_id = azapi_resource.key_vault.id

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}

resource "azurerm_key_vault_secret" "postgres_admin_password" {
  name         = "postgres-admin-password"
  value        = local.postgres_admin_password_effective
  key_vault_id = azapi_resource.key_vault.id

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}

resource "azurerm_key_vault_secret" "postgres_connection_string" {
  name         = "postgres-connection-string"
  value        = local.postgres_connection_string
  key_vault_id = azapi_resource.key_vault.id

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}

resource "azurerm_key_vault_secret" "service_bus_connection_string" {
  name         = "service-bus-connection-string"
  value        = local.service_bus_keys_output.primaryConnectionString
  key_vault_id = azapi_resource.key_vault.id

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}

resource "azapi_resource" "aspire_dashboard" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = local.dashboard_name
  location  = var.location
  tags = merge(local.merged_tags, {
    role = "aspire-dashboard"
  })

  body = {
    kind = "app,linux,container"
    properties = {
      serverFarmId = azapi_resource.app_service_plan.id
      httpsOnly    = true
      siteConfig = {
        linuxFxVersion = "DOCKER|mcr.microsoft.com/dotnet/aspire-dashboard:latest"
        ftpsState      = "Disabled"
        alwaysOn       = true
        http20Enabled  = true
        appSettings = [
          {
            name  = "WEBSITES_PORT"
            value = "18888"
          },
          {
            name  = "ASPNETCORE_URLS"
            value = "http://0.0.0.0:18888"
          },
          {
            name  = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"
            value = "http://0.0.0.0:18889"
          },
          {
            name  = "ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"
            value = "http://0.0.0.0:18888"
          },
          {
            name  = "DASHBOARD__UI__DISABLERESOURCEGRAPH"
            value = "true"
          }
        ]
      }
    }
  }

  response_export_values = ["properties.defaultHostName"]
}

locals {
  aspire_dashboard_output = azapi_resource.aspire_dashboard.output
  otlp_http_endpoint      = "https://${local.aspire_dashboard_output.properties.defaultHostName}"
  app_services_common_settings = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Development"
    },
    {
      name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
      value = local.app_insights_output.properties.ConnectionString
    },
    {
      name  = "APPINSIGHTS_INSTRUMENTATIONKEY"
      value = local.app_insights_output.properties.InstrumentationKey
    },
    {
      name  = "ApplicationInsightsAgent_EXTENSION_VERSION"
      value = "~3"
    },
    {
      name  = "XDT_MicrosoftApplicationInsights_Mode"
      value = "recommended"
    },
    {
      name  = "XDT_MicrosoftApplicationInsights_PreemptSdk"
      value = "disabled"
    },
    {
      name  = "DiagnosticServices_EXTENSION_VERSION"
      value = "~3"
    },
    {
      name  = "APPINSIGHTS_PROFILERFEATURE_VERSION"
      value = "1.0.0"
    },
    {
      name  = "APPINSIGHTS_SNAPSHOTFEATURE_VERSION"
      value = "1.0.0"
    },
    {
      name  = "Aspire__StackExchange__Redis__ConnectionString"
      value = local.redis_connection_string
    },
    {
      name  = "Aspire__Azure__Messaging__ServiceBus__ConnectionString"
      value = local.service_bus_keys_output.primaryConnectionString
    },
    {
      name  = "OTEL_EXPORTER_OTLP_PROTOCOL"
      value = "http/protobuf"
    },
    {
      name  = "OTEL_EXPORTER_OTLP_ENDPOINT"
      value = local.otlp_http_endpoint
    }
  ]
  app_services_sql_settings = [
    {
      name  = "Aspire__Microsoft__Data__SqlClient__ConnectionString"
      value = local.sql_connection_string
    }
  ]
  app_services_postgres_settings = [
    {
      name  = "Aspire__Npgsql__ConnectionString"
      value = local.postgres_connection_string
    }
  ]
}

resource "azapi_resource" "products_api" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = "app-products-api-${var.environment_type}-${local.suffix}"
  location  = var.location
  tags = merge(local.merged_tags, {
    "azd-service-name"                       = "products-api"
    "hidden-link: /app-insights-resource-id" = azapi_resource.app_insights.id
  })

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId              = azapi_resource.app_service_plan.id
      httpsOnly                 = true
      endToEndEncryptionEnabled = true
      siteConfig = {
        linuxFxVersion      = var.app_service_linux_fx_version
        minTlsVersion       = "1.3"
        minTlsCipherSuite   = "TLS_AES_256_GCM_SHA384"
        scmMinTlsVersion    = "1.3"
        netFrameworkVersion = ""
        ftpsState           = "Disabled"
        http20Enabled       = true
        alwaysOn            = true
        appSettings         = concat(local.app_services_common_settings, local.app_services_postgres_settings)
      }
    }
  }

  response_export_values = ["properties.defaultHostName"]
}

locals {
  products_api_output = azapi_resource.products_api.output
}

resource "azapi_resource" "shopping_api" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = "app-shopping-api-${var.environment_type}-${local.suffix}"
  location  = var.location
  tags = merge(local.merged_tags, {
    "azd-service-name"                       = "shopping-api"
    "hidden-link: /app-insights-resource-id" = azapi_resource.app_insights.id
  })

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId              = azapi_resource.app_service_plan.id
      httpsOnly                 = true
      endToEndEncryptionEnabled = true
      siteConfig = {
        linuxFxVersion      = var.app_service_linux_fx_version
        minTlsVersion       = "1.3"
        minTlsCipherSuite   = "TLS_AES_256_GCM_SHA384"
        scmMinTlsVersion    = "1.3"
        netFrameworkVersion = ""
        ftpsState           = "Disabled"
        http20Enabled       = true
        alwaysOn            = true
        appSettings = concat(local.app_services_common_settings, local.app_services_sql_settings, [
          {
            name  = "ProductsApi__BaseAddress"
            value = "https://${local.products_api_output.properties.defaultHostName}"
          }
        ])
      }
    }
  }
}

resource "azapi_resource" "products_outbox_relay" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = "app-products-outbox-relay-${var.environment_type}-${local.suffix}"
  location  = var.location
  tags = merge(local.merged_tags, {
    "azd-service-name"                       = "products-outbox-relay"
    "hidden-link: /app-insights-resource-id" = azapi_resource.app_insights.id
  })

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId              = azapi_resource.app_service_plan.id
      httpsOnly                 = true
      endToEndEncryptionEnabled = true
      siteConfig = {
        linuxFxVersion      = var.app_service_linux_fx_version
        minTlsVersion       = "1.3"
        minTlsCipherSuite   = "TLS_AES_256_GCM_SHA384"
        scmMinTlsVersion    = "1.3"
        netFrameworkVersion = ""
        ftpsState           = "Disabled"
        http20Enabled       = true
        alwaysOn            = true
        appSettings         = concat(local.app_services_common_settings, local.app_services_postgres_settings)
      }
    }
  }
}

resource "azapi_resource" "shopping_outbox_relay" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = "app-shopping-outbox-relay-${var.environment_type}-${local.suffix}"
  location  = var.location
  tags = merge(local.merged_tags, {
    "azd-service-name"                       = "shopping-outbox-relay"
    "hidden-link: /app-insights-resource-id" = azapi_resource.app_insights.id
  })

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId              = azapi_resource.app_service_plan.id
      httpsOnly                 = true
      endToEndEncryptionEnabled = true
      siteConfig = {
        linuxFxVersion      = var.app_service_linux_fx_version
        minTlsVersion       = "1.3"
        minTlsCipherSuite   = "TLS_AES_256_GCM_SHA384"
        scmMinTlsVersion    = "1.3"
        netFrameworkVersion = ""
        ftpsState           = "Disabled"
        http20Enabled       = true
        alwaysOn            = true
        appSettings         = concat(local.app_services_common_settings, local.app_services_sql_settings)
      }
    }
  }
}

resource "azapi_resource" "products_subscribe" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = "app-products-subscribe-${var.environment_type}-${local.suffix}"
  location  = var.location
  tags = merge(local.merged_tags, {
    "azd-service-name"                       = "products-subscribe"
    "hidden-link: /app-insights-resource-id" = azapi_resource.app_insights.id
  })

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId              = azapi_resource.app_service_plan.id
      httpsOnly                 = true
      endToEndEncryptionEnabled = true
      siteConfig = {
        linuxFxVersion      = var.app_service_linux_fx_version
        minTlsVersion       = "1.3"
        minTlsCipherSuite   = "TLS_AES_256_GCM_SHA384"
        scmMinTlsVersion    = "1.3"
        netFrameworkVersion = ""
        ftpsState           = "Disabled"
        http20Enabled       = true
        alwaysOn            = true
        appSettings         = concat(local.app_services_common_settings, local.app_services_postgres_settings)
      }
    }
  }
}

resource "azapi_resource" "shopping_subscribe" {
  type      = "Microsoft.Web/sites@2023-12-01"
  parent_id = azurerm_resource_group.rg.id
  name      = "app-shopping-subscribe-${var.environment_type}-${local.suffix}"
  location  = var.location
  tags = merge(local.merged_tags, {
    "azd-service-name"                       = "shopping-subscribe"
    "hidden-link: /app-insights-resource-id" = azapi_resource.app_insights.id
  })

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId              = azapi_resource.app_service_plan.id
      httpsOnly                 = true
      endToEndEncryptionEnabled = true
      siteConfig = {
        linuxFxVersion      = var.app_service_linux_fx_version
        minTlsVersion       = "1.3"
        minTlsCipherSuite   = "TLS_AES_256_GCM_SHA384"
        scmMinTlsVersion    = "1.3"
        netFrameworkVersion = ""
        ftpsState           = "Disabled"
        http20Enabled       = true
        alwaysOn            = true
        appSettings         = concat(local.app_services_common_settings, local.app_services_sql_settings)
      }
    }
  }
}

data "azurerm_client_config" "current" {}
