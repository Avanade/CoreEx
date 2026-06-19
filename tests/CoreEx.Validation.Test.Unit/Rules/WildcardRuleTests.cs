namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class WildcardRuleTests
{
    [Test]
    public void Wildcard()
    {
        ((string?)null).Validator(c => c.Wildcard()).ValidateAsSuccess();
        "xxx".Validator(c => c.Wildcard()).ValidateAsSuccess();
        "*xxx".Validator(c => c.Wildcard()).ValidateAsSuccess();
        "xxx*".Validator(c => c.Wildcard()).ValidateAsSuccess();
        "*xxx*".Validator(c => c.Wildcard()).ValidateAsSuccess();

        "x*x".Validator(c => c.Wildcard()).ValidateAsError("contains invalid or non-supported wildcard selection.");
        "x*x".Validator(c => c.Wildcard(Wildcards.Wildcard.MultiAll)).ValidateAsSuccess();
        "x?x".Validator(c => c.Wildcard(_ => Wildcards.Wildcard.MultiAll)).ValidateAsError("contains invalid or non-supported wildcard selection.");
        "x?x".Validator(c => c.Wildcard(Wildcards.Wildcard.BothAll)).ValidateAsSuccess();
    }
}