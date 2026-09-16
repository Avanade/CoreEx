using CoreEx.Data.Json;

namespace Contoso.Customers.Test.Api;

internal static class DatabaseSetUp
{
    private static readonly Lazy<IConfigurationRoot> _configuration = new(() => new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build());

    private static string Endpoint => _configuration.Value["CosmosEmulator:Endpoint"] ?? "https://localhost:8081";

    private static string Key => _configuration.Value["CosmosEmulator:Key"] ?? throw new InvalidOperationException("CosmosEmulator:Key configuration is required.");

    private static string DatabaseId => _configuration.Value["CosmosEmulator:DatabaseId"] ?? "Contoso.Customers.Test.Api";

    public static async Task SetUpAsync()
    {
        // Gateway mode + the emulator's self-signed certificate bypass, matching CosmosTestBase's own approach (see tests/CoreEx.Cosmos.Test.Unit/CosmosTestBase.cs) - kept self-contained here rather
        // than reaching into the Api host's own DI-registered CosmosClient.
        using var client = new CosmosClient(Endpoint, Key, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }),
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        });

        // Create the Cosmos database.
        var database = (await client.CreateDatabaseIfNotExistsAsync(DatabaseId).ConfigureAwait(false)).Database;

        // Replace or create "customers" container used by the API. The partition key is assumed to be "/partitionKey" for all containers, which is a common pattern for Cosmos DB.
        await database.ReplaceOrCreateContainerAsync("customers", "/partitionKey").ConfigureAwait(false);

        // Replace or create "ref-data" container used by the API. Reuse the "test" configured reference data and import.
        await database.ReplaceOrCreateContainerAsync("ref-data", "/partitionKey").ConfigureAwait(false);
        var jdr = JsonDataReader.ParseYaml<Contoso.Customers.Test.Common.TestData>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.CamelCase));
        await CosmosDbBatch.ImportDiscriminatedBatchAsync(database, jdr).ConfigureAwait(false);
    }
}
