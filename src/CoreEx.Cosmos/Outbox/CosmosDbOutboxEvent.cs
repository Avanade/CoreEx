namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Represents a single transactional-outbox event document, written atomically alongside its paired business mutation by <see cref="CosmosDbEventPublisher"/> and physically co-located in the same
/// container/partition (forced by <see cref="TransactionalBatch"/>'s single-container/single-partition-key atomicity constraint - see <see cref="CosmosDbUnitOfWork"/>).
/// </summary>
/// <remarks>Recognized (and automatically excluded from ordinary business queries against the same container) via its reserved <see cref="OutboxKeyPrefix"/>-prefixed <see cref="Id"/> -
/// see <see cref="CosmosDbModelOptions{TModel}"/>'s automatic outbox-exclusion filter. A future relay (not built by this package) can read exactly these documents by querying for the same prefix instead of
/// excluding it - the mirror image of the same mechanism.
/// <para>Assumes the owning container's partition key path is <c>/partitionKey</c>, matching <see cref="CosmosDbItemBase"/>'s convention used throughout this package.</para></remarks>
public sealed class CosmosDbOutboxEvent : IIdentifier<string>, IPartitionKey, ITimeToLive
{
    /// <summary>
    /// Gets the reserved <see cref="Id"/> prefix used to identify an outbox event document (see <see cref="CosmosDbModelOptions{TModel}.ApplyFilters(CosmosDbArgs, IQueryable{TModel}, ExecutionContext)"/>'s
    /// automatic exclusion predicate, and <see cref="CosmosDbEventPublisher"/>, which constructs every <see cref="Id"/> as <c><![CDATA[CompositeKey.Create(OutboxKeyPrefix, Guid.NewGuid())]]></c>).
    /// </summary>
    /// <remarks>No legitimate business key would ever start with this reserved, <c>$</c>-prefixed sentinel — chosen deliberately so the automatic exclusion filter can be unconditional (applied whenever
    /// a business model's <c>IdentifierSupport</c> allows it, with no configuration step, and with no risk of ever wrongly excluding real business data).</remarks>
    public const string OutboxKeyPrefix = "$outbox";

    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonPropertyName("partitionKey")]
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the destination (i.e. topic/queue) the <see cref="Event"/> is to ultimately be published to.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized <see cref="CloudNative.CloudEvents.CloudEvent"/> JSON.
    /// </summary>
    public JsonElement Event { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; set; }
}
