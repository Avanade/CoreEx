namespace CoreEx.Validation.Test.Unit;

[TestFixture]
public class CommonValidatorTests
{
    [Test]
    public void Create()
    {
        var cv = Validator.CreateCommon<string>(c => c.Mandatory().MaximumLength(20));
        "abc".Validator(c => c.Common(cv)).ValidateAsSuccess();
        ((string?)null).Validator(c => c.Common(cv)).ValidateAsError(" is required.");
        new string('x', 21).Validator(c => c.Common(cv)).ValidateAsError("must not exceed 20 character(s) in length.");

        var cv2 = Validator.CreateCommon<int>(c => c.Mandatory().Between(0, 20));
        10.Validator(c => c.Common(cv2)).ValidateAsSuccess();
        30.Validator(c => c.Common(cv2)).ValidateAsError("must be between '0' and '20'.");
    }
}