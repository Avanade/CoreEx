namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Enables the <see cref="ICosmosDb"/> multi-set arguments for a specific <typeparamref name="TModel"/>.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
/// <remarks><see cref="IMultiSetArgs.ModelType"/> and <see cref="IMultiSetArgs.TypeDiscriminator"/> are resolved purely from <typeparamref name="TModel"/> - via <see cref="Schemas.Schema.TryGetMetadata{TEntity}(out Schemas.SchemaAttribute)"/>,
/// falling back to <c>typeof(TModel).Name</c> - mirroring <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/>'s own default resolution exactly, since both need to agree on the same value for
/// a given <typeparamref name="TModel"/>. A <typeparamref name="TModel"/> that does not implement <see cref="IReadOnlyTypeDiscriminator"/> cannot be used in a multi-set query; concrete implementations
/// (see <see cref="MultiSetSingleArgs{TModel}"/>/<see cref="MultiSetCollArgs{TColl, TModel}"/>) enforce this via a static guard.</remarks>
public interface IMultiSetArgs<TModel> : IMultiSetArgs where TModel : class, IEntityKey, new()
{
    /// <inheritdoc/>
    Type IMultiSetArgs.ModelType => typeof(TModel);

    /// <inheritdoc/>
    string IMultiSetArgs.TypeDiscriminator
    {
        get
        {
            Schema.TryGetMetadata<TModel>(out var metadata);
            return metadata.Name ?? typeof(TModel).Name;
        }
    }

    /// <inheritdoc/>
    Result<bool> IMultiSetArgs.AddItem(ICosmosDb cosmosDb, string containerId, CosmosDbArgs args, object model)
    {
        var result = cosmosDb.ThrowIfNull().Container<TModel>(containerId.ThrowIfNullOrEmpty()).CheckModel(args.ThrowIfNull(), (TModel)model.ThrowIfNull(), OperationType.Get);
        if (result.IsFailure)
            return (Result)result;

        if (result.Value is null)
            return false; // Silently excluded by CheckModel (e.g. wrong tenant, logically deleted) - not added, and must not count as a received row.

        AddItem(result.Value);
        return true;
    }

    /// <inheritdoc/>
    string? IMultiSetArgs.BuildFilterClause(ICosmosDb cosmosDb, string containerId, JsonSerializerOptions jsonSerializerOptions, string parameterPrefix, IDictionary<string, object?> parameters)
    {
        var options = cosmosDb.ThrowIfNull().Container<TModel>(containerId.ThrowIfNullOrEmpty()).Options;

        // A query-only WithFilter (no nonQueryResult) is applied server-side by CosmosDbQuery<TModel> (see CosmosDbModelOptions<TModel>.ApplyFilters) but is not enforced per-item by CheckFilters/CheckModel
        // (by design - see WithFilter remarks), and cannot be safely translated into this multi-set query's raw SQL text (an arbitrary Func<IQueryable<TModel>, IQueryable<TModel>> has no such translation
        // outside of a real Cosmos LINQ query). Rather than silently returning documents a normal query would have excluded, fail fast and point the caller at the supported alternative.
        if (options.HasQueryOnlyFilters)
            throw new NotSupportedException(
                $"{typeof(TModel).Name} has one or more {nameof(CosmosDbModelOptions<>.WithFilter)} filter(s) registered without a 'nonQueryResult' (i.e. query-only filters); these cannot be " +
                $"used with a multi-set query as they cannot be safely translated into its raw SQL text, and would otherwise silently disagree with an equivalent {nameof(CosmosDbQuery<>)}. Either " +
                $"register the filter with a 'nonQueryResult' (making it also enforced per-item, consistent with multi-set's own per-item check), or do not use {typeof(TModel).Name} in a multi-set query.");

        if (!options.IsTenantFilterEnabled && !options.IsLogicalDeleteFilterEnabled)
            return null;

        var clauses = new List<string>(2);

        if (options.IsTenantFilterEnabled)
        {
            var property = jsonSerializerOptions.ThrowIfNull().PropertyNamingPolicy?.ConvertName(nameof(IReadOnlyTenantId.TenantId)) ?? nameof(IReadOnlyTenantId.TenantId);
            var parameterName = $"{parameterPrefix}_tenantId";
            parameters[parameterName] = cosmosDb.ExecutionContext.ThrowIfNull().TenantId;
            clauses.Add($"(NOT IS_DEFINED(c[\"{property}\"]) OR c[\"{property}\"] = {parameterName})");
        }

        if (options.IsLogicalDeleteFilterEnabled)
        {
            var property = jsonSerializerOptions.ThrowIfNull().PropertyNamingPolicy?.ConvertName(nameof(IReadOnlyLogicallyDeleted.IsDeleted)) ?? nameof(IReadOnlyLogicallyDeleted.IsDeleted);
            clauses.Add($"(NOT IS_DEFINED(c[\"{property}\"]) OR c[\"{property}\"] = false)");
        }

        return string.Join(" AND ", clauses);
    }

    /// <summary>
    /// Adds the already tenant/logical-delete/additive-filter-checked <paramref name="model"/> to the underlying result.
    /// </summary>
    /// <param name="model">The model.</param>
    void AddItem(TModel model);
}
