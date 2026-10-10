namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Represents a named embedded seed data resource, with an optional <see cref="JsonDataReaderOptions"/> factory specific to it.
/// </summary>
/// <param name="Assembly">The <see cref="System.Reflection.Assembly"/> containing the resource.</param>
/// <param name="ResourceName">The resource name (matched to the end of the fully qualified resource name; see <see cref="Resource.GetStream(string, Assembly?)"/>).</param>
/// <param name="DataOptions">The optional factory for the <see cref="JsonDataReaderOptions"/> used to import each container within the resource; a new instance must be returned on each invocation (the options are mutable and may be stateful).
/// Returning <see langword="null"/> defers to the <see cref="CosmosDbContainerDefinition.DataOptions"/> and then the default.</param>
public sealed record CosmosDbDataResource(Assembly Assembly, string ResourceName, Func<CosmosDbDataContext, JsonDataReaderOptions?>? DataOptions = null);