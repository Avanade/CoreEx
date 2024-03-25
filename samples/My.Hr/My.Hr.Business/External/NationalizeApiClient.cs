namespace My.Hr.Business.External;

/// <summary>
/// Http client for https://nationalize.io/
/// </summary>
public class NationalizeApiClient : TypedHttpClientCore<NationalizeApiClient>
{
    public NationalizeApiClient(HttpClient client, IJsonSerializer jsonSerializer, CoreEx.ExecutionContext executionContext, HrSettings settings)
            : base(client, jsonSerializer, executionContext)
    {
        if (!Uri.IsWellFormedUriString(settings.NationalizeApiClientApiEndpointUri, UriKind.Absolute))
            throw new InvalidOperationException(@$"The Api endpoint URI is not valid: {settings.NationalizeApiClientApiEndpointUri}. Provide valid Api endpoint URI in the configuration '{nameof(settings.NationalizeApiClientApiEndpointUri)}'.
                        If Api Client is not needed - remove all references to {nameof(NationalizeApiClient)}.");

        client.BaseAddress = new Uri(settings.NationalizeApiClientApiEndpointUri);
    }

    public override Task<HttpResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return base.HeadAsync(string.Empty, null, HttpArgs.Create(new HttpArg<string>("name", "health")), cancellationToken);
    }

    public async Task<NationalizeResponse> GetNationalityAsync(string name)
    {
        var response = await
            ThrowTransientException()
            .GetAsync<NationalizeResponse>(string.Empty, null, HttpArgs.Create(new HttpArg<string>("name", name)));

        return response.Value;
    }
}