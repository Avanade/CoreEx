namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Provides the arguments for the <see cref="CosmosDbProvisioner"/>; the <b>Cosmos DB</b> equivalent of the <c>DbEx</c> <c>MigrationArgs</c>.
/// </summary>
/// <remarks>Seed data is YAML (<c>.yaml</c>/<c>.yml</c>) or JSON embedded resources where each top-level key is a <see cref="Container.Id"/> and its array value the documents to import. Resources located
/// in a <c>Data</c> folder of any <see cref="Assemblies"/> are always imported (in resource-name order); additional named resources (optionally with their own <see cref="JsonDataReaderOptions"/>) are added via <see cref="AddDataResource(Assembly, string, Func{CosmosDbDataContext, JsonDataReaderOptions?}?)"/> and are imported afterwards.
/// <para>Seeding creates documents directly (see <see cref="CosmosDbBatch"/>), so the JSON must already carry the partition key and, for a <see cref="CosmosDbContainerDefinition.IsReferenceData"/> container, the
/// <c>$^TypeName</c> grouped shorthand is used (<c>$</c> merges (upserts) rather than inserts, making the import re-runnable; <c>^</c> generates the identifier where not specified) (see <see cref="CosmosDbBatch.ImportDiscriminatedBatchAsync(Container, JsonDataReader, string, bool, CancellationToken)"/>).</para></remarks>
public class CosmosDbProvisionArgs
{
    private readonly List<CosmosDbContainerDefinition> _containers = [];
    private readonly List<CosmosDbDataResource> _dataResources = [];

    /// <summary>
    /// Gets or sets the <see cref="Database.Id"/>.
    /// </summary>
    public string? DatabaseId { get; set; }

    /// <summary>
    /// Gets or sets the manually provisioned throughput (RU/s) used when creating each container; defaults to <see langword="null"/> (the account default).
    /// </summary>
    public int? Throughput { get; set; }

    /// <summary>
    /// Gets or sets the default property naming convention of the seed data; defaults to <see cref="JsonPropertyNamingConvention.CamelCase"/>.
    /// </summary>
    public JsonPropertyNamingConvention NamingConvention { get; set; } = JsonPropertyNamingConvention.CamelCase;

    /// <summary>
    /// Gets or sets the <see cref="TextWriter"/> that progress is written to; defaults to <see cref="TextWriter.Null"/>.
    /// </summary>
    public TextWriter Output { get; set; } = TextWriter.Null;

    /// <summary>
    /// Indicates whether confirmation prompts (for the destructive <see cref="CosmosDbProvisionCommand.Drop"/> and <see cref="CosmosDbProvisionCommand.Reset"/> commands) are accepted automatically; used by the <see cref="CosmosDbConsole"/>.
    /// </summary>
    public bool AcceptPrompts { get; set; }

    /// <summary>
    /// Gets the name/value parameters made available to the seed data parser (referenced as <c>^Name</c>, or <c>(^Name)</c> where embedded within a value); these override any same-named parameter configured by the <see cref="JsonDataReaderOptions"/>.
    /// </summary>
    public Dictionary<string, string?> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the <see cref="Assembly"/> list that is scanned for embedded <c>Data</c> resources.
    /// </summary>
    public List<Assembly> Assemblies { get; } = [];

    /// <summary>
    /// Gets the declared <see cref="CosmosDbContainerDefinition"/> list.
    /// </summary>
    public IReadOnlyList<CosmosDbContainerDefinition> Containers => _containers;

    /// <summary>
    /// Gets the additional named data resources.
    /// </summary>
    public IReadOnlyList<CosmosDbDataResource> DataResources => _dataResources;

    /// <summary>
    /// Adds the <see cref="Assembly"/> (inferred from <typeparamref name="TAssembly"/>) to <see cref="Assemblies"/>.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/>.</typeparam>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    public CosmosDbProvisionArgs AddAssembly<TAssembly>() => AddAssembly(typeof(TAssembly).Assembly);

