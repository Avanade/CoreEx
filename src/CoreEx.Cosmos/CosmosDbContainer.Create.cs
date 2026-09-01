namespace CoreEx.Cosmos;

public partial class CosmosDbContainer<TModel>
{
    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public Task<DataResult<TModel>> CreateAsync(TModel model, CancellationToken cancellationToken = default) => CreateAsync(Args, model, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public async Task<DataResult<TModel>> CreateAsync(CosmosDbArgs args, TModel model, CancellationToken cancellationToken = default) => (await CreateWithResultInternalAsync(args, model, nameof(CreateAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public Task<Result<DataResult<TModel>>> CreateWithResultAsync(TModel model, CancellationToken cancellationToken = default) => CreateWithResultAsync(Args, model, cancellationToken);

    /// <summary>
    /// Creates the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DataResult{TModel}"/> containing the created model.</returns>
    public Task<Result<DataResult<TModel>>> CreateWithResultAsync(CosmosDbArgs args, TModel model, CancellationToken cancellationToken = default) => CreateWithResultInternalAsync(args, model, nameof(CreateWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates the model (internal).
    /// </summary>
    private async Task<Result<DataResult<TModel>>> CreateWithResultInternalAsync(CosmosDbArgs args, TModel model, string memberName, CancellationToken cancellationToken)
    {
        model.ThrowIfNull();

        if (model is IReadOnlyLogicallyDeleted ld && ld.IsDeleted)
            throw new InvalidOperationException($"Cannot create a model with a deleted state; {nameof(ILogicallyDeleted.IsDeleted)} must be false.");

        return await CosmosDb.Invoker.InvokeAsync(CosmosDb, args.ThrowIfNull(), async (_, args, cancellationToken) =>
        {
            // Prepare the model (stamps ITenantId/ITypeDiscriminator/IChangeLog as applicable).
            Model.PrepareCreate(model, CosmosDb.ExecutionContext);

            // Apply a computed time-to-live where configured (see CosmosDbModelOptions<TModel>.WithTimeToLive) - a no-op otherwise.
            Options.ApplyTimeToLive(model);

            // Check model is valid.
            var r = CheckModel(args, model, OperationType.Create);
            if (r.IsFailure)
                return r.Bind();

            var partitionKeyValue = Options.GetPartitionKeyValue(model);
            var partitionKey = new PartitionKey(partitionKeyValue);

            // Where an ambient CosmosDbUnitOfWork transaction is active, enlist (queue) rather than execute immediately - see CosmosDbUnitOfWork for the full deferred-execution/atomicity model. The model's
            // ETag is not yet final at this point (the batch has not executed) - see IUnitOfWork.SynchronizeETag for how a caller resolves the true, persisted ETag once the unit-of-work has committed.
            var txn = CosmosDb.CurrentTransaction;
            if (txn is not null)
            {
                txn.Enlist(Container, partitionKey, partitionKeyValue, Options.GetKeyFromModel(model), b => b.CreateItem(model));
                return Result.Ok(new DataResult<TModel>(model, true));
            }

            var response = await Container.CreateItemAsync(model, partitionKey, BuildItemRequestOptions(args), cancellationToken).ConfigureAwait(false);

            // Refresh as required (rarely needed given the SDK already returns the persisted resource).
            var pr = await RefreshPostMutationAsync(args, response.Resource, partitionKey, memberName, cancellationToken).ConfigureAwait(false);
            return pr.ThenAs(m => new DataResult<TModel>(m, true));
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}
