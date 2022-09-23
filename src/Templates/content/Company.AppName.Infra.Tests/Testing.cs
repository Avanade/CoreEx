using System.Collections.Immutable;
using System.Text.Json;
using Company.AppName.Infra.Services;

namespace Company.AppName.Infra.Tests;

public static class Testing
{
    public static async Task<(ImmutableArray<Resource> Resources, IDictionary<string, object?> Outputs, Mock<IDbOperations>)> RunAsync()
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
            ProjectName = "unittest"
        };

        var (resources, outputs) = await Deployment.TestAsync(mocks, options, () => CoreExStack.ExecuteStackAsync(dbOperationsMock.Object));

        return (resources, outputs, dbOperationsMock);
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

                case "azure-native:web:listWebAppHostKeys":
                    outputs.Add("masterKey", "key");
                    break;

                case "azure-native:storage:listStorageAccountKeys":
                    var kvJson = JsonDocument.Parse("[{\"value\":\"valueKeyStorage\"}]").RootElement;
                    outputs.Add("keys", kvJson);
                    break;

                case "azuread:index/getDomains:getDomains":
                    var adJson = JsonDocument.Parse("[{\"domainName\":\"myDomain.onmicrosoft.com\"}]").RootElement;
                    outputs.Add("domains", adJson);
                    break;

                default:
                    throw new InvalidOperationException($"Operation {args.Token} is not supported");
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