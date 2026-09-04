namespace CoreEx.UnitTesting.Cosmos;

/// <summary>
/// Provides <b>Cosmos DB</b> batch data-import extension methods, intended for test data seeding only.
/// </summary>
/// <remarks>Operates on raw JSON (<see cref="JsonArray"/>/<see cref="JsonObject"/>) rather than any <c>CoreEx.Cosmos</c> model type. A fixture author controls the exact document shape directly -
/// including whatever property the container's partition key path points at, and any type-discriminator value for a container hosting multiple document "types" - the same way they would for
/// any other Cosmos document. <c>Container.CreateItemAsync</c> with no explicit partition key auto-extracts it from the item's own serialized shape (empirically confirmed against the emulator,
/// using the same <c>UseSystemTextJsonSerializerWithOptions</c> configuration test setup already uses) - so no partition-key handling is needed here at all, unlike a naive per-batch-partition-key
/// approach.</remarks>
public static class CosmosDbBatch
{
    /// <summary>
    /// Imports (creates) a batch of raw JSON <paramref name="items"/> into the <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The <see cref="Container"/>.</param>
    /// <param name="items">The batch of items to create.</param>
    /// <param name="sequential">Indicates whether the items are created sequentially (order-based and slower) rather than in parallel (no order guarantees, faster); defaults to <see langword="false"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Each item is created individually and is not transactional - a partial failure part-way through leaves the already-created items in place.</remarks>
    public static async Task ImportBatchAsync(this Container container, JsonArray items, bool sequential = false, CancellationToken cancellationToken = default)
    {
        container.ThrowIfNull();
        items.ThrowIfNull();

        var work = items.Where(n => n is not null).Select(n => container.CreateItemAsync(n, cancellationToken: cancellationToken));

        if (sequential)
        {
            foreach (var task in work)
            {
                await task.ConfigureAwait(false);
            }
        }
        else
            await Task.WhenAll(work).ConfigureAwait(false);
    }

    /// <summary>
    /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The <see cref="Container"/>.</param>
    /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
    /// <param name="path">The qualified path to the array of items within the <paramref name="jsonDataReader"/> (see <see cref="JsonDataReader.TryCreateData(string, out JsonNode?)"/>) - e.g. a fixture
    /// with a grouped/nested structure such as <c>Orders: [{ Order: [...] }]</c> would use the path <c>"Orders.Order"</c>.</param>
    /// <param name="sequential">Indicates whether the items are created sequentially rather than in parallel; defaults to <see langword="false"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that one or more items were found at <paramref name="path"/> and imported; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Each item is created individually and is not transactional - a partial failure part-way through leaves the already-created items in place.</remarks>
    public static async Task<bool> ImportBatchAsync(this Container container, JsonDataReader jsonDataReader, string path, bool sequential = false, CancellationToken cancellationToken = default)
    {
        if (!jsonDataReader.ThrowIfNull().TryCreateData(path.ThrowIfNullOrEmpty(), out var node) || node is not JsonArray array)
            return false;

        await ImportBatchAsync(container, array, sequential, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Imports (creates) every top-level array found in the <paramref name="jsonDataReader"/>'s root object, treating each top-level property name as a <see cref="Container.Id"/> within
    /// <paramref name="database"/>.
    /// </summary>
    /// <param name="database">The <see cref="Database"/>.</param>
    /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
    /// <param name="sequential">Indicates whether the items are created sequentially rather than in parallel; defaults to <see langword="false"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>A one-line whole-file convenience for a fixture shaped flatly as <c>ContainerA: [...], ContainerB: [...]</c> - each top-level key names a container, and its array value is the list
    /// of documents to import into it. For a fixture with a grouped/nested structure instead, use the explicit <see cref="ImportBatchAsync(Container, JsonDataReader, string, bool, CancellationToken)"/>
    /// overload naming the exact path.
    /// <para>Each item is created individually and is not transactional - a partial failure part-way through leaves the already-created items in place.</para></remarks>
    public static async Task ImportBatchAsync(this Microsoft.Azure.Cosmos.Database database, JsonDataReader jsonDataReader, bool sequential = false, CancellationToken cancellationToken = default)
    {
        database.ThrowIfNull();
        jsonDataReader.ThrowIfNull();

        // RootNode is the raw, unsubstituted tree - only used here to discover the top-level container-id keys. Each one is then re-resolved via TryCreateData so dynamic parameters
        // (e.g. '^guid', '^1') are substituted the same way the explicit-path overload already does - walking RootNode's children directly would skip substitution entirely.
        if (jsonDataReader.RootNode is not JsonObject root)
            return;

        foreach (var containerId in root.Select(kvp => kvp.Key).ToList())
        {
            await ImportBatchAsync(database.GetContainer(containerId), jsonDataReader, containerId, sequential, cancellationToken).ConfigureAwait(false);
        }
    }
}
