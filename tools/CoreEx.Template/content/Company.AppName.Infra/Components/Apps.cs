using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Company.AppName.Infra.Services;
using Pulumi;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using AzureNative = Pulumi.AzureNative;

namespace Company.AppName.Infra.Components;

public class Apps : ComponentResource
{
    private readonly FunctionArgs args;

    public Output<string> FunctionHealthUrl { get; } = default!;
    public Output<string> AppHealthUrl { get; } = default!;
    public Output<string> FunctionSwaggerUrl { get; } = default!;
    public Output<string> AppSwaggerUrl { get; } = default!;
    public Output<string> FunctionPrincipalId { get; } = default!;
    public Output<string> AppPrincipalId { get; } = default!;
    public Output<string> FunctionOutboundIps { get; } = default!;
    public Output<string> AppOutboundIps { get; } = default!;
    public Output<string> AppServiceName { get; } = default!;
    public Output<string> FunctionName { get; } = default!;

    public Apps(string name, FunctionArgs args, AzureApiService azureApiService, ComponentResourceOptions? options = null)
        : base("Company:AppName:web:apps", name, options)
    {
        this.args = args;

        // publish app and push zip packages to blob storage when app deployment is done via pulumi
        Output<Output<(string appZipUrl, string funZipUrl)>> packageZips = args.IsAppDeploymentEnabled.Apply(async isEnabled =>
        {
            if (isEnabled)
            {
                await PublishApp();

                var appZipUrl = PrepareAppForDeployment("app", "../Company.AppName.Api/bin/Release/net6.0/publish");
                var funZipUrl = PrepareAppForDeployment("function", "../Company.AppName.Functions/bin/Release/net6.0/publish");

                return Output.Tuple(appZipUrl, funZipUrl);
            }

            return Output.Create((string.Empty, string.Empty));
        });

        var packageUrls = packageZips.Apply(t => t);

        // https://github.com/pulumi/examples/blob/master/azure-cs-functions/FunctionsStack.cs
        var appServicePlan = new AppServicePlan("apps-linux-asp", new()
        {
            HyperV = false,
            IsSpot = false,
            IsXenon = false,
            Kind = "Linux", // what kinds are supported? "app" is one of them
            MaximumElasticWorkerCount = 1,
            PerSiteScaling = false,

            // For Linux, you need to change the plan to have Reserved = true property.
            Reserved = true,

            ResourceGroupName = args.ResourceGroupName,
            Sku = new SkuDescriptionArgs
            {
                Capacity = 1,
                Family = "B1",
                Name = "B1",
                Size = "B1",
                Tier = "Basic",
            },
            TargetWorkerCount = 0,
            TargetWorkerSizeId = 0,

            Tags = args.Tags
        }, new CustomResourceOptions { Parent = this });

        var app = new WebApp("app", new WebAppArgs
        {
            ResourceGroupName = args.ResourceGroupName,
            HttpsOnly = true,
            ServerFarmId = appServicePlan.Id,
            Identity = new ManagedServiceIdentityArgs { Type = ManagedServiceIdentityType.SystemAssigned },

            SiteConfig = new SiteConfigArgs
            {
                LinuxFxVersion = "DOTNETCORE|6.0",
                AppSettings = new[]
                    {
                    new NameValuePairArgs{
                        Name = "WEBSITE_RUN_FROM_PACKAGE",
                        // set to 1 if app is going to be deployed separately 
                        Value = args.IsAppDeploymentEnabled.Apply(isEnabled => isEnabled ? packageUrls.Apply(p => p.appZipUrl) : Output.Create("1"))
                    },
                    new NameValuePairArgs{
                        Name = "AzureWebJobsStorage__accountName",
                        Value = args.StorageAccountName
                    },
                    new NameValuePairArgs{
                        Name = "APPINSIGHTS_INSTRUMENTATIONKEY",
                        Value = args.ApplicationInsightsInstrumentationKey
                    },
                    new NameValuePairArgs{
                        Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                        Value = args.ApplicationInsightsInstrumentationKey.Apply(key => $"InstrumentationKey={key}"),
                    },
                    new NameValuePairArgs{
                        Name = "ApplicationInsightsAgent_EXTENSION_VERSION",
                        Value = "~2",
                    },
                    new NameValuePairArgs{
                        Name = "ServiceBusConnection__fullyQualifiedNamespace",
                        Value = Output.Format($"{args.ServiceBusNamespaceName}.servicebus.windows.net"),
                    },
                    new NameValuePairArgs{
                        Name = "HttpLogContent",
                        Value = "true",
                    },
                    new NameValuePairArgs{
                        Name = "AzureFunctionsJobHost__logging__logLevel__CoreEx",
                        Value = "Debug",
                    },
                    new NameValuePairArgs{
                        Name = "ConnectionStrings__Database",
                        Value = args.SqlConnectionString,
                    },
                    new NameValuePairArgs{
                        Name = "VerificationQueueName",
                        Value = args.VerificationResultsQueue,
                    },
                },
            },
            Tags = args.Tags,
        }, new CustomResourceOptions { Parent = this });

        var functionApp = new WebApp("funApp", new WebAppArgs
        {
            Kind = "FunctionApp",
            ResourceGroupName = args.ResourceGroupName,
            ServerFarmId = appServicePlan.Id,
            HttpsOnly = true,
            Identity = new ManagedServiceIdentityArgs { Type = AzureNative.Web.ManagedServiceIdentityType.SystemAssigned },
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                    {
                    new NameValuePairArgs{
                        Name = "AzureWebJobsStorage__accountName",
                        Value = args.StorageAccountName
                    },
                    new NameValuePairArgs{
                        Name = "FUNCTIONS_EXTENSION_VERSION",
                        Value = "~4",
                    },
                    new NameValuePairArgs{
                        Name = "FUNCTIONS_WORKER_RUNTIME",
                        Value = "dotnet",
                    },
                    new NameValuePairArgs{
                        Name = "WEBSITE_RUN_FROM_PACKAGE",
                        // set to 1 if app is going to be deployed separately 
                        Value = args.IsAppDeploymentEnabled.Apply(isEnabled => isEnabled ? packageUrls.Apply(p => p.funZipUrl) : Output.Create("1"))
                    },
                    new NameValuePairArgs{
                        Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                        Value = Output.Format($"InstrumentationKey={args.ApplicationInsightsInstrumentationKey}"),
                    },
                    new NameValuePairArgs{
                        Name = "ServiceBusConnection__fullyQualifiedNamespace",
                        Value = Output.Format($"{args.ServiceBusNamespaceName}.servicebus.windows.net"),
                    },
                    new NameValuePairArgs{
                        Name = "AgifyApiEndpointUri",
                        Value = "https://api.agify.io",
                    },
                    new NameValuePairArgs{
                        Name = "NationalizeApiClientApiEndpointUri",
                        Value = "https://api.nationalize.io",
                    },
                    new NameValuePairArgs{
                        Name = "GenderizeApiClientApiEndpointUri",
                        Value = "https://api.genderize.io",
                    },
                    new NameValuePairArgs{
                        Name = "VerificationQueueName",
                        Value = args.VerificationResultsQueue,
                    },
                    new NameValuePairArgs{
                        Name = "VerificationResultsQueueName",
                        Value = args.VerificationResultsQueue,
                    },
                    new NameValuePairArgs{
                        Name = "MassPublishQueueName",
                        Value = args.MassPublishQueue,
                    },
                    new NameValuePairArgs{
                        Name = "HttpLogContent",
                        Value = "true",
                    },
                    new NameValuePairArgs{
                        Name = "AzureFunctionsJobHost__logging__logLevel__CoreEx",
                        Value = "Debug",
                    },
                    new NameValuePairArgs{
                        Name = "ConnectionStrings__Database",
                        Value = args.SqlConnectionString,
                    },
                },
            },
            Tags = args.Tags,
        }, new CustomResourceOptions
        {
            Parent = this,
            CustomTimeouts = new CustomTimeouts
            {
                Create = TimeSpan.FromMinutes(4)
            }
        });

