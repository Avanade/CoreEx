namespace Contoso.Shopping.Test.Unit.Validators;

public class AddressValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Address_Validate_Empty_AllRequired() => Test.Scoped(test =>
    {
        var a = new Address();
        AddressValidator.Default.AssertErrors(a,
            ("street1", "Street1 is required."),
            ("city", "City is required."),
            ("postCode", "Post code is required."),
            ("state", "State is required."));
    });

    [Test]
    public void Address_Validate_Street1_Required() => Test.Scoped(test =>
    {
        var a = new Address { City = "Sydney", PostCode = "2000", State = "NSW" };
        AddressValidator.Default.AssertErrors(a,
            ("street1", "Street1 is required."));
    });

    [Test]
    public void Address_Validate_City_Required() => Test.Scoped(test =>
    {
        var a = new Address { Street1 = "1 Main St", PostCode = "2000", State = "NSW" };
        AddressValidator.Default.AssertErrors(a,
            ("city", "City is required."));
    });

    [Test]
    public void Address_Validate_PostCode_Required() => Test.Scoped(test =>
    {
        var a = new Address { Street1 = "1 Main St", City = "Sydney", State = "NSW" };
        AddressValidator.Default.AssertErrors(a,
            ("postCode", "Post code is required."));
    });

    [Test]
    public void Address_Validate_State_Required() => Test.Scoped(test =>
    {
        var a = new Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000" };
        AddressValidator.Default.AssertErrors(a,
            ("state", "State is required."));
    });

    [Test]
    public void Address_Validate_Street2_Optional() => Test.Scoped(test =>
    {
        var a = new Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" };
        AddressValidator.Default.AssertSuccess(a);
    });

    [Test]
    public void Address_Validate_Success() => Test.Scoped(test =>
    {
        var a = new Address { Street1 = "1 Main St", Street2 = "Unit 5", City = "Sydney", PostCode = "2000", State = "NSW" };
        AddressValidator.Default.AssertSuccess(a);
    });
}
