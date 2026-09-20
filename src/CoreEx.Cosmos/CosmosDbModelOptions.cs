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
    /// Indicates whether <see cref="IIdentifier{String}"/> and/or <see cref="IReadOnlyIdentifier{String}"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    /// <remarks>Used by <see cref="ApplyFilters(CosmosDbArgs, IQueryable{TModel}, ExecutionContext)"/> to build its automatic outbox-document exclusion predicate directly against this interface (the common
    /// case) - see its remarks for the full mechanism and rationale, and <see cref="ResolveOutboxIdExclusion"/> for the fallback used when this is <em>not</em> supported. Unrelated to
    /// <see cref="ITypeDiscriminator"/>, which is a business-modeling concern, not an infrastructure one.</remarks>
    public FeatureSupport IdentifierSupport { get; } = FeatureSupport.Determine<TModel, IIdentifier<string>, IReadOnlyIdentifier<string>>();

    /// <summary>
    /// Lazily resolves a fallback outbox-document exclusion predicate for a <typeparamref name="TModel"/> that does not implement <see cref="IIdentifier{String}"/>/<see cref="IReadOnlyIdentifier{String}"/>
    /// (see <see cref="IdentifierSupport"/>), by locating whichever property is actually mapped to the reserved Cosmos DB <c>id</c> JSON property - either explicitly via <see cref="JsonPropertyNameAttribute"/>
    /// (the same attribute <see cref="CosmosDbModelBase"/> itself uses), or, failing that, a conventionally-named public <c>string Id</c> property (case-insensitive) that has neither an explicit
    /// <see cref="JsonPropertyNameAttribute"/> (which would mean it is deliberately mapped to a <i>different</i> JSON name) nor a <see cref="JsonIgnoreAttribute"/>, covering a serializer configured with a
    /// naming policy (e.g. <c>JsonNamingPolicy.CamelCase</c>) that maps it to <c>id</c> without requiring an explicit attribute. Cosmos DB requires every physical document to have an <c>id</c> regardless
    /// of which CoreEx interfaces (if any) a model implements, so a <typeparamref name="TModel"/> using
    /// <see cref="WithFormatIdentifier"/>/a composite <see cref="IEntityKey.EntityKey"/> without also implementing <see cref="IReadOnlyIdentifier{String}"/> would otherwise silently receive no
    /// outbox-document exclusion at all.
    /// </summary>
    /// <returns>A compiled <see cref="Expression{TDelegate}"/> equivalent to the <see cref="IIdentifier{String}"/> case, or <see langword="null"/> where no such property can be found (nothing further can be
    /// done here - see <see cref="ApplyFilters(CosmosDbArgs, IQueryable{TModel}, ExecutionContext)"/> remarks).</returns>
    private static Expression<Func<TModel, bool>>? ResolveOutboxIdExclusion()
    {
        var stringProperties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(string));

        var property = stringProperties.FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == "id")
            ?? stringProperties.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)
                && p.GetCustomAttribute<JsonPropertyNameAttribute>() is null && p.GetCustomAttribute<JsonIgnoreAttribute>() is null);

        if (property is null)
            return null;

        var parameter = Expression.Parameter(typeof(TModel), "m");
        var idAccess = Expression.Property(parameter, property);
        var startsWith = Expression.Call(idAccess, StartsWithMethod, Expression.Constant(CosmosDbOutboxEvent.OutboxKeyPrefix));
        return Expression.Lambda<Func<TModel, bool>>(Expression.Not(startsWith), parameter);
    }

    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    private static readonly Lazy<Expression<Func<TModel, bool>>?> _outboxIdExclusion = new(ResolveOutboxIdExclusion);

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
    /// <remarks>Where not specified, and the <typeparamref name="TModel"/> implements <see cref="IReadOnlyPartitionKey"/> (see <see cref="PartitionKeySupport"/>) with a non-empty value, the
    /// <see cref="IReadOnlyPartitionKey.PartitionKey"/> is used by default; otherwise (no override configured, and either the <typeparamref name="TModel"/> does not support it at all, or its value is
    /// empty), <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> is used for a <b>Create</b>/<b>Update</b> - the simplest possible container shape (a single default logical partition, no per-item
    /// partitioning at all) requires zero configuration and no <see cref="IPartitionKey"/>/<see cref="IReadOnlyPartitionKey"/> implementation on the model whatsoever.
    /// <para>Only single-level (v1) Cosmos DB partition keys are supported; hierarchical (multi-level) partition keys are not currently supported.</para>
    /// <para>Because <paramref name="getPartitionKey"/> is invoked per <typeparamref name="TModel"/> instance, it can only ever apply to <b>Create</b>/<b>Update</b> (where a model instance exists) — it
    /// provides no default for <b>Get</b>/<b>Delete</b>'s point-operation <c>partitionKey</c> parameter (see <see cref="GetPartitionKey(string?)"/>); use <see cref="WithFixedPartitionKey"/> if a
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
    /// also used as the default for <b>Get</b>/<b>Delete</b>'s <c>partitionKey</c> parameter when the caller does not supply one (see <see cref="GetPartitionKey(string?)"/>) — it is the only
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
    /// <returns>The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> - <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> where nothing is configured and the model itself has no (or an empty)
    /// partition key value.</returns>
    /// <remarks><see cref="WithPartitionKey"/>'s function or <see cref="WithFixedPartitionKey"/>'s value (at most one of these can be configured — they are mutually exclusive) always wins over the
    /// <paramref name="model"/>'s own <see cref="IReadOnlyPartitionKey.PartitionKey"/>. Where the model also implements <see cref="IReadOnlyPartitionKey"/> with a non-null value that differs from the
    /// configured result, this throws <see cref="InvalidOperationException"/> rather than silently overriding it — unlike a multi-tenant "wrong tenant" lookup (which is expected, routine behaviour), there
    /// is no scenario where a differing partition key is a benign, expected outcome; it is far more likely to indicate that the configuration and the model have drifted out of sync.
    /// <para>Where an override is configured and <typeparamref name="TModel"/> implements the <i>mutable</i> <see cref="IPartitionKey"/>, the resolved value is also written back onto the
    /// <paramref name="model"/> — Cosmos DB requires the document body's value at the partition-key path to agree with the value supplied for the operation itself, so this write-back is required for a
    /// <b>Create</b>/<b>Update</b> using a configured override to succeed at all, not merely a convenience.</para></remarks>
    public PartitionKey GetPartitionKey(TModel model) => ToPartitionKey(GetPartitionKeyValue(model));

    /// <summary>
    /// Gets the raw partition key <see cref="string"/> value for the specified <paramref name="model"/> (see <see cref="GetPartitionKey(TModel)"/>).
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The raw partition key <see cref="string"/> value; <see langword="null"/> where nothing is configured and the model itself has no (or an empty) partition key value (translates to
    /// <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> - see <see cref="GetPartitionKey(TModel)"/>/<see cref="ToPartitionKey(string?)"/>).</returns>
    /// <remarks>Exists (in addition to <see cref="GetPartitionKey(TModel)"/>) because the Cosmos DB SDK's <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> struct exposes no public way to extract its own
    /// value back out once constructed — a <see cref="CosmosDbUnitOfWork"/>-paired outbox-event write (see <see cref="CosmosDbEventPublisher"/>) needs the raw value to co-locate itself in the same
    /// partition, so it is resolved and tracked once here rather than re-derived unreliably later.</remarks>
    internal string? GetPartitionKeyValue(TModel model)
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

            return configured;
        }

        // No override configured; fall back to the model's own value where it supports IReadOnlyPartitionKey - otherwise (or where that value is itself empty), there genuinely is no partition key to
        // use, which is not an error: it simply means the caller wants the simplest possible container shape (see ToPartitionKey - translates to PartitionKey.None).
        if (PartitionKeySupport.IsSupported)
        {
            var modelValue = ((IReadOnlyPartitionKey)model).PartitionKey;
            return string.IsNullOrEmpty(modelValue) ? null : modelValue;
        }

        return null;
    }

    /// <summary>
    /// Resolves the raw partition key <see cref="string"/> value to use for a point operation (<b>Get</b>/<b>Delete</b>) given an optional caller-supplied <paramref name="partitionKey"/>.
    /// </summary>
    /// <param name="partitionKey">The caller-supplied raw partition key value; where <see langword="null"/>, falls back to <see cref="WithFixedPartitionKey"/>'s value (where configured), otherwise
    /// <see langword="null"/> (translates to <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> - see <see cref="ToPartitionKey(string?)"/>).</param>
    /// <returns>The raw partition key value to use.</returns>
    /// <remarks><see cref="WithPartitionKey"/>'s function cannot contribute here — a <b>Get</b>/<b>Delete</b> point operation has no <typeparamref name="TModel"/> instance to invoke it against, only a
    /// <see cref="CompositeKey"/>; only <see cref="WithFixedPartitionKey"/> can provide a default for these operations. Exposed as a raw <see langword="string"/>? (not the SDK's opaque
    /// <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> struct, which cannot be decomposed back into its value once constructed) so a caller enlisting a <b>Delete</b> in a <see cref="CosmosDbUnitOfWork"/>
    /// can also supply this same resolved value to a paired <see cref="CosmosDbEventPublisher"/> outbox-event write, which has no model instance of its own to read one from.</remarks>
    internal string? GetPartitionKeyValue(string? partitionKey) => partitionKey ?? _fixedPartitionKey;

    /// <summary>
    /// Resolves the <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> to use for a point operation (<b>Get</b>/<b>Delete</b>) given an optional caller-supplied raw <paramref name="partitionKey"/> value.
    /// </summary>
    /// <param name="partitionKey">The caller-supplied raw partition key value; where <see langword="null"/>, falls back to <see cref="WithFixedPartitionKey"/>'s value (where
    /// configured), otherwise <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/>.</param>
    /// <returns>The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> to use.</returns>
    /// <remarks>Unlike <see cref="GetPartitionKey(TModel)"/>, there is no model to write back onto here — a <b>Get</b>/<b>Delete</b> reads/removes by key and has no document body to reconcile.</remarks>
    public PartitionKey GetPartitionKey(string? partitionKey) => ToPartitionKey(GetPartitionKeyValue(partitionKey));

    /// <summary>
    /// Converts a raw partition key <paramref name="value"/> (as resolved by <see cref="GetPartitionKeyValue(TModel)"/> or <see cref="WithFixedPartitionKey"/>) to its corresponding
    /// <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.
    /// </summary>
    /// <param name="value">The raw partition key value.</param>
    /// <returns><see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> where <paramref name="value"/> is <see langword="null"/>; otherwise, <c>new <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>(value)</c>.</returns>
    /// <remarks><see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> (no partition key at all - the simplest possible container shape) is deliberately distinct from what the SDK's own
    /// <c>new PartitionKey((string?)null)</c> would produce (an explicit JSON <see langword="null"/> partition-key <i>value</i>, for a container that still has a defined partition key path) - this
    /// method is the single place that distinction is made, so every caller resolving a possibly-absent partition key goes through it rather than constructing <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>
    /// directly from a nullable string.</remarks>
    internal static PartitionKey ToPartitionKey(string? value) => value is null ? PartitionKey.None : new PartitionKey(value);

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
    /// <remarks>This is the additive extension point for filters that are not one of the built-in <see cref="WithTenantFilter"/>/<see cref="WithLogicalDeleteFilter"/>/<see cref="WithTypeDiscriminator(string?)"/>
    /// concerns — for example, an authorization-related filter that restricts which documents a given caller may see or mutate. The <paramref name="nonQueryResult"/> enables a different result to be returned for
    /// non-query operations when the filter excludes; for example, a <see cref="Result.AuthenticationError"/> could be returned for an authorization filter. Where a <paramref name="nonQueryResult"/> is <i>not</i>
    /// specified then the specified <paramref name="filter"/> is only applied for queries (see <see cref="ApplyFilters"/>) and has no effect on non-query operations (see <see cref="CheckFilters"/>).
    /// <para>The <see cref="CosmosDbArgs.BypassFilters"/> can be used to bypass filters registered with <paramref name="allowFilterBypass"/> set to <see langword="true"/>.</para>
    /// <para>Each filter is applied individually, in the order specified.</para>
    /// <para>The <paramref name="filter"/> is evaluated in two different contexts and must be expressible in both: against the real Cosmos DB LINQ query (translated to a Cosmos DB SQL query) for
    /// <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/>, and against an in-memory, single-item <see cref="IQueryable{T}"/> (LINQ-to-Objects) for the non-query pre-check performed by
    /// <see cref="CheckFilters"/> — this is intentional, avoiding a second round-trip to re-verify a model already in hand, but it means the predicate cannot use Cosmos-LINQ-only constructs that have
    /// no meaning against an in-memory sequence.</para>
    /// <para>A query-only <paramref name="filter"/> (no <paramref name="nonQueryResult"/> — see <see cref="HasQueryOnlyFilters"/>) is <b>not</b> supported for a <typeparamref name="TModel"/> used in a
    /// <c>CoreEx.Cosmos.Extended.CosmosDbMultiSetExtensions.SelectMultiSetAsync</c> multi-set query — an arbitrary <paramref name="filter"/> cannot be safely translated into that query's raw SQL text, so
    /// doing so throws <see cref="NotSupportedException"/> rather than silently returning documents an equivalent single-set query would have excluded. Supply a <paramref name="nonQueryResult"/> to make
    /// the filter also enforced per-item (consistent with multi-set's own per-item <c>CheckModel</c> check) if it needs to be usable there.</para></remarks>
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
    /// Indicates whether any <see cref="WithFilter"/> filter has been specified <i>without</i> a <c>nonQueryResult</c> (i.e. a filter that only affects <see cref="ApplyFilters"/> and has no effect on
    /// <see cref="CheckFilters"/>/non-query operations).
    /// </summary>
    /// <remarks>Consumed by <c>CoreEx.Cosmos.Extended.IMultiSetArgs.BuildFilterClause</c> to guard against a multi-set query silently disagreeing with the equivalent <see cref="CosmosDbQuery{TModel}"/>:
    /// unlike <see cref="WithTenantFilter"/>/<see cref="WithLogicalDeleteFilter"/>, an arbitrary <see cref="WithFilter"/> predicate cannot be safely translated into the raw SQL text a multi-set query
    /// requires, so a <typeparamref name="TModel"/> with a query-only filter configured is not supported for multi-set use - see <see cref="WithFilter"/> remarks for why registering a <c>nonQueryResult</c>
    /// (making the filter also enforced per-item via <see cref="CheckFilters"/>, exactly as multi-set's own per-item <c>CheckModel</c> call already requires) is the supported alternative.</remarks>
    public bool HasQueryOnlyFilters => _filters.Any(f => f.NonQueryResult is null);

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
    /// whether this filter has been configured; this only controls whether <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/> also applies the equivalent predicate (and, for <c>Extended</c>
    /// multi-set queries, an equivalent defensive server-side SQL predicate - see <see cref="IsTenantFilterEnabled"/>).</remarks>
    public CosmosDbModelOptions<TModel> WithTenantFilter()
    {
        if (!TenantSupport.IsSupported)
            throw new NotSupportedException($"{nameof(WithTenantFilter)} is not supported; model must implement {nameof(IReadOnlyTenantId)} to enable.");

        _tenantFilterEnabled = true;
        return this;
    }

    /// <summary>
    /// Indicates whether the <see cref="WithTenantFilter"/> query-only filter has been configured.
    /// </summary>
    /// <remarks>Consumed by <c>CoreEx.Cosmos.Extended.CosmosDbMultiSetExtensions.SelectMultiSetAsync</c> to add an equivalent, defensive (<c>IS_DEFINED</c>-guarded) server-side SQL predicate - it has no
    /// bearing on the always-applied, per-item <c>CosmosDbContainer{TModel}.CheckModel</c> tenant check.</remarks>
    public bool IsTenantFilterEnabled => _tenantFilterEnabled;

    /// <summary>
    /// Adds a logical delete (<see cref="IReadOnlyLogicallyDeleted.IsDeleted"/>) query-only filter (where <see cref="LogicalDeleteSupport"/> is supported).
    /// </summary>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Non-query operations always check the <see cref="IReadOnlyLogicallyDeleted.IsDeleted"/> state where supported irrespective of whether this filter has been configured; this only controls whether
    /// <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/> also applies the equivalent predicate (and, for <c>Extended</c> multi-set queries, an equivalent defensive server-side SQL predicate -
    /// see <see cref="IsLogicalDeleteFilterEnabled"/>).</remarks>
    public CosmosDbModelOptions<TModel> WithLogicalDeleteFilter()
    {
        if (!LogicalDeleteSupport.IsSupported)
            throw new NotSupportedException($"{nameof(WithLogicalDeleteFilter)} is not supported; model must implement {nameof(IReadOnlyLogicallyDeleted)} to enable.");

        _logicalDeleteFilterEnabled = true;
        return this;
    }

    /// <summary>
    /// Indicates whether the <see cref="WithLogicalDeleteFilter"/> query-only filter has been configured.
    /// </summary>
    /// <remarks>Consumed by <c>CoreEx.Cosmos.Extended.CosmosDbMultiSetExtensions.SelectMultiSetAsync</c> to add an equivalent, defensive (<c>IS_DEFINED</c>-guarded) server-side SQL predicate - it has no
    /// bearing on the always-applied, per-item <c>CosmosDbContainer{TModel}.CheckModel</c> logical-delete check.</remarks>
    public bool IsLogicalDeleteFilterEnabled => _logicalDeleteFilterEnabled;

    /// <summary>
    /// Adds a type discriminator (<see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/>) query-only filter (where <see cref="TypeDiscriminatorSupport"/> is supported), enabling several business model
    /// types to safely share the same container/partition.
    /// </summary>
    /// <param name="typeDiscriminator">The type discriminator value to filter on; defaults to the <see cref="Schemas.SchemaAttribute.Name"/> where specified, otherwise the <typeparamref name="TModel"/> name
    /// (i.e. the same default resolution used by <c>Model.PrepareTypeDiscriminator</c> when stamping a model prior to create/update).</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>An explicit <paramref name="typeDiscriminator"/> override is enforced end-to-end: <see cref="ApplyTypeDiscriminator(TModel)"/> re-stamps it onto the model on every Create/Update/Upsert
    /// <i>after</i> <c>Model.PrepareCreate</c>/<c>PrepareUpdate</c>/<c>PrepareTypeDiscriminator</c> have already stamped their own default (<see cref="Schemas.SchemaAttribute.Name"/>/type name) - without
    /// this, a model persisted with an explicit override here would instead be written with the default discriminator, immediately fail <see cref="IsTypeDiscriminatorMismatch(TModel)"/>'s check against
    /// this configured value, and become invisible to this container's own queries/point reads.</remarks>
    public CosmosDbModelOptions<TModel> WithTypeDiscriminator(string? typeDiscriminator = null)
    {
        if (!TypeDiscriminatorSupport.IsSupported)
            throw new NotSupportedException($"{nameof(WithTypeDiscriminator)} is not supported; model must implement {nameof(IReadOnlyTypeDiscriminator)} to enable.");

        _typeDiscriminatorValue = string.IsNullOrEmpty(typeDiscriminator)
            ? (Schema.TryGetMetadata<TModel>(out var metadata) ? metadata.Name : typeof(TModel).Name)
            : typeDiscriminator;

        _typeDiscriminatorFilterEnabled = true;
        return this;
    }

    /// <summary>
    /// Applies the configured <see cref="WithTypeDiscriminator(string?)"/> value (where configured and <typeparamref name="TModel"/> supports the <i>mutable</i> <see cref="ITypeDiscriminator"/>) to the
    /// <paramref name="model"/>, overriding whatever default value <c>Model.PrepareCreate</c>/<c>PrepareUpdate</c>/<c>PrepareTypeDiscriminator</c> already stamped.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <remarks>Must be called on every Create/Update/Upsert path <b>after</b> <c>Model.PrepareCreate</c>/<c>PrepareUpdate</c>/<c>PrepareTypeDiscriminator</c> - see <see cref="WithTypeDiscriminator(string?)"/>
    /// remarks for why. A no-op where <see cref="WithTypeDiscriminator(string?)"/> has not been configured, or where <typeparamref name="TModel"/> only supports the read-only <see cref="IReadOnlyTypeDiscriminator"/>
    /// (nothing to write back to in that case) - the value already stamped by <c>Model.PrepareCreate</c>/<c>PrepareUpdate</c>/<c>PrepareTypeDiscriminator</c> (its own default resolution) is then left as-is.</remarks>
    public void ApplyTypeDiscriminator(TModel model)
    {
        if (!_typeDiscriminatorFilterEnabled || model.ThrowIfNull() is not ITypeDiscriminator td)
            return;

        td.TypeDiscriminator = _typeDiscriminatorValue;
    }

    /// <summary>
    /// Determines whether the specified <paramref name="model"/>'s <see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/> disagrees with the configured <see cref="WithTypeDiscriminator(string?)"/> value.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns><see langword="true"/> where <see cref="WithTypeDiscriminator(string?)"/> is configured and the model's discriminator does not match; otherwise, <see langword="false"/> (including where
    /// <see cref="WithTypeDiscriminator(string?)"/> was never configured, since there is then nothing to isolate against).</returns>
    /// <remarks>Used by <see cref="CosmosDbContainer{TModel}.CheckModel"/> to apply the same type-discriminator isolation to point <c>Get</c>/<c>Delete</c>/<c>Update</c> operations that
    /// <see cref="ApplyFilters"/> already applies to queries - without this, a shared multi-type container could deserialize, delete, or replace a same-id/partition document belonging to a different
    /// configured type, silently bypassing the isolation <see cref="WithTypeDiscriminator(string?)"/> promises. Unlike <see cref="IsTenantFilterEnabled"/>/<see cref="IsLogicalDeleteFilterEnabled"/> (whose
    /// equivalent <c>CheckModel</c> checks are unconditional whenever <typeparamref name="TModel"/> merely implements the relevant interface), this check is deliberately gated on
    /// <see cref="WithTypeDiscriminator(string?)"/> having been called - there is no ambient "expected type" to compare against otherwise (tenant compares to <see cref="ExecutionContext.TenantId"/>;
    /// logical-delete compares to a fixed <see langword="false"/>), so an ungated check could wrongly reject a document in a single-type container that never opted into discriminator isolation.</remarks>
    internal bool IsTypeDiscriminatorMismatch(TModel model) => _typeDiscriminatorFilterEnabled && model is IReadOnlyTypeDiscriminator td && td.TypeDiscriminator != _typeDiscriminatorValue;

    /// <summary>
    /// Indicates whether the <see cref="WithTypeDiscriminator(string?)"/> filter has been configured.
    /// </summary>
    /// <remarks>Consumed by <see cref="CosmosDbContainer{TModel}.DeleteWithResultInternalAsync"/>'s equivalent fast-path check (alongside <see cref="IsTenantFilterEnabled"/>/<see cref="IsLogicalDeleteFilterEnabled"/>/
    /// <see cref="HasFilters"/>) to decide whether a plain, key-based delete can skip the pre-read <c>CheckModel</c> performs - see <see cref="IsTypeDiscriminatorMismatch(TModel)"/> remarks for why, unlike
    /// those two, this one genuinely gates <c>CheckModel</c>'s own type-discriminator check too.</remarks>
    public bool IsTypeDiscriminatorFilterEnabled => _typeDiscriminatorFilterEnabled;

    /// <summary>
    /// Applies the configured query-only filters (<see cref="WithTenantFilter"/>, <see cref="WithLogicalDeleteFilter"/>, <see cref="WithTypeDiscriminator(string?)"/> and any additive
    /// <see cref="WithFilter"/> registrations), plus an automatic outbox-document exclusion predicate, to the <paramref name="query"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>; used only to check <see cref="CosmosDbArgs.BypassFilters"/> against any bypassable <see cref="WithFilter"/> registrations.</param>
    /// <param name="query">The <see cref="IQueryable{TModel}"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/> resolved by the owning <see cref="ICosmosDb"/> (see <see cref="ICosmosDb.ExecutionContext"/>); used only by the <see cref="WithTenantFilter"/>
    /// predicate, where configured.</param>
    /// <returns>The filtered <see cref="IQueryable{TModel}"/>.</returns>
    /// <remarks>Whenever <see cref="IdentifierSupport"/> is supported, this <b>always</b> also excludes any <see cref="CosmosDbOutboxEvent"/> documents that may be physically co-located in the same
    /// container (a <see cref="CosmosDbUnitOfWork"/>-paired outbox write has no other choice — Cosmos DB's <c>TransactionalBatch</c> only supports a single container, so a dedicated outbox container,
    /// like a relational store's separate table, is not possible) — no <c>WithXxx()</c> opt-in call is needed, and this applies even to a container that has never itself been used with a
    /// <see cref="CosmosDbUnitOfWork"/>. This is intentional and safe unconditionally: no legitimate business key would ever start with <see cref="CosmosDbOutboxEvent.OutboxKeyPrefix"/>, so the predicate
    /// can never wrongly exclude real business data, and a business developer is never required to add or even be aware of any interface/discriminator solely to accommodate this — unlike an earlier design
    /// considered and rejected, which would have reused <see cref="ITypeDiscriminator"/> for this purpose (conflating a genuine business-modeling decision with an unrelated infrastructure concern).
    /// <para>Uses the same cast-to-interface-in-a-LINQ-predicate shape already used above for <see cref="WithTenantFilter"/>/<see cref="WithLogicalDeleteFilter"/>/<see cref="WithTypeDiscriminator(string?)"/>,
    /// not a new or unproven LINQ pattern.</para>
    /// <para>Where <see cref="IdentifierSupport"/> is <em>not</em> supported (a <typeparamref name="TModel"/> using a composite <see cref="IEntityKey.EntityKey"/>/<see cref="WithFormatIdentifier"/> without
    /// also implementing <see cref="IReadOnlyIdentifier{String}"/>), the exclusion is not simply skipped: <see cref="ResolveOutboxIdExclusion"/> falls back to locating whichever property is actually
    /// mapped to the reserved Cosmos DB <c>id</c> JSON property (via <see cref="JsonPropertyNameAttribute"/>) and applies the identical predicate against it directly, since Cosmos DB requires every
    /// physical document to have an <c>id</c> regardless of which CoreEx interfaces a model implements. Only where no such property can be found at all (a working <typeparamref name="TModel"/> would
    /// always have one, since Cosmos DB itself would otherwise reject every write) is the exclusion genuinely skipped.</para></remarks>
    public IQueryable<TModel> ApplyFilters(CosmosDbArgs args, IQueryable<TModel> query, ExecutionContext executionContext)
    {
        args.ThrowIfNull();
        query.ThrowIfNull();

        if (IdentifierSupport.IsSupported)
            query = query.Where(m => !((IReadOnlyIdentifier<string>)m).Id!.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));
        else if (_outboxIdExclusion.Value is not null)
            query = query.Where(_outboxIdExclusion.Value);

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
