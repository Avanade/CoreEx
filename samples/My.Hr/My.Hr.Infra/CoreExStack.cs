using System.Collections.Generic;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.AzureNative.Resources;
using AD = Pulumi.AzureAD;

namespace CoreEx.Infra;

public class CoreExStack
{
    public async Task<IDictionary<string, object?>> ExecuteStackAsync()
    {
        var config = await StackConfiguration.CreateConfiguration();
        Log.Info("Configuration completed");

        var tags = new InputMap<string> { { "App", "CoreEx" } };

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
        serviceBus.AddQueue(config.pendingVerificationsQueue);
        serviceBus.AddQueue(config.verificationResultsQueue!);
        serviceBus.AddQueue(config.massPublishQueue, batchOperationsEnabled: true);

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
            SqlAdAdminLogin = config.sqlAdAdminLogin!,
            SqlAdAdminPassword = config.sqlAdAdminPassword!,
            IsDBSchemaDeploymentEnabled = config.isDBSchemaDeploymentEnabled,
            Tags = tags
        });

        var apps = new Components.Apps("apps", new Components.Apps.FunctionArgs
        {
            ResourceGroupName = resourceGroup.Name,
            StorageAccountName = storage.AccountName,
            StorageDeploymentContainerName = storage.DeploymentContainerName,
            ServiceBusNamespaceName = serviceBus.NamespaceName,
            SqlConnectionString = sql.SqlDatabaseConnectionString,
            ApplicationInsightsInstrumentationKey = appInsights.InstrumentationKey,
            PendingVerificationsQueue = config.pendingVerificationsQueue,
            VerificationResultsQueue = config.verificationResultsQueue,
            MassPublishQueue = config.massPublishQueue,
            IsAppDeploymentEnabled = config.isAppsDeploymentEnabled,
            Tags = tags
        });

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
        public Input<string>? sqlAdAdminLogin { get; private set; }
        public Input<string>? sqlAdAdminPassword { get; private set; }
        public bool isAppsDeploymentEnabled { get; private set; }
        public bool isDBSchemaDeploymentEnabled { get; private set; }
        public string pendingVerificationsQueue { get; private set; } = default!;
        public string verificationResultsQueue { get; private set; } = default!;
        public string massPublishQueue { get; private set; } = default!;

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

            Pulumi.Log.Info($"Default username is: {defaultUsername}");

            return new StackConfiguration
            {
                sqlAdAdminLogin = Extensions.GetConfigValue("sqlAdAdmin", defaultUsername),
                sqlAdAdminPassword = Extensions.GetConfigValue("sqlAdPassword", defaultPassword),
                isAppsDeploymentEnabled = config.GetBoolean("isAppsDeploymentEnabled") ?? false,
                isDBSchemaDeploymentEnabled = config.GetBoolean("isDBSchemaDeploymentEnabled") ?? false,

                pendingVerificationsQueue = config.Get("pendingVerificationsQueue") ?? "pendingVerifications",
                verificationResultsQueue = config.Get("verificationResultsQueue") ?? "verificationResults",
                massPublishQueue = config.Get("massPublishQueue") ?? "massPublish"
            };
        }
    }
}