        FunctionPrincipalId = functionApp.Identity.Apply(identity => identity?.PrincipalId ?? "11111111-1111-1111-1111-111111111111");
        AppPrincipalId = app.Identity.Apply(identity => identity?.PrincipalId ?? "11111111-1111-1111-1111-111111111111");

        FunctionOutboundIps = functionApp.OutboundIpAddresses;
        AppOutboundIps = app.OutboundIpAddresses;

        var functionKey = Output.CreateSecret(azureApiService.GetHostKeys(args.ResourceGroupName, functionApp.Name));

        // sync function app service triggers
        azureApiService.SyncFunctionAppTriggers(args.ResourceGroupName, functionApp.Name);

        FunctionHealthUrl = Output.Format($"https://{functionApp.DefaultHostName}/api/health?code={functionKey}");
        AppHealthUrl = Output.Format($"https://{app.DefaultHostName}/api/health");
        FunctionSwaggerUrl = Output.Format($"https://{functionApp.DefaultHostName}/api/swagger/ui?code={functionKey}");
        AppSwaggerUrl = Output.Format($"https://{app.DefaultHostName}/swagger/index.html");
        AppServiceName = app.Name;
        FunctionName = functionApp.Name;

        RegisterOutputs();
    }

    private static async Task PublishApp()
    {
        if (Deployment.Instance.IsDryRun)
        {
            Directory.CreateDirectory("../Company.AppName.Api/bin/Release/net6.0/publish");
            Directory.CreateDirectory("../Company.AppName.Functions/bin/Release/net6.0/publish");
            return;
        }

        Log.Info("Setting up deployments from zip for the app and function and executing [dotnet publish]");

        var sw = Stopwatch.StartNew();
        var publishProcess = Process.Start(new ProcessStartInfo
        {
            WorkingDirectory = "../",
            FileName = "dotnet",
            Arguments = "publish --nologo -c RELEASE",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        await publishProcess!.WaitForExitAsync();
        sw.Stop();
        Log.Info($"[dotnet publish] completed in {sw.Elapsed}");
    }

    private Output<string> PrepareAppForDeployment(string name, string path)
    {
        var blob = new Blob($"{name}_zip", new BlobArgs
        {
            AccountName = args.StorageAccountName,
            ContainerName = args.StorageDeploymentContainerName,
            ResourceGroupName = args.ResourceGroupName,
            Type = BlobType.Block,
            Source = new FileArchive(path)
        }, new CustomResourceOptions { Parent = this });

        var codeBlobUrl = Output.Format($"https://{args.StorageAccountName}.blob.core.windows.net/{args.StorageDeploymentContainerName}/{blob.Name}");

        return codeBlobUrl;
    }

    public class FunctionArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public Input<string> StorageAccountName { get; set; } = default!;
        public Input<string> ServiceBusNamespaceName { get; set; } = default!;
        public Input<string> SqlConnectionString { get; set; } = default!;
        public Input<string> ApplicationInsightsInstrumentationKey { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
        public string PendingVerificationsQueue { get; set; } = default!;
        public string VerificationResultsQueue { get; set; } = default!;
        public string MassPublishQueue { get; set; } = default!;
        public Input<bool> IsAppDeploymentEnabled { get; set; } = default!;
        public Input<string> StorageDeploymentContainerName { get; set; } = default!;
    }
}