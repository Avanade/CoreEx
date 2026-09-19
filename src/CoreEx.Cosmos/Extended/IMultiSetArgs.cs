namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Enables the <see cref="ICosmosDb"/> multi-set arguments.
/// </summary>
/// <remarks>Unlike the relational <c>CoreEx.Database.Extended.IMultiSetArgs</c> - where each result set is identified purely by its <i>position</i> within a single multi-statement query - a
/// Cosmos DB multi-set query has no equivalent notion of ordered result sets; instead, a single round-trip returns a mixed stream of documents from the same container/partition, each demuxed to
/// its corresponding <see cref="IMultiSetArgs{TModel}"/> via its <see cref="TypeDiscriminator"/> (see <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/>).</remarks>
public interface IMultiSetArgs : IMultiSetArgsCore
{
    /// <summary>
    /// Gets the model <see cref="Type"/>.
    /// </summary>
    Type ModelType { get; }

    /// <summary>
    /// Gets the <see cref="IReadOnlyTypeDiscriminator.TypeDiscriminator"/> value used to demux a document to this <see cref="IMultiSetArgs"/>.
    /// </summary>
    string TypeDiscriminator { get; }

    /// <summary>
    /// Adds the <paramref name="model"/> - having first been checked (see <c>CosmosDbContainer{TModel}.CheckModel</c>) for tenant/logical-delete/additive-filter eligibility - to the underlying result.
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="model">The deserialized model.</param>
    /// <remarks>Invoked once per matching document by the multi-set query engine (see <c>CosmosDbMultiSetExtensions.SelectMultiSetAsync</c>); a model that fails the per-item check is silently
    /// excluded (not added), exactly as a single-item <see cref="CosmosDbContainer{TModel}.GetAsync(CompositeKey, string, CancellationToken)"/> would exclude it.</remarks>
    void AddItem(ICosmosDb cosmosDb, string containerId, CosmosDbArgs args, object model);

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
