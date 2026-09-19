namespace Contoso.Customers.Infrastructure.Repositories;

public partial class ReferenceDataRepository(CustomersCosmosDb cosmos)
{
    private CustomersCosmosDb _cosmos = cosmos.ThrowIfNull(); 
}
