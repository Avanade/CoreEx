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

            // Override with an explicit WithTypeDiscriminator value where configured (see CosmosDbModelOptions<TModel>.ApplyTypeDiscriminator) - a no-op otherwise; must run after PrepareUpdate above,
            // which otherwise leaves its own default (Schema/type name) stamped instead.
            Options.ApplyTypeDiscriminator(model);

            // Apply a computed time-to-live where configured (see CosmosDbModelOptions<TModel>.WithTimeToLive) - a no-op otherwise.
            Options.ApplyTimeToLive(model);

            // Check model is valid.
            var r = CheckModel(args, model, OperationType.Update);
            if (r.IsFailure)
                return r.Bind();

            var partitionKeyValue = Options.GetPartitionKeyValue(model);
            var partitionKey = CosmosDbModelOptions<TModel>.ToPartitionKey(partitionKeyValue);
            var id = Options.FormatIdentifier(Options.GetKeyFromModel(model));

            // The existing (persisted) document is always read (a change tracker hit where already read within this scope) for the following reasons:
            //  - The CheckModel call above only validates the incoming model, which is always self-consistent (Model.PrepareUpdate stamps its ITenantId/ITypeDiscriminator from the caller's own execution
            //    context/type before the check runs), so it cannot detect that the PERSISTED document at this id/partition actually belongs to a different tenant, is logically deleted, fails an additive
            //    WithFilter authorization rule, or belongs to a different configured type.
            //  - The server-managed values (change-log created, additional properties not known to the model) must be copied back as a Cosmos DB replace overwrites the whole document.
            //  - A no-op update (nothing but server-managed values differ) can be detected and skipped.
            // A missing document surfaces as the same Result.NotFoundError() a subsequent ReplaceItemAsync 404 would have produced anyway (unlike Delete, a missing document is not treated as an idempotent no-op
            // for Update). The request's own ETag (captured below from the incoming model, not this pre-read) is what still governs optimistic concurrency for the replace itself.
            var er = await GetWithResultInternalAsync(args, Options.GetKeyFromModel(model), partitionKey, memberName, treatNullAsNotFound: true, cancellationToken).ConfigureAwait(false);
            if (er.IsFailure)
                return er.Bind();

            var existing = er.Value!;
            CopyServerManagedValues(existing, model);

            // Cosmos DB's native If-Match optimistic concurrency is enforced server-side (returns a 412 directly), unlike a relational/EF detached-entity comparison; the CosmosDbInvoker maps a 412 to a
            // ConcurrencyException/Result.ConcurrencyError automatically. Note: AutoMapETag only synthesizes an ItemRequestOptions when the caller has not already supplied one (args.ItemRequestOptions is
            // null) so as to never mutate a caller-owned/shared ItemRequestOptions instance; where a caller supplies their own ItemRequestOptions they are expected to set IfMatchEtag themselves.
            var options = BuildItemRequestOptions(args);
            if (options is null && args.AutoMapETag && model is IReadOnlyETag etag && !string.IsNullOrEmpty(etag.ETag))
                options = new ItemRequestOptions { IfMatchEtag = etag.ETag };

            // Skip the replace where nothing has changed - unless an ETag is being asserted that differs from the persisted one, in which case the server must be allowed to fail it (412) as it otherwise would. The ETags are compared
            // normalized (quote-bookends stripped) as the persisted value is Cosmos DB's native quote-wrapped "_etag" whereas one arriving via an If-Match header has already been parsed.
            if (AreEquivalent(existing, model) && (string.IsNullOrEmpty(options?.IfMatchEtag) || ETag.ParseETag(options.IfMatchEtag) == ETag.ParseETag((existing as IReadOnlyETag)?.ETag ?? string.Empty)))
                return Result.Ok(new DataResult<TModel>(existing, false));

            EvictFromChangeTracker(partitionKey, id);

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
