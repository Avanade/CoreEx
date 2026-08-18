namespace CoreEx.Cosmos;

public partial class CosmosDbMappedContainer<TValue, TModel, TBiDirectionMapper>
{
    /// <summary>
    /// Gets the value for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where found; otherwise, <see langword="null"/>.</returns>
    public Task<TValue?> GetAsync(CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default) => GetAsync(Container.Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Gets the value for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where found; otherwise, <see langword="null"/>.</returns>
    public async Task<TValue?> GetAsync(CosmosDbArgs args, CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default)
    {
        var m = await Container.GetAsync(args, key, partitionKey, cancellationToken).ConfigureAwait(false);
        return Mapper.From.Map(m);
    }

    /// <summary>
    /// Gets the value for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value.</returns>
    public Task<Result<TValue>> GetWithResultAsync(CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default) => GetWithResultAsync(Container.Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Gets the value for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value.</returns>
    public async Task<Result<TValue>> GetWithResultAsync(CosmosDbArgs args, CompositeKey key, PartitionKey partitionKey, CancellationToken cancellationToken = default)
    {
        var r = await Container.GetWithResultAsync(args, key, partitionKey, cancellationToken).ConfigureAwait(false);
        return r.IsSuccess ? Mapper.From.Map(r.Value) : r.Bind();
    }
}
