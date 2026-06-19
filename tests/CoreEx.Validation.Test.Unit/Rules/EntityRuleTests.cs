using CoreEx.Validation;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class EntityRuleTests
{
    [Test]
    public void WithValidator_InlineConfigure_NoErrantValueSegment()
    {
        // Inline configure path must not inject ".value" into the error property path.
        // Path must be "name" — NOT "name.value".
        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Name, c => c.Entity(c => c.WithValidator(v => v.MaximumLength(4))));
        var p = new Person { Name = "toolong" };
        pv.ValidateAsError(p, "name", "must not exceed 4 character(s) in length.");
    }

    [Test]
    public void WithValidator_DirectValidator_Parity()
    {
        // Direct IValidatorEx<TProperty> path (entity validator) must produce the same nested path as the inline configure path.
        var av = Validator.Create<Address>().HasProperty(p => p.Street, c => c.Mandatory().MaximumLength(20));
        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Address, c => c.Entity(c => c.WithValidator(av)));
        var p = new Person { Address = new Address() };
        pv.ValidateAsError(p, "address.street", "Street is required.");
    }

    public class Person
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
    }
}
