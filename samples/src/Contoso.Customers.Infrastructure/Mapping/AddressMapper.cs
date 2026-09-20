namespace Contoso.Customers.Infrastructure.Mapping;

public class AddressMapper : BiDirectionMapper<Contracts.Address, Persistence.Address, AddressMapper>
{
    protected override Persistence.Address OnMap(Contracts.Address source) => new()
    {
        Street1 = source.Street1!,
        Street2 = source.Street2,
        City = source.City!,
        PostCode = source.PostCode!,
        State = source.State!
    };

    protected override Contracts.Address OnMap(Persistence.Address source) => new()
    {
        Street1 = source.Street1,
        Street2 = source.Street2,
        City = source.City,
        PostCode = source.PostCode,
        State = source.State
    };
}
