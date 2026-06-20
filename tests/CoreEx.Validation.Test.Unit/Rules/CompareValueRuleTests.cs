using CoreEx.Results;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class CompareValueRuleTests
{
    [Test]
    public void Compare_Int32()
    {
        ((int?)null).Validator(c => c.Compare(CompareOperator.Equal, 1)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.Equal, 1)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.NotEqual, 2)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.GreaterThan, 0)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, 1)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.LessThan, 2)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, 1)).ValidateAsSuccess();

        1.Validator(c => c.Equal(1)).ValidateAsSuccess();
        1.Validator(c => c.NotEqual(2)).ValidateAsSuccess();
        1.Validator(c => c.GreaterThan(0)).ValidateAsSuccess();
        1.Validator(c => c.GreaterThanOrEqualTo(1)).ValidateAsSuccess();
        1.Validator(c => c.LessThan(2)).ValidateAsSuccess();
        1.Validator(c => c.LessThanOrEqualTo(1)).ValidateAsSuccess();

        1.Validator(c => c.Compare(CompareOperator.Equal, 2)).ValidateAsError(" must be equal to '2'.");
        1.Validator(c => c.Compare(CompareOperator.NotEqual, 1)).ValidateAsError(" must not be equal to '1'.");
        1.Validator(c => c.Compare(CompareOperator.GreaterThan, 1)).ValidateAsError(" must be greater than '1'.");
        1.Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, 2)).ValidateAsError(" must be greater than or equal to '2'.");
        1.Validator(c => c.Compare(CompareOperator.LessThan, 1)).ValidateAsError(" must be less than '1'.");
        1.Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, 0)).ValidateAsError(" must be less than or equal to '0'.");

        1.Validator(c => c.Equal(2)).ValidateAsError(" must be equal to '2'.");
        1.Validator(c => c.NotEqual(1)).ValidateAsError(" must not be equal to '1'.");
        1.Validator(c => c.GreaterThan(1)).ValidateAsError(" must be greater than '1'.");
        1.Validator(c => c.GreaterThanOrEqualTo(2)).ValidateAsError(" must be greater than or equal to '2'.");
        1.Validator(c => c.LessThan(1)).ValidateAsError(" must be less than '1'.");
        1.Validator(c => c.LessThanOrEqualTo(0)).ValidateAsError(" must be less than or equal to '0'.");
    }

    [Test]
    public void Compare_Nullable_Int32()
    {
        ((int?)1).Validator(c => c.Compare(CompareOperator.Equal, 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.NotEqual, 2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThan, 0)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThan, 2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, 1)).ValidateAsSuccess();

        ((int?)1).Validator(c => c.Equal(1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.NotEqual(2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.GreaterThan(0)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.GreaterThanOrEqualTo(1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.LessThan(2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.LessThanOrEqualTo(1)).ValidateAsSuccess();

        ((int?)1).Validator(c => c.Compare(CompareOperator.Equal, 2)).ValidateAsError(" must be equal to '2'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.NotEqual, 1)).ValidateAsError(" must not be equal to '1'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThan, 1)).ValidateAsError(" must be greater than '1'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, 2)).ValidateAsError(" must be greater than or equal to '2'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThan, 1)).ValidateAsError(" must be less than '1'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, 0)).ValidateAsError(" must be less than or equal to '0'.");

        ((int?)1).Validator(c => c.Equal(2)).ValidateAsError(" must be equal to '2'.");
        ((int?)1).Validator(c => c.NotEqual(1)).ValidateAsError(" must not be equal to '1'.");
        ((int?)1).Validator(c => c.GreaterThan(1)).ValidateAsError(" must be greater than '1'.");
        ((int?)1).Validator(c => c.GreaterThanOrEqualTo(2)).ValidateAsError(" must be greater than or equal to '2'.");
        ((int?)1).Validator(c => c.LessThan(1)).ValidateAsError(" must be less than '1'.");
        ((int?)1).Validator(c => c.LessThanOrEqualTo(0)).ValidateAsError(" must be less than or equal to '0'.");
    }

    [Test]
    public void Compare_String()
    {
        ((string?)null).Validator(c => c.Compare(CompareOperator.Equal, "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.Equal, "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.Equal, "A", comparer: StringComparer.OrdinalIgnoreCase)).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.NotEqual, "b")).ValidateAsSuccess();
        "b".Validator(c => c.Compare(CompareOperator.GreaterThan, "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.LessThan, "b")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, "a")).ValidateAsSuccess();

        "a".Validator(c => c.Equal("a")).ValidateAsSuccess();
        "a".Validator(c => c.Equal("A", comparer: StringComparer.OrdinalIgnoreCase)).ValidateAsSuccess();
        "a".Validator(c => c.NotEqual("b")).ValidateAsSuccess();
        "b".Validator(c => c.GreaterThan("a")).ValidateAsSuccess();
        "a".Validator(c => c.GreaterThanOrEqualTo("a")).ValidateAsSuccess();
        "a".Validator(c => c.LessThan("b")).ValidateAsSuccess();
        "a".Validator(c => c.LessThanOrEqualTo("a")).ValidateAsSuccess();

        "a".Validator(c => c.Compare(CompareOperator.Equal, "b")).ValidateAsError(" must be equal to 'b'.");
        "a".Validator(c => c.Compare(CompareOperator.NotEqual, "a")).ValidateAsError(" must not be equal to 'a'.");
        "a".Validator(c => c.Compare(CompareOperator.GreaterThan, "a")).ValidateAsError(" must be greater than 'a'.");
        "a".Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, "b")).ValidateAsError(" must be greater than or equal to 'b'.");
        "a".Validator(c => c.Compare(CompareOperator.LessThan, "a")).ValidateAsError(" must be less than 'a'.");
        "b".Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, "a", comparer: StringComparer.OrdinalIgnoreCase)).ValidateAsError(" must be less than or equal to 'a'.");

        "a".Validator(c => c.Equal("b")).ValidateAsError(" must be equal to 'b'.");
        "a".Validator(c => c.NotEqual("a")).ValidateAsError(" must not be equal to 'a'.");
        "a".Validator(c => c.GreaterThan("a")).ValidateAsError(" must be greater than 'a'.");
        "a".Validator(c => c.GreaterThanOrEqualTo("b")).ValidateAsError(" must be greater than or equal to 'b'.");
        "a".Validator(c => c.LessThan("a")).ValidateAsError(" must be less than 'a'.");
        "b".Validator(c => c.LessThanOrEqualTo("a", comparer: StringComparer.OrdinalIgnoreCase)).ValidateAsError(" must be less than or equal to 'a'.");
    }

    [Test]
    public void Compare_Int32_Func()
    {
        ((int?)null).Validator(c => c.Compare(CompareOperator.Equal, _ => 1)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.Equal, _ => 1)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.NotEqual, _ => 2)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.GreaterThan, _ => 0)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, _ => 1)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.LessThan, _ => 2)).ValidateAsSuccess();
        1.Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => 1)).ValidateAsSuccess();

        1.Validator(c => c.Equal(_ => 1)).ValidateAsSuccess();
        1.Validator(c => c.NotEqual(_ => 2)).ValidateAsSuccess();
        1.Validator(c => c.GreaterThan(_ => 0)).ValidateAsSuccess();
        1.Validator(c => c.GreaterThanOrEqualTo(_ => 1)).ValidateAsSuccess();
        1.Validator(c => c.LessThan(_ => 2)).ValidateAsSuccess();
        1.Validator(c => c.LessThanOrEqualTo(_ => 1)).ValidateAsSuccess();

        1.Validator(c => c.Compare(CompareOperator.Equal, _ => 2)).ValidateAsError(" must be equal to '2'.");
        1.Validator(c => c.Compare(CompareOperator.NotEqual, _ => 1)).ValidateAsError(" must not be equal to '1'.");
        1.Validator(c => c.Compare(CompareOperator.GreaterThan, _ => 1)).ValidateAsError(" must be greater than '1'.");
        1.Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, _ => 2)).ValidateAsError(" must be greater than or equal to '2'.");
        1.Validator(c => c.Compare(CompareOperator.LessThan, _ => 1)).ValidateAsError(" must be less than '1'.");
        1.Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => 0)).ValidateAsError(" must be less than or equal to '0'.");

        1.Validator(c => c.Equal(_ => 2)).ValidateAsError(" must be equal to '2'.");
        1.Validator(c => c.NotEqual(_ => 1)).ValidateAsError(" must not be equal to '1'.");
        1.Validator(c => c.GreaterThan(_ => 1)).ValidateAsError(" must be greater than '1'.");
        1.Validator(c => c.GreaterThanOrEqualTo(_ => 2)).ValidateAsError(" must be greater than or equal to '2'.");
        1.Validator(c => c.LessThan(_ => 1)).ValidateAsError(" must be less than '1'.");
        1.Validator(c => c.LessThanOrEqualTo(_ => 0)).ValidateAsError(" must be less than or equal to '0'.");
    }

    [Test]
    public void Compare_Nullable_Int32_Func()
    {
        ((int?)null).Validator(c => c.Compare(CompareOperator.Equal, _ => 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.Equal, _ => 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.NotEqual, _ => 2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThan, _ => 0)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, _ => 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThan, _ => 2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => 1)).ValidateAsSuccess();

        ((int?)1).Validator(c => c.Equal(_ => 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.NotEqual(_ => 2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.GreaterThan(_ => 0)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.GreaterThanOrEqualTo(_ => 1)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.LessThan(_ => 2)).ValidateAsSuccess();
        ((int?)1).Validator(c => c.LessThanOrEqualTo(_ => 1)).ValidateAsSuccess();

        ((int?)1).Validator(c => c.Compare(CompareOperator.Equal, _ => 2)).ValidateAsError(" must be equal to '2'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.NotEqual, _ => 1)).ValidateAsError(" must not be equal to '1'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThan, _ => 1)).ValidateAsError(" must be greater than '1'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, _ => 2)).ValidateAsError(" must be greater than or equal to '2'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThan, _ => 1)).ValidateAsError(" must be less than '1'.");
        ((int?)1).Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => 0)).ValidateAsError(" must be less than or equal to '0'.");

        ((int?)1).Validator(c => c.Equal(_ => 2)).ValidateAsError(" must be equal to '2'.");
        ((int?)1).Validator(c => c.NotEqual(_ => 1)).ValidateAsError(" must not be equal to '1'.");
        ((int?)1).Validator(c => c.GreaterThan(_ => 1)).ValidateAsError(" must be greater than '1'.");
        ((int?)1).Validator(c => c.GreaterThanOrEqualTo(_ => 2)).ValidateAsError(" must be greater than or equal to '2'.");
        ((int?)1).Validator(c => c.LessThan(_ => 1)).ValidateAsError(" must be less than '1'.");
        ((int?)1).Validator(c => c.LessThanOrEqualTo(_ => 0)).ValidateAsError(" must be less than or equal to '0'.");
    }

    [Test]
    public void Compare_String_Func()
    {
        ((string?)null).Validator(c => c.Compare(CompareOperator.Equal, _ => "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.Equal, _ => "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.Equal, _ => "A", comparer: StringComparer.OrdinalIgnoreCase)).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.NotEqual, _ => "b")).ValidateAsSuccess();
        "b".Validator(c => c.Compare(CompareOperator.GreaterThan, _ => "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, _ => "a")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.LessThan, _ => "b")).ValidateAsSuccess();
        "a".Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => "a")).ValidateAsSuccess();

        "a".Validator(c => c.Compare(CompareOperator.Equal, _ => "b")).ValidateAsError(" must be equal to 'b'.");
        "a".Validator(c => c.Compare(CompareOperator.NotEqual, _ => "a")).ValidateAsError(" must not be equal to 'a'.");
        "a".Validator(c => c.Compare(CompareOperator.GreaterThan, _ => "a")).ValidateAsError(" must be greater than 'a'.");
        "a".Validator(c => c.Compare(CompareOperator.GreaterThanOrEqualTo, _ => "b")).ValidateAsError(" must be greater than or equal to 'b'.");
        "a".Validator(c => c.Compare(CompareOperator.LessThan, _ => "a")).ValidateAsError(" must be less than 'a'.");
        "b".Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => "a", _ => "Aye", StringComparer.OrdinalIgnoreCase)).ValidateAsError(" must be less than or equal to Aye.");

        "b".Validator(c => c.Compare(CompareOperator.LessThanOrEqualTo, _ => "a", _ => "Aye", StringComparer.OrdinalIgnoreCase).WithMessage("Oh no!")).ValidateAsError("Oh no!");
    }
}