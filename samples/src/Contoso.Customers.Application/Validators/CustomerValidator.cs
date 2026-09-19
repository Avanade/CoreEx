namespace Contoso.Customers.Application.Validators;

public class CustomerValidator : Validator<Contracts.Customer, CustomerValidator>
{
    public CustomerValidator()
    {
        Property(c => c.FirstName).Mandatory().MaximumLength(100);
        Property(c => c.LastName).Mandatory().MaximumLength(100);
        Property(c => c.Email).Mandatory().MaximumLength(250);
        Property(c => c.Phone).MaximumLength(50);
        Property(c => c.CustomerType).Mandatory().IsValid();
        Property(c => c.ContactMethod).Mandatory().IsValid();
        Property(c => c.ShippingAddress).Entity(AddressValidator.Default);
    }
}
