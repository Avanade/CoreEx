using CoreEx.Results;

namespace CoreEx.Validation.Test.Unit.Clauses;

[TestFixture]
public class WhenClauseTests
{
    [Test]
    public void When()
    {
        0.Validator(c => c.Mandatory().WhenEntity(e => e.Value == 0)).ValidateAsError("is required.");
        0.Validator(c => c.Mandatory().WhenValue(v => v == 0)).ValidateAsError("is required.");
        0.Validator(c => c.Mandatory().When(true)).ValidateAsError("is required.");
        0.Validator(c => c.Mandatory().When(() => true)).ValidateAsError("is required.");
        0.Validator(c => c.Mandatory().When((c, _) => Task.FromResult(true))).ValidateAsError("is required.");
        1.Validator(c => c.Mandatory().WhenHasValue()).ValidateAsSuccess();

        1.Validator(c => c.Mandatory().WhenEntity(e => e.Value == 0)).ValidateAsSuccess();
        1.Validator(c => c.Mandatory().WhenValue(v => v == 0)).ValidateAsSuccess();
        1.Validator(c => c.Mandatory().When(false)).ValidateAsSuccess();
        1.Validator(c => c.Mandatory().When(() => false)).ValidateAsSuccess();
        1.Validator(c => c.Mandatory().When((c, _) => Task.FromResult(false))).ValidateAsSuccess();
        ((int?)null).Validator(c => c.Mandatory().WhenHasValue()).ValidateAsSuccess();
    }
}