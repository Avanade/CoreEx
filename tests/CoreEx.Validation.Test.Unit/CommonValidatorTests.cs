namespace CoreEx.Validation.Test.Unit;

[TestFixture]
public class CommonValidatorTests
{
    [Test]
    public void Create()
    {
        var cv = Validator.CreateCommon<string>(c => c.Mandatory().MaximumLength(20));
        var cv2 = Validator.CreateCommon<int>(c => c.Mandatory().Between(0, 20));
    }
}