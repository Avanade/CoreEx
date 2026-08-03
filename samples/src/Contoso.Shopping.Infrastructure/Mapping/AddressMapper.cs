namespace Contoso.Shopping.Infrastructure.Mapping;

public class AddressMapper : BiDirectionMapper<Persistence.Address, Domain.ValueObjects.Address, AddressMapper>
{
    protected override Domain.ValueObjects.Address OnMap(Persistence.Address source) => new()
    {
        Street1 = source.Street1,
        Street2 = source.Street2,
        City = source.City,
        PostCode = source.PostCode,
        State = source.State
    };

    protected override Persistence.Address OnMap(Domain.ValueObjects.Address source) => new()
    {
        Street1 = source.Street1,
        Street2 = source.Street2,
        City = source.City,
        PostCode = source.PostCode,
        State = source.State
    };
}
