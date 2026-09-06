namespace Contoso.Customers.Infrastructure.Repositories;

public class CustomersCosmosDb(CosmosClient client, string databaseId) : CosmosDb(client, databaseId, _options)
{
    private const string RefDataContainerId = "ref-data";

    private static readonly CosmosDbOptions _options = new CosmosDbOptions();

    public CosmosDbContainer<Persistence.ContactMethod> ContactMethods => Container<Persistence.ContactMethod>(RefDataContainerId, o => o.WithTypeDiscriminator());

    public CosmosDbContainer<Persistence.CustomerType> CustomerTypes => Container<Persistence.CustomerType>(RefDataContainerId, o => o.WithTypeDiscriminator());
}
