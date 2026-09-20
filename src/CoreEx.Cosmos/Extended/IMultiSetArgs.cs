namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Enables the <see cref="ICosmosDb"/> multi-set arguments.
/// </summary>
/// <remarks>Unlike the relational <c>CoreEx.Database.Extended.IMultiSetArgs</c> - where each result set is identified purely by its <i>position</i> within a single multi-statement query - a
/// Cosmos DB multi-set query has no equivalent notion of ordered result sets; instead, a single round-trip returns a mixed stream of documents from the same container/partition, each demuxed to
/// its corresponding <see cref="IMultiSetArgs{TModel}"/> via its resolved <see cref="ResolveTypeDiscriminator(ICosmosDb, string)"/> value (see <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/>).</remarks>
public interface IMultiSetArgs : IMultiSetArgsCore
{
    /// <summary>
    /// Gets the model <see cref="Type"/>.
    /// </summary>
    Type ModelType { get; }

    /// <summary>
    /// Resolves the <see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/> value used to demux a document to this <see cref="IMultiSetArgs"/>.
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <remarks>Resolved from the target <see cref="ModelType"/>'s own <c>CosmosDbModelOptions{TModel}.EffectiveTypeDiscriminator</c> - the explicit <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/>
    /// override where configured, otherwise the same schema/CLR-type-name default <c>Model.PrepareTypeDiscriminator</c> stamps automatically - so this always agrees with whatever value is actually
    /// persisted on documents of this type, never merely the unconfigured default.</remarks>
    string ResolveTypeDiscriminator(ICosmosDb cosmosDb, string containerId);

    /// <summary>
    /// Adds the <paramref name="model"/> - having first been checked (see <c>CosmosDbContainer{TModel}.CheckModel</c>) for tenant/logical-delete/additive-filter eligibility - to the underlying result.
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The deserialized model.</param>
    /// <returns>The <see cref="Result{T}"/>, whose <see cref="Result{T}.Value"/> indicates whether the <paramref name="model"/> was actually added (<see langword="true"/>) or was silently excluded by the
    /// per-item check (<see langword="false"/>).</returns>
    /// <remarks>Invoked once per matching document by the multi-set query engine (see <c>CosmosDbMultiSetExtensions.SelectMultiSetAsync</c>); a model that fails the per-item check (see
    /// <c>CosmosDbContainer{TModel}.CheckModel</c>) is silently excluded (not added) where that check itself resolves to <see langword="null"/> (e.g. wrong tenant, logically deleted) - exactly as a
    /// single-item <see cref="CosmosDbContainer{TModel}.GetAsync(CompositeKey, string, CancellationToken)"/> would exclude it - as opposed to a genuine <see cref="Result.IsFailure"/> (e.g. a
    /// <see cref="CosmosDbModelOptions{TModel}.WithFilter"/> that itself fails), which is propagated rather than swallowed. The caller (<c>CosmosDbMultiSetExtensions.SelectMultiSetInternalAsync</c>) relies
    /// on the returned <see cref="Result{T}.Value"/> - not merely <see cref="Result.IsSuccess"/> - to decide whether to count the document towards <see cref="IMultiSetArgsCore.MinimumRows"/>/
    /// <see cref="IMultiSetArgsCore.MaximumRows"/>/<see cref="IMultiSetArgsCore.StopOnNull"/>; a silently-excluded document must never count as a received row, otherwise a mandatory single-item read could
    /// wrongly satisfy <see cref="IMultiSetArgsCore.MinimumRows"/> while never actually invoking its result callback.</remarks>
    Result<bool> AddItem(ICosmosDb cosmosDb, string containerId, CosmosDbArgs args, object model);

    /// <summary>
    /// Builds an additional, defensive server-side SQL predicate (adding any required parameters into <paramref name="parameters"/>) enforcing this <see cref="IMultiSetArgs"/>'s <see cref="ModelType"/>'s
    /// configured tenant/logical-delete query filters (see <see cref="CosmosDbModelOptions{TModel}.WithTenantFilter"/>/<see cref="CosmosDbModelOptions{TModel}.WithLogicalDeleteFilter"/>), or
    /// <see langword="null"/> where neither is configured for this model.
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> used to resolve the tenant/logical-delete JSON property names (mirroring how the type-discriminator property name
    /// itself is resolved - see <see cref="CosmosDbMultiSetExtensions"/>'s remarks).</param>
    /// <param name="parameterPrefix">The unique SQL parameter name prefix for this <see cref="IMultiSetArgs"/>, avoiding collisions with other <see cref="IMultiSetArgs"/> in the same query.</param>
    /// <param name="parameters">The dictionary any required SQL parameter values are added to.</param>
    /// <remarks>This is purely a server-side (RU/bandwidth) optimization - the same tenant/logical-delete eligibility is always re-checked, unconditionally, per returned item via
    /// <see cref="AddItem(ICosmosDb, string, CosmosDbArgs, object)"/> (see <c>CosmosDbContainer{TModel}.CheckModel</c>); a document that somehow slips through this predicate is still excluded there.
    /// Each predicate defensively lets through a document that predates the property being added at all (<c>IS_DEFINED</c>-guarded), so historical documents are not silently excluded by a filter
    /// enabled after they were originally written - the same intent as <see cref="CosmosDbModelOptions{TModel}.ApplyFilters"/>, but expressed defensively since a multi-set query commonly spans many
    /// document shapes, of varying vintage, within one long-lived container.</remarks>
    string? BuildFilterClause(ICosmosDb cosmosDb, string containerId, JsonSerializerOptions jsonSerializerOptions, string parameterPrefix, IDictionary<string, object?> parameters);
}
