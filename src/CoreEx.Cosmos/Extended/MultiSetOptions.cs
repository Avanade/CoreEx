namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides the options for a <see cref="CosmosDbMultiSetExtensions.SelectMultiSetAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/> multi-set query.
/// </summary>
/// <remarks>Bundles the per-call inputs (<see cref="PartitionKey"/>, <see cref="Args"/>, <see cref="MultiSetArgs"/>) that would otherwise need to be threaded through as separate, ever-growing method
/// parameters/overloads. A future capability addition (e.g. a query-level row-count cap) can be layered on as an additional property here without altering the method's signature or any existing call
/// site - i.e. without a breaking method-contract change.</remarks>
public sealed record MultiSetOptions
{
    /// <summary>
    /// Gets the raw partition key value.
    /// </summary>
    /// <remarks><see langword="null"/> resolves to <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/> unless overridden by an already-configured, non-<see langword="null"/> partition key on
    /// <see cref="Args"/>'s <see cref="CosmosDbArgs.QueryRequestOptions"/> (which takes precedence - see <see cref="CosmosDbMultiSetExtensions"/>'s remarks).</remarks>
    public string? PartitionKey { get; init; }

    /// <summary>
    /// Gets the <see cref="CosmosDbArgs"/>.
    /// </summary>
    /// <remarks>Defaults to the owning <see cref="ICosmosDb.DbArgs"/> where not specified.</remarks>
    public CosmosDbArgs? Args { get; init; }

    /// <summary>
    /// Gets the one or more <see cref="IMultiSetArgs"/>.
    /// </summary>
    public IEnumerable<IMultiSetArgs> MultiSetArgs { get; init; } = [];
}
