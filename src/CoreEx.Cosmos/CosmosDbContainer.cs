namespace CoreEx.Cosmos;

/// <summary>
/// Provides the extended <see cref="ICosmosDb"/>-based <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/">Azure Cosmos DB</see> container model functionality.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
public sealed partial class CosmosDbContainer<TModel> where TModel : class, IEntityKey, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbContainer{TModel}"/> class.
    /// </summary>
    /// <param name="cosmosDb">The owning <see cref="ICosmosDb"/>.</param>
    /// <param name="container">The underlying <see cref="Microsoft.Azure.Cosmos.Container"/>.</param>
    /// <param name="options">The <see cref="CosmosDbModelOptions{TModel}"/>.</param>
    internal CosmosDbContainer(ICosmosDb cosmosDb, Container container, CosmosDbModelOptions<TModel> options)
    {
        CosmosDb = cosmosDb.ThrowIfNull();
        Container = container.ThrowIfNull();
        Options = options.ThrowIfNull();
    }

    /// <summary>
    /// Gets the owning <see cref="ICosmosDb"/>.
    /// </summary>
    public ICosmosDb CosmosDb { get; }

    /// <summary>
    /// Gets the underlying <see cref="Microsoft.Azure.Cosmos.Container"/>.
    /// </summary>
    public Container Container { get; }

    /// <summary>
    /// Gets the <see cref="CosmosDbModelOptions{TModel}"/>.
    /// </summary>
    public CosmosDbModelOptions<TModel> Options { get; }

    /// <summary>
    /// Gets the default <see cref="CosmosDbArgs"/>.
    /// </summary>
    /// <remarks>Uses the <see cref="CosmosDbModelOptions{TModel}.Args"/> where specified; otherwise, the <see cref="CosmosDbOptions.Args"/>.</remarks>
    public CosmosDbArgs Args => Options.Args ?? CosmosDb.DbArgs;

    /// <summary>
    /// Checks (ensures) that the <paramref name="model"/> is valid.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="operationType">The <see cref="OperationType"/>.</param>
    /// <param name="treatNullAsNotFound">Indicates whether to treat a <see langword="null"/> model as a not found error.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    [return: NotNullIfNotNull(nameof(model))]
    public Result<TModel?> CheckModel(CosmosDbArgs args, TModel? model, OperationType operationType, bool treatNullAsNotFound = false)
    {
        args.ThrowIfNull();

        if (model is null)
            return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);

        // Check valid tenant where multi-tenancy is being used.
        if (model is IReadOnlyTenantId tenant)
        {
            // TenantId is stamped automatically (see Model.PrepareCreate/PrepareUpdate) and is never caller-supplied; a null/empty value is an internal data-integrity/environment problem, not a bad request from the caller.
            if (string.IsNullOrEmpty(tenant.TenantId))
                throw new InvalidOperationException($"The model's {nameof(ITenantId.TenantId)} is null or empty; {nameof(IReadOnlyTenantId)} requires tenant stamping to have occurred prior to this check.");

            if (tenant.TenantId != CosmosDb.ExecutionContext.TenantId)
                return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);
        }

        // Check not logically deleted.
        if (model is IReadOnlyLogicallyDeleted ld && ld.IsDeleted)
            return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);

        // Check any additive developer-supplied filters (see CosmosDbModelOptions<TModel>.WithFilter) - e.g. authorization.
        return Options.CheckFilters(args, model, operationType);
    }

    /// <summary>
    /// Builds the <see cref="ItemRequestOptions"/> for a point operation from the specified <paramref name="args"/>.
    /// </summary>
    private static ItemRequestOptions? BuildItemRequestOptions(CosmosDbArgs args) => args.ItemRequestOptions;

    /// <summary>
    /// Refreshes the model post-mutation (as required).
    /// </summary>
    private async Task<Result<TModel>> RefreshPostMutationAsync(CosmosDbArgs args, TModel model, PartitionKey partitionKey, string memberName, CancellationToken cancellationToken)
    {
        // Refresh the model as requested.
        if (args.Refresh)
            return Result.Go((await GetWithResultInternalAsync(args, Options.GetKeyFromModel(model), partitionKey, memberName, treatNullAsNotFound: true, cancellationToken).ConfigureAwait(false)).ThenAs(v => v!));

        // Return the current (already persisted/returned-by-the-SDK) model.
        return Result.Ok(model);
    }

    /// <summary>
    /// Creates a <see cref="CosmosDbMappedContainer{T, TModel, TBiDirectionMapper}"/> that provides mapped <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> operations
    /// (Create, Read, Update and Delete).
    /// </summary>
    /// <typeparam name="T">The mapped <see cref="Type"/>.</typeparam>
    /// <typeparam name="TBiDirectionMapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IBiDirectionMapper{TSource, TDestination}"/>.</param>
    /// <returns>The <see cref="CosmosDbMappedContainer{T, TModel, TBiDirectionMapper}"/>.</returns>
    public CosmosDbMappedContainer<T, TModel, TBiDirectionMapper> ToMappedModel<T, TBiDirectionMapper>(TBiDirectionMapper mapper) where T : class where TBiDirectionMapper : IBiDirectionMapper<T, TModel> => new(this, mapper);
}
