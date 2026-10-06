namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Defines a <b>Cosmos DB</b> container to be provisioned by the <see cref="CosmosDbProvisioner"/>.
/// </summary>
/// <param name="id">The <see cref="Container.Id"/>.</param>
/// <param name="partitionKeyPath">The partition key path.</param>
/// <param name="isReferenceData">Indicates whether the container hosts type-discriminated reference data (seed data is imported using <see cref="CosmosDbBatch.ImportDiscriminatedBatchAsync(Container, JsonDataReader, string, bool, CancellationToken)"/>).</param>
/// <param name="configure">An optional action to further configure the <see cref="ContainerProperties"/> (e.g. indexing policy, unique keys, time-to-live).</param>
/// <param name="dataOptions">An optional factory for the <see cref="JsonDataReaderOptions"/> used to import this container's seed data; a new instance must be returned on each invocation (the options are mutable and may be stateful).
/// Returning <see langword="null"/> uses the default (<see cref="JsonDataReaderOptions.CreateForReferenceData"/> for <paramref name="isReferenceData"/>; otherwise, a plain <see cref="JsonDataReaderOptions"/>).</param>
public sealed class CosmosDbContainerDefinition(string id, string partitionKeyPath, bool isReferenceData = false, Action<ContainerProperties>? configure = null, Func<CosmosDbDataContext, JsonDataReaderOptions?>? dataOptions = null)
{
    /// <summary>
    /// Gets the <see cref="Container.Id"/>.
    /// </summary>
    public string Id { get; } = id.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets the partition key path.
    /// </summary>
    public string PartitionKeyPath { get; } = partitionKeyPath.ThrowIfNullOrEmpty();

    /// <summary>
    /// Indicates whether the container hosts type-discriminated reference data.
    /// </summary>
    public bool IsReferenceData { get; } = isReferenceData;

    /// <summary>
    /// Gets the optional <see cref="JsonDataReaderOptions"/> factory.
    /// </summary>
    public Func<CosmosDbDataContext, JsonDataReaderOptions?>? DataOptions { get; } = dataOptions;

    /// <summary>
    /// Creates the <see cref="ContainerProperties"/> for this definition.
    /// </summary>
    public ContainerProperties CreateProperties()
    {
        var cp = new ContainerProperties(Id, PartitionKeyPath);

        // Unique keys are enforced per logical partition, are case-sensitive, and are immutable once the container is created. The outbox events co-located in this container must carry a unique 'code'
        // (see CosmosDbContainerOptions.WithReferenceDataOutboxEvent) or they would collide on the (null, null) tuple.
        if (IsReferenceData)
            cp.UniqueKeyPolicy.UniqueKeys.Add(new UniqueKey { Paths = { "/typeDiscriminator", "/code" } });

        configure?.Invoke(cp);
        return cp;
    }
}