    /// <summary>
    /// Adds the <paramref name="assemblies"/> to <see cref="Assemblies"/> (where not already present).
    /// </summary>
    /// <param name="assemblies">The <see cref="Assembly"/> array.</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    public CosmosDbProvisionArgs AddAssembly(params Assembly[] assemblies)
    {
        foreach (var a in assemblies.ThrowIfNull())
        {
            if (!Assemblies.Contains(a))
                Assemblies.Add(a);
        }

        return this;
    }

    /// <summary>
    /// Adds named embedded data resources from the <see cref="Assembly"/> inferred from <typeparamref name="TAssembly"/>.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/>.</typeparam>
    /// <param name="resourceNames">The resource names (matched to the end of the fully qualified resource name; see <see cref="Resource.GetStream(string, Assembly?)"/>).</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    public CosmosDbProvisionArgs AddDataResource<TAssembly>(params string[] resourceNames) => AddDataResource(typeof(TAssembly).Assembly, resourceNames);

    /// <summary>
    /// Adds a named embedded data resource from the <see cref="Assembly"/> inferred from <typeparamref name="TAssembly"/> with resource-specific <see cref="JsonDataReaderOptions"/>.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/>.</typeparam>
    /// <param name="resourceName">The resource name (matched to the end of the fully qualified resource name; see <see cref="Resource.GetStream(string, Assembly?)"/>).</param>
    /// <param name="dataOptions">The factory for the <see cref="JsonDataReaderOptions"/> (see <see cref="CosmosDbDataResource.DataOptions"/>).</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    public CosmosDbProvisionArgs AddDataResource<TAssembly>(string resourceName, Func<CosmosDbDataContext, JsonDataReaderOptions?> dataOptions) => AddDataResource(typeof(TAssembly).Assembly, resourceName, dataOptions);

    /// <summary>
    /// Adds named embedded data resources from the <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/>.</param>
    /// <param name="resourceNames">The resource names (matched to the end of the fully qualified resource name; see <see cref="Resource.GetStream(string, Assembly?)"/>).</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    public CosmosDbProvisionArgs AddDataResource(Assembly assembly, params string[] resourceNames)
    {
        foreach (var rn in resourceNames.ThrowIfNull())
        {
            AddDataResource(assembly, rn, (Func<CosmosDbDataContext, JsonDataReaderOptions?>?)null);
        }

        return this;
    }

