using CoreEx.Entities;
using CoreEx.Validation;
using System.Globalization;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class CollectionRuleTests
{
    [Test]
    public void MaxCount()
    {
        new int[] { 1, 2 }.Validator(c => c.Collection(2)).ValidateAsSuccess();
        ((IEnumerable<int>)[1, 2]).Validator(c => c.Collection(2)).ValidateAsSuccess();
        Array.Empty<int>().Validator(c => c.Collection(2)).ValidateAsSuccess();
        Enumerable.Empty<int>().Validator(c => c.Collection(2)).ValidateAsSuccess();
        ((IEnumerable<int>?)null).Validator(c => c.Collection(2)).ValidateAsSuccess();

        new int[] { 1, 2, 3 }.Validator(c => c.Collection(2)).ValidateAsError("must not exceed 2 item(s).");
        ((IEnumerable<int>)[1, 2, 3]).Validator(c => c.Collection(2)).ValidateAsError("must not exceed 2 item(s).");
    }

    [Test]
    public void MaxCount_Func()
    {
        new int[] { 1, 2 }.Validator(c => c.Collection(_ => 2)).ValidateAsSuccess();
        ((IEnumerable<int>)[1, 2]).Validator(c => c.Collection(_ => 2)).ValidateAsSuccess();
        Array.Empty<int>().Validator(c => c.Collection(_ => 2)).ValidateAsSuccess();
        Enumerable.Empty<int>().Validator(c => c.Collection(_ => 2)).ValidateAsSuccess();
        ((IEnumerable<int>?)null).Validator(c => c.Collection(_ => 2)).ValidateAsSuccess();

        new int[] { 1, 2, 3 }.Validator(c => c.Collection(_ => 2)).ValidateAsError("must not exceed 2 item(s).");
        ((IEnumerable<int>)[1, 2, 3]).Validator(c => c.Collection(_ => 2)).ValidateAsError("must not exceed 2 item(s).");
    }

    [Test]
    public void MinCount()
    {
        new int[] { 1, 2 }.Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsSuccess();
        ((IEnumerable<int>)[1, 2]).Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsSuccess();

        List<int>? list = null;
        list.Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsSuccess();

        Array.Empty<int>().Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        Enumerable.Empty<int>().Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        new int[] { 1 }.Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        ((IEnumerable<int>)[1]).Validator(c => c.Collection(minCount: 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
    }

    [Test]
    public void MinCount_Func()
    {
        new int[] { 1, 2 }.Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsSuccess();
        ((IEnumerable<int>)[1, 2]).Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsSuccess();

        List<int>? list = null;
        list.Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsSuccess();

        Array.Empty<int>().Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        Enumerable.Empty<int>().Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        new int[] { 1 }.Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
        ((IEnumerable<int>)[1]).Validator(c => c.Collection(minCount: _ => 2, maxCount: null)).ValidateAsError("must have at least 2 item(s).");
    }

    [Test]
    public void CommonValidator()
    {
        var ic = Validator.CreateCommon<int>(v => v.Between(10, 20));

        new int[] { 11, 18 }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Between(10, 20)))).ValidateAsSuccess();
        new int?[] { 11, null, 18 }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Between(10, 20)).AllowNullItems())).ValidateAsSuccess();
        new int[] { 11, 18 }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Common(ic)))).ValidateAsSuccess();
        new int?[] { 11, null, 18 }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Common(ic)).AllowNullItems())).ValidateAsSuccess();

        new List<int> { 11, 18 }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Between(10, 20)))).ValidateAsSuccess();
        new List<int?> { 11, null, 18 }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Between(10, 20)).AllowNullItems())).ValidateAsSuccess();

        new string[] { "AAA", "BBB" }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Length(3)))).ValidateAsSuccess();
        new string?[] { "AAA", null, "BBB" }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Length(3)).AllowNullItems())).ValidateAsSuccess();

        new string?[] { "AAA", null, "BBB" }.Validator(c => c.Collection(w => w.WithItemValidator(v => v.Length(3)))).ValidateAsError("contains one or more items that are not specified.");
    }

    [Test]
    public void DuplicateCheck_IEquatable()
    {
        new int[] { 1, 2, 3 }.Validator(c => c.Collection(w => w.WithDuplicateCheck())).ValidateAsSuccess();
        new int[] { 1, 2, 1 }.Validator(c => c.Collection(w => w.WithDuplicateCheck())).ValidateAsError("contains duplicates; Item specified more than once.");
    }

    [Test]
    public void DuplicateCheck_KeySelector()
    {
        new int[] { 1, 2, 3 }.Validator(c => c.Collection(w => w.WithDuplicateCheck(i => i))).ValidateAsSuccess();
        new int[] { 1, 2, 1 }.Validator(c => c.Collection(w => w.WithDuplicateCheck(i => i))).ValidateAsError("contains duplicates; Item specified more than once.");
    }

    [Test]
    public void DuplicateIdCheck()
    {
        var list = new List<Person>
        {
            new() { Id = "A", Name = "Bob" },
            new() { Id = "B", Name = "Kate" }
        };

        list[0].Id = "B";
        list.Validator(c => c.Collection(w => w.WithDuplicateIdCheck())).ValidateAsError("contains duplicates; Identifier specified more than once.");
    }

    [Test]
    public void DuplicateKeyCheck()
    {
        var list = new List<Person>
        {
            new() { Id = "A", Name = "Bob" },
            new() { Id = "B", Name = "Kate" }
        };

        list.Validator(c => c.Collection(w => w.WithDuplicateKeyCheck())).ValidateAsSuccess();

        list[0].Id = "B";
        list.Validator(c => c.Collection(w => w.WithDuplicateKeyCheck())).ValidateAsError("contains duplicates; Key specified more than once.");
    }

    [Test]
    public void DuplicatePropertyCheck()
    {
        var list = new List<Person>
        {
            new() { Id = "A", Name = "Bob" },
            new() { Id = "B", Name = "Kate" }
        };

        list.Validator(c => c.Collection(w => w.WithDuplicatePropertyCheck(p => p.Name))).ValidateAsSuccess();

        list[0].Name = "Kate";
        list.Validator(c => c.Collection(w => w.WithDuplicatePropertyCheck(p => p.Name))).ValidateAsError("contains duplicates; Name specified more than once.");
    }

    [Test]
    public void Entity()
    {
        var av = Validator.Create<Address>()
            .HasProperty(p => p.Street, c => c.Mandatory().MaximumLength(20));

        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Id, c => c.Mandatory())
            .HasProperty(p => p.Name, c => c.Mandatory())
            .HasProperty(p => p.Addresses, c => c.Mandatory().Collection(c => c.WithItemValidator(av).WithDuplicatePropertyCheck(a => a.Street)));

        var p = new Person
        {
            Id = "1",
            Name = "John",
            Addresses =
            [
                new Address { Street = "1 St" },
                new Address { Street = "2 St" }
            ]
        };

        pv.ValidateAsSuccess(p);

        p.Addresses.Add(new Address());
        pv.ValidateAsError(p, "addresses[2].street", "Street is required.");

        p.Addresses[2]!.Street = "1 St";
        pv.ValidateAsError(p, "addresses", "Addresses contains duplicates; Street specified more than once.");
    }

    [Test]
    public void EnclosingCollection()
    {
        var av = Validator.Create<Address>().HasProperty(p => p.Street, c => c.Mandatory().MaximumLength(20));
        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Id, c => c.Mandatory())
            .HasProperty(p => p.Name, c => c.Mandatory())
            .HasProperty(p => p.Addresses, c => c.Mandatory().Collection(c => c.WithItemValidator(av).WithDuplicatePropertyCheck(a => a.Street)));

        var pc = new PersonCollection
        {
            new Person
            {
                Id = "1",
                Name = "John",
                Addresses =
                [
                    new Address { Street = "1 St" },
                    new Address { Street = "2 St" }
                ]
            },
            new Person
            {
                Id = "2",
                Name = "Jane",
                Addresses =
                [
                    new Address { Street = "A St" },
                    new Address { Street = "B St" }
                ]
            }
        };

        var pcv = Validator.Create<PersonCollection>().Self(c => c.WithText("Collection").Collection(c => c.WithItemValidator(pv).WithDuplicateIdCheck()));
        
        pcv.ValidateAsSuccess(pc);
        pc.Validator(c => c.Collection(c => c.WithItemValidator(pv).WithDuplicateIdCheck())).ValidateAsSuccess();

        pc[1].Addresses!.Add(new Address());
        pc.Validator(c => c.Collection(c => c.WithItemValidator(pv).WithDuplicateIdCheck())).ValidateAsError("pc[1].addresses[2].street", "Street is required.");

        pcv.ValidateAsError(pc, "[1].addresses[2].street", "Street is required.");

        pc[1].Addresses![2]!.Street = "A St";
        pc.Validator(c => c.Collection(c => c.WithItemValidator(pv).WithDuplicateIdCheck())).ValidateAsError("pc[1].addresses", "Addresses contains duplicates; Street specified more than once.");

        pcv.ValidateAsError(pc, "[1].addresses", "Addresses contains duplicates; Street specified more than once.");


        pc[1].Addresses![2]!.Street = "C St";

        pc.Add(new Person
        {
            Id = "1",
            Name = "Jake",
            Addresses =
                [
                    new Address { Street = "X St" },
                    new Address { Street = "Y St" }
                ]
        });

        pc.Validator(c => c.Collection(c => c.WithItemValidator(pv).WithDuplicateIdCheck())).ValidateAsError("pc", "Pc contains duplicates; Identifier specified more than once.");

        pcv.ValidateAsError(pc, "", "Collection contains duplicates; Identifier specified more than once.");
    }

    [Test]
    public void GetCollectionIndex()
    {
        var pc = new PersonCollection
        {
            new Person
            {
                Id = "1",
                Name = "John"
            }
        };

        pc.Validator(c => c.Collection(w => w.WithItemValidator(iv => iv.NotFound().When(ctx => ctx.GetCollectionIndex() != 0)))).ValidateAsSuccess();
    }

    [Test]
    public void WithItemValidator_InlineConfigure_NoErrantValueSegment()
    {
        // Inline configure path must not inject ".value" into the error property path.
        // Path must be "tags[2]" — NOT "tags[2].value".
        var pv = Validator.Create<PersonWithTags>()
            .HasProperty(p => p.Tags, c => c.Collection(c => c.WithItemValidator(v => v.MaximumLength(4))));
        var p = new PersonWithTags { Tags = ["ok", "fine", "toolong"] };
        pv.ValidateAsError(p, "tags[2]", "must not exceed 4 character(s) in length.");
    }

    [Test]
    public void WithItemValidator_DirectValidator_Parity()
    {
        // Direct IValidatorEx<TItem> path (entity validator) must produce the same nested path as the inline configure path.
        var av = Validator.Create<Address>().HasProperty(p => p.Street, c => c.Mandatory().MaximumLength(20));
        var pv = Validator.Create<Person>()
            .HasProperty(p => p.Addresses, c => c.Collection(c => c.WithItemValidator(av)));
        var p = new Person
        {
            Id = "1",
            Name = "John",
            Addresses = [new Address { Street = "1 St" }, new Address { Street = "2 St" }, new Address()]
        };
        pv.ValidateAsError(p, "addresses[2].street", "Street is required.");
    }

    public class PersonCollection : List<Person> { }

    public class PersonWithTags
    {
        public List<string>? Tags { get; set; }
    }

    public class Person : IIdentifier<string?>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int? Age { get; set; }
        public List<Address>? Addresses { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
    }
}