namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class ComparePropertyRuleTests
{
    [Test]
    public void CompareProperty()
    {
        var v = Validator.Create<Ranges>()
            .HasProperty(p => p.ToNumber, c => c.CompareProperty(CompareOperator.GreaterThanOrEqualTo, p => p.FromNumber))
            .HasProperty(p => p.ToText, c => c.CompareProperty(CompareOperator.GreaterThanOrEqualTo, p => p.FromText));

        v.ValidateAsSuccess(new Ranges());
        v.ValidateAsSuccess(new Ranges { FromNumber = null, ToNumber = 2 });
        v.ValidateAsSuccess(new Ranges { FromNumber = 1, ToNumber = null });
        v.ValidateAsSuccess(new Ranges { FromNumber = 1, ToNumber = 2 });
        v.ValidateAsError(new Ranges { FromNumber = 2, ToNumber = 1 }, "toNumber", "To number must be greater than or equal to '2'.");

        v.ValidateAsSuccess(new Ranges());
        v.ValidateAsSuccess(new Ranges { FromText = null, ToText = "b" });
        v.ValidateAsSuccess(new Ranges { FromText = "a", ToText = null });
        v.ValidateAsSuccess(new Ranges { FromText = "a", ToText = "b" });
        v.ValidateAsError(new Ranges { FromText = "b", ToText = "a" }, "toText", "To text must be greater than or equal to 'b'.");
    }

    [Test]
    public void CompareProperty_CompareToText()
    {
        var v = Validator.Create<Ranges>().HasProperty(p => p.ToNumber, c => c.CompareProperty(CompareOperator.GreaterThanOrEqualTo, p => p.FromNumber, _ => "Two"));
        v.ValidateAsError(new Ranges { FromNumber = 2, ToNumber = 1 }, "toNumber", "To number must be greater than or equal to Two.");
    }

    [Test]
    public void CompareProperty_Cast_Exception()
    {
        var v = Validator.Create<Ranges>().HasProperty(p => p.ToNumber, c => c.CompareProperty(CompareOperator.GreaterThanOrEqualTo, p => p.FromText!));
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () => await v.ValidateAsync(new Ranges { FromNumber = 2, ToNumber = 1, FromText = "a", ToText = "b" }));
        ex.Message.Should().Contain("Property 'FromText' and 'ToNumber' are incompatible for a comparison: The input string 'a' was not in a correct format.");
    }

    [Test]
    public void CompareProperty_WithMessage()
    {
        var v = Validator.Create<Ranges>().HasProperty(p => p.ToNumber, c => c.CompareProperty(CompareOperator.GreaterThanOrEqualTo, p => p.FromNumber).WithMessage("Oh no!"));
        v.ValidateAsError(new Ranges { FromNumber = 2, ToNumber = 1 }, "toNumber", "Oh no!");
    }

    private class Ranges
    {
        public int? FromNumber { get; set; }
        public int? ToNumber { get; set; }
        public string? FromText { get; set; }
        public string? ToText { get; set; }
    }
}