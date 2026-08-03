namespace Contoso.Shopping.Application.Validators;

public class AddressValidator : AbstractValidator<Address, AddressValidator>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street1).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.PostCode).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
    }
}
