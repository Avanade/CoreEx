namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides options for the <see cref="EfDbModel{TModel}"/>.
/// </summary>
public class EfDbModelOptions<TModel> where TModel : class
{
    private readonly List<(Func<IQueryable<TModel>, IQueryable<TModel>> Filter, Func<TModel, OperationType, Result>? NonQueryResult, bool AllowFilterBypass)> _filters = [];
    private Func<TModel, CompositeKey> _getKey = m => m is IEntityKey ek ? ek.EntityKey : throw new InvalidOperationException($"The model does not implement {nameof(IEntityKey)}; as such, the {nameof(WithGetKey)} must be specified to enable.");
    private Func<TModel, OperationType, Result>? _onBeforeCreateOrUpdate;
    private Func<TModel, TModel, bool>? _updateModelMapper;

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
    /// Indicates whether <see cref="Entities.Abstractions.IIdentifier"/> and/or <see cref="Entities.Abstractions.IReadOnlyIdentifier"/> is supported for the <typeparamref name="TModel"/>.
    /// </summary>
    public FeatureSupport IdentifierSupport { get; } = FeatureSupport.Determine<TModel, Entities.Abstractions.IIdentifier, Entities.Abstractions.IReadOnlyIdentifier>();

    /// <summary>
    /// Gets the default <see cref="EfDbArgs"/>.
    /// </summary>
    public EfDbArgs? Args { get; private set; }

