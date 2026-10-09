namespace Contoso.Customers.Test.Relay;

public class ClientInstrumentationTests
{
    [Test]
    public async Task ClientBackgroundRefresh_IsSuppressed_WhileForegroundReadIsNot()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json").AddEnvironmentVariables().Build();
        var connectionString = configuration.GetConnectionString("Cosmos") ?? throw new InvalidOperationException("The Cosmos connection string is missing.");
        var requests = new ConcurrentQueue<(string Path, bool Suppressed)>();
        var spans = new ConcurrentQueue<Activity>();
        using var telemetry = Sdk.CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation()
            .AddProcessor(new SimpleActivityExportProcessor(new CapturingExporter(spans)))
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(_ => CosmosDbClientFactory.Create(connectionString, options =>
        {
            options.HttpClientFactory = () => new HttpClient(new RecordingHandler(requests));
        }));
        services.SuppressCosmosClientBackgroundInstrumentation();
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<CosmosClient>();
        Sdk.SuppressInstrumentation.Should().BeFalse();
        await client.GetDatabase("contoso").ReadAsync().ConfigureAwait(false);
        requests.Should().Contain(r => r.Path == "/dbs/contoso" && !r.Suppressed);
        requests.Where(r => r.Path == "/").Should().NotBeEmpty().And.OnlyContain(r => r.Suppressed);
        spans.Should().NotBeEmpty();

        // Exercise the SDK's real five-minute timer, not only its initial account discovery.
        var initialAccountReads = requests.Count(r => r.Path == "/");
        var initialSpanCount = spans.Count;
        var timeout = Stopwatch.StartNew();
        while (requests.Count(r => r.Path == "/") == initialAccountReads && timeout.Elapsed < TimeSpan.FromSeconds(320))
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

        requests.Count(r => r.Path == "/").Should().BeGreaterThan(initialAccountReads);
        requests.Where(r => r.Path == "/").Should().OnlyContain(r => r.Suppressed);
        spans.Count.Should().Be(initialSpanCount);
    }

    private sealed class RecordingHandler(ConcurrentQueue<(string Path, bool Suppressed)> requests) : DelegatingHandler(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? throw new InvalidOperationException("The request URL is missing.");
            var suppressed = Sdk.SuppressInstrumentation;
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            requests.Enqueue((path, suppressed));
            return response;
        }
    }

    private sealed class CapturingExporter(ConcurrentQueue<Activity> spans) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var span in batch)
                spans.Enqueue(span);

            return ExportResult.Success;
        }
    }
}
