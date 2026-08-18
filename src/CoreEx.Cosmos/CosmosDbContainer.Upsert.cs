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
    /// and <see cref="UpdateWithResultAsync(CosmosDbArgs, TModel, CancellationToken)"/> pipelines (change-log stamping, ETag concurrency, tenant/logical-delete checks) to ensure consistent CoreEx semantics.</remarks>
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
            return Result.GoAsync(() => UpdateWithResultAsync(args, model, cancellationToken))
                .OnFailureAsync(r => r.IsNotFoundError ? CreateWithResultAsync(args, model, cancellationToken) : r.AsTask());
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}
