namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides <b>Cosmos DB</b> batch data-import extension methods over raw JSON, suitable for data seeding, bulk/one-off loads, and migrations alike.
/// </summary>
/// <remarks>Operates on raw JSON (<see cref="JsonArray"/>/<see cref="JsonObject"/>) rather than any <c>CoreEx.Cosmos</c> model type. A caller controls the exact document shape directly -
/// including whatever property the container's partition key path points at, and any type-discriminator value for a container hosting multiple document "types" - the same way they would for
/// any other Cosmos document. <c>Container.CreateItemAsync</c> with no explicit partition key auto-extracts it from the item's own serialized shape (empirically confirmed against the emulator,
/// when the <see cref="CosmosClient"/> is configured with <c>UseSystemTextJsonSerializerWithOptions</c>) - so no partition-key handling is needed here at all, unlike a naive per-batch-partition-key
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
    public static Task ImportBatchAsync(this Container container, JsonArray items, bool sequential = false, CancellationToken cancellationToken = default)
    {
        container.ThrowIfNull();
        items.ThrowIfNull();

        return CreateBatchAsync(container, items, sequential, cancellationToken);
    }

    /// <summary>
    /// Imports (creates) a batch of raw JSON <paramref name="items"/>, returning the counts (all created).
    /// </summary>
    internal static Task<(int Created, int Replaced)> ImportBatchCountAsync(this Container container, JsonArray items, CancellationToken cancellationToken)
        => CreateBatchAsync(container, items, false, cancellationToken);

    /// <summary>
    /// Creates each item, returning the counts (all created).
    /// </summary>
    private static Task<(int Created, int Replaced)> CreateBatchAsync(Container container, JsonArray items, bool sequential, CancellationToken cancellationToken)
        => RunAsync(items, sequential, async n =>
        {
            await container.CreateItemAsync(n, cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        });

    /// <summary>
    /// Runs the <paramref name="operation"/> for each non-null item; items are started lazily so <paramref name="sequential"/> is genuinely one-at-a-time. The <paramref name="operation"/> returns <see langword="true"/> where
    /// the item was created (versus replaced).
    /// </summary>
    private static async Task<(int Created, int Replaced)> RunAsync(JsonArray items, bool sequential, Func<JsonNode, Task<bool>> operation)
    {
        var work = items.Where(n => n is not null).Select(n => operation(n!));
        var results = new List<bool>();

        if (sequential)
        {
            foreach (var task in work)
            {
                results.Add(await task.ConfigureAwait(false));
            }
        }
        else
            results.AddRange(await Task.WhenAll(work).ConfigureAwait(false));

        var created = results.Count(x => x);
        return (created, results.Count - created);
    }

    /// <summary>
    /// Merges (upserts) a batch of raw JSON <paramref name="items"/> into the <paramref name="container"/>; unlike <see cref="ImportBatchAsync(Container, JsonArray, bool, CancellationToken)"/> this is re-runnable
    /// where each item has a stable <c>id</c>.
    /// </summary>
    internal static Task<(int Created, int Replaced)> MergeBatchAsync(this Container container, JsonArray items, bool sequential = false, CancellationToken cancellationToken = default)
        => RunAsync(items, sequential, async n => (await container.UpsertItemAsync(n, cancellationToken: cancellationToken).ConfigureAwait(false)).StatusCode == HttpStatusCode.Created);

    /// <summary>
    /// Merges (upserts) a batch of named items from the <paramref name="jsonDataReader"/> into the <paramref name="container"/>; see <see cref="MergeBatchAsync(Container, JsonArray, bool, CancellationToken)"/>.
    /// </summary>
    internal static async Task<bool> MergeBatchAsync(this Container container, JsonDataReader jsonDataReader, string path, bool sequential = false, CancellationToken cancellationToken = default)
    {
        if (!jsonDataReader.ThrowIfNull().TryCreateData(path.ThrowIfNullOrEmpty(), out var node) || node is not JsonArray array)
            return false;

        await MergeBatchAsync(container, array, sequential, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Merges (upserts) a discriminated group of items. Seed items generate a new <c>id</c> on every parse so, to be re-runnable, an item's <c>id</c> is replaced by that of the existing item having the same
    /// type discriminator and code (where one exists) before being upserted - this honors the unique key convention of a reference data container and keeps identifiers stable across runs.
    /// </summary>
    private static Task<(int Created, int Replaced)> MergeDiscriminatedItemsAsync(Container container, JsonDataReaderOptions options, JsonArray array, bool sequential, CancellationToken cancellationToken)
    {
        var typeProperty = options.ConvertPropertyName(nameof(ITypeDiscriminator.TypeDiscriminator))!;
        var codeProperty = options.ConvertPropertyName(nameof(RefData.Abstractions.IReferenceData.Code))!;
        var idProperty = options.ConvertPropertyName(nameof(IIdentifier.Id))!;

        return RunAsync(array, sequential, async n =>
        {
            if (n is JsonObject jo && jo[typeProperty]?.GetValue<string>() is string type && jo[codeProperty]?.GetValue<string>() is string code
                && await FindIdAsync(container, idProperty, typeProperty, codeProperty, type, code, cancellationToken).ConfigureAwait(false) is string existingId)
                jo[idProperty] = existingId;

            return (await container.UpsertItemAsync(n, cancellationToken: cancellationToken).ConfigureAwait(false)).StatusCode == HttpStatusCode.Created;
        });
    }

    /// <summary>
    /// Finds the identifier of the existing item (across partitions) with the specified type discriminator and code.
    /// </summary>
    private static async Task<string?> FindIdAsync(Container container, string idProperty, string typeProperty, string codeProperty, string type, string code, CancellationToken cancellationToken)
    {
        // The property names originate from the naming convention (never external input) so are safe to embed; the values are parameterized.
        var query = new QueryDefinition($"SELECT VALUE c[\"{idProperty}\"] FROM c WHERE c[\"{typeProperty}\"] = @type AND c[\"{codeProperty}\"] = @code").WithParameter("@type", type).WithParameter("@code", code);
        using var iterator = container.GetItemQueryIterator<string>(query);
        while (iterator.HasMoreResults)
        {
            foreach (var id in await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>
    /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The <see cref="Container"/>.</param>
    /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
    /// <param name="path">The qualified path to the array of items within the <paramref name="jsonDataReader"/> (see <see cref="JsonDataReader.TryCreateData(string, out JsonNode?)"/>) - e.g. a payload
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
    /// <remarks>A one-line whole-file convenience for a payload shaped flatly as <c>ContainerA: [...], ContainerB: [...]</c> - each top-level key names a container, and its array value is the list
    /// of documents to import into it. For a payload with a grouped/nested structure instead, use the explicit <see cref="ImportBatchAsync(Container, JsonDataReader, string, bool, CancellationToken)"/>
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

    /// <summary>
    /// Imports (creates) every top-level container's discriminated data found in the <paramref name="jsonDataReader"/>'s root object, treating each top-level property name as a
    /// <see cref="Container.Id"/> within <paramref name="database"/>, and each of its child property names as an <see cref="ITypeDiscriminator.TypeDiscriminator"/> value.
    /// </summary>
    /// <param name="database">The <see cref="Database"/>.</param>
    /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
    /// <param name="sequential">Indicates whether the items are created sequentially rather than in parallel; defaults to <see langword="false"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>A one-line whole-file convenience for a payload shaped as <c>ContainerA: [{ Person: [...] }, { Organization: [...] }], ContainerB: [...]</c> - each top-level key names a
    /// container, and its array value contains objects whose keys each name an <see cref="ITypeDiscriminator"/> value, with the corresponding array being the list of documents to import
    /// into that container, stamped with the corresponding <see cref="ITypeDiscriminator.TypeDiscriminator"/>.
    /// <para>Each item is created individually and is not transactional - a partial failure part-way through leaves the already-created items in place.</para></remarks>
    public static async Task ImportDiscriminatedBatchAsync(this Microsoft.Azure.Cosmos.Database database, JsonDataReader jsonDataReader, bool sequential = false, CancellationToken cancellationToken = default)
    {
        database.ThrowIfNull();
        jsonDataReader.ThrowIfNull();

        // RootNode is the raw, unsubstituted tree - only used here to discover the top-level container-id keys; each is then re-resolved (with substitution) by the per-container overload.
        if (jsonDataReader.RootNode is not JsonObject root)
            return;

        foreach (var containerId in root.Select(kvp => kvp.Key).ToList())
        {
            await ImportDiscriminatedBatchAsync(database.GetContainer(containerId), jsonDataReader, containerId, sequential, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Imports (creates) the discriminated data found at the top-level <paramref name="path"/> of the <paramref name="jsonDataReader"/> into the <paramref name="container"/>, treating each child
    /// property name as an <see cref="ITypeDiscriminator.TypeDiscriminator"/> value. A child property name prefixed with '<c>$</c>' (the <c>DbEx</c> convention) is instead <i>merged</i> (upserted), making that group re-runnable; see remarks.
    /// </summary>
    /// <param name="container">The <see cref="Container"/>.</param>
    /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
    /// <param name="path">The top-level property name whose array value is shaped as <c>[{ Person: [...] }, { Organization: [...] }]</c>.</param>
    /// <param name="sequential">Indicates whether the items are created sequentially rather than in parallel; defaults to <see langword="false"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that the <paramref name="path"/> was found and processed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Each item is created individually and is not transactional - a partial failure part-way through leaves the already-created items in place.
    /// <para>A group name may be prefixed with '<c>$</c>' (merge) and/or '<c>^</c>' (generate the <c>id</c> where not specified), as per <c>DbEx</c>; a group without '<c>^</c>' does not generate an <c>id</c> so each item must specify its own.
    /// For a '<c>$</c>'-prefixed group an item's <c>id</c> is replaced by that of the existing item with the same <c>typeDiscriminator</c> and <c>code</c> (where one exists) before being upserted, as seed identifiers are generated on each parse.</para></remarks>
    public static Task<bool> ImportDiscriminatedBatchAsync(this Container container, JsonDataReader jsonDataReader, string path, bool sequential = false, CancellationToken cancellationToken = default)
        => ImportDiscriminatedBatchAsync(container, jsonDataReader, path, null, sequential, cancellationToken);

    /// <summary>
    /// As <see cref="ImportDiscriminatedBatchAsync(Container, JsonDataReader, string, bool, CancellationToken)"/> but reports each group (type discriminator, merged, created count, replaced count) to the <paramref name="report"/> action.
    /// </summary>
    internal static Task<bool> ImportDiscriminatedBatchAsync(Container container, JsonDataReader jsonDataReader, string path, Action<string, bool, int, int>? report, bool sequential, CancellationToken cancellationToken)
        => ImportDiscriminatedAsync(container, jsonDataReader, path, async (name, array, merge) =>
        {
            var (created, replaced) = merge ? await MergeDiscriminatedItemsAsync(container, jsonDataReader.Options, array, sequential, cancellationToken).ConfigureAwait(false) : await CreateBatchAsync(container, array, sequential, cancellationToken).ConfigureAwait(false);
            report?.Invoke(name, merge, created, replaced);
        }, cancellationToken);

    /// <summary>
    /// Stamps and resolves each discriminated group at <paramref name="path"/> then passes it (with its type discriminator name and whether to merge) to <paramref name="import"/>.
    /// </summary>
    private static async Task<bool> ImportDiscriminatedAsync(Container container, JsonDataReader jsonDataReader, string path, Func<string, JsonArray, bool, Task> import, CancellationToken cancellationToken)
    {
        container.ThrowIfNull();
        jsonDataReader.ThrowIfNull();
        path.ThrowIfNullOrEmpty();

        // RootNode is the raw, unsubstituted tree - only used here to discover the child type-discriminator keys. Each is then re-resolved via TryCreateData so dynamic parameters
        // (e.g. '^guid', '^1') are substituted the same way the explicit-path overload already does - walking RootNode's children directly would skip substitution entirely.
        if (jsonDataReader.RootNode is not JsonObject root || root[path] is not JsonArray containerArray)
            return false;

        var typeDiscriminatorProperty = jsonDataReader.Options.ConvertPropertyName(nameof(ITypeDiscriminator.TypeDiscriminator))!;
        var hadExistingTypeDiscriminatorProperty = jsonDataReader.Options.Properties.TryGetValue(typeDiscriminatorProperty, out var existingTypeDiscriminatorValue);

        var idProperty = jsonDataReader.Options.ConvertPropertyName(nameof(IIdentifier.Id))!;
        var hadExistingIdProperty = jsonDataReader.Options.Properties.TryGetValue(idProperty, out var existingIdValue);

        try
        {
            // Only single-key objects (see also RootNodePreProcessor's identical convention for the '{ code: text }' shorthand) are treated as '$^TypeName'-style discriminator group markers - any
            // other shape found in the same array (e.g. a flat, already-fully-formed document) is ignored here rather than silently misread as a bogus discriminator.
            var discriminators = containerArray.OfType<JsonObject>().Where(jo => jo.Count == 1).SelectMany(jo => jo.Select(kvp => kvp.Key)).Distinct().ToList();

            foreach (var discriminator in discriminators)
            {
                // The discriminator key may be prefixed with '$' and/or '^' (as per DbEx) to signify additional behaviors; '$' indicates merge rather than insert, and '^' indicates that an identifier is generated where not
                // specified. These are stripped before use as the actual property value in the resulting document.
                var name = discriminator.TrimStart('$', '^');
                var prefix = discriminator[..^name.Length];
                jsonDataReader.Options.Properties[typeDiscriminatorProperty] = name;

                // Without '^' no identifier is generated (even where the options would otherwise default one), so each item must specify its own; with '^' the options' identifier generation is used, else the default.
                if (!prefix.Contains('^'))
                    jsonDataReader.Options.Properties.Remove(idProperty);
                else if (!jsonDataReader.Options.Properties.ContainsKey(idProperty))
                    jsonDataReader.Options.Properties[idProperty] = hadExistingIdProperty ? existingIdValue : "^id";

                if (!jsonDataReader.TryCreateData($"{path}.{discriminator}", out var node) || node is not JsonArray array)
                    continue;

                await import(name, array, prefix.Contains('$')).ConfigureAwait(false);
            }
        }
        finally
        {
            // Options is caller-owned and may outlive this call (e.g. reused for further, unrelated seeding) - restore whatever the caller had before we mutated it (a prior value, or
            // absence), rather than unconditionally removing the property and silently discarding a value the caller had already configured.
            if (hadExistingTypeDiscriminatorProperty)
                jsonDataReader.Options.Properties[typeDiscriminatorProperty] = existingTypeDiscriminatorValue;
            else
                jsonDataReader.Options.Properties.Remove(typeDiscriminatorProperty);

            if (hadExistingIdProperty)
                jsonDataReader.Options.Properties[idProperty] = existingIdValue;
            else
                jsonDataReader.Options.Properties.Remove(idProperty);
        }

        return true;
    }
}
