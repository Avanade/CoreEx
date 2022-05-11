using CoreEx.Http;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using My.Hr.Business.ServiceContracts;

namespace My.Hr.Business.Services;

/// <summary>
/// Http client for https://genderize.io/
/// </summary>
public class GenderizeApiClient : TypedHttpClientCore<GenderizeApiClient>
{
    public GenderizeApiClient(HttpClient client, IJsonSerializer jsonSerializer, CoreEx.ExecutionContext executionContext, HrSettings settings, ILogger<TypedHttpClientCore<GenderizeApiClient>> logger)
            : base(client, jsonSerializer, executionContext, settings, logger)
    {
        if (!Uri.IsWellFormedUriString(settings.GenderizeApiClientApiEndpointUri, UriKind.Absolute))
            throw new InvalidOperationException(@$"The Api endpoint URI is not valid: {settings.GenderizeApiClientApiEndpointUri}. Provide valid Api endpoint URI in the configuration '{nameof(settings.GenderizeApiClientApiEndpointUri)}'.
                        If Api Client is not needed - remove all references to {nameof(GenderizeApiClient)}.");

        client.BaseAddress = new Uri(settings.GenderizeApiClientApiEndpointUri);
    }

    public override Task<HttpResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return base.HeadAsync(string.Empty, null, new HttpArg<string>("name", "health"));
    }

    public async Task<GenderizeResponse> GetGenderAsync(string name)
    {
        var response = await
            WithRetry(1, 5)
            .ThrowTransientException()
            .GetAsync<GenderizeResponse>(string.Empty, null, new HttpArg<string>("name", name));

        return response.Value;
    }
}