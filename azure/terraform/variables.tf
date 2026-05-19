variable "resource_group_name" {
  description = "Existing resource group where all resources will be deployed."
  type        = string
}

variable "environment_type" {
  description = "Deployment environment."
  type        = string
  validation {
    condition     = contains(["dev", "test", "prod"], var.environment_type)
    error_message = "environment_type must be one of: dev, test, prod."
  }
}

variable "location" {
  description = "Azure region for all resources."
  type        = string
}

variable "name_suffix" {
  description = "Short lowercase suffix used in resource names."
  type        = string
}

variable "tags" {
  description = "Base tags applied to all resources."
  type        = map(string)
  default     = {}
}

variable "app_service_plan_sku_name" {
  description = "App Service Plan SKU name."
  type        = string
}

variable "app_service_plan_sku_tier" {
  description = "App Service Plan SKU tier."
  type        = string
}

variable "app_service_plan_capacity" {
  description = "App Service Plan instance count."
  type        = number
}

variable "app_service_linux_fx_version" {
  description = "Linux runtime stack for code-based app services. Example: DOTNETCORE|10.0."
  type        = string
}

variable "service_bus_sku_name" {
  description = "Service Bus namespace SKU name."
  type        = string
}

variable "sql_admin_login" {
  description = "SQL admin username."
  type        = string
}

variable "sql_admin_password" {
  description = "SQL admin password."
  type        = string
  sensitive   = true
}

variable "sql_database_name" {
  description = "SQL database name."
  type        = string
}

variable "sql_firewall_client_ip" {
  description = "Optional public IPv4 for runner firewall allow rule."
  type        = string
  default     = ""
}

variable "key_vault_firewall_client_ip" {
  description = "Optional public IPv4 to allow through Key Vault firewall."
  type        = string
  default     = ""
}

variable "sql_sku_name" {
  description = "SQL database SKU name."
  type        = string
}

variable "sql_sku_tier" {
  description = "SQL database SKU tier."
  type        = string
}

variable "sql_min_capacity" {
  description = "SQL serverless min capacity."
  type        = number
}

variable "sql_auto_pause_delay" {
  description = "SQL auto-pause delay in minutes."
  type        = number
}

variable "postgres_admin_login" {
  description = "Postgres admin username."
  type        = string
}

variable "postgres_admin_password" {
  description = "Postgres admin password. Defaults to SQL admin password when omitted."
  type        = string
  sensitive   = true
  default     = null
}

variable "postgres_database_name" {
  description = "Postgres database name used by Products domain."
  type        = string
}

variable "postgres_firewall_client_ip" {
  description = "Optional public IPv4 for Postgres firewall allow rule."
  type        = string
  default     = ""
}

variable "postgres_sku_name" {
  description = "Postgres flexible server SKU name."
  type        = string
}

variable "postgres_sku_tier" {
  description = "Postgres flexible server SKU tier."
  type        = string
}

variable "postgres_version" {
  description = "Postgres major version."
  type        = string
}

variable "postgres_storage_mb" {
  description = "Postgres storage size in MB."
  type        = number
}

variable "redis_sku_name" {
  description = "Azure Managed Redis SKU name. Example: Balanced_B0."
  type        = string
}

variable "redis_high_availability" {
  description = "Azure Managed Redis high availability mode."
  type        = string
  validation {
    condition     = contains(["Enabled", "Disabled"], var.redis_high_availability)
    error_message = "redis_high_availability must be Enabled or Disabled."
  }
}
