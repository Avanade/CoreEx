using System.Collections;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class MandatoryRuleTests
{
    private const string _errorIsRequired = " is required.";

    [Test]
    public void Mandatory()
    {
        new object().Validator(c => c.Mandatory()).ValidateAsSuccess();
        "XXX".Validator(c => c.Mandatory()).ValidateAsSuccess();
        1.Validator(c => c.Mandatory()).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Mandatory()).ValidateAsSuccess();
        new int[] { 1, 2 }.Validator(c => c.Mandatory()).ValidateAsSuccess();
        Enumerable.Range(1, 2).Validator(c => c.Mandatory()).ValidateAsSuccess();

        ((int?)0).Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        ((object)null!).Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        ((string?)null).Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        "".Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        " ".Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        0.Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        ((int?)null).Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        Array.Empty<int>().Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        Enumerable.Empty<int>().Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
    }

    [Test]
    public void NotEmpty()
    {
        new object().Validator(c => c.NotEmpty()).ValidateAsSuccess();
        "XXX".Validator(c => c.NotEmpty()).ValidateAsSuccess();
        1.Validator(c => c.NotEmpty()).ValidateAsSuccess();
        ((int?)1).Validator(c => c.NotEmpty()).ValidateAsSuccess();
        new int[] { 1, 2 }.Validator(c => c.NotEmpty()).ValidateAsSuccess();
        Enumerable.Range(1, 2).Validator(c => c.NotEmpty()).ValidateAsSuccess();

        ((int?)0).Validator(c => c.Mandatory()).ValidateAsError(_errorIsRequired);
        ((object)null!).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        ((string?)null).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        "".Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        " ".Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        0.Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        ((int?)null).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        Array.Empty<int>().Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        Enumerable.Empty<int>().Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
    }

    [Test]
    public void NotNull()
    {
        new object().Validator(c => c.NotNull()).ValidateAsSuccess();
        "XXX".Validator(c => c.NotNull()).ValidateAsSuccess();
        "".Validator(c => c.NotNull()).ValidateAsSuccess();
        " ".Validator(c => c.NotNull()).ValidateAsSuccess();
        0.Validator(c => c.NotNull()).ValidateAsSuccess();
        1.Validator(c => c.NotNull()).ValidateAsSuccess();
        ((int?)0).Validator(c => c.NotNull()).ValidateAsSuccess();
        ((int?)1).Validator(c => c.NotNull()).ValidateAsSuccess();
        new int[] { 1, 2 }.Validator(c => c.NotNull()).ValidateAsSuccess();
        Enumerable.Range(1, 2).Validator(c => c.NotNull()).ValidateAsSuccess();
        Array.Empty<int>().Validator(c => c.NotNull()).ValidateAsSuccess();
        Enumerable.Empty<int>().Validator(c => c.NotNull()).ValidateAsSuccess();

        ((object)null!).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        ((string?)null).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        ((int?)null).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        ((ICollection)null!).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
        ((IEnumerable)null!).Validator(c => c.NotEmpty()).ValidateAsError(_errorIsRequired);
    }
}