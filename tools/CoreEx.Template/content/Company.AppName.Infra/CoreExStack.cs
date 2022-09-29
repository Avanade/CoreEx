using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Company.AppName.Infra.Services;
using Pulumi;
using Pulumi.AzureNative.Resources;
using AD = Pulumi.AzureAD;

namespace Company.AppName.Infra;

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
        }, dbOperations, azureApiClient);

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

        // Developer group
        var devSetup = new Components.DevSetup("devs", config.DeveloperEmails);

        // Permissions for function app
        storage.AddAccess(apps.FunctionPrincipalId, "functionApp");
        serviceBus.AddAccess(apps.FunctionPrincipalId, "functionApp");

        // Permissions for app service
        storage.AddAccess(apps.AppPrincipalId, "appService");
        serviceBus.AddAccess(apps.AppPrincipalId, "appService");

        // Permissions for dev group
        storage.AddAccess(devSetup.DevelopersGroupId, "devGroup", principalType: "Group");
        serviceBus.AddAccess(devSetup.DevelopersGroupId, "devGroup", principalType: "Group");

        // allow app and function to query/use DB
        sql.AddToSqlDatabaseAuthorizedGroup("functionGroupMember", apps.FunctionPrincipalId);
        sql.AddToSqlDatabaseAuthorizedGroup("appGroupMember", apps.AppPrincipalId);

        // allow dev team to query/use DB
        sql.AddToSqlDatabaseAuthorizedGroup("devGroupMember", devSetup.DevelopersGroupId);

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
}