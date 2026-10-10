namespace app-name.Infrastructure.Repositories;

/// <summary>Provides the <b>domain-name</b> <see cref="CosmosDb"/> wrapper; declares the containers and their accessors.</summary>
/// <remarks>The container identifiers declared here must match those provisioned by <c>app-name.Database</c> (see <c>Program.ConfigureProvisionArgs</c>).</remarks>
public class domain-nameCosmosDb(CosmosClient client, string databaseId) : CosmosDb(client, databaseId, _options)
{
// #if refdata-enabled
    private const string RefDataContainerId = "ref-data";

    // The reference-data container hosts the transactional outbox events for reference data; add further containers (each with their own outbox, as required) alongside.
    private static readonly CosmosDbOptions _options = new CosmosDbOptions().Container(RefDataContainerId, c => c.WithReferenceDataOutboxEvent());

    // Add one accessor per reference data type; e.g. CodeGen emits the persistence model and mapper per type, then expose using:
    //   public CosmosDbContainer<Persistence.Gender> Genders => Container<Persistence.Gender>(RefDataContainerId, o => o.WithTypeDiscriminator());
// #else
    private static readonly CosmosDbOptions _options = new();
// #endif
}
