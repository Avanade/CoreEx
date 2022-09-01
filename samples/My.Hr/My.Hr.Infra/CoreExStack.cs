using Pulumi;
using Pulumi.AzureNative.Resources;
using AD = Pulumi.AzureAD;

namespace CoreEx.Infra;

public class CoreExStack : Stack
{
    public CoreExStack()
    {
        // read stack config
        var config = new Config();

        // get some info from Azure AD
        var domainResult = AD.GetDomains.Invoke(new AD.GetDomainsInvokeArgs { OnlyDefault = true });
        var defaultUsername = Output.Format($"sqlGlobalAdAdmin@{domainResult.Apply(d => d.Domains[0].DomainName)}");
        var defaultPassword = new Pulumi.Random.RandomPassword("sqlAdPassword", new()
        {
            Length = 16,
            Special = true,
            OverrideSpecial = "@",
        }).Result;

        Input<string> sqlAdAdminLogin = Extensions.GetConfigValue("sqlAdAdmin", defaultUsername);
        Input<string> sqlAdAdminPassword = Extensions.GetConfigValue("sqlAdPassword", defaultPassword);
        Input<bool> isAppsDeploymentEnabled = config.GetBoolean("isAppsDeploymentEnabled") ?? false;

        var pendingVerificationsQueue = config.Get("pendingVerificationsQueue") ?? "pendingVerifications";
        var verificationResultsQueue = config.Get("verificationResultsQueue") ?? "verificationResults";
        var massPublishQueue = config.Get("massPublishQueue") ?? "massPublish";

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
        serviceBus.AddQueue(pendingVerificationsQueue);
        serviceBus.AddQueue(verificationResultsQueue);
        serviceBus.AddQueue(massPublishQueue, batchOperationsEnabled: true);

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
            SqlAdAdminLogin = sqlAdAdminLogin,
            SqlAdAdminPassword = sqlAdAdminPassword,
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
            PendingVerificationsQueue = pendingVerificationsQueue,
            VerificationResultsQueue = verificationResultsQueue,
            MassPublishQueue = massPublishQueue,
            IsAppDeploymentEnabled = isAppsDeploymentEnabled,
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

        SqlDatabaseConnectionString = sql.SqlDatabaseConnectionString;
        FunctionHealthUrl = apps.FunctionHealthUrl;
        FunctionSwaggerUrl = apps.FunctionSwaggerUrl;
        AppSwaggerUrl = apps.AppSwaggerUrl;
    }

    [Output]
    public Output<string> SqlDatabaseConnectionString { get; set; }

    [Output]
    public Output<string> AppSwaggerUrl { get; set; }

    [Output]
    public Output<string> FunctionHealthUrl { get; set; }

    [Output]
    public Output<string> FunctionSwaggerUrl { get; set; }
}