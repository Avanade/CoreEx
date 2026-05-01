using CoreEx.Entities;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class DictionaryRuleTests
{
    [Test]
    public void MinCount()
    {
        new Dictionary<string, string> { { "abc", "mnop" }, { "def", "qrst" } }.Validator(c => c.Dictionary(minCount: 2, maxCount: null)).ValidateAsSuccess();
        new Dictionary<string, string> { { "abc", "mnop" } }.Validator(c => c.Dictionary(minCount: 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        new Dictionary<string, string>().Validator(c => c.Dictionary(minCount: 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        ((Dictionary<string, string>?)null).Validator(c => c.Dictionary(minCount: 2, maxCount: null)).ValidateAsSuccess();
    }

    [Test]
    public void MinCount_Func()
    {
        new Dictionary<string, string> { { "abc", "mnop" }, { "def", "qrst" } }.Validator(c => c.Dictionary(minCount: _ => 2, maxCount: null)).ValidateAsSuccess();
        new Dictionary<string, string> { { "abc", "mnop" } }.Validator(c => c.Dictionary(minCount: _ => 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        new Dictionary<string, string>().Validator(c => c.Dictionary(minCount: _ => 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        ((Dictionary<string, string>?)null).Validator(c => c.Dictionary(minCount: _ => 2, maxCount: null)).ValidateAsSuccess();
    }

    [Test]
    public void MaxCount()
    {
        new Dictionary<string, string> { { "abc", "mnop" }, { "def", "qrst" } }.Validator(c => c.Dictionary(2)).ValidateAsSuccess();
        new Dictionary<string, string> { { "abc", "mnop" } }.Validator(c => c.Dictionary(2)).ValidateAsSuccess();
        new Dictionary<string, string>().Validator(c => c.Dictionary(2)).ValidateAsSuccess();
        ((Dictionary<string, string>?)null).Validator(c => c.Dictionary(2)).ValidateAsSuccess();
        new Dictionary<string, string> { { "abc", "mnop" }, { "def", "qrst" }, { "ghi", "uvwx" } }.Validator(c => c.Dictionary(2)).ValidateAsError("must not exceed 2 item(s).");
    }

    [Test]
    public void MaxCount_Func()
    {
        new Dictionary<string, string> { { "abc", "mnop" }, { "def", "qrst" } }.Validator(c => c.Dictionary(_ => 2)).ValidateAsSuccess();
        new Dictionary<string, string> { { "abc", "mnop" } }.Validator(c => c.Dictionary(_ => 2)).ValidateAsSuccess();
        new Dictionary<string, string>().Validator(c => c.Dictionary(_ => 2)).ValidateAsSuccess();
        ((Dictionary<string, string>?)null).Validator(c => c.Dictionary(_ => 2)).ValidateAsSuccess();
        new Dictionary<string, string> { { "abc", "mnop" }, { "def", "qrst" }, { "ghi", "uvwx" } }.Validator(c => c.Dictionary(_ => 2)).ValidateAsError("must not exceed 2 item(s).");
    }

    [Test]
    public void CommonValidator()
    {
        new Dictionary<string, string> { { "abc", "mnop" } }.Validator(c => c.Dictionary(c => c.WithKeyValidator(k => k.MaximumLength(3)).WithValueValidator(v => v.MaximumLength(4)))).ValidateAsSuccess();
        new Dictionary<string, string> { { "abcd", "mnop" } }.Validator(c => c.Dictionary(c => c.WithKeyValidator(k => k.MaximumLength(3)).WithValueValidator(v => v.MaximumLength(4)))).ValidateAsError("Key must not exceed 3 character(s) in length.");
        new Dictionary<string, string> { { "abd", "mnopq" } }.Validator(c => c.Dictionary(c => c.WithKeyValidator(k => k.MaximumLength(3)).WithValueValidator(v => v.MaximumLength(4)))).ValidateAsError("Value must not exceed 4 character(s) in length.");
    }

    [Test]
    public void Entity()
    {
        var av = Validator.Create<Address>().HasProperty(p => p.Street, c => c.Mandatory().MaximumLength(20));
        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Id, c => c.Mandatory())
            .HasProperty(p => p.Name, c => c.Mandatory())
            .HasProperty(p => p.Addresses, c => c.Mandatory().Dictionary(c => c.WithKeyValidator("Address code", k => k.Mandatory().MaximumLength(4)).WithValueValidator(av)));

        var p = new Person
        {
            Id = "1",
            Name = "John",
            Addresses = new Dictionary<string, Address>
            {
                { "home", new Address { Street = "1 St" } },
                { "post", new Address { Street = "2 St" } }
            }
        };

        pv.ValidateAsSuccess(p);

        p.Addresses.Clear();
        p.Addresses.Add("other", new Address { Street = "3 St" });
        var vr = pv.ValidateAsError(p, "addresses.other", "Address code must not exceed 4 character(s) in length.");

        p.Addresses.Remove("other");

        p.Addresses.Add("othr", new Address());
        pv.ValidateAsError(p, "addresses.othr.street", "Street is required.");

        p.Addresses["othr"] = new Address { Street = "This street address is way too long" };
        pv.ValidateAsError(p, "addresses.othr.street", "Street must not exceed 20 character(s) in length.");

        p.Addresses["othr"] = null!;
        pv.ValidateAsError(p, "addresses", "Addresses contains one or more values that are not specified.");

        p.Addresses.Remove("othr");
        p.Addresses.Add("", new Address { Street = "33rd rd" });
        pv.ValidateAsError(p, "addresses.key", "Address code is required.");
    }

    [Test]
    public void GetDictionaryKey()
    {
        var av = Validator.Create<Address>().HasProperty(p => p.Street, c
            => c.Mandatory().MaximumLength(20).NotFound().When(ctx => ctx.GetDictionaryKey<string>() != "home" && ctx.GetDictionaryKey<string>() != "post"));

        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Id, c => c.Mandatory())
            .HasProperty(p => p.Name, c => c.Mandatory())
            .HasProperty(p => p.Addresses, c => c.Mandatory().Dictionary(c => c.WithKeyValidator("Address code", k => k.Mandatory().MaximumLength(4)).WithValueValidator(av)));

        var p = new Person
        {
            Id = "1",
            Name = "John",
            Addresses = new Dictionary<string, Address>
            {
                { "home", new Address { Street = "1 St" } },
                { "post", new Address { Street = "2 St" } }
            }
        };

        pv.ValidateAsSuccess(p);
    }

    public class Person : IIdentifier<string?>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int? Age { get; set; }
        public Dictionary<string, Address>? Addresses { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
    }
}