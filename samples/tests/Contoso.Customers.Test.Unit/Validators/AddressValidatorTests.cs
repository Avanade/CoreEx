namespace Contoso.Customers.Test.Unit.Validators;

public class AddressValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var a = new Address();
        AddressValidator.Default.AssertErrors(a,
            ("street1", "Street1 is required."),
            ("city", "City is required."),
            ("postCode", "Post code is required."),
            ("state", "State is required."));
    });

    [Test]
    public void Success() => Test.Scoped(test =>
    {
        var a = new Address { Street1 = "1 Main Street", City = "Springfield", PostCode = "12345", State = "IL" };
        AddressValidator.Default.AssertSuccess(a);
    });
}
