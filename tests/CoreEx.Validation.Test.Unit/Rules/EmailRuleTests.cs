namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class EmailRuleTests
{
    [Test]
    public void Email()
    {
        ((string?)null).Validator(c => c.Email()).ValidateAsSuccess();
        "blah@domain".Validator(c => c.Email()).ValidateAsSuccess();
        "blah@domain.co.nz".Validator(c => c.Email()).ValidateAsSuccess();
        $"mynameis@{new string('x', 250)}.com".Validator(c => c.Email()).ValidateAsSuccess();
        $"mynameis@{new string('x', 250)}.com".Validator(c => c.Email(null)).ValidateAsSuccess();

        "".Validator(c => c.Email()).ValidateAsError("is an invalid e-mail address.");
        ((string?)"blah").Validator(c => c.Email()).ValidateAsError("is an invalid e-mail address.");
        "blah@".Validator(c => c.Email()).ValidateAsError("is an invalid e-mail address.");
        "blah@.com".Validator(c => c.Email()).ValidateAsError("is an invalid e-mail address.");
        $"mynameis@{new string('x', 250)}.com".Validator(c => c.Email(100)).ValidateAsError("must not exceed 100 character(s) in length.");

        $"mynameis@{new string('x', 250)}.com".Validator(c => c.Email(_ => 100)).ValidateAsError("must not exceed 100 character(s) in length.");
    }
}