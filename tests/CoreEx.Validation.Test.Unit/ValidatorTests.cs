using CoreEx.Entities;
using CoreEx.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace CoreEx.Validation.Test.Unit;

public class ValidatorTests
{
    [Test]
    public void BaseValidator_Include()
    {
        var p = new Person(1) { Name = "a", Code = "b", Age = 2 };
        var pv = new PersonValidator();

        pv.ValidateAsSuccess(p);

        // Verify base validator.
        p.Name = "c";
        pv.ValidateAsError(p, "name", "Name must be between 'a' and 'b'.");

        // Verify main validator.
        p.Name = "a";
        p.Code = "c";
        pv.ValidateAsError(p, "code", "Code must be between 'a' and 'b'.");
    }

    [Test]
    public void InlineValidator_Include()
    {
        var p = new Person(1) { Name = "a", Code = "b", Age = 2 };

        var pb = Validator.Create<PersonBase>()
            .HasProperty(p => p.Id, p => p.Between(1, 2))
            .HasProperty(p => p.Name, p => p.MaximumLength(10).Between("a", "b"));

        var pv = Validator.Create<Person>()
            .Include(pb)
            .HasProperty(p => p.Code, p => p.Mandatory().Between("a", "b"))
            .HasProperty(p => p.Age, p => p.Mandatory().Between(1, 4).DependsOn(p => p.Code).WhenValue(v => v == 2).Between(1, 4));

        pv.ValidateAsSuccess(p);

        // Verify base validator.
        p.Name = "c";
        pv.ValidateAsError(p, "name", "Name must be between 'a' and 'b'.");

        // Verify main validator.
        p.Name = "a";
        p.Code = "c";
        pv.ValidateAsError(p, "code", "Code must be between 'a' and 'b'.");
    }

    [Test]
    public void WithText()
    {
        var pv = Validator.Create<Person>().HasProperty(p => p.Name, c => c.WithText("Fullname").Mandatory());
        pv.ValidateAsError(new Person(1), "name", "Fullname is required.");

        // Verify Include also works.
        var pb = Validator.Create<PersonBase>().HasProperty(p => p.Name, c => c.WithText("Fullname").Mandatory());
        pv = Validator.Create<Person>().Include(pb);
        pv.ValidateAsError(new Person(1), "name", "Fullname is required.");
    }

    [Test]
    public void WithFormat_And_Localization()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var pv = Validator.Create<Person>().HasProperty(p => p.Salary, c => c.Between(100000m, 200000m));
            pv.ValidateAsError(new Person(1) { Salary = 99999m }, "salary", "Monies must be between '100000' and '200000'.");

            pv = Validator.Create<Person>().HasProperty(p => p.Salary, c => c.WithFormat("C").Between(100000m, 200000m));
            pv.ValidateAsError(new Person(1) { Salary = 99999m }, "salary", "Monies must be between '$100,000.00' and '$200,000.00'.");

            pv = Validator.Create<Person>().HasProperty(p => p.Salary, c => c.WithFormat("{0:C}").Between(100000m, 200000m));
            pv.ValidateAsError(new Person(1) { Salary = 99999m }, "salary", "Monies must be between '$100,000.00' and '$200,000.00'.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void WithFormat_Reflection()
    {
        var pv = Validator.Create<Person>().HasProperty(p => p.Age, c => c.Between(18, 60));
        pv.ValidateAsError(new Person(1) { Age = 12 }, "age", "Age must be between '018' and '060'.");
    }

    [Test]
    public void RuleSet()
    {
        var pv = Validator.Create<Person>()
            .HasRuleSet(x => x.Value.Code == "A", c =>
            {
                c.HasProperty(p => p.Age, c => c.Equal(18));
            })
            .HasRuleSet(x => x.Value.Code == "B", c =>
            {
                c.HasProperty(p => p.Age, c => c.Equal(19, _ => "nineteen"));
            });

        pv.ValidateAsSuccess(new Person(1) { Code = "A", Age = 18 });
        pv.ValidateAsSuccess(new Person(1) { Code = "B", Age = 19 });
        pv.ValidateAsSuccess(new Person(1) { Code = "C", Age = 20 });

        pv.ValidateAsError(new Person(1) { Code = "A", Age = 19 }, "age", "Age must be equal to '018'.");
        pv.ValidateAsError(new Person(1) { Code = "B", Age = 18 }, "age", "Age must be equal to nineteen.");
    }

    private class PersonBase(int id) : IReadOnlyIdentifier<int>
    {
        public int Id { get; } = id;

        public string? Name { get; set; }
    }

    private class Person(int id) : PersonBase(id)
    {
        public string Code { get; set; } = "A";

        [DisplayFormat(DataFormatString = "{0:D3}")]
        public int? Age { get; set; }

        [Localization("Monies")]
        public decimal Salary { get; init; }
    }

    private class PersonBaseValidator : Validator<PersonBase>
    {
        public PersonBaseValidator()
        {
            Property(p => p.Id).Between(1, 2);
            Property(p => p.Name).MaximumLength(10).Between("a", "b");
        }
    }

    private class PersonValidator : Validator<Person>
    {
        public PersonValidator()
        {
            Include(new PersonBaseValidator());
            Property(p => p.Code).Mandatory().Between("a", "b");
            Property(p => p.Age).Between(1, 4).Mandatory().DependsOn(p => p.Code).WhenValue(v => v == 2).Between(1, 4);
        }
    }
}