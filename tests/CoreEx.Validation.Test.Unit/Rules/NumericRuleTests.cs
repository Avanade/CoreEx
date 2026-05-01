namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class NumericRuleTests
{
    [Test]
    public void Numeric()
    {
        0.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        123.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        (-123).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        int.MinValue.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        int.MaxValue.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        0L.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        123L.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        (-123L).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        long.MinValue.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        long.MaxValue.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        0.0M.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        123.45M.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        (-123.45M).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        decimal.MinValue.Validator(c => c.Numeric(true)).ValidateAsSuccess();
        decimal.MaxValue.Validator(c => c.Numeric(true)).ValidateAsSuccess();

        ((int?)0).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((int?)123).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((int?)-123).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((int?)int.MinValue).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((int?)int.MaxValue).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((long?)null).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((long?)0).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((long?)123).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((long?)-123).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((long?)long.MinValue).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((long?)long.MaxValue).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((decimal?)null).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((decimal?)0).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((decimal?)123.45M).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((decimal?)-123.45M).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((decimal?)decimal.MinValue).Validator(c => c.Numeric(true)).ValidateAsSuccess();
        ((decimal?)decimal.MaxValue).Validator(c => c.Numeric(true)).ValidateAsSuccess();

        0.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        123.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        (-123).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        int.MinValue.Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        int.MaxValue.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        0L.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        123L.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        (-123L).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        long.MinValue.Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        long.MaxValue.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        0.0M.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        123.45M.Validator(c => c.Numeric(false)).ValidateAsSuccess();
        (-123.45M).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        decimal.MinValue.Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        decimal.MaxValue.Validator(c => c.Numeric(false)).ValidateAsSuccess();

        ((int?)null).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((int?)0).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((int?)123).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((int?)-123).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        ((int?)int.MinValue).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        ((int?)int.MaxValue).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((long?)null).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((long?)0).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((long?)123).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((long?)-123).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        ((long?)long.MinValue).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        ((long?)long.MaxValue).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((decimal?)null).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((decimal?)0).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((decimal?)123.45M).Validator(c => c.Numeric(false)).ValidateAsSuccess();
        ((decimal?)-123.45M).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        ((decimal?)decimal.MinValue).Validator(c => c.Numeric(false)).ValidateAsError("must not be negative.");
        ((decimal?)decimal.MaxValue).Validator(c => c.Numeric(false)).ValidateAsSuccess();
    }

    [Test]
    public void Positive()
    {
        0.Validator(c => c.Positive()).ValidateAsSuccess();
        123.Validator(c => c.Positive()).ValidateAsSuccess();
        (-123).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        int.MinValue.Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        int.MaxValue.Validator(c => c.Positive()).ValidateAsSuccess();
        0L.Validator(c => c.Positive()).ValidateAsSuccess();
        123L.Validator(c => c.Positive()).ValidateAsSuccess();
        (-123L).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        long.MinValue.Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        long.MaxValue.Validator(c => c.Positive()).ValidateAsSuccess();
        0.0M.Validator(c => c.Positive()).ValidateAsSuccess();
        123.45M.Validator(c => c.Positive()).ValidateAsSuccess();
        (-123.45M).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        decimal.MinValue.Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        decimal.MaxValue.Validator(c => c.Positive()).ValidateAsSuccess();

        ((int?)null).Validator(c => c.Positive()).ValidateAsSuccess();
        ((int?)0).Validator(c => c.Positive()).ValidateAsSuccess();
        ((int?)123).Validator(c => c.Positive()).ValidateAsSuccess();
        ((int?)-123).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        ((int?)int.MinValue).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        ((int?)int.MaxValue).Validator(c => c.Positive()).ValidateAsSuccess();
        ((long?)null).Validator(c => c.Positive()).ValidateAsSuccess();
        ((long?)0).Validator(c => c.Positive()).ValidateAsSuccess();
        ((long?)123).Validator(c => c.Positive()).ValidateAsSuccess();
        ((long?)-123).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        ((long?)long.MinValue).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        ((long?)long.MaxValue).Validator(c => c.Positive()).ValidateAsSuccess();
        ((decimal?)null).Validator(c => c.Positive()).ValidateAsSuccess();
        ((decimal?)0).Validator(c => c.Positive()).ValidateAsSuccess();
        ((decimal?)123.45M).Validator(c => c.Positive()).ValidateAsSuccess();
        ((decimal?)-123.45M).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        ((decimal?)decimal.MinValue).Validator(c => c.Positive()).ValidateAsError("must not be negative.");
        ((decimal?)decimal.MaxValue).Validator(c => c.Positive()).ValidateAsSuccess();
    }
}
