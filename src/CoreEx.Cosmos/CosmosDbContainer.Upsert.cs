namespace CoreEx.Cosmos;

public partial class CosmosDbContainer<TModel>
{
    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    /// <remarks>An upsert operation will attempt to update the model if it exists, and then create a new model if it does not (i.e. the update results in a <see cref="NotFoundException"/>). Note: this is
    /// <i>not</i> a single atomic operation (unlike the underlying Cosmos DB SDK's own <c>UpsertItemAsync</c>), as it is applied via the same <see cref="CreateWithResultAsync(CosmosDbArgs, TModel, CancellationToken)"/>
    /// and <see cref="UpdateWithResultAsync(CosmosDbArgs, TModel, CancellationToken)"/> pipelines (change-log stamping, ETag concurrency, tenant/logical-delete checks) to ensure consistent CoreEx semantics.
    /// <para>Inside an active <see cref="CosmosDbUnitOfWork"/>, "attempt update, retry as create on Not Found" is not possible - a <see cref="TransactionalBatch"/> only enlists (queues) the operation and
    /// cannot observe a 404 until the whole batch executes, at which point retrying is too late. A forced pre-read (mirroring <see cref="DeleteAsync(CompositeKey, CancellationToken)"/>'s equivalent
    /// transactional fast-path) determines existence up-front instead, so the correct operation is enlisted the first and only time.</para></remarks>
    public Task<DataResult<TModel>> UpsertAsync(TModel model, CancellationToken cancellationToken = default) => UpsertAsync(Args, model, cancellationToken);

    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    public async Task<DataResult<TModel>> UpsertAsync(CosmosDbArgs args, TModel model, CancellationToken cancellationToken = default) => (await UpsertWithResultInternalAsync(args, model, nameof(UpsertAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    public Task<Result<DataResult<TModel>>> UpsertWithResultAsync(TModel model, CancellationToken cancellationToken = default) => UpsertWithResultAsync(Args, model, cancellationToken);

    /// <summary>
    /// Upserts the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the upserted model.</returns>
    public Task<Result<DataResult<TModel>>> UpsertWithResultAsync(CosmosDbArgs args, TModel model, CancellationToken cancellationToken = default) => UpsertWithResultInternalAsync(args, model, nameof(UpsertWithResultAsync), cancellationToken);

    /// <summary>
    /// Upserts the model (internal).
    /// </summary>
    private async Task<Result<DataResult<TModel>>> UpsertWithResultInternalAsync(CosmosDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        model.ThrowIfNull();

        return await CosmosDb.Invoker.InvokeAsync(CosmosDb, args.ThrowIfNull(), (_, args, cancellationToken) =>
        {
            // Inside an active CosmosDbUnitOfWork, Create/ReplaceItem are only enlisted (queued) into the TransactionalBatch - a 404 for a missing item cannot be observed until the whole batch executes,
            // by which point it is too late to retry as a Create (the batch has already failed as a whole; see CosmosDbUnitOfWork's remarks). A forced pre-read (mirroring DeleteWithResultInternalAsync's
            // equivalent transactional fast-path) determines existence up-front instead, so the correct operation - with its correct Create-vs-Update model stamping (IChangeLog Created vs Updated, etc.)
            // - is enlisted the first and only time.
            if (CosmosDb.CurrentTransaction is not null)
                return UpsertWithinTransactionAsync(args, model, memberName, cancellationToken);

            return Result.GoAsync(() => UpdateWithResultAsync(args, model, cancellationToken))
                .OnFailureAsync(r => r.IsNotFoundError ? CreateWithResultAsync(args, model, cancellationToken) : r.AsTask());
        }, cancellationToken, memberName).ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts the model within an active <see cref="CosmosDbUnitOfWork"/> transaction (internal) - see <see cref="UpsertWithResultInternalAsync"/>'s remarks for why this cannot simply retry <see cref="CreateWithResultAsync(CosmosDbArgs, TModel, CancellationToken)"/> on a Not Found failure the way the non-transactional path does.
    /// </summary>
    private async Task<Result<DataResult<TModel>>> UpsertWithinTransactionAsync(CosmosDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        var gr = await GetWithResultInternalAsync(args, Options.GetKeyFromModel(model), Options.GetPartitionKey(model), memberName, treatNullAsNotFound: false, cancellationToken).ConfigureAwait(false);
        if (gr.IsFailure)
            return gr.Bind();

        return gr.Value is null
            ? await CreateWithResultInternalAsync(args, model, memberName, cancellationToken).ConfigureAwait(false)
            : await UpdateWithResultInternalAsync(args, model, memberName, cancellationToken).ConfigureAwait(false);
    }
}
