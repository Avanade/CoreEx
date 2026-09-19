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
