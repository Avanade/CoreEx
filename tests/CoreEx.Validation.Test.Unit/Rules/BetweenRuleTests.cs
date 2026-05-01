using CoreEx.Results;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class BetweenRuleTests
{
    [Test]
    public void Between_Values()
    {
        ((int?)null).Validator(c => c.Between(1, 3)).ValidateAsSuccess();
        1.Validator(c => c.Between(1, 3)).ValidateAsSuccess();
        2.Validator(c => c.Between(1, 3)).ValidateAsSuccess();
        ((int?)3).Validator(c => c.Between(1, 3)).ValidateAsSuccess();
        ((string?)null).Validator(c => c.Between("a", "g")).ValidateAsSuccess();
        "b".Validator(c => c.Between("a", "g")).ValidateAsSuccess();

        0.Validator(c => c.Between(1, 3)).ValidateAsError(" must be between '1' and '3'.");
        4.Validator(c => c.Between(1, 3)).ValidateAsError(" must be between '1' and '3'.");
        ((int?)0).Validator(c => c.Between(1, 3)).ValidateAsError(" must be between '1' and '3'.");
        ((int?)4).Validator(c => c.Between(1, 3)).ValidateAsError(" must be between '1' and '3'.");

        ((int?)4).Validator(c => c.Between(1, 3, "One", "Three")).ValidateAsError(" must be between One and Three.");
    }

    [Test]
    public void Between_Funcs()
    {
        ((int?)null).Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsSuccess();
        1.Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsSuccess();
        2.Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsSuccess();
        ((int?)3).Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsSuccess();
        ((string?)null).Validator(c => c.Between(_ => "a", _ => "g")).ValidateAsSuccess();
        "b".Validator(c => c.Between(_ => "a", _ => "g")).ValidateAsSuccess();

        0.Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsError(" must be between '1' and '3'.");
        4.Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsError(" must be between '1' and '3'.");
        ((int?)0).Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsError(" must be between '1' and '3'.");
        ((int?)4).Validator(c => c.Between(_ => 1, _ => 3)).ValidateAsError(" must be between '1' and '3'.");

        ((int?)4).Validator(c => c.Between(_ => 1, _ => 3, _ => "One", _ => "Three")).ValidateAsError(" must be between One and Three.");
    }

    [Test]
    public void Between_Funcs_Args()
    {
        var args = new ValidationArgs { Parameters = new Dictionary<string, object?> { { "Min", 1 }, { "Max", 3 } } };
        1.Validator(c => c.Between((c) => Result.Ok((int)c.Parameters!["Min"]!), (c) => Result.Ok((int)c.Parameters!["Max"]!))).ValidateAsSuccess(args);

        5.Validator(c => c.Between((c) => Result.Ok((int)c.Parameters!["Min"]!), (c) => Result.Ok((int)c.Parameters!["Max"]!))).ValidateAsError(" must be between '1' and '3'.", args);
    }
}