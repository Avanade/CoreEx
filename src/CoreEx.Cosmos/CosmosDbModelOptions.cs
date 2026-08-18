namespace CoreEx.Cosmos;

/// <summary>
/// Provides options for the <see cref="CosmosDbContainer{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
public class CosmosDbModelOptions<TModel> where TModel : class, IEntityKey, new()
{
    private readonly List<(Func<IQueryable<TModel>, IQueryable<TModel>> Filter, Func<TModel, OperationType, Result>? NonQueryResult, bool AllowFilterBypass)> _filters = [];
    private Func<TModel, CompositeKey> _getKey = m => m.EntityKey;
    private Func<CompositeKey, string> _formatIdentifier = key => key.ToString() ?? string.Empty;
    private Func<TModel, string?>? _getPartitionKey;
    private string? _fixedPartitionKey;
    private Func<TModel, int?>? _getTimeToLive;
    private bool _tenantFilterEnabled;
    private bool _logicalDeleteFilterEnabled;
    private bool _typeDiscriminatorFilterEnabled;
    private string? _typeDiscriminatorValue;

    /// <summary>
    /// Indicates whether <see cref="ILogicallyDeleted"/> and/or <see cref="IReadOnlyLogicallyDeleted"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport LogicalDeleteSupport { get; } = FeatureSupport.Determine<TModel, ILogicallyDeleted, IReadOnlyLogicallyDeleted>();

    /// <summary>
    /// Indicates whether <see cref="ITenantId"/> and/or <see cref="IReadOnlyTenantId"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport TenantSupport { get; } = FeatureSupport.Determine<TModel, ITenantId, IReadOnlyTenantId>();

    /// <summary>
    /// Indicates whether <see cref="ITypeDiscriminator"/> and/or <see cref="IReadOnlyTypeDiscriminator"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport TypeDiscriminatorSupport { get; } = FeatureSupport.Determine<TModel, ITypeDiscriminator, IReadOnlyTypeDiscriminator>();

    /// <summary>
    /// Indicates whether <see cref="IETag"/> and/or <see cref="IReadOnlyETag"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport ETagSupport { get; } = FeatureSupport.Determine<TModel, IETag, IReadOnlyETag>();

    /// <summary>
    /// Indicates whether <see cref="IPartitionKey"/> and/or <see cref="IReadOnlyPartitionKey"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport PartitionKeySupport { get; } = FeatureSupport.Determine<TModel, IPartitionKey, IReadOnlyPartitionKey>();

    /// <summary>
    /// Indicates whether <see cref="ITimeToLive"/> and/or <see cref="IReadOnlyTimeToLive"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport TimeToLiveSupport { get; } = FeatureSupport.Determine<TModel, ITimeToLive, IReadOnlyTimeToLive>();

    /// <summary>
    /// Gets the default <see cref="CosmosDbArgs"/>.
    /// </summary>
    public CosmosDbArgs? Args { get; private set; }

