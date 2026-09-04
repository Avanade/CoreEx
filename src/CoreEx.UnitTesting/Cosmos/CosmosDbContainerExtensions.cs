namespace CoreEx.UnitTesting.Cosmos;

/// <summary>
/// Provides <b>Cosmos DB</b> container lifecycle extension methods, intended for test database/container setup only.
/// </summary>
/// <remarks>Operates directly on the raw <see cref="Microsoft.Azure.Cosmos"/> SDK types - there is no dependency on <c>CoreEx.Cosmos</c> here at all, since resetting/creating a container for a test
/// fixture needs none of that package's model-driven behavior (partition-key computation, type-discriminator stamping, etc.).</remarks>
public static class CosmosDbContainerExtensions
{
    /// <summary>
    /// Deletes the <see cref="Container"/> with the specified <paramref name="containerId"/> where it exists; otherwise, does nothing.
    /// </summary>
    /// <param name="database">The <see cref="Database"/>.</param>
    /// <param name="containerId">The <see cref="Container.Id"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public static async Task DeleteContainerIfExistsAsync(this Microsoft.Azure.Cosmos.Database database, string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await database.ThrowIfNull().GetContainer(containerId.ThrowIfNullOrEmpty()).DeleteContainerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound) { /* Already gone - nothing to do. */ }
    }

    /// <summary>
    /// Deletes the <see cref="Container"/> described by <paramref name="containerProperties"/> where it exists, then creates it fresh.
    /// </summary>
    /// <param name="database">The <see cref="Database"/>.</param>
    /// <param name="containerProperties">The <see cref="ContainerProperties"/>.</param>
    /// <param name="throughput">The throughput (RU/s); where not specified, the database's shared/default throughput applies.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly-created <see cref="Container"/>.</returns>
    /// <remarks>A test fixture's most common need: start each run from a known-empty container with a specific partition key path (and, optionally, a unique key policy), rather than accumulating
    /// state across runs or assuming a container already exists with the right shape.</remarks>
    public static async Task<Container> ReplaceOrCreateContainerAsync(this Microsoft.Azure.Cosmos.Database database, ContainerProperties containerProperties, int? throughput = null, CancellationToken cancellationToken = default)
    {
        database.ThrowIfNull();
        containerProperties.ThrowIfNull();

        await database.DeleteContainerIfExistsAsync(containerProperties.Id, cancellationToken).ConfigureAwait(false);

        var response = await database.CreateContainerAsync(containerProperties, throughput, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Container;
    }

    /// <summary>
    /// Deletes the <see cref="Container"/> with the specified <paramref name="containerId"/> and <paramref name="partitionKeyPath"/> where it exists, then creates it fresh.
    /// </summary>
    /// <param name="database">The <see cref="Database"/>.</param>
    /// <param name="containerId">The <see cref="Container.Id"/>.</param>
    /// <param name="partitionKeyPath">The partition key path (e.g. <c>/partitionKey</c>).</param>
    /// <param name="throughput">The throughput (RU/s); where not specified, the database's shared/default throughput applies.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly-created <see cref="Container"/>.</returns>
    public static Task<Container> ReplaceOrCreateContainerAsync(this Microsoft.Azure.Cosmos.Database database, string containerId, string partitionKeyPath, int? throughput = null, CancellationToken cancellationToken = default)
        => ReplaceOrCreateContainerAsync(database, new ContainerProperties(containerId.ThrowIfNullOrEmpty(), partitionKeyPath.ThrowIfNullOrEmpty()), throughput, cancellationToken);
}
