namespace Contoso.Test.Aspire;

[TestFixture]
public class DemoCatalogProgressTests
{
    [Test]
    public void Progress_RendersMeasuredCountsAndStageTimingsWithoutClaimingBootstrapSuccess()
    {
        using var console = new ProductSearchOperationTests.ScriptConsole();
        var operation = new DemoCatalogOperation(console);
        operation.Report(new("Lexical [index]"));
        operation.Report(new("Lexical [index]", IsComplete: true));
        for (var i = 0; i <= 23; i++)
            operation.Report(new("Embedding products", i, 23));
        operation.Report(new("Embedding products", 23, 23, IsComplete: true));
        operation.Report(new("Publishing semantic index"));

        console.Output.Should().Contain("Lexical [index]").And.Contain("Embedding products 0/23")
            .And.Contain("Embedding products 1/23").And.Contain("Embedding products 10/23")
            .And.Contain("Embedding products 20/23").And.Contain("Embedding products 23/23")
            .And.NotContain("Embedding products 2/23").And.NotContain("%").And.NotContain("Demo bootstrap complete:");
        console.Output.Split('✓').Should().HaveCount(3, "only lexical work and embeddings have completed, not publication");
        console.Output.IndexOf("23/23", StringComparison.Ordinal).Should().BeLessThan(console.Output.IndexOf("Publishing semantic index", StringComparison.Ordinal));
    }

    [Test]
    public async Task ConfigurationFailure_RemainsVisibleAndNeverPrintsSuccess()
    {
        using var console = new ProductSearchOperationTests.ScriptConsole();
        var operation = new DemoCatalogOperation(console);
        var run = () => operation.RunAsync(new ConfigurationBuilder().Build());

        await run.Should().ThrowAsync<ArgumentException>();

        console.Output.Should().Contain("Demo bootstrap failed during configuration").And.NotContain("Demo bootstrap complete:").And.NotContain("✓");
    }

    [Test]
    public async Task CancelledBootstrap_ReportsCancellationWithoutStartingStagesOrPrintingSuccess()
    {
        using var console = new ProductSearchOperationTests.ScriptConsole();
        var operation = new DemoCatalogOperation(console);
        var run = () => operation.RunAsync(Configuration(), new CancellationToken(true));

        await run.Should().ThrowAsync<OperationCanceledException>();

        console.Output.Should().Contain("Demo bootstrap cancelled").And.NotContain("Demo bootstrap complete:").And.NotContain("✓");
    }

    [Test]
    public async Task StageFailure_StopsNotificationsWithoutReportingCompletionOrStartingLaterWork()
    {
        var progress = new List<ProductSearchIndex.RebuildProgress>();
        var failure = new InvalidOperationException("Stop before any database work.");
        var run = () => DemoCatalogBootstrap.RunAsync(Configuration(), update =>
        {
            progress.Add(update);
            throw failure;
        });

        (await run.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);

        progress.Should().ContainSingle().Which.Should().Be(new ProductSearchIndex.RebuildProgress("Migrating/resetting Products and loading catalog"));
    }

    [Test]
    public async Task CancelledIndex_DoesNotStartLexicalWorkOrReportAnyCompletedEmbeddings()
    {
        using var host = DemoCatalogBootstrap.CreateProductsHost(Configuration(), "Host=localhost;Database=unused;Username=unused;Password=unused");
        await using var scope = host.Services.CreateAsyncScope();
        var index = scope.ServiceProvider.GetRequiredService<ProductSearchIndex>();
        var progress = new List<ProductSearchIndex.RebuildProgress>();
        var run = () => index.RebuildAsync(progress.Add, new CancellationToken(true));

        await run.Should().ThrowAsync<OperationCanceledException>();

        progress.Should().BeEmpty();
    }

    [Test]
    public void BootstrapHosts_FilterRoutineNoiseButKeepWarningsAndUnrelatedInformation()
    {
        using var host = DemoCatalogBootstrap.CreateProductsHost(Configuration(), "Host=localhost;Database=unused;Username=unused;Password=unused");
        var logging = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        foreach (var category in new[] { "CoreEx.RefData.ReferenceDataOrchestrator", "Polly", "System.Net.Http.HttpClient.Ollama.LogicalHandler" })
        {
            var logger = logging.CreateLogger(category);
            logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information).Should().BeFalse();
            logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning).Should().BeTrue();
            logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error).Should().BeTrue();
        }

        logging.CreateLogger("ZiggyCreatures.Caching.Fusion").IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning).Should().BeTrue();
        logging.CreateLogger("Unrelated").IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information).Should().BeTrue();
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["E2E:Products:ConnectionString"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
        ["E2E:Shopping:ConnectionString"] = "Server=localhost;Initial Catalog=unused;User Id=unused;Password=unused",
        ["Ollama:BaseAddress"] = "http://localhost:11434"
    }).Build();
}
