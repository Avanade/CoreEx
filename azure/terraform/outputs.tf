output "app_service_plan_name" {
  value = azapi_resource.app_service_plan.name
}

output "app_insights_connection_string" {
  value     = local.app_insights_output.properties.ConnectionString
  sensitive = true
}

output "key_vault_name" {
  value = azapi_resource.key_vault.name
}

output "service_bus_namespace_name" {
  value = azapi_resource.service_bus.name
}

output "redis_host_name" {
  value = local.redis_output.properties.hostName
}

output "sql_server_name" {
  value = azapi_resource.sql_server.name
}

output "sql_database_name" {
  value = azapi_resource.sql_database.name
}

output "products_api_app_name" {
  value = azapi_resource.products_api.name
}

output "shopping_api_app_name" {
  value = azapi_resource.shopping_api.name
}

output "products_outbox_relay_app_name" {
  value = azapi_resource.products_outbox_relay.name
}

output "shopping_outbox_relay_app_name" {
  value = azapi_resource.shopping_outbox_relay.name
}

output "products_subscribe_app_name" {
  value = azapi_resource.products_subscribe.name
}

output "shopping_subscribe_app_name" {
  value = azapi_resource.shopping_subscribe.name
}

output "aspire_dashboard_app_name" {
  value = azapi_resource.aspire_dashboard.name
}

output "aspire_dashboard_uri" {
  value = "https://${local.aspire_dashboard_output.properties.defaultHostName}"
}

output "aspire_dashboard_otlp_http_endpoint" {
  value = "https://${local.aspire_dashboard_output.properties.defaultHostName}"
}
