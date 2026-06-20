namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class CommonRuleTests
{
    [Test]
    public void Common_String()
    {   
        var cv = Validator.CreateCommon<string>(c => c.Mandatory().MaximumLength(5));

        "abc".Validator(c => c.Common(cv)).ValidateAsSuccess();
        "abcdef".Validator(c => c.Common(cv)).ValidateAsError("must not exceed 5 character(s) in length.");
        ((string?)null).Validator(c => c.Common(cv)).ValidateAsError("is required.");
    }

    [Test]
    public void Common_Int32()
    {
        var cv = Validator.CreateCommon<int>(c => c.Mandatory().Between(0, 10));

        ((int?)5).Validator(c => c.Common(cv)).ValidateAsSuccess();
        ((int?)20).Validator(c => c.Common(cv)).ValidateAsError("must be between '0' and '10'.");
        ((int?)null).Validator(c => c.Common(cv)).ValidateAsError("is required.");

        5.Validator(c => c.Common(cv)).ValidateAsSuccess();
        20.Validator(c => c.Common(cv)).ValidateAsError("must be between '0' and '10'.");

    }

    [Test]
    public void Common_Entity()
    {
        var cvs = Validator.CreateCommon<string>(c => c.Mandatory().MaximumLength(5));
        var cvi = Validator.CreateCommon<int>(c => c.Mandatory().Between(0, 10));

        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Name, c => c.Common(cvs))
            .HasProperty(p => p.Age, c => c.Common(cvi))
            .HasProperty(p => p.Address, c => c.Entity(new AddressValidator()));

        pv.ValidateAsSuccess(new Person { Name = "John", Age = 5, Address = new Address { Street = "1 St" } });
        pv.ValidateAsError(new Person { Name = "John", Age = -5, Address = new Address { Street = "1 St" } }, "age", "Age must be between '0' and '10'.");
        pv.ValidateAsError(new Person { Name = "John", Age = 5, Address = new Address { Street = "1 Street" } }, "address.street", "Street must not exceed 5 character(s) in length.");
    }

    public class Person
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public Address? Address { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
    }

    public class AddressValidator : Validator<Address>
    { 
        public AddressValidator()
        {
            Property(a => a.Street).Mandatory().MaximumLength(5);
        }
    }
}