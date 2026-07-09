namespace Contoso.Products.Infrastructure.Repositories;

public partial class ReferenceDataRepository(ProductsEfDb ef)
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
}
