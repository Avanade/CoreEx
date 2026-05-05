resource_group_name = "rg-test"
environment_type    = "test"
location            = "eastus"
name_suffix         = "test01"

tags = {
  workload    = "coreex"
  environment = "test"
}

app_service_plan_sku_name = "B1"
app_service_plan_sku_tier = "Basic"
app_service_plan_capacity = 1

# Derived from AZD_DOTNET_TARGET_FRAMEWORK by apply script.
app_service_linux_fx_version = "DOTNETCORE|10.0"

service_bus_sku_name = "Standard"

sql_admin_login      = "coreexadmin"
sql_database_name    = "coreextest"
sql_sku_name         = "GP_S_Gen5_1"
sql_sku_tier         = "GeneralPurpose"
sql_min_capacity     = 0.5
sql_auto_pause_delay = 60

# Derived from current public IP by apply script.
sql_firewall_client_ip = ""

redis_sku_name          = "Balanced_B0"
redis_high_availability = "Enabled"
