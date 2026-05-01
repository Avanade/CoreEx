using CoreEx.Validation.Abstractions;

namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class DecimalRuleTests
{
    [Test]
    public void Decimal()
    {
        1.0m.Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        1.20m.Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        1.230m.Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        12.34m.Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        123.45m.Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        (-1.0m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        (-1.20m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        (-1.230m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        (-12.34m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        (-123.45m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();

        1234.56m.Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        12345.78m.Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        12345.789m.Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        (-1234.56m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        (-12345.78m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        (-12345.789m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum digits (5).");

        123.456m.Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        0.123m.Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        (-123.456m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        (-0.123m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        (-1.0m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void Decimal_Func()
    {
        1.0m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        1.20m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        1.230m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        12.34m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        123.45m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        (-1.0m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        (-1.20m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        (-1.230m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        (-12.34m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        (-123.45m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();

        1234.56m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        12345.78m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        12345.789m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        (-1234.56m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");
        (-12345.78m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");
        (-12345.789m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");

        123.456m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum decimal places (2).");
        0.123m.Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum decimal places (2).");
        (-123.456m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        (-0.123m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        (-1.0m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void Decimal_Nullable()
    {
        ((decimal?)1.0m).Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        ((decimal?)1.20m).Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        ((decimal?)1.230m).Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        ((decimal?)12.34m).Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        ((decimal?)123.45m).Validator(c => c.Decimal(5, 2, false)).ValidateAsSuccess();
        ((decimal?)-1.0m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        ((decimal?)-1.20m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        ((decimal?)-1.230m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        ((decimal?)-12.34m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();
        ((decimal?)-123.45m).Validator(c => c.Decimal(5, 2)).ValidateAsSuccess();

        ((decimal?)1234.56m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.78m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.789m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-1234.56m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.78m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.789m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum digits (5).");

        ((decimal?)123.456m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)0.123m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-123.456m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-0.123m).Validator(c => c.Decimal(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        ((decimal?)-1.0m).Validator(c => c.Decimal(5, 2, false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void Decimal_Nullable_Func()
    {
        ((decimal?)1.0m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)1.20m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)1.230m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)12.34m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)123.45m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)-1.0m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-1.20m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-1.230m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-12.34m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-123.45m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsSuccess();

        ((decimal?)1234.56m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.78m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.789m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-1234.56m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.78m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.789m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");

        ((decimal?)123.456m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)0.123m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-123.456m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-0.123m).Validator(c => c.Decimal(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        ((decimal?)-1.0m).Validator(c => c.Decimal(_ => 5, _ => 2, _ => false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void PrecisionScale()
    {
        1.0m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        1.20m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        1.230m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        12.34m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        123.45m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        (-1.0m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        (-1.20m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        (-1.230m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        (-12.34m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        (-123.45m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();

        1234.56m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        12345.78m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        12345.789m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        (-1234.56m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        (-12345.78m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        (-12345.789m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum digits (5).");

        123.456m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        0.123m.Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        (-123.456m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        (-0.123m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        (-1.0m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void PrecisionScale_Nullable()
    {
        ((decimal?)1.0m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        ((decimal?)1.20m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        ((decimal?)1.230m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        ((decimal?)12.34m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        ((decimal?)123.45m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsSuccess();
        ((decimal?)-1.0m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        ((decimal?)-1.20m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        ((decimal?)-1.230m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        ((decimal?)-12.34m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();
        ((decimal?)-123.45m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsSuccess();

        ((decimal?)1234.56m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.78m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.789m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-1234.56m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.78m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.789m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum digits (5).");

        ((decimal?)123.456m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)0.123m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-123.456m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-0.123m).Validator(c => c.PrecisionScale(5, 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        ((decimal?)-1.0m).Validator(c => c.PrecisionScale(5, 2, false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void PrecisionScale_Nullable_Func()
    {
        ((decimal?)1.0m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)1.20m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)1.230m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)12.34m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)123.45m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsSuccess();
        ((decimal?)-1.0m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-1.20m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-1.230m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-12.34m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsSuccess();
        ((decimal?)-123.45m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsSuccess();

        ((decimal?)1234.56m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.78m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)12345.789m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-1234.56m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.78m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");
        ((decimal?)-12345.789m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum digits (5).");

        ((decimal?)123.456m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)0.123m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-123.456m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum decimal places (2).");
        ((decimal?)-0.123m).Validator(c => c.PrecisionScale(_ => 5, _ => 2)).ValidateAsError("exceeds the maximum decimal places (2).");

        ((decimal?)-1.0m).Validator(c => c.PrecisionScale(_ => 5, _ => 2, _ => false)).ValidateAsError("must not be negative.");
    }

    [Test]
    public void CalcIntegralPartLength()
    {
        DecimalRuleHelper.CalcIntegralPartLength(0m).Should().Be(0);
        DecimalRuleHelper.CalcIntegralPartLength(0.0000001m).Should().Be(0);
        DecimalRuleHelper.CalcIntegralPartLength(0.9999999m).Should().Be(0);
        DecimalRuleHelper.CalcIntegralPartLength(1.0000001m).Should().Be(1);
        DecimalRuleHelper.CalcIntegralPartLength(9.9999999m).Should().Be(1);
        DecimalRuleHelper.CalcIntegralPartLength(10.0000001m).Should().Be(2);
        DecimalRuleHelper.CalcIntegralPartLength(99.9999999m).Should().Be(2);
        DecimalRuleHelper.CalcIntegralPartLength(decimal.MaxValue).Should().Be(29);

        DecimalRuleHelper.CalcIntegralPartLength(-0.0000001m).Should().Be(0);
        DecimalRuleHelper.CalcIntegralPartLength(-0.9999999m).Should().Be(0);
        DecimalRuleHelper.CalcIntegralPartLength(-1.0000001m).Should().Be(1);
        DecimalRuleHelper.CalcIntegralPartLength(-9.9999999m).Should().Be(1);
        DecimalRuleHelper.CalcIntegralPartLength(-10.0000001m).Should().Be(2);
        DecimalRuleHelper.CalcIntegralPartLength(-99.9999999m).Should().Be(2);
        DecimalRuleHelper.CalcIntegralPartLength(decimal.MinValue).Should().Be(29);
    }

    [Test]
    public void CalcFractionalPartLength()
    {
        DecimalRuleHelper.CalcFractionalPartLength(0m).Should().Be(0);
        DecimalRuleHelper.CalcFractionalPartLength(0.0000000m).Should().Be(0);
        DecimalRuleHelper.CalcFractionalPartLength(0.0001000m).Should().Be(4);
        DecimalRuleHelper.CalcFractionalPartLength(0.0000001m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(0.9999999m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(1.0000001m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(9.9999999m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(10.0000001m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(99.9999999m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(decimal.MaxValue).Should().Be(0);

        DecimalRuleHelper.CalcFractionalPartLength(-0.0000000m).Should().Be(0);
        DecimalRuleHelper.CalcFractionalPartLength(-0.0001000m).Should().Be(4);
        DecimalRuleHelper.CalcFractionalPartLength(-0.0000001m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(-0.9999999m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(-1.0000001m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(-9.9999999m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(-10.0000001m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(-99.9999999m).Should().Be(7);
        DecimalRuleHelper.CalcFractionalPartLength(decimal.MinValue).Should().Be(0);
    }
}