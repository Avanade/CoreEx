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

    /// <summary>
    /// The horizontal rule written between sections.
    /// </summary>
    internal static readonly string Rule = new('-', 80);

    private string DatabaseId => _args.DatabaseId.ThrowIfNullOrEmpty(nameof(CosmosDbProvisionArgs.DatabaseId));

    /// <summary>
    /// Runs the <paramref name="command"/> in the order <see cref="CosmosDbProvisionCommand.Drop"/>, <see cref="CosmosDbProvisionCommand.Create"/>, <see cref="CosmosDbProvisionCommand.Reset"/>, then <see cref="CosmosDbProvisionCommand.Data"/>.
    /// </summary>
    /// <param name="command">The <see cref="CosmosDbProvisionCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task RunAsync(CosmosDbProvisionCommand command, CancellationToken cancellationToken = default)
    {
        _args.Output.WriteLine(Rule);
        _args.Output.WriteLine();

        if (command.HasFlag(CosmosDbProvisionCommand.Drop))
            await SectionAsync("DATABASE DROP: Dropping the database where found...", () => DropAsync(cancellationToken)).ConfigureAwait(false);

        if (command.HasFlag(CosmosDbProvisionCommand.Create))
            await SectionAsync("DATABASE CREATE: Checking database and container existence and creating where not found...", () => CreateAsync(cancellationToken)).ConfigureAwait(false);

        if (command.HasFlag(CosmosDbProvisionCommand.Reset))
            await SectionAsync("CONTAINER RESET: Replacing (emptying) the declared containers...", () => ResetAsync(cancellationToken)).ConfigureAwait(false);

        if (command.HasFlag(CosmosDbProvisionCommand.Data))
            await SectionAsync("DATABASE DATA: Insert or merge the embedded data [yaml|json]..", () => DataAsync(cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the <paramref name="command"/> (see <see cref="RunAsync(CosmosDbProvisionCommand, CancellationToken)"/>) capturing the output, and reports the outcome rather than throwing; the <b>Cosmos DB</b> equivalent of the <c>DbEx</c> <c>MigrateAndLogAsync</c>.
    /// </summary>
    /// <param name="command">The <see cref="CosmosDbProvisionCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <c>Success</c> indicator and the captured <c>Output</c>; where unsuccessful the <c>Output</c> is appended with the failure message.</returns>
    /// <remarks>The <see cref="CosmosDbProvisionArgs.Output"/> is replaced for the duration of the run (so the instance must not be run concurrently) and then restored, with the captured output also written to it. A <see cref="OperationCanceledException"/> is not
    /// treated as a failure and is therefore thrown.</remarks>
    public async Task<(bool Success, string Output)> RunAndLogAsync(CosmosDbProvisionCommand command, CancellationToken cancellationToken = default)
    {
        var original = _args.Output;
        var captured = new StringWriter();
        _args.Output = captured;

        try
        {
            await RunAsync(command, cancellationToken).ConfigureAwait(false);
            return (true, captured.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            captured.WriteLine($"Failed: {ex.Message}");
            return (false, captured.ToString());
        }
        finally
        {
            _args.Output = original;
            original.Write(captured.ToString());
            original.Flush();
        }
    }

    /// <summary>
    /// Runs the <paramref name="action"/> as a titled, timed section followed by a rule.
    /// </summary>
    private async Task SectionAsync(string title, Func<Task> action)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _args.Output.WriteLine(title);
        await action().ConfigureAwait(false);
        _args.Output.WriteLine();
        _args.Output.WriteLine($"Complete. [{sw.Elapsed.TotalMilliseconds:0.0}ms]");
        _args.Output.WriteLine();
        _args.Output.WriteLine(Rule);
        _args.Output.WriteLine();
    }

    /// <summary>
    /// Deletes the database; a non-existent database is not an error.
    /// </summary>
    public async Task DropAsync(CancellationToken cancellationToken = default)
    {
        _args.Output.WriteLine("  Drop database...");

        try
        {
            await _client.GetDatabase(DatabaseId).DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _args.Output.WriteLine($"    Database '{DatabaseId}' dropped.");
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
        {
            _args.Output.WriteLine($"    Database '{DatabaseId}' does not exist and therefore not dropped.");
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
            var label = Label(c);
            _args.Output.WriteLine($"  Create {label}...");
            var response = await RetryAsync(() => database.CreateContainerIfNotExistsAsync(c.CreateProperties(), _args.Throughput, cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);
            _args.Output.WriteLine(response.StatusCode == HttpStatusCode.Created ? $"    {Capitalize(label)} created." : $"    {Capitalize(label)} already exists and therefore not created.");
        }
    }

    /// <summary>
    /// Replaces (deletes and recreates; therefore empty) every declared container; the database must already exist (as per <c>DbEx</c>, a reset does not implicitly create it - include <see cref="CosmosDbProvisionCommand.Create"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The database does not exist.</exception>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var database = _client.GetDatabase(DatabaseId);

        try
        {
            await RetryAsync(() => database.ReadAsync(cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Database '{DatabaseId}' does not exist; a reset does not create it (use the Create command, e.g. 'All' or 'Create,ResetAndData').", cex);
        }

        foreach (var c in _args.Containers)
        {
            var label = Label(c);
            _args.Output.WriteLine($"  Reset {label}...");
            await RetryAsync(() => database.ReplaceOrCreateContainerAsync(c.CreateProperties(), _args.Throughput, cancellationToken), cancellationToken).ConfigureAwait(false);
            _args.Output.WriteLine($"    {Capitalize(label)} replaced (empty).");
        }
    }

    /// <summary>
    /// Gets the output label for the container (identifying the outbox relay lease container).
    /// </summary>
    private static string Label(CosmosDbContainerDefinition c) => c.IsOutboxLease ? $"outbox lease container '{c.Id}'" : $"container '{c.Id}'";

    /// <summary>
    /// Capitalizes the first character.
    /// </summary>
    private static string Capitalize(string text) => char.ToUpperInvariant(text[0]) + text[1..];

    /// <summary>
    /// Imports the seed data; the <see cref="CosmosDbProvisionArgs.Assemblies"/> <c>Data</c> resources first, followed by any remaining <see cref="CosmosDbProvisionArgs.DataResources"/>.
    /// </summary>
    public async Task DataAsync(CancellationToken cancellationToken = default)
    {
        var database = _client.GetDatabase(DatabaseId);
        _args.Output.WriteLine($"  Probing for embedded resources: {string.Join(", ", _args.Assemblies.Select(a => $"{a.GetName().Name}.Data.*"))}");

        foreach (var (name, content, resourceOptions) in GetDataResources())
        {
            _args.Output.WriteLine();
            _args.Output.WriteLine($"** Parsing and executing: {name}");
            var isJson = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

            // A plain reader is used only to discover the top-level container keys; each container is then imported using its own resolved options.
            if ((isJson ? JsonDataReader.ParseJson(content, new JsonDataReaderOptions(_args.NamingConvention)) : JsonDataReader.ParseYaml(content, new JsonDataReaderOptions(_args.NamingConvention))).RootNode is not JsonObject root)
                continue;

            // As per DbEx, a '$' prefix indicates merge (upsert) rather than insert; for a reference data container it is the type discriminator groups that carry the prefix instead. A leading '$' is not
            // addressable in a JSON path (it denotes the root) so the prefix is removed from the top-level keys, noting which were merges, before the data is re-parsed.
            var merges = root.Where(kvp => kvp.Key.StartsWith('$')).Select(kvp => kvp.Key.TrimStart('$')).ToHashSet();
            var normalized = new JsonObject();
            foreach (var kvp in root)
            {
                normalized[kvp.Key.TrimStart('$')] = kvp.Value?.DeepClone();
            }

            var json = normalized.ToJsonString();
            JsonDataReader Parse(JsonDataReaderOptions options) => JsonDataReader.ParseJson(json, options);

            foreach (var containerId in normalized.Select(kvp => kvp.Key).ToList())
            {
                var merge = merges.Contains(containerId);
                var definition = _args.Containers.FirstOrDefault(c => c.Id == containerId)
                    ?? throw new InvalidOperationException($"Data '{name}' top-level key '{containerId}' does not match a declared container ({string.Join(", ", _args.Containers.Select(c => $"'{c.Id}'"))}).");

                var container = database.GetContainer(definition.Id);
                var jdr = Parse(ResolveDataOptions(name, resourceOptions, definition));
                var verb = merge ? "Merging" : "Inserting";

                if (definition.IsReferenceData)
                {
                    await CosmosDbBatch.ImportDiscriminatedBatchAsync(container, jdr, containerId, (group, groupMerge, created, replaced) => WriteResult($"{(groupMerge ? "Merging" : "Inserting")} '{containerId}' / {group}", groupMerge, created, replaced), false, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (!jdr.TryCreateData(containerId, out var node) || node is not JsonArray array)
                    continue;

                if (merge)
                {
                    var (created, replaced) = await container.MergeBatchAsync(array, cancellationToken: cancellationToken).ConfigureAwait(false);
                    WriteResult($"{verb} '{containerId}'", true, created, replaced);
                }
                else
                {
                    var (created, replaced) = await container.ImportBatchCountAsync(array, cancellationToken).ConfigureAwait(false);
                    WriteResult($"{verb} '{containerId}'", false, created, replaced);
                }
            }
        }
    }

    /// <summary>
    /// Writes the result of an import or merge.
    /// </summary>
    private void WriteResult(string title, bool merge, int created, int replaced)
    {
        _args.Output.WriteLine();
        _args.Output.WriteLine($"---- {title}:");
        _args.Output.WriteLine(merge ? $"Result: {created + replaced} item(s) ({created} created, {replaced} replaced)." : $"Result: {created} item(s) created.");
    }

    /// <summary>
    /// Resolves the <see cref="JsonDataReaderOptions"/>; the resource <see cref="CosmosDbDataResource.DataOptions"/> first, then the <see cref="CosmosDbContainerDefinition.DataOptions"/>, then the default.
    /// </summary>
    private JsonDataReaderOptions ResolveDataOptions(string resourceName, Func<CosmosDbDataContext, JsonDataReaderOptions?>? resourceOptions, CosmosDbContainerDefinition definition)
    {
        var context = new CosmosDbDataContext(resourceName, definition, _args.NamingConvention);
        var options = resourceOptions?.Invoke(context)
            ?? definition.DataOptions?.Invoke(context)
            ?? (definition.IsReferenceData ? JsonDataReaderOptions.CreateForReferenceData(_args.NamingConvention) : new JsonDataReaderOptions(_args.NamingConvention));

        foreach (var (name, value) in _args.Parameters)
        {
            options.Parameters[name] = _ => value;
        }

        return options;
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
        _args.Output.WriteLine("  Create database...");
        var response = await RetryAsync(() => _client.CreateDatabaseIfNotExistsAsync(DatabaseId, cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);
        _args.Output.WriteLine(response.StatusCode == HttpStatusCode.Created ? $"    Database '{DatabaseId}' created." : $"    Database '{DatabaseId}' already exists and therefore not created.");
        return response.Database;
    }

    /// <summary>
    /// The local emulator occasionally responds with a transient <c>503 ServiceUnavailable</c> ("high demand") when several containers are created in quick succession, and intermittently drops the TLS handshake.
    /// </summary>
    /// <remarks>Note: the legacy emulator image also returned a <c>503</c> once its container-count cap was exhausted; that was deterministic and retrying would not resolve it (the vNext emulator has no such cap).</remarks>
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
