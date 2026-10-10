namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Provides the context for resolving the <see cref="JsonDataReaderOptions"/> used to import a single container's seed data.
/// </summary>
/// <param name="ResourceName">The data resource name (file) being imported.</param>
/// <param name="Container">The target <see cref="CosmosDbContainerDefinition"/>.</param>
/// <param name="NamingConvention">The <see cref="CosmosDbProvisionArgs.NamingConvention"/>.</param>
public sealed record CosmosDbDataContext(string ResourceName, CosmosDbContainerDefinition Container, JsonPropertyNamingConvention NamingConvention);