    /// <summary>
    /// Adds a named embedded data resource from the <paramref name="assembly"/> with resource-specific <see cref="JsonDataReaderOptions"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/>.</param>
    /// <param name="resourceName">The resource name (matched to the end of the fully qualified resource name; see <see cref="Resource.GetStream(string, Assembly?)"/>).</param>
    /// <param name="dataOptions">The optional factory for the <see cref="JsonDataReaderOptions"/> (see <see cref="CosmosDbDataResource.DataOptions"/>).</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Where the resource is also located in a <c>Data</c> folder of any <see cref="Assemblies"/> then it is imported once, in its default position, using the <paramref name="dataOptions"/>; otherwise, it is imported afterwards.
    /// Adding the same resource more than once is not permitted.</remarks>
    public CosmosDbProvisionArgs AddDataResource(Assembly assembly, string resourceName, Func<CosmosDbDataContext, JsonDataReaderOptions?>? dataOptions)
    {
        assembly.ThrowIfNull();
        resourceName.ThrowIfNullOrEmpty();

        if (_dataResources.Any(x => x.Assembly == assembly && string.Equals(x.ResourceName, resourceName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Data resource '{resourceName}' has already been added.");

        _dataResources.Add(new CosmosDbDataResource(assembly, resourceName, dataOptions));
        return this;
    }

    /// <summary>
    /// Declares a container.
    /// </summary>
    /// <param name="id">The <see cref="Container.Id"/>.</param>
    /// <param name="partitionKeyPath">The partition key path; defaults to <c>/partitionKey</c>.</param>
    /// <param name="configure">An optional action to further configure the <see cref="ContainerProperties"/> (e.g. indexing policy, unique keys, time-to-live).</param>
    /// <param name="dataOptions">An optional factory for the seed data <see cref="JsonDataReaderOptions"/> (see <see cref="CosmosDbContainerDefinition.DataOptions"/>).</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    public CosmosDbProvisionArgs Container(string id, string partitionKeyPath = "/partitionKey", Action<ContainerProperties>? configure = null, Func<CosmosDbDataContext, JsonDataReaderOptions?>? dataOptions = null)
        => Add(new CosmosDbContainerDefinition(id, partitionKeyPath, false, configure, dataOptions));

    /// <summary>
    /// Declares a type-discriminated reference data container, including the <c>/typeDiscriminator</c> and <c>/code</c> unique key that rejects duplicate codes (see <see cref="CosmosDbReferenceData"/>).
    /// </summary>
    /// <param name="id">The <see cref="Container.Id"/>.</param>
    /// <param name="partitionKeyPath">The partition key path; defaults to <c>/partitionKey</c>.</param>
    /// <param name="configure">An optional action to further configure the <see cref="ContainerProperties"/>.</param>
    /// <param name="dataOptions">An optional factory for the seed data <see cref="JsonDataReaderOptions"/> (see <see cref="CosmosDbContainerDefinition.DataOptions"/>); use <see cref="JsonDataReaderOptions.CreateForReferenceData"/> as the starting point (e.g. for a custom identifier generator or tenant).</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The consuming <see cref="CosmosDb"/> must register the same container using <see cref="CosmosDbContainerOptions.WithReferenceDataOutboxEvent"/>.</remarks>
    public CosmosDbProvisionArgs ReferenceDataContainer(string id, string partitionKeyPath = "/partitionKey", Action<ContainerProperties>? configure = null, Func<CosmosDbDataContext, JsonDataReaderOptions?>? dataOptions = null)
        => Add(new CosmosDbContainerDefinition(id, partitionKeyPath, true, configure, dataOptions));

    /// <summary>
    /// Declares the Change Feed Processor lease container shared by the outbox relay(s) (see <c>AddCosmosDbOutboxRelayHostedService</c>).
    /// </summary>
    /// <param name="leaseContainerId">The lease <see cref="Container.Id"/>; where not specified, defaults to <see cref="CosmosDbOutboxRelayOptions.DefaultLeaseContainerId"/> (the same default used by the relay).</param>
    /// <param name="configure">An optional action to further configure the <see cref="ContainerProperties"/>.</param>
    /// <returns>The <see cref="CosmosDbProvisionArgs"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The outbox relay never creates its lease container; it fails fast where it does not exist. Creating a container is a control-plane operation that a production host identity (e.g. Entra ID data-plane RBAC)
    /// is not expected to be permitted, so it must be provisioned up front - here (for dev/test/CI and admin-run migrations) or via infrastructure-as-code. The lease container is partitioned on <c>/id</c> (the Change Feed Processor's
    /// own lease-document convention), and is created, reset and dropped (as part of the database) with the other declared containers. A single lease container is safely shared by all relays (each Change Feed Processor
    /// is uniquely named per monitored container, and leases are further scoped to the monitored container's resource identity).</remarks>
    public CosmosDbProvisionArgs OutboxLeaseContainer(string? leaseContainerId = null, Action<ContainerProperties>? configure = null)
        => Add(new CosmosDbContainerDefinition(leaseContainerId ?? CosmosDbOutboxRelayOptions.DefaultLeaseContainerId, "/id", false, configure, null, true));

    /// <summary>
    /// Adds the <paramref name="definition"/> to the declared <see cref="Containers"/>.
    /// </summary>
    private CosmosDbProvisionArgs Add(CosmosDbContainerDefinition definition)
    {
        if (_containers.Any(x => x.Id == definition.Id))
            throw new InvalidOperationException($"Container '{definition.Id}' has already been declared.");

        _containers.Add(definition);
        return this;
    }
}
