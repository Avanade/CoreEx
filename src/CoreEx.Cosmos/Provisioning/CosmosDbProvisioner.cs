namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Provisions a <b>Cosmos DB</b> database and its containers, and imports seed data; the <b>Cosmos DB</b> equivalent of the <c>DbEx</c> migration engine.
/// </summary>
/// <param name="client">The <see cref="CosmosClient"/>.</param>
/// <param name="args">The <see cref="CosmosDbProvisionArgs"/>.</param>
/// <remarks>Intended for development, test and CI (e.g. the emulator); production provisioning remains the responsibility of infrastructure-as-code. A container's unique key policy and partition key path are
/// immutable once created, so <see cref="CosmosDbProvisionCommand.Create"/> will <i>not</i> alter an existing container; use <see cref="CosmosDbProvisionCommand.Reset"/> (or <see cref="CosmosDbProvisionCommand.Drop"/>) instead.</remarks>
public class CosmosDbProvisioner(CosmosClient client, CosmosDbProvisionArgs args)
{
    private readonly CosmosClient _client = client.ThrowIfNull();
    private readonly CosmosDbProvisionArgs _args = args.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="CosmosDbProvisionArgs"/>.
    /// </summary>
    public CosmosDbProvisionArgs Args => _args;

    private string DatabaseId => _args.DatabaseId.ThrowIfNullOrEmpty(nameof(CosmosDbProvisionArgs.DatabaseId));

    /// <summary>
    /// Runs the <paramref name="command"/> in the order <see cref="CosmosDbProvisionCommand.Drop"/>, <see cref="CosmosDbProvisionCommand.Reset"/> (or <see cref="CosmosDbProvisionCommand.Create"/>), then <see cref="CosmosDbProvisionCommand.Data"/>.
    /// </summary>
    /// <param name="command">The <see cref="CosmosDbProvisionCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task RunAsync(CosmosDbProvisionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.HasFlag(CosmosDbProvisionCommand.Drop))
            await DropAsync(cancellationToken).ConfigureAwait(false);

        if (command.HasFlag(CosmosDbProvisionCommand.Reset))
            await ResetAsync(cancellationToken).ConfigureAwait(false);
        else if (command.HasFlag(CosmosDbProvisionCommand.Create))
            await CreateAsync(cancellationToken).ConfigureAwait(false);

