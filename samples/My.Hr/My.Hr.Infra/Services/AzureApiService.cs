using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;
using Pulumi;
using Pulumi.AzureNative.Authorization;

namespace My.Hr.Infra.Services;

public class AzureApiService
{
    private readonly AzureApiClient apiClient;

    public AzureApiService(AzureApiClient apiClient)
    {
        this.apiClient = apiClient;
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
        var token = await GetClientToken.InvokeAsync();

        // Unfortunately, Microsoft hasn't shipped an .NET5-compatible SDK at the time of writing this.
        // So, we have to hand-craft an HTTP request to retrieve a role definition.
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        var response = await httpClient.GetAsync($"https://management.azure.com/subscriptions/{config.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions?api-version=2018-01-01-preview&$filter=roleName%20eq%20'{roleName}'");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with {response.StatusCode}");
        }
        var body = await response.Content.ReadAsStringAsync();
        var definition = JsonSerializer.Deserialize<RoleDefinition>(body)!;
        return definition.Value[0].Id;
    }


    public Output<string> GetHostKeys(Output<string> rgName, Output<string> functionName)
    {
        return Output.Tuple(rgName, functionName).Apply(async t =>
        {
            var (resourceGroupName, siteName) = t;

            var config = await GetClientConfig.InvokeAsync();
            var token = await GetClientToken.InvokeAsync();

            // var client = new AzureApiClient(httpClient);
            // await client
            //     .WithRetry(count: 4, seconds: 20)
            //     .PostAsync($"https://management.azure.com/subscriptions/{config.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}/host/default/listkeys?api-version=2022-03-01");

            var httpResponse = await HttpPolicyExtensions.HandleTransientHttpError().OrResult(result => result.StatusCode == System.Net.HttpStatusCode.BadRequest)
               .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(4, retryAttempt)))
                .ExecuteAsync(async () =>
                {
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                    Log.Info("Trying to get host keys from Azure");
                    return await httpClient.PostAsync($"https://management.azure.com/subscriptions/{config.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}/host/default/listkeys?api-version=2022-03-01", null);

                }).ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            var body = await httpResponse.Content.ReadAsStringAsync();
            var definition = JsonSerializer.Deserialize<HostKeys>(body)!;

            return definition.FunctionKeys.Key;
        });
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