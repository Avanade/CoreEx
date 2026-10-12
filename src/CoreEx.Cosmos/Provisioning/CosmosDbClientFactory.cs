namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Provides a <see cref="CosmosClient"/> factory suitable for provisioning/tooling and testing, particularly against the local <b>Cosmos DB</b> emulator.
/// </summary>
public static class CosmosDbClientFactory
{
    /// <summary>
    /// Creates a <see cref="CosmosClient"/> from the <paramref name="connectionString"/> using the <b>System.Text.Json</b> serializer (camelCase), as required by <c>CoreEx.Cosmos</c>.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="configure">An optional action to further configure the <see cref="CosmosClientOptions"/>.</param>
    /// <returns>The <see cref="CosmosClient"/>.</returns>
    /// <remarks>Where the connection string targets a local endpoint (i.e. the emulator; <c>localhost</c>, <c>127.0.0.1</c> or <c>[::1]</c>) <see cref="ConnectionMode.Gateway"/> is used and the emulator's self-signed
    /// certificate is accepted; this is never applied to a non-local endpoint.</remarks>
    public static CosmosClient Create(string connectionString, Action<CosmosClientOptions>? configure = null)
    {
        var options = new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        };

        if (IsLocalEndpoint(connectionString.ThrowIfNullOrEmpty()))
        {
            options.ConnectionMode = ConnectionMode.Gateway;
            options.HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
        }

        configure?.Invoke(options);
        return new CosmosClient(connectionString, options);
    }

    /// <summary>
    /// Determines whether the connection string <c>AccountEndpoint</c> is a local endpoint.
    /// </summary>
    private static bool IsLocalEndpoint(string connectionString)
    {
        var endpoint = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Split('=', 2))
            .FirstOrDefault(x => x.Length == 2 && string.Equals(x[0].Trim(), "AccountEndpoint", StringComparison.OrdinalIgnoreCase))?[1];

        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
    }
}
