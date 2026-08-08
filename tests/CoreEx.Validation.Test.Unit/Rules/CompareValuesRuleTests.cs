using CoreEx.Results;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class CompareValuesRuleTests
{
    [Test]
    public void CompareValues_Int32()
    {
        var vals = new[] { 1, 2, 3 };

        ((int?)null).Validator(c => c.CompareValues(vals)).ValidateAsSuccess();
        1.Validator(c => c.CompareValues(vals)).ValidateAsSuccess();

        4.Validator(c => c.CompareValues(vals)).ValidateAsError("is invalid.");
    }

    [Test]
    public void CompareValues_String()
    {
        var vals = new[] { "a", "b", "c" };

        ((string?)null).Validator(c => c.CompareValues(vals)).ValidateAsSuccess();
        "c".Validator(c => c.CompareValues(vals)).ValidateAsSuccess();
        "C".Validator(c => c.CompareValues(vals, StringComparer.OrdinalIgnoreCase)).ValidateAsSuccess();

        "C".Validator(c => c.CompareValues(vals)).ValidateAsError("is invalid.");
        "C".Validator(c => c.CompareValues(vals).WithMessage("Oh no!")).ValidateAsError("Oh no!");
    }

    [Test]
    public void CompareValues_Int32_Func()
    {
        var vals = new[] { 1, 2, 3 };

        ((int?)null).Validator(c => c.CompareValues(_ => vals)).ValidateAsSuccess();
        1.Validator(c => c.CompareValues(_ => vals)).ValidateAsSuccess();

        4.Validator(c => c.CompareValues(_ => vals)).ValidateAsError("is invalid.");
    }

    [Test]
    public void CompareValues_String_Func()
    {
        var vals = new[] { "a", "b", "c" };

        ((string?)null).Validator(c => c.CompareValues(_ => vals)).ValidateAsSuccess();
        "c".Validator(c => c.CompareValues(_ => vals)).ValidateAsSuccess();
        "C".Validator(c => c.CompareValues(_ => vals, StringComparer.OrdinalIgnoreCase)).ValidateAsSuccess();

        "C".Validator(c => c.CompareValues(_ => vals)).ValidateAsError("is invalid.");
        "C".Validator(c => c.CompareValues(_ => vals).WithMessage("Oh no!")).ValidateAsError("Oh no!");
    }

    [Test]
    public void CompareValues_Override()
    {
        var vals = new[] { "A", "B", "C" };
        var v = Validator.Create<Person>()
            .HasProperty(p => p.Id, p => p.CompareValues(vals, StringComparer.OrdinalIgnoreCase, true))
            .HasProperty(p => p.Code, p => p.CompareValues(vals, StringComparer.OrdinalIgnoreCase, true));

        v.ValidateAsSuccess(new Person("A"));

        var r = v.ValidateAsSuccess(new Person("A") { Code = "a" });
        r.Value!.Code.Should().NotBeNull().And.Be("A");

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await v.ValidateAsync(new Person("a")));
        ex.Message.Should().Be("The property 'Id' is read-only and cannot be overridden.");
    }

    [Test]
    public void CompareValues_Override_NoMatch_DoesNotThrow()
    {
        // Regression: overrideValueWhereMatched combined with a non-matching value must produce a normal validation
        // error rather than crashing with "Sequence contains no matching element" from an unconditional First().
        var vals = new[] { "A", "B", "C" };

        "Z".Validator(c => c.CompareValues(vals, overrideValueWhereMatched: true)).ValidateAsError("is invalid.");
        "Z".Validator(c => c.CompareValues(_ => vals, overrideValueWhereMatched: true)).ValidateAsError("is invalid.");
    }

    [Test]
    public void CompareValues_Override_PropagatesToChainedRule()
    {
        // Regression: a rule chained after CompareValues(overrideValueWhereMatched: true) must observe the overridden
        // value, not the stale pre-override value (PropertyContext is a struct passed by value through the chain).
        var vals = new[] { "A", "B", "C" };
        var v = Validator.Create<Person>()
            .HasProperty(p => p.Code, p => p.CompareValues(vals, StringComparer.OrdinalIgnoreCase, true).CompareValues(new[] { "A" }));

        var r = v.ValidateAsSuccess(new Person("A") { Code = "a" });
        r.Value!.Code.Should().Be("A");
    }

    public class Person(string id)
    {
        public string Id { get; } = id;
        public string? Code { get; set; }
    }
}