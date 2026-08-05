using CoreEx.Validation;

namespace CoreEx.Test.Unit.Validation;

[TestFixture]
public class DecimalRuleHelperTests
{
    [Test]
    public void CalcIntegralPartLength_Zero_ReturnsZero()
        => DecimalRuleHelper.CalcIntegralPartLength(0m).Should().Be(0);

    [TestCase(1, 1)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(99, 2)]
    [TestCase(100, 3)]
    public void CalcIntegralPartLength_SmallValues(int value, int expectedLength)
        => DecimalRuleHelper.CalcIntegralPartLength(value).Should().Be(expectedLength);

    [Test]
    public void CalcIntegralPartLength_PowerOfTenBoundary_ValueJustBelow_IsNotOverCounted()
    {
        // 17 nines: casting to double rounds this up to exactly 1e17, which previously caused an off-by-one
        // overcount (18 instead of the correct 17) via the Log10-based estimate.
        DecimalRuleHelper.CalcIntegralPartLength(99999999999999999m).Should().Be(17);
    }

    [Test]
    public void CalcIntegralPartLength_ExactPowerOfTen_IsCorrect()
    {
        DecimalRuleHelper.CalcIntegralPartLength(10000000000000000m).Should().Be(17); // 1e16
        DecimalRuleHelper.CalcIntegralPartLength(100000000000000000m).Should().Be(18); // 1e17
    }

    [Test]
    public void CalcIntegralPartLength_LargeValue_AboveDoublePrecision_IsCorrect()
    {
        DecimalRuleHelper.CalcIntegralPartLength(1000000000000000000m).Should().Be(19); // 1e18
        DecimalRuleHelper.CalcIntegralPartLength(9999999999999999999999999999m).Should().Be(28);
    }

    [Test]
    public void CheckPrecisionAndScale_BoundaryValue_PreviouslyMiscountedAsOverPrecision_IsValid()
    {
        // With the Log10 overcount bug, 99999999999999999m (17 digits) was measured as 18 digits and would
        // incorrectly fail precision-17 validation.
        DecimalRuleHelper.CheckPrecisionAndScale(99999999999999999m, precision: 17, scale: 0).Should().BeTrue();
        DecimalRuleHelper.CheckPrecisionAndScale(999999999999999999m, precision: 17, scale: 0).Should().BeFalse(); // 18 digits, exceeds precision 17
    }
}
