namespace CoreEx.Cosmos;

/// <summary>
/// Provides an <see cref="IHealthCheck"/> for an <see cref="ICosmosDb"/>, verifying its configured <see cref="ICosmosDb.Database"/> is reachable and exists.
/// </summary>
/// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
/// <remarks>Aspire's own Cosmos DB client integration (<c>Aspire.Microsoft.Azure.Cosmos</c>) does not register a health check of its own, unlike its Npgsql/SqlClient counterparts (which default to enabled,
/// only opting out via a <c>DisableHealthChecks</c> setting) - this fills that gap; see <see cref="Microsoft.Extensions.DependencyInjection.CoreExCosmosExtensions.AddCosmosDbHealthCheck(IServiceCollection, string)"/>.
/// <para>Performs a lightweight <see cref="Database.ReadAsync(Microsoft.Azure.Cosmos.RequestOptions?, CancellationToken)"/> - cheap, and (unlike a check that only verified the <see cref="CosmosClient"/>
/// itself) also surfaces "the configured database doesn't exist" as an explicit, named health-check failure rather than an opaque error on first real request.</para></remarks>
public sealed class CosmosDbHealthCheck(ICosmosDb cosmosDb) : IHealthCheck
{
    private readonly ICosmosDb _cosmosDb = cosmosDb.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cosmosDb.Database.ReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (CosmosException cex)
        {
            return HealthCheckResult.Unhealthy($"Cosmos DB database '{_cosmosDb.Database.Id}' is not reachable: {cex.Message}", cex);
        }
    }
}