    /// <summary>
    /// Sets (overrides) the default <see cref="Args"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public CosmosDbModelOptions<TModel> WithArgs(CosmosDbArgs? args)
    {
        Args = args;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the function to get the <see cref="CompositeKey"/> for the <typeparamref name="TModel"/>.
    /// </summary>
    /// <param name="getKey">The function to get the key.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Defaults to the <typeparamref name="TModel"/> <see cref="IEntityKey.EntityKey"/>.</remarks>
    public CosmosDbModelOptions<TModel> WithGetKey(Func<TModel, CompositeKey> getKey)
    {
        _getKey = getKey.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Gets the <see cref="CompositeKey"/> from the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The <see cref="CompositeKey"/>.</returns>
    public CompositeKey GetKeyFromModel(TModel model) => _getKey(model.ThrowIfNull());

    /// <summary>
    /// Sets (overrides) the function used by <see cref="FormatIdentifier(CompositeKey)"/> to derive the physical Cosmos DB document <c>id</c> from a <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="formatIdentifier">The function to format the identifier.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public CosmosDbModelOptions<TModel> WithFormatIdentifier(Func<CompositeKey, string> formatIdentifier)
    {
        _formatIdentifier = formatIdentifier.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Formats (derives) the physical Cosmos DB document <c>id</c> from the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <returns>The physical Cosmos DB document <c>id</c>.</returns>
    /// <remarks>Defaults to <see cref="CompositeKey.ToString"/>.</remarks>
    public string FormatIdentifier(CompositeKey key) => _formatIdentifier(key);

    /// <summary>
    /// Sets (overrides) the function to get the partition key value for a <typeparamref name="TModel"/> instance.
    /// </summary>
    /// <param name="getPartitionKey">The function to get the partition key value.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Where not specified, and the <typeparamref name="TModel"/> implements <see cref="IReadOnlyPartitionKey"/> (see <see cref="PartitionKeySupport"/>), the <see cref="IReadOnlyPartitionKey.PartitionKey"/> is used
    /// by default; otherwise, an <see cref="InvalidOperationException"/> will be thrown when required (i.e. for a <b>Create</b> or <b>Update</b> operation).
    /// <para>Only single-level (v1) Cosmos DB partition keys are supported; hierarchical (multi-level) partition keys are not currently supported.</para>
    /// <para>Because <paramref name="getPartitionKey"/> is invoked per <typeparamref name="TModel"/> instance, it can only ever apply to <b>Create</b>/<b>Update</b> (where a model instance exists) — it
    /// provides no default for <b>Get</b>/<b>Delete</b>'s point-operation <c>partitionKey</c> parameter (see <see cref="GetPartitionKey(PartitionKey?)"/>); use <see cref="WithFixedPartitionKey"/> if a
    /// default for those is also needed. Mutually exclusive with <see cref="WithFixedPartitionKey"/> — configuring both throws <see cref="InvalidOperationException"/>.</para>
    /// <para>This takes a raw <see langword="string"/>? (not the Cosmos DB SDK's <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> struct) because the resolved value must, where the <typeparamref name="TModel"/>
    /// implements the <i>mutable</i> <see cref="IPartitionKey"/>, be written back onto the model before a <b>Create</b>/<b>Update</b> — Cosmos DB requires the document body's value at the partition-key
    /// path to agree with the value supplied for the operation itself, and the SDK's <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> struct exposes no public way to extract its underlying value back out
    /// once constructed, so working in <see langword="string"/> throughout (only converting to <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> at the point of the actual SDK call) is what makes that
    /// write-back possible at all.</para></remarks>
    public CosmosDbModelOptions<TModel> WithPartitionKey(Func<TModel, string?> getPartitionKey)
    {
        if (_fixedPartitionKey is not null)
            throw new InvalidOperationException($"{nameof(WithPartitionKey)} cannot be specified when {nameof(WithFixedPartitionKey)} has already been configured; the two are mutually exclusive.");

        _getPartitionKey = getPartitionKey.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Sets (overrides) a single, constant partition key value used for <i>every</i> item in the container.
    /// </summary>
    /// <param name="partitionKey">The fixed partition key value.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Suitable for small, bounded containers where partitioning is not meaningful — Cosmos DB's own guidance is that a container which stays well under the 20 GB/10,000 RU/s per-logical-partition
    /// limits, and typically requires only one or two physical partitions, does not need a high-cardinality partition key
    /// (see <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview">Partitioning and horizontal scaling</see>).
    /// <para>Unlike <see cref="WithPartitionKey"/> (which computes a value per <typeparamref name="TModel"/> instance, and therefore can only apply to <b>Create</b>/<b>Update</b>), this fixed value is
    /// also used as the default for <b>Get</b>/<b>Delete</b>'s <c>partitionKey</c> parameter when the caller does not supply one (see <see cref="GetPartitionKey(PartitionKey?)"/>) — it is the only
    /// mechanism that can provide a default for those point operations. Mutually exclusive with <see cref="WithPartitionKey"/> — configuring both throws <see cref="InvalidOperationException"/>.</para>
    /// <para>Where the <typeparamref name="TModel"/> also implements <see cref="IReadOnlyPartitionKey"/> with a non-null value that differs from this fixed value, <see cref="GetPartitionKey(TModel)"/>
    /// throws <see cref="InvalidOperationException"/> rather than silently overriding it — configuration always wins, but a genuine mismatch between the two is far more likely to indicate a
    /// configuration/logic error than routine, expected behaviour. For a <b>Create</b>/<b>Update</b> to actually succeed against a real Cosmos DB container, <typeparamref name="TModel"/> must implement
    /// the <i>mutable</i> <see cref="IPartitionKey"/> (see <see cref="PartitionKeySupport"/>) so this fixed value can be written back onto the model — Cosmos DB rejects a write where the document body's
    /// value at the partition-key path disagrees with the value supplied for the operation.</para></remarks>
    public CosmosDbModelOptions<TModel> WithFixedPartitionKey(string? partitionKey)
    {
        if (_getPartitionKey is not null)
            throw new InvalidOperationException($"{nameof(WithFixedPartitionKey)} cannot be specified when {nameof(WithPartitionKey)} has already been configured; the two are mutually exclusive.");

        _fixedPartitionKey = partitionKey;
        return this;
    }

    /// <summary>
    /// Gets the <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> for the specified <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</returns>
    /// <remarks><see cref="WithPartitionKey"/>'s function or <see cref="WithFixedPartitionKey"/>'s value (at most one of these can be configured — they are mutually exclusive) always wins over the
    /// <paramref name="model"/>'s own <see cref="IReadOnlyPartitionKey.PartitionKey"/>. Where the model also implements <see cref="IReadOnlyPartitionKey"/> with a non-null value that differs from the
    /// configured result, this throws <see cref="InvalidOperationException"/> rather than silently overriding it — unlike a multi-tenant "wrong tenant" lookup (which is expected, routine behaviour), there
    /// is no scenario where a differing partition key is a benign, expected outcome; it is far more likely to indicate that the configuration and the model have drifted out of sync.
    /// <para>Where an override is configured and <typeparamref name="TModel"/> implements the <i>mutable</i> <see cref="IPartitionKey"/>, the resolved value is also written back onto the
    /// <paramref name="model"/> — Cosmos DB requires the document body's value at the partition-key path to agree with the value supplied for the operation itself, so this write-back is required for a
    /// <b>Create</b>/<b>Update</b> using a configured override to succeed at all, not merely a convenience.</para></remarks>
    public PartitionKey GetPartitionKey(TModel model)
    {
        model.ThrowIfNull();

        var configured = _getPartitionKey is not null ? _getPartitionKey(model) : _fixedPartitionKey;
        if (configured is not null)
        {
            if (PartitionKeySupport.IsSupported)
            {
                var modelValue = ((IReadOnlyPartitionKey)model).PartitionKey;
                if (!string.IsNullOrEmpty(modelValue) && modelValue != configured)
                    throw new InvalidOperationException($"The model's {nameof(IReadOnlyPartitionKey.PartitionKey)} ('{modelValue}') does not match the configured partition key ('{configured}'); this likely represents a configuration or logic error (see {nameof(WithPartitionKey)}/{nameof(WithFixedPartitionKey)}).");
            }

            if (PartitionKeySupport.IsMutable)
                ((IPartitionKey)model).PartitionKey = configured;

            return new PartitionKey(configured);
        }

        if (PartitionKeySupport.IsSupported)
            return new PartitionKey(((IReadOnlyPartitionKey)model).PartitionKey);

        throw new InvalidOperationException($"The model does not implement {nameof(IReadOnlyPartitionKey)}; as such, {nameof(WithPartitionKey)} or {nameof(WithFixedPartitionKey)} must be specified to enable.");
    }

    /// <summary>
    /// Resolves the <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> to use for a point operation (<b>Get</b>/<b>Delete</b>) given an optional caller-supplied <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="partitionKey">The caller-supplied <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>; where <see langword="null"/>, falls back to <see cref="WithFixedPartitionKey"/>'s value (where configured).</param>
    /// <returns>The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> to use.</returns>
    /// <remarks><see cref="WithPartitionKey"/>'s function cannot contribute here — a <b>Get</b>/<b>Delete</b> point operation has no <typeparamref name="TModel"/> instance to invoke it against, only a
    /// <see cref="CompositeKey"/>; only <see cref="WithFixedPartitionKey"/> can provide a default for these operations. Unlike <see cref="GetPartitionKey(TModel)"/>, there is no model to write back onto
    /// here — a <b>Get</b>/<b>Delete</b> reads/removes by key and has no document body to reconcile.</remarks>
    public PartitionKey GetPartitionKey(PartitionKey? partitionKey)
        => partitionKey ?? (_fixedPartitionKey is not null ? new PartitionKey(_fixedPartitionKey) : throw new InvalidOperationException($"No {nameof(PartitionKey)} was specified and no default is configured; either supply one explicitly, or configure {nameof(WithFixedPartitionKey)}."));

    /// <summary>
    /// Sets (overrides) the function to compute the <see cref="ITimeToLive.TimeToLive"/> for a <typeparamref name="TModel"/> instance (where <see cref="TimeToLiveSupport"/> is <see cref="FeatureSupport.Mutable"/>).
    /// </summary>
    /// <param name="getTimeToLive">The function to compute the time-to-live (in seconds; <see langword="null"/> indicates no expiry).</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Unlike <see cref="WithPartitionKey"/> (whose resolved value is passed directly as a separate Cosmos DB SDK call parameter), a computed time-to-live can only take effect by being written back
    /// onto the <typeparamref name="TModel"/> instance itself — Cosmos DB's <c>ttl</c> is purely a document-body field, there is no separate request-option equivalent. This therefore requires the
    /// <typeparamref name="TModel"/> to implement the <i>mutable</i> <see cref="ITimeToLive"/> (not merely <see cref="IReadOnlyTimeToLive"/>); an unconfigured model with no override simply never expires via
    /// this mechanism, which is the common case and requires no configuration at all.
    /// <para>Applied automatically on <b>Create</b> and <b>Update</b> (see <see cref="ApplyTimeToLive(TModel)"/>), after <c>Model.PrepareCreate</c>/<c>PrepareUpdate</c> stamping and before the model is
    /// persisted — so <paramref name="getTimeToLive"/> may itself inspect other already-stamped properties (e.g. <see cref="ITypeDiscriminator"/>) if useful.</para></remarks>
    public CosmosDbModelOptions<TModel> WithTimeToLive(Func<TModel, int?> getTimeToLive)
    {
        if (!TimeToLiveSupport.IsMutable)
            throw new NotSupportedException($"{nameof(WithTimeToLive)} is not supported; model must implement {nameof(ITimeToLive)} to enable.");

        _getTimeToLive = getTimeToLive.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Applies the <see cref="WithTimeToLive"/>-computed time-to-live (where configured) to the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <remarks>A no-op where <see cref="WithTimeToLive"/> has not been configured — an implementing <typeparamref name="TModel"/>'s own <see cref="ITimeToLive.TimeToLive"/> value (if any) is otherwise left
    /// untouched and simply serializes through as-is; there is nothing to "apply" in that case.</remarks>
    public void ApplyTimeToLive(TModel model)
    {
        if (_getTimeToLive is null)
            return;

        ((ITimeToLive)model.ThrowIfNull()).TimeToLive = _getTimeToLive(model);
    }

    /// <summary>
    /// Adds a filter to be applied to all operations (get, create, update, delete, and query).
    /// </summary>
    /// <param name="filter">The filter query to apply.</param>
    /// <param name="nonQueryResult">The optional <see cref="Result"/> to return for non-query operations when the filter excludes.</param>
    /// <param name="allowFilterBypass">Indicates whether the filter can be bypassed via <see cref="CosmosDbArgs.BypassFilters"/>; defaults to <see langword="false"/>.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is the additive extension point for filters that are not one of the built-in <see cref="WithTenantFilter"/>/<see cref="WithLogicalDeleteFilter"/>/<see cref="WithTypeDiscriminatorFilter(string?)"/>
    /// concerns — for example, an authorization-related filter that restricts which documents a given caller may see or mutate. The <paramref name="nonQueryResult"/> enables a different result to be returned for
    /// non-query operations when the filter excludes; for example, a <see cref="Result.AuthenticationError"/> could be returned for an authorization filter. Where a <paramref name="nonQueryResult"/> is <i>not</i>
    /// specified then the specified <paramref name="filter"/> is only applied for queries (see <see cref="ApplyFilters"/>) and has no effect on non-query operations (see <see cref="CheckFilters"/>).
    /// <para>The <see cref="CosmosDbArgs.BypassFilters"/> can be used to bypass filters registered with <paramref name="allowFilterBypass"/> set to <see langword="true"/>.</para>
    /// <para>Each filter is applied individually, in the order specified.</para>
    /// <para>The <paramref name="filter"/> is evaluated in two different contexts and must be expressible in both: against the real Cosmos DB LINQ query (translated to a Cosmos DB SQL query) for
    /// <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/>, and against an in-memory, single-item <see cref="IQueryable{T}"/> (LINQ-to-Objects) for the non-query pre-check performed by
    /// <see cref="CheckFilters"/> — this is intentional, avoiding a second round-trip to re-verify a model already in hand, but it means the predicate cannot use Cosmos-LINQ-only constructs that have
    /// no meaning against an in-memory sequence.</para></remarks>
    public CosmosDbModelOptions<TModel> WithFilter(Func<IQueryable<TModel>, IQueryable<TModel>> filter, Func<TModel, OperationType, Result>? nonQueryResult = null, bool allowFilterBypass = false)
    {
        _filters.Add((filter.ThrowIfNull(), nonQueryResult, allowFilterBypass));
        return this;
    }

    /// <summary>
    /// Indicates whether any <see cref="WithFilter"/> filters have been specified.
    /// </summary>
    public bool HasFilters => _filters.Count > 0;

    /// <summary>
    /// Checks the non-query <see cref="WithFilter"/> filters against the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="operationType">The <see cref="OperationType"/>.</param>
    /// <returns>The <see cref="Result"/> of the filters check.</returns>
    /// <remarks>See <see cref="WithFilter"/> for more information. Invoked internally by <see cref="CosmosDbContainer{TModel}.CheckModel"/> for the <c>Get</c>/<c>Create</c>/<c>Update</c>/<c>Delete</c> operations.</remarks>
    public Result<TModel?> CheckFilters(CosmosDbArgs args, TModel? model, OperationType operationType)
    {
        args.ThrowIfNull();

        if (model is null || !HasFilters)
            return Result.Ok(model);

        var q = new[] { model }.AsQueryable();

        foreach (var (filter, nonQueryResult, allowFilterBypass) in _filters)
        {
            // Bypass filter where selected to do so and allowed.
            if (args.BypassFilters && allowFilterBypass)
                continue;

            // Apply the filter to the single model query; if no match, then carry on.
            if (nonQueryResult is null || filter(q).Any())
                continue;

            // Match; so, return the non-query result (should be an error).
            return nonQueryResult(model, operationType);
        }

        return Result.Ok<TModel?>(model);
    }

    /// <summary>
    /// Adds a tenant (<see cref="IReadOnlyTenantId.TenantId"/>) query-only filter (where <see cref="TenantSupport"/> is supported).
    /// </summary>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Non-query operations (<c>GetAsync</c>, etc.) always check the <see cref="IReadOnlyTenantId.TenantId"/> where supported (see <c>CosmosDbContainer{TModel}.CheckModel</c>) irrespective of
    /// whether this filter has been configured; this only controls whether <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/> also applies the equivalent predicate.</remarks>
    public CosmosDbModelOptions<TModel> WithTenantFilter()
    {
        if (!TenantSupport.IsSupported)
            throw new NotSupportedException($"{nameof(WithTenantFilter)} is not supported; model must implement {nameof(IReadOnlyTenantId)} to enable.");

        _tenantFilterEnabled = true;
        return this;
    }

    /// <summary>
    /// Adds a logical delete (<see cref="IReadOnlyLogicallyDeleted.IsDeleted"/>) query-only filter (where <see cref="LogicalDeleteSupport"/> is supported).
    /// </summary>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Non-query operations always check the <see cref="IReadOnlyLogicallyDeleted.IsDeleted"/> state where supported irrespective of whether this filter has been configured; this only controls whether
    /// <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/> also applies the equivalent predicate.</remarks>
    public CosmosDbModelOptions<TModel> WithLogicalDeleteFilter()
    {
        if (!LogicalDeleteSupport.IsSupported)
            throw new NotSupportedException($"{nameof(WithLogicalDeleteFilter)} is not supported; model must implement {nameof(IReadOnlyLogicallyDeleted)} to enable.");

        _logicalDeleteFilterEnabled = true;
        return this;
    }

    /// <summary>
    /// Adds a type discriminator (<see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/>) query-only filter (where <see cref="TypeDiscriminatorSupport"/> is supported), enabling several business model
    /// types to safely share the same container/partition.
    /// </summary>
    /// <param name="typeDiscriminator">The type discriminator value to filter on; defaults to the <see cref="Schemas.SchemaAttribute.Name"/> where specified, otherwise the <typeparamref name="TModel"/> name
    /// (i.e. the same default resolution used by <c>Model.PrepareTypeDiscriminator</c> when stamping a model prior to create/update).</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public CosmosDbModelOptions<TModel> WithTypeDiscriminatorFilter(string? typeDiscriminator = null)
    {
        if (!TypeDiscriminatorSupport.IsSupported)
            throw new NotSupportedException($"{nameof(WithTypeDiscriminatorFilter)} is not supported; model must implement {nameof(IReadOnlyTypeDiscriminator)} to enable.");

        _typeDiscriminatorValue = string.IsNullOrEmpty(typeDiscriminator)
            ? (Schema.TryGetMetadata<TModel>(out var metadata) ? metadata.Name : typeof(TModel).Name)
            : typeDiscriminator;

        _typeDiscriminatorFilterEnabled = true;
        return this;
    }

    /// <summary>
    /// Applies the configured query-only filters (<see cref="WithTenantFilter"/>, <see cref="WithLogicalDeleteFilter"/>, <see cref="WithTypeDiscriminatorFilter(string?)"/> and any additive
    /// <see cref="WithFilter"/> registrations) to the <paramref name="query"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>; used only to check <see cref="CosmosDbArgs.BypassFilters"/> against any bypassable <see cref="WithFilter"/> registrations.</param>
    /// <param name="query">The <see cref="IQueryable{TModel}"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/> resolved by the owning <see cref="ICosmosDb"/> (see <see cref="ICosmosDb.ExecutionContext"/>); used only by the <see cref="WithTenantFilter"/>
    /// predicate, where configured.</param>
    /// <returns>The filtered <see cref="IQueryable{TModel}"/>.</returns>
    public IQueryable<TModel> ApplyFilters(CosmosDbArgs args, IQueryable<TModel> query, ExecutionContext executionContext)
    {
        args.ThrowIfNull();
        query.ThrowIfNull();

        if (_tenantFilterEnabled)
        {
            var tenantId = executionContext.ThrowIfNull().TenantId;
            query = query.Where(m => ((IReadOnlyTenantId)m).TenantId == tenantId);
        }

        if (_logicalDeleteFilterEnabled)
            query = query.Where(m => !((IReadOnlyLogicallyDeleted)m).IsDeleted);

        if (_typeDiscriminatorFilterEnabled)
        {
            var discriminator = _typeDiscriminatorValue;
            query = query.Where(m => ((IReadOnlyTypeDiscriminator)m).TypeDiscriminator == discriminator);
        }

        if (HasFilters)
        {
            foreach (var (filter, _, allowFilterBypass) in _filters)
            {
                // Bypass filter where selected to do so and allowed.
                if (args.BypassFilters && allowFilterBypass)
                    continue;

                query = filter(query);
            }
        }

        return query;
    }
}
