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

        // Check the type discriminator agrees where configured (see CosmosDbModelOptions<TModel>.WithTypeDiscriminator) - a shared multi-type container otherwise has no other point-operation defence
        // against deserializing/deleting/replacing a same-id/partition document belonging to a different configured type; ApplyFilters already applies the equivalent check on the query path.
        if (Options.IsTypeDiscriminatorMismatch(model))
            return treatNullAsNotFound ? Result.NotFoundError() : Result.Ok<TModel?>(null);

        // Check any additive developer-supplied filters (see CosmosDbModelOptions<TModel>.WithFilter) - e.g. authorization.
        return Options.CheckFilters(args, model, operationType);
    }

    // Top-level properties that are server-managed (or carried separately) and therefore never considered when determining whether an Update is a no-op.
    private static readonly string[] _noOpExcludedProperties = [nameof(IETag.ETag), nameof(IChangeLog.ChangeLog), nameof(IChangeLogEx.CreatedBy), nameof(IChangeLogEx.CreatedOn), nameof(IChangeLogEx.UpdatedBy), nameof(IChangeLogEx.UpdatedOn), nameof(IExtensionData.ExtensionData)];

    // Cosmos DB system properties that surface in the additional properties bag; these are server-owned and never forwarded/compared.
    private static readonly string[] _cosmosSystemProperties = ["_rid", "_self", "_etag", "_attachments", "_ts"];

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used to snapshot to/from the <see cref="ICosmosDb.ChangeTracker"/>; <see langword="null"/> where tracking is not enabled (no <c>System.Text.Json</c> serializer configured).
    /// </summary>
    private JsonSerializerOptions? ChangeTrackerSerializerOptions => CosmosDb.Client.ClientOptions.UseSystemTextJsonSerializerWithOptions;

    /// <summary>
    /// Evicts the specified document from the <see cref="ICosmosDb.ChangeTracker"/>.
    /// </summary>
    private void EvictFromChangeTracker(PartitionKey partitionKey, string id) => CosmosDb.ChangeTracker.Remove(Container.Id, partitionKey, id);

    /// <summary>
    /// Copies the server-managed values from the <paramref name="existing"/> (persisted) model onto the <paramref name="model"/> (candidate) being updated so they are not lost by the replace.
    /// </summary>
    /// <remarks>The <see cref="IETag.ETag"/> is deliberately <b>never</b> copied; the incoming value drives the server-side <c>If-Match</c> optimistic concurrency check.
    /// <para>The persisted <see cref="ITimeToLive.TimeToLive"/> is only carried forward where the candidate has none and <see cref="CosmosDbModelOptions{TModel}.WithTimeToLive"/> is not configured (where configured the value is
    /// always recomputed); otherwise a replace would silently remove the expiry. An explicit value (e.g. <c>-1</c> for never-expires) overrides.</para></remarks>
    private void CopyServerManagedValues(TModel existing, TModel model)
    {
        if (!Options.HasTimeToLive && model is ITimeToLive ttl && ttl.TimeToLive is null && existing is IReadOnlyTimeToLive ettl)
            ttl.TimeToLive = ettl.TimeToLive;

        if (model is IChangeLog cl)
        {
            var ecl = (existing as IReadOnlyChangeLog)?.ChangeLog;
            cl.ChangeLog = (cl.ChangeLog ?? new ChangeLog()) with { CreatedBy = ecl?.CreatedBy, CreatedOn = ecl?.CreatedOn };
        }
        else if (model is IChangeLogEx cle && existing is IReadOnlyChangeLogEx ecle)
        {
            cle.CreatedBy = ecle.CreatedBy;
            cle.CreatedOn = ecle.CreatedOn;
        }

        // Forward any persisted additional properties the candidate does not itself supply (the candidate wins on a key conflict) as a replace otherwise drops anything the model is unaware of.
        if (model is IExtensionData map && existing is IExtensionData eap && eap.ExtensionData is not null)
        {
            foreach (var kvp in eap.ExtensionData)
            {
                if (_cosmosSystemProperties.Contains(kvp.Key))
                    continue;

                (map.ExtensionData ??= []).TryAdd(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Determines whether the <paramref name="model"/> (candidate) is equivalent to the <paramref name="existing"/> (persisted) model, ignoring the server-managed values (see <see cref="CopyServerManagedValues"/>).
    /// </summary>
    private static bool AreEquivalent(TModel existing, TModel model)
    {
        if (!RuntimeMetadata.AreEqual(existing, model, _noOpExcludedProperties))
            return false;

        if (existing is IExtensionData eap && model is IExtensionData map)
            return RuntimeMetadata.AreEqual(WithoutSystemProperties(eap.ExtensionData), WithoutSystemProperties(map.ExtensionData));

        return true;
    }

    private static Dictionary<string, object?> WithoutSystemProperties(Dictionary<string, object?>? bag)
        => bag is null ? [] : bag.Where(kvp => !_cosmosSystemProperties.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
