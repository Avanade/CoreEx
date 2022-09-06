using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pulumi.AzureNative.Authorization;

namespace CoreEx.Infra.Roles;

public static class BuiltInRolesIds
{
    public const string StorageBlobDataOwner = "/providers/Microsoft.Authorization/roleDefinitions/b7e6dc6d-f1e8-4753-8033-0f276bb0955b";

    // https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#azure-service-bus-data-receiver
    public const string ServiceBusDataReceiver = "/providers/Microsoft.Authorization/roleDefinitions/4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0";

    // https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#azure-service-bus-data-sender
    public const string ServiceBusDataSender = "/providers/Microsoft.Authorization/roleDefinitions/69a216fc-b8fb-44d8-bc22-1f3c2cd27a39";

    /// <summary>
    /// Gets role id based on the provided role name. This method can be used instead of hardcoded role Ids above
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown when request fails</exception>
    /// <other>code from: https://github.com/pulumi/examples/blob/28b559d68eb6a67f3e6b5fb3d2a337b5b9ed35d5/azure-cs-call-azure-api/Program.cs#L45 </other>
    public static async System.Threading.Tasks.Task<string> GetRoleIdByName(string roleName, string? scope = null)
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
        return definition.Value[0].id;
    }

    public class RoleDefinition
    {
        [JsonPropertyName("value")]
        public List<RoleDefinitionValue> Value { get; set; } = default!;
    }
    public class RoleDefinitionValue
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = default!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
    }
}