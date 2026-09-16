namespace Contoso.Customers.Infrastructure.Repositories;

public class CustomersCosmosDb(CosmosClient client, string databaseId) : CosmosDb(client, databaseId, _options)
{
    private const string RefDataContainerId = "ref-data";
    private const string CustomersContainerId = "customers";

    private static readonly CosmosDbOptions _options = new CosmosDbOptions();

    public CosmosDbContainer<Persistence.ContactMethod> ContactMethods => Container<Persistence.ContactMethod>(RefDataContainerId, o => o.WithTypeDiscriminator());

    public CosmosDbContainer<Persistence.CustomerType> CustomerTypes => Container<Persistence.CustomerType>(RefDataContainerId, o => o.WithTypeDiscriminator());

    // PartitionKey defaults from the model's own IReadOnlyPartitionKey.PartitionKey (set to Id by CustomerMapper) - no WithPartitionKey/WithFixedPartitionKey override needed; kept deliberately simple.
    public CosmosDbContainer<Persistence.Customer> Customers => Container<Persistence.Customer>(CustomersContainerId);
}
