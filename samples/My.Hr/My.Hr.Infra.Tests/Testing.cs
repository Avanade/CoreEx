using System.Collections.Immutable;
using System.Text.Json;
using My.Hr.Infra.Services;
using UnitTestEx.NUnit;

namespace My.Hr.Infra.Tests;

public static class Testing
{
    // mocked subscription Id
    public const string SubscriptionId = "622637dd-a3e9-4e54-test-56d9247c70ee";
    public const string StackName = "unit-test-stack";

    public static async Task<(ImmutableArray<Resource> Resources, IDictionary<string, object?> Outputs, Mock<IDbOperations>)> RunAsync()
    {
        var dbOperationsMock = new Mock<IDbOperations>();
        var mcf = MockHttpClientFactory.Create();
        var mc = mcf.CreateClient("azure");

        mc.Request(HttpMethod.Post, $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/coreEx-{StackName}/providers/Microsoft.Web/sites/funApp/host/default/listkeys?api-version=2022-03-01")
            .Respond.WithJson(new AzureApiService.HostKeys { FunctionKeys = new AzureApiService.FunctionKeysValue { Key = "mocked-key" } });

        mc.Request(HttpMethod.Get, "https://api.ipify.org")
            .Respond.With(new StringContent("215.45.1.567"));

        mc.Request(HttpMethod.Post, $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/coreEx-{StackName}/providers/Microsoft.Web/sites/funApp/syncfunctiontriggers?api-version=2016-08-01")
            .Respond.With(statusCode: System.Net.HttpStatusCode.NoContent);

        var (resources, outputs) = await RunAsync(() => CoreExStack.ExecuteStackAsync(dbOperationsMock.Object, mcf.GetHttpClient("azure")!));

        return (resources, outputs, dbOperationsMock);
    }

    public static async Task<(ImmutableArray<Resource> Resources, IDictionary<string, object?> Outputs)> RunAsync(Func<Task<IDictionary<string, object?>>> createResources)
    {
        var config = new Dictionary<string, object>{
            {"unittest:sqlAdAdmin", "sqlAdAdmin"},
            {"unittest:sqlAdPassword", "sqlAdPassword"},
            {"unittest:isAppsDeploymentEnabled", "true"},
            {"unittest:isDBSchemaDeploymentEnabled", "true"}
        };

        Environment.SetEnvironmentVariable("PULUMI_CONFIG", JsonSerializer.Serialize(config));
        var mocks = new Mocks();
        var dbOperationsMock = new Mock<IDbOperations>();

        TestOptions options = new()
        {
            IsPreview = false,
            ProjectName = "unittest",
            StackName = StackName
        };

        var (resources, outputs) = await Deployment.TestAsync(mocks, options, () => createResources());

        return (resources, outputs);
    }

    public static async Task<T> RunAsync<T>(Func<T> createResources) where T : class
    {
        var (resources, outputs) = await RunAsync(() =>
        {
            var result = createResources();
            return Task.FromResult<IDictionary<string, object?>>(new Dictionary<string, object?>
            {
                ["result"] = result
            });
        });

        return (T)outputs["result"]!;
    }

    public class Mocks : IMocks
    {
        public Task<object> CallAsync(MockCallArgs args)
        {
            var outputs = ImmutableDictionary.CreateBuilder<string, object>();
            outputs.AddRange(args.Args);

            switch (args.Token)
            {
                case "azure:keyvault/getKeyVault:getKeyVault":
                    outputs.Add("id", Guid.NewGuid().ToString());
                    break;

                // case "azure-native:web:listWebAppHostKeys":
                //     outputs.Add("masterKey", "key");
                //     break;

                case "azure-native:storage:listStorageAccountKeys":
                    var kvJson = JsonDocument.Parse("[{\"value\":\"valueKeyStorage\"}]").RootElement;
                    outputs.Add("keys", kvJson);
                    break;

                case "azuread:index/getDomains:getDomains":
                    var adJson = JsonDocument.Parse("[{\"domainName\":\"myDomain.onmicrosoft.com\"}]").RootElement;
                    outputs.Add("domains", adJson);
                    break;

                case "azure-native:authorization:getClientConfig":
                    outputs.Add("subscriptionId", SubscriptionId);
                    break;

                case "azure-native:authorization:getClientToken":
                    outputs.Add("token", "not-a-real-token");
                    break;

                default:
                    throw new InvalidOperationException($"Operation {args.Token} is not supported. Fix your mock setup.");
            }

            return Task.FromResult((object)outputs);
        }

        public Task<(string? id, object state)> NewResourceAsync(MockResourceArgs args)
        {
            var outputs = ImmutableDictionary.CreateBuilder<string, object>();

            // Forward all input parameters as resource outputs, so that we could test them.
            outputs.AddRange(args.Inputs);

            // Set the name to resource name if it's not set explicitly in inputs.
            if (!args.Inputs.ContainsKey("name"))
                outputs.Add("name", args.Name ?? "name");

            // <-- We'll customize the mocks here
            if (args.Type == "azure-native:web:WebApp")
            {
                outputs["outboundIpAddresses"] = "192.167.12.1,24.56.76.1";
                outputs["defaultHostName"] = "unittest.azurewebsites.net";
            }

            // Default the resource ID to `{name}_id`.
            string? id = $"{args.Id}_{args.Name}_id";
            return Task.FromResult<(string? id, object state)>((id, state: outputs));
        }
    }
}