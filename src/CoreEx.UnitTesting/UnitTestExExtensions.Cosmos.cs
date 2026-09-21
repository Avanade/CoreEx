#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method that defaults the <paramref name="serviceKey"/> to <see cref="CosmosDbEventPublisher.DefaultServiceKey"/> where invoking the underlying <see cref="UseExpectedEventPublisher(IServiceCollection, string, bool)"/>.
    /// <para>The <paramref name="bypassPassThrough"/> when set to <see langword="true"/> will bypass the pass-through to the original event publisher and leverage the <see cref="NoOpEventPublisher"/> instead.</para></remarks>
    public static IServiceCollection UseExpectedCosmosDbOutboxPublisher(this IServiceCollection services, string serviceKey = CosmosDbEventPublisher.DefaultServiceKey, bool bypassPassThrough = false)
        => UseExpectedEventPublisher(services, serviceKey, bypassPassThrough);

    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions; whilst also adding post-run expectations for the captured events.
    /// </summary>
    /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="AspNetCore.ApiTester{TEntryPoint}"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <param name="expectNoEvents">Indicates whether to expect no events to be published.</param>
    /// <returns>The <see cref="AspNetCore.ApiTester{TEntryPoint}"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="expectNoEvents"/> parameter is only actioned when no explicit event expectations are defined for the underlying test; acts as a catch all.</remarks>
    public static AspNetCore.ApiTester<TEntryPoint> UseExpectedCosmosDbOutboxPublisher<TEntryPoint>(this AspNetCore.ApiTester<TEntryPoint> tester, string serviceKey = CosmosDbEventPublisher.DefaultServiceKey, bool bypassPassThrough = false, bool expectNoEvents = true) where TEntryPoint : class
        => tester.ConfigureServices(services => services.UseExpectedCosmosDbOutboxPublisher(serviceKey, bypassPassThrough))
                 .AddEventExpectationsPostRun(serviceKey, expectNoEvents);

    /// <summary>
    /// Gets the <see cref="Database"/> resolved from the running host's own <see cref="ICosmosDb"/> registration, ensuring it exists (see <see cref="CosmosClient.CreateDatabaseIfNotExistsAsync(string, int?, Microsoft.Azure.Cosmos.RequestOptions?, CancellationToken)"/>).
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Database"/>.</returns>
    /// <remarks>Deliberately resolves <see cref="ICosmosDb"/> from <see cref="TesterBase.Services"/> (the actual running host's DI container) rather than constructing a new, test-owned <see cref="CosmosClient"/>
    /// from configuration - unlike a relational connection string, a Cosmos DB database identifier is typically a host-owned literal (e.g. <c>services.AddCosmosDb&lt;TCosmosDb&gt;("contoso")</c>), not
    /// something re-derivable from configuration alone; resolving the host's own <see cref="ICosmosDb"/> guarantees test seeding always targets the exact same database the host itself reads/writes,
    /// structurally ruling out a test/host database-name mismatch rather than merely avoiding it by convention.
    /// <para>This is the first call made against the local Cosmos DB emulator for each running test host, and the emulator is known to intermittently drop the TLS handshake (<see cref="System.Net.Http.HttpRequestException"/>
    /// wrapping a connection-reset) under sustained load (e.g. repeated cross-TFM test passes in CI) - a small bounded retry-with-backoff is applied here, scoped purely to this test-setup call, rather than
    /// altering any production <see cref="CosmosClient"/>/<see cref="CosmosDbOptions"/> configuration.</para></remarks>
    public static async Task<Database> GetCosmosDatabaseAsync(this TesterBase tester, CancellationToken cancellationToken = default)
    {
        // ICosmosDb is registered scoped (see CoreExCosmosExtensions.AddCosmosDb), so it cannot be resolved directly from the host's root IServiceProvider - a short-lived scope is created purely to
        // resolve it; the underlying CosmosClient it wraps is independently DI-registered (typically as a singleton via Aspire's AddAzureCosmosClient), so it, and the Database reference obtained from
        // it, remain perfectly usable after this scope is disposed.
        using var scope = tester.ThrowIfNull().Services.CreateScope();
        var cosmosDb = scope.ServiceProvider.GetRequiredService<ICosmosDb>();

        const int maxAttempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await cosmosDb.Client.CreateDatabaseIfNotExistsAsync(cosmosDb.Database.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                return cosmosDb.Database;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientCosmosConnectionFailure(ex))
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Determines whether the <paramref name="exception"/> represents a transient connection failure (e.g. a dropped TLS handshake) rather than a genuine configuration or data error.
    /// </summary>
    private static bool IsTransientCosmosConnectionFailure(Exception exception) => exception is System.Net.Http.HttpRequestException or IOException or System.Net.Sockets.SocketException
        || exception.InnerException is not null && IsTransientCosmosConnectionFailure(exception.InnerException);
}
