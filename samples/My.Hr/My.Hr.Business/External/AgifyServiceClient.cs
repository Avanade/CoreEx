namespace My.Hr.Business.External;

/// <summary>
/// Http client for https://agify.io/
/// </summary>
public class AgifyApiClient : TypedHttpClientCore<AgifyApiClient>
{
    public AgifyApiClient(HttpClient client, IJsonSerializer jsonSerializer, CoreEx.ExecutionContext executionContext, HrSettings settings)
            : base(client, jsonSerializer, executionContext)
    {
        if (!Uri.IsWellFormedUriString(settings.AgifyApiEndpointUri, UriKind.Absolute))
            throw new InvalidOperationException(@$"The Api endpoint URI is not valid: {settings.AgifyApiEndpointUri}. Provide valid Api endpoint URI in the configuration '{nameof(settings.AgifyApiEndpointUri)}'.
                        If Api Client is not needed - remove all references to {nameof(AgifyApiClient)}.");

        client.BaseAddress = new Uri(settings.AgifyApiEndpointUri);
    }

    public override Task<HttpResult> HealthCheckAsync(CancellationToken cancellationToken)
    {
        return base.HeadAsync(string.Empty, null, new HttpArg<string>[] { new("name", "health") }, cancellationToken);
    }

    public async Task<AgifyResponse> GetAgeAsync(string name)
    {
        var response = await
            ThrowTransientException()
            .GetAsync<AgifyResponse>(string.Empty, null, HttpArgs.Create(new HttpArg<string>("name", name)));

        return response.Value;
    }
}