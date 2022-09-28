using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Configuration;
using CoreEx.Http;
using Microsoft.Extensions.Configuration;

namespace Company.AppName.Infra.Services;

/// <summary>
/// Http client for Azure APIs
/// </summary>
public class AzureApiClient : TypedHttpClientCore<AzureApiClient>
{
    public AzureApiClient(HttpClient client)
        : base(client, CoreEx.Json.JsonSerializer.Default, new CoreEx.ExecutionContext(), new DefaultSettings(new ConfigurationBuilder().Build()), PulumiLogger<TypedHttpClientCore<AzureApiClient>>.Instance)
    {
    }

    protected override async Task OnBeforeRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await Pulumi.AzureNative.Authorization.GetClientToken.InvokeAsync();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        await base.OnBeforeRequest(request, cancellationToken);
    }

    public async Task<string> GetMyIP()
    {
        // use base client directly to skip OnBeforeRequest
        return await base.Client.GetStringAsync("https://api.ipify.org");
    }
}