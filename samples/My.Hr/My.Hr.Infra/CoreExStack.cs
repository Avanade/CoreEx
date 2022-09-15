using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using My.Hr.Infra.Services;
using Pulumi;
using Pulumi.AzureNative.Resources;
using AD = Pulumi.AzureAD;

namespace My.Hr.Infra;

public static class CoreExStack
{
    public static async Task<IDictionary<string, object?>> ExecuteStackAsync(IDbOperations dbOperations, HttpClient client)
    {
        var config = await StackConfiguration.CreateConfiguration();
        Log.Info("Configuration completed");

        var tags = new InputMap<string> { { "App", "CoreEx" } };

        // Create Azure API client for direct HTTP calls
        var azureApiClient = new AzureApiClient(client);
        var azureApiService = new AzureApiService(azureApiClient);

        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup($"coreEx-{Pulumi.Deployment.Instance.StackName}", new ResourceGroupArgs
        {
            Tags = tags
        });

        var serviceBus = new Components.Messaging("coreExBus", new Components.Messaging.MessagingArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Tags = tags
        });
        serviceBus.AddQueue(config.PendingVerificationsQueue);
        serviceBus.AddQueue(config.VerificationResultsQueue!);
        serviceBus.AddQueue(config.MassPublishQueue, batchOperationsEnabled: true);

        var storage = new Components.Storage("sa", new Components.Storage.StorageArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Tags = tags
        });

        var appInsights = new Components.Diagnostics("insights", new Components.Diagnostics.DiagnosticsArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Tags = tags
        });

        var sql = new Components.Sql("sql", new Components.Sql.SqlArgs
        {
            ResourceGroupName = resourceGroup.Name,
            SqlAdAdminLogin = config.SqlAdAdminLogin!,
            SqlAdAdminPassword = config.SqlAdAdminPassword!,
            IsDBSchemaDeploymentEnabled = config.IsDBSchemaDeploymentEnabled,
            Tags = tags
        }, dbOperations);

        var apps = new Components.Apps("apps", new Components.Apps.FunctionArgs
        {
            ResourceGroupName = resourceGroup.Name,
            StorageAccountName = storage.AccountName,
            StorageDeploymentContainerName = storage.DeploymentContainerName,
            ServiceBusNamespaceName = serviceBus.NamespaceName,
            SqlConnectionString = sql.SqlDatabaseConnectionString,
            ApplicationInsightsInstrumentationKey = appInsights.InstrumentationKey,
            PendingVerificationsQueue = config.PendingVerificationsQueue,
            VerificationResultsQueue = config.VerificationResultsQueue,
            MassPublishQueue = config.MassPublishQueue,
            IsAppDeploymentEnabled = config.IsAppsDeploymentEnabled,
            Tags = tags
        }, azureApiService);

        // Permissions for function app
        storage.AddAccess(apps.FunctionPrincipalId, "functionApp");
        serviceBus.AddAccess(apps.FunctionPrincipalId, "functionApp");

        // Permissions for app service
        storage.AddAccess(apps.AppPrincipalId, "appService");
        serviceBus.AddAccess(apps.AppPrincipalId, "appService");

        // allow app and function to query/use DB
        sql.AddToSqlDatabaseAuthorizedGroup("functionGroupMember", apps.FunctionPrincipalId);
        sql.AddToSqlDatabaseAuthorizedGroup("appGroupMember", apps.AppPrincipalId);

        // allow app and function through SQL firewall
        sql.AddFirewallRule(apps.FunctionOutboundIps, "appService");
        sql.AddFirewallRule(apps.AppOutboundIps, "appService");

        return new Dictionary<string, object?>
        {
            ["SqlDatabaseConnectionString"] = sql.SqlDatabaseConnectionString,
            ["FunctionHealthUrl"] = apps.FunctionHealthUrl,
            ["FunctionSwaggerUrl"] = apps.FunctionSwaggerUrl,
            ["AppSwaggerUrl"] = apps.AppSwaggerUrl,
        };
    }

    public class StackConfiguration
    {
        public Input<string>? SqlAdAdminLogin { get; private set; }
        public Input<string>? SqlAdAdminPassword { get; private set; }
        public bool IsAppsDeploymentEnabled { get; private set; }
        public bool IsDBSchemaDeploymentEnabled { get; private set; }
        public string PendingVerificationsQueue { get; private set; } = default!;
        public string VerificationResultsQueue { get; private set; } = default!;
        public string MassPublishQueue { get; private set; } = default!;

        private StackConfiguration() { }

        public static async Task<StackConfiguration> CreateConfiguration()
        {
            // read stack config
            var config = new Config();

            // get some info from Azure AD
            var domainResult = await AD.GetDomains.InvokeAsync(new AD.GetDomainsArgs { OnlyDefault = true });
            var defaultUsername = $"sqlGlobalAdAdmin{Pulumi.Deployment.Instance.StackName}@{domainResult.Domains[0].DomainName}";
            var defaultPassword = new Pulumi.Random.RandomPassword("sqlAdPassword", new()
            {
                Length = 32,
                Upper = true,
                Number = true,
                Special = true,
                OverrideSpecial = "@",
                MinLower = 2,
                MinUpper = 2,
                MinSpecial = 2,
                MinNumeric = 2
            }).Result;

            Log.Info($"Default username is: {defaultUsername}");

            return new StackConfiguration
            {
                SqlAdAdminLogin = Extensions.GetConfigValue("sqlAdAdmin", defaultUsername),
                SqlAdAdminPassword = Extensions.GetConfigValue("sqlAdPassword", defaultPassword),
                IsAppsDeploymentEnabled = config.GetBoolean("isAppsDeploymentEnabled") ?? false,
                IsDBSchemaDeploymentEnabled = config.GetBoolean("isDBSchemaDeploymentEnabled") ?? false,

                PendingVerificationsQueue = config.Get("pendingVerificationsQueue") ?? "pendingVerifications",
                VerificationResultsQueue = config.Get("verificationResultsQueue") ?? "verificationResults",
                MassPublishQueue = config.Get("massPublishQueue") ?? "massPublish"
            };
        }
    }
}