namespace CoreEx.Cosmos;

public partial class CosmosDbContainer<TModel>
{
    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    /// <remarks>A delete is considered idempotent (a <c>404</c> is not treated as an error) unless logical delete is active (see <see cref="CosmosDbModelOptions{TModel}.LogicalDeleteSupport"/>), in which case a
    /// missing document results in a <see cref="Result.NotFoundError"/> equivalent (as a read-modify-write is required to logically delete). The returning
    /// <see cref="DataResult.WasMutated"/> is informational only.
    /// <para>Unless the <typeparamref name="TModel"/> has none of <see cref="CosmosDbModelOptions{TModel}.TenantSupport"/>, logical delete, or <see cref="CosmosDbModelOptions{TModel}.WithFilter"/>
    /// registrations configured, a delete first performs a read (the same tenant-ownership/filter checks <c>GetAsync</c>/<c>UpdateAsync</c> apply) before deleting by key — a raw <c>DeleteItemAsync</c> call
    /// has no other way to enforce them, since Cosmos DB deletes purely by id + partition key with no awareness of tenant/filter concerns. Where none of those are configured, there is nothing for the
    /// pre-read to catch, so it is skipped entirely and the delete goes straight to Cosmos DB — the common case for a plain, key-based, high-volume delete pays no extra read cost.</para></remarks>
    public Task<DataResult> DeleteAsync(CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => DeleteAsync(Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    public async Task<DataResult> DeleteAsync(CosmosDbArgs args, CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => (await DeleteWithResultInternalAsync(args, key, Options.GetPartitionKey(partitionKey), nameof(DeleteAsync), cancellationToken).ConfigureAwait(false)).ThrowOnError();

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    public Task<Result<DataResult>> DeleteWithResultAsync(CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => DeleteWithResultAsync(Args, key, partitionKey, cancellationToken);

    /// <summary>
    /// Deletes the model for the specified <paramref name="key"/> and <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where not specified, falls back to <see cref="CosmosDbModelOptions{TModel}.WithFixedPartitionKey"/>'s configured value
    /// (see <see cref="CosmosDbModelOptions{TModel}.GetPartitionKey(PartitionKey?)"/>), throwing <see cref="InvalidOperationException"/> if neither is available.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="DataResult"/>.</returns>
    public Task<Result<DataResult>> DeleteWithResultAsync(CosmosDbArgs args, CompositeKey key, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => DeleteWithResultInternalAsync(args, key, Options.GetPartitionKey(partitionKey), nameof(DeleteWithResultAsync), cancellationToken);

    /// <summary>
    /// Deletes the model (internal).
    /// </summary>
    private async Task<Result<DataResult>> DeleteWithResultInternalAsync(CosmosDbArgs args, CompositeKey key, PartitionKey partitionKey, string memberName, CancellationToken cancellationToken = default) => await CosmosDb.Invoker.InvokeAsync(CosmosDb, args.ThrowIfNull(), async (_, args, cancellationToken) =>
    {
        // Logical delete (ambiguous exception) - a type-level configuration issue, not model-instance-dependent, so fail fast before fetching anything.
        if (Options.LogicalDeleteSupport.IsReadOnly)
            throw new InvalidOperationException($"The model implements {nameof(IReadOnlyLogicallyDeleted)} which is ambiguous for a delete operation; the model must implement {nameof(ILogicallyDeleted)} not {nameof(IReadOnlyLogicallyDeleted)}.");

        var id = Options.FormatIdentifier(key);

        async Task<Result<DataResult>> PhysicalDeleteAsync()
        {
            try
            {
                await Container.DeleteItemAsync<TModel>(id, partitionKey, BuildItemRequestOptions(args), cancellationToken).ConfigureAwait(false);
                return Result.Ok(DataResult.True);
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                // A delete is considered idempotent; a 'not found' is not an error.
                return Result.Ok(DataResult.False);
            }
        }

        // Fast path: nothing is configured for CheckModel to check (no ITenantId/IReadOnlyTenantId support, no logical delete, no WithFilter registrations) - go straight to Cosmos DB with no pre-read at
        // all, since there is nothing a pre-read could catch. This is the low-cost path for the common case of a plain, key-based physical delete.
        if (Options.LogicalDeleteSupport.IsNone && !Options.TenantSupport.IsSupported && !Options.HasFilters)
            return await PhysicalDeleteAsync().ConfigureAwait(false);

        // Fetch first (via CheckModel) so tenant ownership and any configured WithFilter checks are enforced consistently with Get/Update for both a physical and a logical delete - a physical
        // DeleteItemAsync/ReplaceItemAsync call has no other opportunity to apply them, as Cosmos DB deletes/replaces purely by id + partition key with no awareness of our tenant/filter concerns.
        var gr = await GetWithResultInternalAsync(args, key, partitionKey, memberName, treatNullAsNotFound: false, cancellationToken).ConfigureAwait(false);
        if (gr.IsFailure)
            return gr.Bind();

        if (gr.Value is null)
            return Result.Ok(DataResult.False);

        var model = gr.Value;

        // Physical delete (tenant support and/or filters are configured, hence the pre-read above; still no logical delete).
        if (Options.LogicalDeleteSupport.IsNone)
        {
            // A 'not found' here (a race between the fetch above and this call) is still idempotent, same as the fast path.
            return await PhysicalDeleteAsync().ConfigureAwait(false);
        }

        // Logical delete (read-modify-write); reuses the already-fetched/checked model.
        ((ILogicallyDeleted)model).IsDeleted = true;
        Model.PrepareUpdate(model, CosmosDb.ExecutionContext);

        var options = BuildItemRequestOptions(args);
        if (options is null && args.AutoMapETag && model is IReadOnlyETag etag && !string.IsNullOrEmpty(etag.ETag))
            options = new ItemRequestOptions { IfMatchEtag = etag.ETag };

        await Container.ReplaceItemAsync(model, id, partitionKey, options, cancellationToken).ConfigureAwait(false);
        return Result.Ok(DataResult.True);
    }, cancellationToken, memberName).ConfigureAwait(false);
}
