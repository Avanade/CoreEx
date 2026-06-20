namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class NullNoneEmptyRuleTests
{
    private const string _errorContains = " must not be specified.";

    [Test]
    public void Null()
    {
        ((object?)null!).Validator(c => c.Null()).ValidateAsSuccess();
        ((object)null!).Validator(c => c.Null()).ValidateAsSuccess();
        ((string?)null).Validator(c => c.Null()).ValidateAsSuccess();
        ((string)null!).Validator(c => c.Null()).ValidateAsSuccess();
        ((int?)null).Validator(c => c.Null()).ValidateAsSuccess();

        new object().Validator(c => c.Null()).ValidateAsError(_errorContains);
        ((string)"XXX").Validator(c => c.Null()).ValidateAsError(_errorContains);
        ((string?)"XXX").Validator(c => c.Null()).ValidateAsError(_errorContains);
        ((int?)0).Validator(c => c.Null()).ValidateAsError(_errorContains);
    }

    [Test]
    public void None()
    {
        new object().Validator(c => c.None()).ValidateAsError(_errorContains);
        1.Validator(c => c.None()).ValidateAsError(_errorContains);
        ((int?)1).Validator(c => c.None()).ValidateAsError(_errorContains);
        string.Empty.Validator(c => c.None()).ValidateAsError(_errorContains);
        " ".Validator(c => c.None()).ValidateAsError(_errorContains);
        Array.Empty<int>().Validator(c => c.None()).ValidateAsError(_errorContains);
        new int[] { 1, 2 }.Validator(c => c.None()).ValidateAsError(_errorContains);
        Enumerable.Empty<int>().Validator(c => c.None()).ValidateAsError(_errorContains);
        Enumerable.Range(1, 2).Validator(c => c.None()).ValidateAsError(_errorContains);

        ((string?)null).Validator(c => c.None()).ValidateAsSuccess();
        0.Validator(c => c.None()).ValidateAsSuccess();
        ((int?)0).Validator(c => c.None()).ValidateAsSuccess();
        ((int[])null!).Validator(c => c.None()).ValidateAsSuccess();
        ((IEnumerable<int>)null!).Validator(c => c.None()).ValidateAsSuccess();
    }

    [Test]
    public void Empty()
    {
        new object().Validator(c => c.Empty()).ValidateAsError(_errorContains);
        "XXX".Validator(c => c.Empty()).ValidateAsError(_errorContains);
        ((string?)"XXX").Validator(c => c.Empty()).ValidateAsError(_errorContains);
        new int[] { 1, 2 }.Validator(c => c.Empty()).ValidateAsError(_errorContains);
        Enumerable.Range(1, 2).Validator(c => c.Empty()).ValidateAsError(_errorContains);

        ((string?)null).Validator(c => c.Empty()).ValidateAsSuccess();
        string.Empty.Validator(c => c.Empty()).ValidateAsSuccess();
        " ".Validator(c => c.Empty()).ValidateAsSuccess();
        ((int[])null!).Validator(c => c.Empty()).ValidateAsSuccess();
        Array.Empty<int>().Validator(c => c.Empty()).ValidateAsSuccess();
        ((IEnumerable<int>)null!).Validator(c => c.Empty()).ValidateAsSuccess();
        Enumerable.Empty<int>().Validator(c => c.Empty()).ValidateAsSuccess();
    }
}