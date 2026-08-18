namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Base class for tests that require a live Cosmos DB endpoint (the local emulator started via the root <c>docker-compose.yml</c> <c>cosmos-emulator</c> service - bring it up with
/// <c>podman compose -f docker-compose.yml up -d cosmos-emulator</c> or <c>docker compose -f docker-compose.yml up -d cosmos-emulator</c>).
/// </summary>
/// <remarks>Where the emulator is not reachable (e.g. not started, or still warming up) all tests in the deriving fixture are skipped (<see cref="Assert.Ignore(string)"/>) rather than failed, consistent with
/// this repository's general preference for real dependencies over mocks while still allowing the broader test run to succeed in environments where the emulator cannot be brought up.</remarks>
public abstract class CosmosTestBase
{
    private static readonly Lazy<IConfigurationRoot> _configuration = new(() => new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build());
    private static CosmosClient? _client;
    private static Database? _database;
    private static bool? _isAvailable;

    /// <summary>
    /// Gets the shared <see cref="CosmosClient"/> (Gateway mode, pointed at the local emulator, accepting its self-signed certificate).
    /// </summary>
    protected static CosmosClient Client => _client ??= new CosmosClient(Endpoint, Key, new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }),
        // CosmosDbItemBase uses System.Text.Json's [JsonPropertyName] to map the id/_etag/ttl reserved properties; the SDK's default serializer is Newtonsoft.Json-based and would not honour those
        // attributes, so opt into the SDK's System.Text.Json serializer explicitly (camelCase for everything else, matching typical Cosmos DB document conventions).
        UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
    });

    /// <summary>
    /// Gets the test <see cref="Database"/> (created on first use).
    /// </summary>
    protected static Database TestDatabase => _database ?? throw new InvalidOperationException($"{nameof(TestDatabase)} is not available; ensure {nameof(EnsureAvailableOrIgnoreAsync)} has been awaited first.");

    private static string Endpoint => _configuration.Value["CosmosEmulator:Endpoint"] ?? "https://localhost:8081";

    private static string Key => _configuration.Value["CosmosEmulator:Key"] ?? throw new InvalidOperationException("CosmosEmulator:Key configuration is required.");

    private static string DatabaseId => _configuration.Value["CosmosEmulator:DatabaseId"] ?? "CoreEx.Cosmos.Test.Unit";

    /// <summary>
    /// Creates a new <see cref="CosmosDb"/> wrapping the shared <see cref="Client"/>/<see cref="TestDatabase"/>.
    /// </summary>
    protected static CosmosDb CreateCosmosDb() => CreateCosmosDb("tenant-a");

    /// <summary>
    /// Creates a new <see cref="CosmosDb"/> wrapping the shared <see cref="Client"/>/<see cref="TestDatabase"/>, with the specified <paramref name="tenantId"/> - used to simulate two different callers for
    /// multi-tenancy isolation tests.
    /// </summary>
    protected static CosmosDb CreateCosmosDb(string tenantId) => new(Client, DatabaseId, executionContext: new ExecutionContext { TenantId = tenantId });

    /// <summary>
    /// Ensures the Cosmos DB emulator is reachable and the test database exists; where not reachable, ignores (skips) the current test.
    /// </summary>
    [SetUp]
    public async Task EnsureAvailableOrIgnoreAsync()
    {
        if (_isAvailable is null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var response = await Client.CreateDatabaseIfNotExistsAsync(DatabaseId, cancellationToken: cts.Token).ConfigureAwait(false);
                _database = response.Database;
                _isAvailable = true;
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                TestContext.Progress.WriteLine($"Cosmos DB emulator is not reachable at '{Endpoint}': {ex.Message}");
            }
        }

        if (_isAvailable != true)
            Assert.Ignore($"Cosmos DB emulator is not reachable at '{Endpoint}'; start it with 'podman compose -f docker-compose.yml up -d cosmos-emulator' (or the 'docker compose' equivalent) and retry.");
    }

    /// <summary>
    /// Creates (if not already existing) a test container with the specified <paramref name="id"/> and <c>/partitionKey</c> partition key path.
    /// </summary>
    /// <remarks>The local emulator occasionally responds with a transient <c>503 ServiceUnavailable</c> ("high demand") when several containers are created in quick succession (e.g. across multiple test
    /// fixtures); a small retry-with-backoff smooths over this emulator-only quirk rather than failing otherwise-valid tests.</remarks>
    protected static async Task<Container> GetOrCreateContainerAsync(string id)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await TestDatabase.CreateContainerIfNotExistsAsync(id, "/partitionKey").ConfigureAwait(false);
                return response.Container;
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt)).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Generates a new unique identifier (string) suitable for use as a test document id/partition key.
    /// </summary>
    protected static string NewId() => Guid.NewGuid().ToString("N");
}
