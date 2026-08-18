namespace CoreEx.Cosmos;

public partial class CosmosDbContainer<TModel>
{
    /// <summary>
    /// Gets the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model where found; otherwise, <see langword="null"/> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
    public Task<TModel?> GetAsync(CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => GetAsync(Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Gets the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model where found; otherwise, <see langword="null"/> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
    public async Task<TModel?> GetAsync(CosmosDbArgs args, CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default)
        => (await GetWithResultInternalAsync(args, key, Options.GetPartitionKey(partitionKey), nameof(GetAsync), treatNullAsNotFound: !args.ThrowIfNull().NullOnNotFound, cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Gets the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model.</returns>
    public Task<Result<TModel>> GetWithResultAsync(CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => GetWithResultAsync(Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Gets the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The model.</returns>
    public async Task<Result<TModel>> GetWithResultAsync(CosmosDbArgs args, CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default)
        => (await GetWithResultInternalAsync(args, key, Options.GetPartitionKey(partitionKey), nameof(GetWithResultAsync), treatNullAsNotFound: true, cancellationToken).ConfigureAwait(false)).ThenAs(v => v!);

    /// <summary>
    /// Gets the model (internal).
    /// </summary>
    private async Task<Result<TModel?>> GetWithResultInternalAsync(CosmosDbArgs args, CompositeKey key, PartitionKey partitionKey, string memberName, bool treatNullAsNotFound, CancellationToken cancellationToken)
        => await CosmosDb.Invoker.InvokeAsync(CosmosDb, args.ThrowIfNull(), async (_, args, cancellationToken) =>
        {
            var id = Options.FormatIdentifier(key);

            try
            {
                var response = await Container.ReadItemAsync<TModel>(id, partitionKey, BuildItemRequestOptions(args), cancellationToken).ConfigureAwait(false);
                return CheckModel(args, response.Resource, OperationType.Get, treatNullAsNotFound);
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                // A 'not found' for a Get is not necessarily an error; whether it results in a Result-level not-found error depends on the caller's intent (treatNullAsNotFound).
                return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);
            }
        }, cancellationToken, memberName).ConfigureAwait(false);
}
