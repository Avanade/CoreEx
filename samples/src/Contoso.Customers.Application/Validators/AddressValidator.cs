namespace Contoso.Customers.Application.Validators;

public class AddressValidator : Validator<Contracts.Address, AddressValidator>
{
    public AddressValidator()
    {
        Property(a => a.Street1).Mandatory().MaximumLength(100);
        Property(a => a.City).Mandatory().MaximumLength(100);
        Property(a => a.PostCode).Mandatory().MaximumLength(20);
        Property(a => a.State).Mandatory().MaximumLength(100);
    }
}
