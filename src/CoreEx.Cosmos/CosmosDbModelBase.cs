namespace CoreEx.Cosmos;

/// <summary>
/// Provides an <b>optional</b> convenience base class for a Cosmos DB model implementing the standard <see cref="CoreEx.Entities.IIdentifier{TId}"/>, <see cref="IETag"/>, <see cref="IChangeLog"/>, <see cref="IPartitionKey"/> and
/// <see cref="ITimeToLive"/> capabilities using the corresponding Cosmos DB reserved system property names (<c>id</c>, <c>_etag</c> and <c>ttl</c>).
/// </summary>
/// <remarks>Nothing within <see cref="CosmosDbContainer{TModel}"/> requires this base class; it only requires <c>TModel : class, <see cref="IEntityKey"/>, new()</c>, with everything else (<see cref="IETag"/>,
/// <see cref="IPartitionKey"/>, <see cref="ITenantId"/>, <see cref="ITypeDiscriminator"/>, <see cref="ILogicallyDeleted"/>, <see cref="ITimeToLive"/>) duck-typed via <c>is</c> checks within
/// <see cref="CosmosDbModelOptions{TModel}"/> (exactly as <c>EfDbModelOptions</c> does today). Existing domain models that already implement these interfaces directly do not need this base class at all.
/// <para>The <see cref="PartitionKey"/> JSON property name (<c>partitionKey</c>) is a sensible default only; a container's actual partition key path is an application/infrastructure choice made at container-creation
/// time, so implement <see cref="IPartitionKey"/> directly (rather than deriving from this base class) where a different property name is required.</para>
/// <para><see cref="ITimeToLive"/> lives in core <c>CoreEx.Data</c> (alongside <see cref="IPartitionKey"/>/<see cref="ITypeDiscriminator"/>), not <c>CoreEx.Cosmos</c>, since a future non-Cosmos NoSQL data-access
/// package (e.g. MongoDB, which has its own distinct TTL-index mechanism) can reuse the same storage-agnostic contract.</para></remarks>
public abstract class CosmosDbModelBase : IIdentifier<string>, IChangeLog, IETag, IPartitionKey, ITimeToLive
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(-999)]
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Serialization omits this property entirely when <see langword="null"/> (<see cref="JsonIgnoreCondition.WhenWritingNull"/>) rather than writing a JSON <c>null</c> - a document with an
    /// <i>absent</i> partition-key field resolves to <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/>, whereas one with an <i>explicit</i> JSON <c>null</c> value resolves to the distinct
    /// <see cref="Microsoft.Azure.Cosmos.PartitionKey.Null"/> - writing the latter for a model with no configured/model-supplied partition key value would mismatch the <see cref="Microsoft.Azure.Cosmos.PartitionKey.None"/>
    /// resolved for the operation itself (see <see cref="CosmosDbModelOptions{TModel}.ToPartitionKey(string?)"/>) and the write would fail with a raw, undiagnosable <c>BadRequest</c>.</remarks>
    [JsonPropertyName("partitionKey")]
    [JsonPropertyOrder(-998)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartitionKey { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("changeLog")]
    [JsonPropertyOrder(100000)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChangeLog? ChangeLog { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("_etag")]
    [JsonPropertyOrder(100001)]
    public string? ETag { get; set; }

    /// <inheritdoc/>
    /// <remarks>Serialization omits this property entirely when <see langword="null"/> (<see cref="JsonIgnoreCondition.WhenWritingNull"/>) rather than writing a JSON <c>null</c> - the Cosmos DB service/emulator
    /// rejects an explicit <c>"ttl": null</c> on create/replace ("The input ttl 'null' is invalid...").</remarks>
    [JsonPropertyName("ttl")]
    [JsonPropertyOrder(100002)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TimeToLive { get; set; }
}
