namespace CoreEx.Cosmos;

/// <summary>
/// Provides the scoped, per-<see cref="ICosmosDb"/> change tracker; a snapshot cache of the persisted documents read by <see cref="CosmosDbContainer{TModel}"/> point <b>Get</b> operations.
/// </summary>
/// <remarks>The snapshot is the <i>as-read</i> serialized document; every cache hit deserializes a fresh <c>TModel</c> instance, so a caller mutating a returned instance can never corrupt the snapshot (nor
/// another caller's instance) - this is deliberately a snapshot, not an identity map. The lifetime is that of the owning (typically DI-scoped, i.e. per-request) <see cref="ICosmosDb"/>, mirroring the Entity Framework
/// change tracker.
/// <para>The snapshot is used to: avoid repeat reads (RU cost) of the same document within a scope; let <b>Update</b> perform its own pre-read in order to copy back server-managed values (change-log creation, unmapped
/// additional properties) and to detect a no-op. Every write (Create/Update/Upsert/Delete) evicts the affected entry, and the whole tracker is cleared when the root <see cref="CosmosDbUnitOfWork"/> transaction ends (as there
/// may be any number of server-side effects). A single <see cref="CosmosDbArgs.ClearChangeTrackerAfterGet"/> opts a call out.</para>
/// <para>Tracking is only enabled where the underlying <see cref="CosmosClient"/> is configured with <see cref="CosmosClientOptions.UseSystemTextJsonSerializerWithOptions"/>; otherwise this is silently inert.</para></remarks>
public sealed class CosmosDbChangeTracker
{
    private readonly ConcurrentDictionary<(string ContainerId, Type ModelType, string PartitionKey, string Id), byte[]> _snapshots = new();

    /// <summary>
    /// Gets the number of tracked (snapshotted) documents.
    /// </summary>
    public int Count => _snapshots.Count;

    /// <summary>
    /// Clears (evicts) all tracked documents.
    /// </summary>
    public void Clear() => _snapshots.Clear();

    /// <summary>
    /// Attempts to get a fresh copy of the snapshotted model.
    /// </summary>
    internal bool TryGet<TModel>(JsonSerializerOptions options, string containerId, PartitionKey partitionKey, string id, [NotNullWhen(true)] out TModel? model) where TModel : class
    {
        if (_snapshots.TryGetValue((containerId, typeof(TModel), partitionKey.ToString(), id), out var bytes))
        {
            model = JsonSerializer.Deserialize<TModel>(bytes, options);
            return model is not null;
        }

        model = null;
        return false;
    }

    /// <summary>
    /// Snapshots the <paramref name="model"/>.
    /// </summary>
    internal void Set<TModel>(JsonSerializerOptions options, string containerId, PartitionKey partitionKey, string id, TModel model) where TModel : class
        => _snapshots[(containerId, typeof(TModel), partitionKey.ToString(), id)] = JsonSerializer.SerializeToUtf8Bytes(model, options);

    /// <summary>
    /// Evicts the document (for all model types) from the tracker.
    /// </summary>
    internal void Remove(string containerId, PartitionKey partitionKey, string id)
    {
        var pk = partitionKey.ToString();
        foreach (var key in _snapshots.Keys)
        {
            if (key.ContainerId == containerId && key.PartitionKey == pk && key.Id == id)
                _snapshots.TryRemove(key, out _);
        }
    }
}