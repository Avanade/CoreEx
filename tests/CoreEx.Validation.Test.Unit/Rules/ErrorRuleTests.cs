namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class ErrorRuleTests
{
    [Test]
    public void Error()
    {
        0.Validator(c => c.Error("An error occurred.")).ValidateAsError("An error occurred.");
        0.Validator(c => c.Error("An error occurred.").WhenValue(v => v == 0)).ValidateAsError("An error occurred.");
        1.Validator(c => c.Error("An error occurred.").WhenValue(v => v == 0)).ValidateAsSuccess();
    }

    [Test]
    public void Duplicate()
    {
        0.Validator(c => c.Duplicate()).ValidateAsError("0 already exists and would result in a duplicate.");
        0.Validator(c => c.Duplicate().WhenValue(v => v == 0)).ValidateAsError("0 already exists and would result in a duplicate.");
        1.Validator(c => c.Duplicate().WhenValue(v => v == 0)).ValidateAsSuccess();
    }

    [Test]
    public void NotFound()
    {
        0.Validator(c => c.NotFound()).ValidateAsError("0 was not found.");
        0.Validator(c => c.NotFound().WhenValue(v => v == 0)).ValidateAsError("0 was not found.");
        1.Validator(c => c.NotFound().WhenValue(v => v == 0)).ValidateAsSuccess();
    }

    [Test]
    public void Invalid()
    {
        0.Validator(c => c.Invalid()).ValidateAsError("0 is invalid.");
        0.Validator(c => c.Invalid().WhenValue(v => v == 0)).ValidateAsError("0 is invalid.");
        1.Validator(c => c.Invalid().WhenValue(v => v == 0)).ValidateAsSuccess();
    }

    [Test]
    public void Immutable()
    {
        0.Validator(c => c.Immutable()).ValidateAsError("0 is not allowed to change; please reset value.");
        0.Validator(c => c.Immutable().WhenValue(v => v == 0)).ValidateAsError("0 is not allowed to change; please reset value.");
        1.Validator(c => c.Immutable().WhenValue(v => v == 0)).ValidateAsSuccess();
    }
}