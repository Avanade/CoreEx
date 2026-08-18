namespace CoreEx.Cosmos;

public partial class CosmosDbMappedContainer<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    /// <remarks>A delete is considered idempotent and as such no <see cref="NotFoundException"/> will be thrown. The returning <see cref="DataResult.WasMutated"/> is informational only.</remarks>
    public Task<DataResult> DeleteAsync(CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default) => DeleteAsync(Container.Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    public Task<DataResult> DeleteAsync(CosmosDbArgs args, CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default) => Container.DeleteAsync(args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    public Task<Result<DataResult>> DeleteWithResultAsync(CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default) => DeleteWithResultAsync(Container.Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    public Task<Result<DataResult>> DeleteWithResultAsync(CosmosDbArgs args, CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default) => Container.DeleteWithResultAsync(args, key, partitionKey, cancellationToken);
}