    /// <summary>
    /// Sets (overrides) the default <see cref="Args"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public EfDbModelOptions<TModel> WithArgs(EfDbArgs? args)
    {
        Args = args;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the function to get the <see cref="CompositeKey"/> for the <typeparamref name="TModel"/>.
    /// </summary>
    /// <param name="getKey">The function to get the key.</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>By default, where the <typeparamref name="TModel"/> implements <see cref="IEntityKey"/>, that is used; otherwise, an <see cref="InvalidOperationException"/> is thrown.</remarks>
    public EfDbModelOptions<TModel> WithGetKey(Func<TModel, CompositeKey> getKey)
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
    /// Adds a filter to be applied to all operations (get, create, update, delete, and query).
    /// </summary>
    /// <param name="filter">The filter query to apply.</param>
    /// <param name="nonQueryResult">The optional <see cref="Result"/> to return for non-query operations when the filter excludes.</param>
    /// <param name="allowFilterBypass">Indicates whether the filter can be bypassed via the <see cref="EfDbArgs.BypassFilters"/>; defaults to <see langword="false"/>.</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="nonQueryResult"/> enables different results to be returned for non-query operations when the filter excludes; for example, a <see cref="Result.AuthenticationError"/> 
    /// could be returned for an authorization filter. Where a <paramref name="nonQueryResult"/> is <i>not</i> specified then the specified <paramref name="filter"/> is only applied for queries.
    /// <para>This is not a replacement for EF Core's built-in filtering mechanisms (see <see href="https://learn.microsoft.com/en-us/ef/core/querying/filters"/>); but, should be considered additive. EF built-in filtering can be used
    /// to enable the likes of logical delete and tenant support to ensure the broadest usage. Note that the <see cref="EfDbModel{TModel}"/> non-query operations handle the logical delete (<see cref="ILogicallyDeleted"/>) and
    /// tenant (<see cref="ITenantId"/>) checks automatically internally.</para>
    /// <para>The <see cref="EfDbArgs.BypassFilters"/> can be used to bypass these filters for queries as required.</para>
    /// <para>Each filter is applied individually in the order specified.</para>
    /// </remarks>
    public EfDbModelOptions<TModel> WithFilter(Func<IQueryable<TModel>, IQueryable<TModel>> filter, Func<TModel, OperationType, Result>? nonQueryResult = null, bool allowFilterBypass = true)
    {
        _filters.Add((filter.ThrowIfNull(), nonQueryResult, allowFilterBypass));
        return this;
    }

    /// <summary>
    /// Adds a logical delete (<see cref="ILogicallyDeleted.IsDeleted"/>) query-only filter (where <see cref="LogicalDeleteSupport"/> is supported).
    /// </summary>
    /// <param name="allowFilterBypass">Indicates whether the filter can be bypassed via the <see cref="EfDbArgs.BypassFilters"/>; defaults to <see langword="false"/>.</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public EfDbModelOptions<TModel> WithLogicalDeleteFilter(bool allowFilterBypass = false)
    {
        if (LogicalDeleteSupport.IsSupported)
            WithFilter(q => q.Where(m => !((IReadOnlyLogicallyDeleted)m).IsDeleted), allowFilterBypass: allowFilterBypass);
        else
            throw new NotSupportedException($"{nameof(WithLogicalDeleteFilter)} is not supported; model must implement {nameof(IReadOnlyLogicallyDeleted)} to enable.");

        return this;
    }

    /// <summary>
    /// Adds a tenant (<see cref="IReadOnlyTenantId.TenantId"/>) query-only filter (where <see cref="TenantSupport"/> is supported).
    /// </summary>
    /// <param name="allowFilterBypass">Indicates whether the filter can be bypassed via the <see cref="EfDbArgs.BypassFilters"/>; defaults to <see langword="false"/>.</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public EfDbModelOptions<TModel> WithTenantFilter(bool allowFilterBypass = false)
    {
        if (TenantSupport.IsSupported)
        {
            WithFilter(q =>
            {
                var tenantId = ExecutionContext.Current.TenantId;
                return q.Where(m => ((IReadOnlyTenantId)m).TenantId == tenantId);
            }, allowFilterBypass: allowFilterBypass);
        }
        else
            throw new NotSupportedException($"{nameof(WithTenantFilter)} is not supported; model must implement {nameof(IReadOnlyTenantId)} to enable.");

        return this;
    }

    /// <summary>
    /// Indicates whether any filters have been specified.
    /// </summary>
    /// <returns><see langword="true"/> indicates that an authorization filter has been specified; otherwise, <see langword="false"/>.</returns>
    /// <remarks>See <see cref="WithFilter"/> for more information.</remarks>
    public bool HasFilters => _filters.Count > 0;

    /// <summary>
    /// Applies the filters to the <paramref name="query"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="query">The <see cref="IQueryable{TModel}"/>.</param>
    /// <returns>The filtered <see cref="IQueryable{TModel}"/>.</returns>
    /// <remarks>This applies all specified filters to the <paramref name="query"/> excluding the non-query result handling; unless, <see cref="EfDbArgs.BypassFilters"/> is set to <see langword="true"/>.
    /// <para>See <see cref="WithFilter"/> for more information.</para></remarks>
    public IQueryable<TModel> ApplyFilters(EfDbArgs args, IQueryable<TModel> query)
    {
        query.ThrowIfNull();
        if (!HasFilters)
            return query;

        foreach (var (filter, _, allowFilterBypass) in _filters)
        {
            // Bypass filter where selected to do so and allowed.
            if (args.BypassFilters && allowFilterBypass)
                continue;

            // Apply the filter.
            query = filter(query);
        }

        return query;
    }

    /// <summary>
    /// Checks the non-query filters against the <paramref name="model"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <param name="model">The model.</param>
    /// <param name="operationType">The <see cref="OperationType"/>.</param>
    /// <returns>The <see cref="Result"/> of the filters check.</returns>
    /// <remarks>This checks all specified filters against the <paramref name="model"/> and executes the corresponding non-query result handling.
    /// <para>See <see cref="WithFilter"/> for more information.</para></remarks>
    public Result<TModel?> CheckFilters(EfDbArgs args, TModel? model, OperationType operationType)
    {
        if (model is null || !HasFilters)
            return Result.Ok(model);

        var q = new[] { model.ThrowIfNull() }.AsQueryable();

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

        // Sweet!
        return Result.Ok<TModel?>(model);
    }

    /// <summary>
    /// Sets (or overrides) the function to perform processing on the model prior to create or update.
    /// </summary>
    /// <param name="onBeforeCreateOrUpdate">The function.</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    public EfDbModelOptions<TModel> WithOnBeforeCreateOrUpdate(Func<TModel, OperationType, Result> onBeforeCreateOrUpdate)
    {
        _onBeforeCreateOrUpdate = onBeforeCreateOrUpdate.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Indicates whether <see cref="WithOnBeforeCreateOrUpdate(Func{TModel, OperationType, Result})"/> has been set.
    /// </summary>
    public bool HasOnBeforeCreateOrUpdate => _onBeforeCreateOrUpdate != null;

    /// <summary>
    /// Executes the <see cref="WithOnBeforeCreateOrUpdate(Func{TModel, OperationType, Result})"/> where set.
    /// </summary>
    /// <param name="model">The model value.</param>
    /// <param name="operationType">The <see cref="OperationType"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    internal Result OnBeforeCreateOrUpdate(TModel model, OperationType operationType) => _onBeforeCreateOrUpdate?.Invoke(model, operationType) ?? Result.Success;

    /// <summary>
    /// Sets (or overrides) the action to map the updated model into the existing (just read) model.
    /// </summary>
    /// <param name="updateModelMapper">The function to map the updated model (left) into the existing model (right).</param>
    /// <returns>The <see cref="EfDbModelOptions{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>
    /// The <paramref name="updateModelMapper"/> function takes two parameters: the updated model (first parameter) and the existing model (second parameter) and returns a <see cref="bool"/> indicating whether any changes were made.
    /// <para>This enables custom mapping logic to be specified for update operations; otherwise, by default, <see cref="Metadata.RuntimeMetadata.TryCopyInto{TFrom, TInto}(TFrom, TInto)"/> is used internally.</para></remarks>
    public EfDbModelOptions<TModel> WithUpdateModelMapper(Func<TModel, TModel, bool> updateModelMapper)
    {
        _updateModelMapper = updateModelMapper.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Maps the <paramref name="update"/> model into the <paramref name="existing"/> model.
    /// </summary>
    /// <param name="update">The updated model.</param>
    /// <param name="existing">The existing model.</param>
    /// <returns><see langword="true"/> where changes were made; otherwise, <see langword="false"/>.</returns>
    internal bool MapModelForUpdate(TModel update, TModel existing)
    {
        if (_updateModelMapper is null)
            return Metadata.RuntimeMetadata.TryCopyInto(update, existing);
        else
            return _updateModelMapper(update, existing);
    }
}