namespace Contoso.Shopping.Application.Mapping;

public class AddressMapper : BiDirectionMapper<Domain.ValueObjects.Address, Contracts.Address, AddressMapper>
{
    protected override Contracts.Address OnMap(Domain.ValueObjects.Address source) => new()
    {
        Street1 = source.Street1,
        Street2 = source.Street2,
        City = source.City,
        PostCode = source.PostCode,
        State = source.State
    };

    protected override Domain.ValueObjects.Address OnMap(Contracts.Address source) => new()
    {
        Street1 = source.Street1,
        Street2 = source.Street2,
        City = source.City!,
        PostCode = source.PostCode!,
        State = source.State!
    };
}
