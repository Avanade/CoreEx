namespace Contoso.Customers.Infrastructure.Repositories;

public class CustomersCosmosDb(CosmosClient client, string databaseId) : CosmosDb(client, databaseId, _options)
{
    private const string RefDataContainerId = "ref-data";
    private const string CustomersContainerId = "customers";

    private static readonly CosmosDbOptions _options = new();

    public CosmosDbContainer<Persistence.ContactMethod> ContactMethods => Container<Persistence.ContactMethod>(RefDataContainerId, o => o.WithTypeDiscriminator());

    public CosmosDbContainer<Persistence.CustomerType> CustomerTypes => Container<Persistence.CustomerType>(RefDataContainerId, o => o.WithTypeDiscriminator());

    // The Customers container is mapped to the Contracts.Customer model using the CustomerMapper to handle the mapping between the persistence model and the contract model as the default access.
    public CosmosDbMappedContainer<Contracts.Customer, Persistence.Customer, CustomerMapper> Customers
        => Container<Persistence.Customer>(CustomersContainerId).ToMappedModel<Contracts.Customer, CustomerMapper>(new CustomerMapper());
}
