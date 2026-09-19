namespace Contoso.Customers.Test.Unit.Validators;

public class CustomerValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var c = new Customer();
        CustomerValidator.Default.AssertErrors(c,
            ("firstName", "First name is required."),
            ("lastName", "Last name is required."),
            ("email", "Email is required."),
            ("customerType", "Customer type is required."),
            ("contactMethod", "Contact method is required."));
    });

    [Test]
    public void Invalid_ReferenceData() => Test.Scoped(test =>
    {
        var c = new Customer { FirstName = "Homer", LastName = "Simpson", Email = "homer@example.com", CustomerTypeCode = "XX", ContactMethodCode = "XX" };
        CustomerValidator.Default.AssertErrors(c,
            ("customerType", "Customer type is invalid."),
            ("contactMethod", "Contact method is invalid."));
    });

    [Test]
    public void ShippingAddress_Invalid() => Test.Scoped(test =>
    {
        var c = new Customer
        {
            FirstName = "Homer",
            LastName = "Simpson",
            Email = "homer@example.com",
            CustomerTypeCode = "IND",
            ContactMethodCode = "EM",
            ShippingAddress = new Address()
        };

        CustomerValidator.Default.AssertErrors(c,
            ("shippingAddress.street1", "Street1 is required."),
            ("shippingAddress.city", "City is required."),
            ("shippingAddress.postCode", "Post code is required."),
            ("shippingAddress.state", "State is required."));
    });

    [Test]
    public void Success() => Test.Scoped(test =>
    {
        var c = new Customer { FirstName = "Homer", LastName = "Simpson", Email = "homer@example.com", CustomerTypeCode = "IND", ContactMethodCode = "EM" };
        CustomerValidator.Default.AssertSuccess(c);
    });
}
