namespace CoreEx.Cosmos;

public partial class CosmosDbContainer<TModel>
{
    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public Task<DataResult<TModel>> UpdateAsync(TModel model, CancellationToken cancellationToken = default) => UpdateAsync(Args, model, cancellationToken);

    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public async Task<DataResult<TModel>> UpdateAsync(CosmosDbArgs args, TModel model, CancellationToken cancellationToken = default) => (await UpdateWithResultInternalAsync(args, model, nameof(UpdateAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public Task<Result<DataResult<TModel>>> UpdateWithResultAsync(TModel model, CancellationToken cancellationToken = default) => UpdateWithResultAsync(Args, model, cancellationToken);

    /// <summary>
    /// Updates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the updated model.</returns>
    public Task<Result<DataResult<TModel>>> UpdateWithResultAsync(CosmosDbArgs args, TModel model, CancellationToken cancellationToken = default) => UpdateWithResultInternalAsync(args, model, nameof(UpdateWithResultAsync), cancellationToken);

    /// <summary>
    /// Updates the model (internal).
    /// </summary>
    private async Task<Result<DataResult<TModel>>> UpdateWithResultInternalAsync(CosmosDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        model.ThrowIfNull();

        if (model is IReadOnlyLogicallyDeleted ld && ld.IsDeleted)
            throw new InvalidOperationException($"Cannot update a model and set to the deleted state ({nameof(ILogicallyDeleted.IsDeleted)} must be false); use the delete operation to perform.");

        return await CosmosDb.Invoker.InvokeAsync(CosmosDb, args.ThrowIfNull(), async (_, args, cancellationToken) =>
        {
            // Prepare the model (stamps ITenantId/ITypeDiscriminator/IChangeLog as applicable).
            Model.PrepareUpdate(model, CosmosDb.ExecutionContext);

            // Apply a computed time-to-live where configured (see CosmosDbModelOptions<TModel>.WithTimeToLive) - a no-op otherwise.
            Options.ApplyTimeToLive(model);

            // Check model is valid.
            var r = CheckModel(args, model, OperationType.Update);
            if (r.IsFailure)
                return r.Bind();

            var partitionKeyValue = Options.GetPartitionKeyValue(model);
            var partitionKey = new PartitionKey(partitionKeyValue);
            var id = Options.FormatIdentifier(Options.GetKeyFromModel(model));

            // Cosmos DB's native If-Match optimistic concurrency is enforced server-side (returns a 412 directly), unlike a relational/EF detached-entity comparison; the CosmosDbInvoker maps a 412 to a
            // ConcurrencyException/Result.ConcurrencyError automatically. Note: AutoMapETag only synthesizes an ItemRequestOptions when the caller has not already supplied one (args.ItemRequestOptions is
            // null) so as to never mutate a caller-owned/shared ItemRequestOptions instance; where a caller supplies their own ItemRequestOptions they are expected to set IfMatchEtag themselves.
            var options = BuildItemRequestOptions(args);
            if (options is null && args.AutoMapETag && model is IReadOnlyETag etag && !string.IsNullOrEmpty(etag.ETag))
                options = new ItemRequestOptions { IfMatchEtag = etag.ETag };

            // Where an ambient CosmosDbUnitOfWork transaction is active, enlist (queue) rather than execute immediately - see CosmosDbUnitOfWork for the full deferred-execution/atomicity model. The model's
            // ETag is not yet final at this point (the batch has not executed) - see IUnitOfWork.SynchronizeETag for how a caller resolves the true, persisted ETag once the unit-of-work has committed.
            var txn = CosmosDb.CurrentTransaction;
            if (txn is not null)
            {
                var batchOptions = options is null ? null : new TransactionalBatchItemRequestOptions { IfMatchEtag = options.IfMatchEtag };
                txn.Enlist(Container, partitionKey, partitionKeyValue, Options.GetKeyFromModel(model), b => b.ReplaceItem(id, model, batchOptions));
                return Result.Ok(new DataResult<TModel>(model, true));
            }

            var response = await Container.ReplaceItemAsync(model, id, partitionKey, options, cancellationToken).ConfigureAwait(false);

            // Refresh as required (rarely needed given the SDK already returns the persisted resource).
            var pr = await RefreshPostMutationAsync(args, response.Resource, partitionKey, memberName, cancellationToken).ConfigureAwait(false);
            return pr.ThenAs(m => new DataResult<TModel>(m, true));
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}
