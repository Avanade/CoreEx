using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Polly.Extensions.Http;
using Pulumi;
using Pulumi.AzureNative.Authorization;

namespace My.Hr.Infra.Services;

public class AzureApiService
{

    public AzureApiClient ApiClient { get; private set; }

    public AzureApiService(AzureApiClient apiClient)
    {
        ApiClient = apiClient;
    }

    /// <summary>
    /// Gets role id based on the provided role name. This method can be used instead of hardcoded role Ids above
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown when request fails</exception>
    /// <other>code from: https://github.com/pulumi/examples/blob/28b559d68eb6a67f3e6b5fb3d2a337b5b9ed35d5/azure-cs-call-azure-api/Program.cs#L45 </other>
    public async Task<string> GetRoleIdByName(string roleName, string? scope = null)
    {
        var config = await GetClientConfig.InvokeAsync();

        var result = await ApiClient
            .ThrowTransientException()
            .WithRetry()
            .GetAsync<RoleDefinition>($"https://management.azure.com/subscriptions/{config.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions?api-version=2018-01-01-preview&$filter=roleName%20eq%20'{roleName}'");

        return result.Value.Value[0].Id;
    }

    public Output<string> GetHostKeys(Output<string> rgName, Output<string> functionName)
    {
        return Output.Tuple(rgName, functionName).Apply(async t =>
        {
            var (resourceGroupName, siteName) = t;

            var config = await GetClientConfig.InvokeAsync();

            var result = await ApiClient
                .WithRetry(count: 4, seconds: 5)
                // Azure returns 400 BadRequest when Function App is not ready
                .WithCustomRetryPolicy(HttpPolicyExtensions.HandleTransientHttpError().OrResult(http => http.StatusCode == System.Net.HttpStatusCode.BadRequest || http.StatusCode == System.Net.HttpStatusCode.NotFound))
                .PostAsync<HostKeys>($"https://management.azure.com/subscriptions/{config.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}/host/default/listkeys?api-version=2022-03-01");

            return result.Value.FunctionKeys.Key;
        });
    }

    public async Task SyncFunctionAppTriggers(string hostName, string functionKey)
    {
        Log.Info("Syncing Function App triggers");
        var syncUrl = $"https://{hostName}/admin/host/synctriggers?code={functionKey}";

        await ApiClient
            .WithRetry()
            .ThrowTransientException()
            .PostAsync(syncUrl);
    }

    public class HostKeys
    {
        [JsonPropertyName("masterKey")]
        public string MasterKey { get; set; } = default!;

        [JsonPropertyName("functionKeys")]
        public FunctionKeysValue FunctionKeys { get; set; } = default!;
    }

    public class FunctionKeysValue
    {
        [JsonPropertyName("default")]
        public string Key { get; set; } = default!;
    }

    public class RoleDefinition
    {
        [JsonPropertyName("value")]
        public List<RoleDefinitionValue> Value { get; set; } = default!;
    }
    public class RoleDefinitionValue
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
    }
}