        if (command.HasFlag(CosmosDbProvisionCommand.Data))
            await DataAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the database; a non-existent database is not an error.
    /// </summary>
    public async Task DropAsync(CancellationToken cancellationToken = default)
    {
        _args.Output.WriteLine($"Dropping database '{DatabaseId}'...");

        try
        {
            await _client.GetDatabase(DatabaseId).DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
        {
            _args.Output.WriteLine("  Database does not exist.");
        }
    }

    /// <summary>
    /// Creates the database and any declared containers that do not already exist.
    /// </summary>
    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        var database = await CreateDatabaseAsync(cancellationToken).ConfigureAwait(false);

        foreach (var c in _args.Containers)
        {
            _args.Output.WriteLine($"Creating container '{c.Id}' (where not existing)...");
            await RetryAsync(() => database.CreateContainerIfNotExistsAsync(c.CreateProperties(), _args.Throughput, cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Replaces (deletes and recreates; therefore empty) every declared container, creating the database where required.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var database = await CreateDatabaseAsync(cancellationToken).ConfigureAwait(false);

        foreach (var c in _args.Containers)
        {
            _args.Output.WriteLine($"Resetting container '{c.Id}'...");
            await RetryAsync(() => database.ReplaceOrCreateContainerAsync(c.CreateProperties(), _args.Throughput, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Imports the seed data; the <see cref="CosmosDbProvisionArgs.Assemblies"/> <c>Data</c> resources first, followed by any remaining <see cref="CosmosDbProvisionArgs.DataResources"/>.
    /// </summary>
    public async Task DataAsync(CancellationToken cancellationToken = default)
    {
        var database = _client.GetDatabase(DatabaseId);

        foreach (var (name, content, resourceOptions) in GetDataResources())
        {
            _args.Output.WriteLine($"Importing data '{name}'...");
            var isJson = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

            JsonDataReader Parse(JsonDataReaderOptions options) => isJson ? JsonDataReader.ParseJson(content, options) : JsonDataReader.ParseYaml(content, options);

            // A plain reader is used only to discover the top-level container keys; each container is then imported using its own resolved options.
            if (Parse(new JsonDataReaderOptions(_args.NamingConvention)).RootNode is not JsonObject root)
                continue;

            foreach (var key in root.Select(kvp => kvp.Key).ToList())
            {
                var definition = _args.Containers.FirstOrDefault(c => c.Id == key)
                    ?? throw new InvalidOperationException($"Data '{name}' top-level key '{key}' does not match a declared container ({string.Join(", ", _args.Containers.Select(c => $"'{c.Id}'"))}).");

                var container = database.GetContainer(definition.Id);
                var jdr = Parse(ResolveDataOptions(name, resourceOptions, definition));
                if (definition.IsReferenceData)
                    await container.ImportDiscriminatedBatchAsync(jdr, key, cancellationToken: cancellationToken).ConfigureAwait(false);
                else
                    await container.ImportBatchAsync(jdr, key, cancellationToken: cancellationToken).ConfigureAwait(false);

                _args.Output.WriteLine($"  Container '{key}' imported.");
            }
        }
    }

    /// <summary>
    /// Resolves the <see cref="JsonDataReaderOptions"/>; the resource <see cref="CosmosDbDataResource.DataOptions"/> first, then the <see cref="CosmosDbContainerDefinition.DataOptions"/>, then the default.
    /// </summary>
    private JsonDataReaderOptions ResolveDataOptions(string resourceName, Func<CosmosDbDataContext, JsonDataReaderOptions?>? resourceOptions, CosmosDbContainerDefinition definition)
    {
        var context = new CosmosDbDataContext(resourceName, definition, _args.NamingConvention);
        return resourceOptions?.Invoke(context)
            ?? definition.DataOptions?.Invoke(context)
            ?? (definition.IsReferenceData ? JsonDataReaderOptions.CreateForReferenceData(_args.NamingConvention) : new JsonDataReaderOptions(_args.NamingConvention));
    }

    /// <summary>
    /// Gets the data resources (name, content and optional resource-specific options) in import order.
    /// </summary>
    private IEnumerable<(string Name, string Content, Func<CosmosDbDataContext, JsonDataReaderOptions?>? DataOptions)> GetDataResources()
    {
        var explicitly = _args.DataResources.ToList();
        var matched = new HashSet<CosmosDbDataResource>();

        foreach (var assembly in _args.Assemblies)
        {
            foreach (var name in assembly.GetManifestResourceNames().Where(IsDataResource).OrderBy(x => x, StringComparer.Ordinal))
            {
                // An explicitly added resource that is also a scanned one is imported once, here, using its options.
                var resource = explicitly.FirstOrDefault(x => x.Assembly == assembly && name.EndsWith(x.ResourceName, StringComparison.OrdinalIgnoreCase));
                if (resource is not null)
                    matched.Add(resource);

                using var sr = new StreamReader(assembly.GetManifestResourceStream(name)!);
                yield return (name, sr.ReadToEnd(), resource?.DataOptions);
            }
        }

        foreach (var resource in explicitly.Where(x => !matched.Contains(x)))
        {
            using var sr = new StreamReader(Resource.GetStream(resource.ResourceName, resource.Assembly));
            yield return (resource.ResourceName, sr.ReadToEnd(), resource.DataOptions);
        }
    }
    /// <summary>
    /// Determines whether the manifest resource name is a <c>Data</c> folder YAML or JSON file.
    /// </summary>
    private static bool IsDataResource(string name)
        => name.Contains(".Data.", StringComparison.OrdinalIgnoreCase) && (name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Creates the database where not existing.
    /// </summary>
    private async Task<Microsoft.Azure.Cosmos.Database> CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        _args.Output.WriteLine($"Creating database '{DatabaseId}' (where not existing)...");
        var response = await RetryAsync(() => _client.CreateDatabaseIfNotExistsAsync(DatabaseId, cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);
        return response.Database;
    }

    /// <summary>
    /// The local emulator occasionally responds with a transient <c>503 ServiceUnavailable</c> ("high demand") when several containers are created in quick succession, and intermittently drops the TLS handshake.
    /// </summary>
    /// <remarks>Note: a <c>503</c> is also what the emulator returns when its container-count cap (<c>AZURE_COSMOS_EMULATOR_PARTITION_COUNT</c>) is exhausted; that is deterministic and retrying will not resolve it.</remarks>
    private static async Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < maxAttempts && (ex is CosmosException { StatusCode: HttpStatusCode.ServiceUnavailable } || ex is HttpRequestException || ex.InnerException is HttpRequestException))
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
