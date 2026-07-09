namespace Contoso.Shopping.Infrastructure.Repositories;

public partial class ReferenceDataRepository(ShoppingEfDb ef)
{
    private readonly ShoppingEfDb _ef = ef.ThrowIfNull();
}
