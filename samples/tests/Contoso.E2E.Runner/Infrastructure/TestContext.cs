namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Provides the context for end-to-end testing, including configuration, HTTP clients, and scenario management.
/// </summary>
public sealed class TestContext
{
    private readonly ScenarioManager _scenarioManager;
    private readonly SetUpManager _setUpManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class.
    /// </summary>
    public TestContext(IConfiguration config)
    {
        Config = config;

        ProductsHttpClient = new HttpClient { BaseAddress = new Uri(config["E2E:Products:BaseAddress"] ?? throw new InvalidOperationException("E2E:Products:BaseAddress configuration value is missing.")), Timeout = TimeSpan.FromSeconds(30) };
        ShoppingHttpClient = new HttpClient { BaseAddress = new Uri(config["E2E:Shopping:BaseAddress"] ?? throw new InvalidOperationException("E2E:Shopping:BaseAddress configuration value is missing.")), Timeout = TimeSpan.FromSeconds(30) };
        OrdersHttpClient = new HttpClient { BaseAddress = new Uri(config["E2E:Orders:BaseAddress"] ?? throw new InvalidOperationException("E2E:Orders:BaseAddress configuration value is missing.")), Timeout = TimeSpan.FromSeconds(30) };

        PerStepMinDelayMilliseconds = config.GetValue<int>("E2E:PerStepMinDelayMilliseconds");
        PerStepMaxDelayMilliseconds = config.GetValue<int>("E2E:PerStepMaxDelayMilliseconds");

        _scenarioManager = ScenarioManager.Create();
        _setUpManager = SetUpManager.Create();
    }

    /// <summary>
    /// Gets the application's configuration settings.
    /// </summary>
    public IConfiguration Config { get; }

    /// <summary>
    /// Gets the "Products" domain HTTP client configured with the base address and timeout specified in the configuration.
    /// </summary>
    public HttpClient ProductsHttpClient { get; }

    /// <summary>
    /// Gets the "Shopping" domain HTTP client configured with the base address and timeout specified in the configuration.
    /// </summary>
    public HttpClient ShoppingHttpClient { get; }

    /// <summary>
    /// Gets the "Orders" domain HTTP client configured with the base address and timeout specified in the configuration.
    /// </summary>
    public HttpClient OrdersHttpClient { get; }

    /// <summary>
    /// Gets the collection of set-up scenario definitions, keyed by scenario name.
    /// </summary>
    public IReadOnlyDictionary<string, ScenarioDefinition> SetUps => _setUpManager.SetUps;

    /// <summary>
    /// Gets the collection of available scenario definitions, keyed by scenario name.
    /// </summary>
    public IReadOnlyDictionary<string, ScenarioDefinition> Scenarios => _scenarioManager.Scenarios;

    /// <summary>
    /// Gets the per step minimum delay in milliseconds.
    /// </summary>
    public int PerStepMinDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets the per step maximum delay in milliseonds.
    /// </summary>
    public int PerStepMaxDelayMilliseconds { get; set; }

    /// <summary>
    /// Performs a health check against the "/health/ready" endpoint of the provided HTTP client to determine if the associated API is responsive and healthy.
    /// </summary>
    public static async Task<bool> HealthCheckAsync(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("/health/ready");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}