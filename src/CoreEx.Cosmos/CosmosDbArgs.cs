namespace CoreEx.Cosmos;

/// <summary>
/// Provides the <see cref="ICosmosDb"/>/<see cref="CosmosDbContainer{TModel}"/> arguments.
/// </summary>
/// <remarks>The <see cref="CosmosDbArgs"/> is intended, and expected, to be immutable. Therefore, when implementing/extending, please ensure additional properties are enabled as such to ensure there are
/// not any unintended side-effects.
/// <para>There is deliberately no static/default <c>PartitionKey</c> here; the partition key is derived per-model (see <see cref="CosmosDbModelOptions{TModel}.WithPartitionKey"/>) since a
/// <see cref="CosmosDbModelOptions{TModel}"/> instance is commonly shared/cached across callers and cannot itself close over one fixed value.</para></remarks>
public record class CosmosDbArgs
{
    /// <summary>
    /// Indicates whether a <c>404 Not Found</c> response for a <b>Get</b> operation results in a <see langword="null"/> (rather than a <see cref="NotFoundException"/>) for the throwing (non-<c>WithResult</c>) method overloads.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>. The <c>WithResult</c> (ROP) overloads always return a <see cref="Result.NotFoundError"/> failure on a <c>404</c> irrespective of this setting.</remarks>
    public bool NullOnNotFound { get; init; } = true;

    /// <summary>
    /// Indicates whether the model's <see cref="IETag.ETag"/> is automatically mapped into the <c>ItemRequestOptions.IfMatchEtag</c> for an <b>Update</b> operation to enable native Cosmos DB optimistic concurrency.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>. Where the model does not implement <see cref="IETag"/> this has no effect.</remarks>
    public bool AutoMapETag { get; init; } = true;

    /// <summary>
    /// Indicates whether the data should be refreshed (re-selected) after a <b>Create</b> or <b>Update</b> operation.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Given the Cosmos DB SDK already returns the persisted resource (see <see cref="ItemResponse{T}.Resource"/>) as part of a create/update/upsert response, this is
    /// rarely required; it is provided primarily for structural/behavioral parity with other CoreEx data access layers.</remarks>
    public bool Refresh { get; init; } = false;

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.Azure.Cosmos.ItemRequestOptions"/> applied to point operations (Get/Create/Update/Delete).
    /// </summary>
    public ItemRequestOptions? ItemRequestOptions { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.Azure.Cosmos.QueryRequestOptions"/> applied to <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/> operations.
    /// </summary>
    public QueryRequestOptions? QueryRequestOptions { get; init; }

    /// <summary>
    /// Indicates whether to bypass any filters configured via <see cref="CosmosDbModelOptions{TModel}.WithFilter"/> that were opted in to being bypassable (<c>allowFilterBypass</c>).
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Has no effect on the built-in tenant/logical-delete/type-discriminator filters, nor on a <see cref="CosmosDbModelOptions{TModel}.WithFilter"/>
    /// registration where <c>allowFilterBypass</c> was not specified as <see langword="true"/>.</remarks>
    public bool BypassFilters { get; init; } = false;
}
