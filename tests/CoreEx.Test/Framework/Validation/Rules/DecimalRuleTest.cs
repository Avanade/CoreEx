using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using CoreEx.Validation.Rules;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class DecimalRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_AllowNegatives()
        {
            var v1 = await (123).Validate("value").Numeric().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (-123).Validate("value").Numeric().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not be negative."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await (-123).Validate("value").Numeric(true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var v2 = await (123m).Validate("value").Numeric().ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await (-123m).Validate("value").Numeric().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v2.HasErrors, Is.True);
                Assert.That(v2.Messages!, Has.Count.EqualTo(1));
                Assert.That(v2.Messages![0].Text, Is.EqualTo("Value must not be negative."));
                Assert.That(v2.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v2.Messages[0].Property, Is.EqualTo("value"));
            });

            v2 = await (-123m).Validate("value").Numeric(true).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_MaxDigits()
        {
            var v1 = await (123).Validate("value").Numeric(maxDigits: 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (12345).Validate("value").Numeric(maxDigits: 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (123456).Validate("value").Numeric(maxDigits: 5).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not exceed 5 digits in total."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            var v2 = await (12.34m).Validate("value").Numeric(maxDigits: 5).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await (12.345m).Validate("value").Numeric(maxDigits: 5).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await (1.23456m).Validate("value").Numeric(maxDigits: 5).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v2.HasErrors, Is.True);
                Assert.That(v2.Messages!, Has.Count.EqualTo(1));
                Assert.That(v2.Messages![0].Text, Is.EqualTo("Value must not exceed 5 digits in total."));
                Assert.That(v2.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v2.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_DecimalPlaces()
        {
            var v1 = await (12.3m).Validate("value").Numeric(decimalPlaces: 2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (123.400m).Validate("value").Numeric(decimalPlaces: 2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (0.123m).Validate("value").Numeric(decimalPlaces: 2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value exceeds the maximum specified number of decimal places (2)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_MaxDigits_And_DecimalPlaces()
        {
            var v1 = await (12.3m).Validate("value").Numeric(maxDigits: 5, decimalPlaces: 2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (123.400m).Validate("value").Numeric(maxDigits: 5, decimalPlaces: 2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (0.123m).Validate("value").Numeric(maxDigits: 5, decimalPlaces: 2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value exceeds the maximum specified number of decimal places (2)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await (1234.0m).Validate("value").Numeric(maxDigits: 5, decimalPlaces: 2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not exceed 5 digits in total."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public void CalcIntegralLength()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(0m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(0.0000001m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(0.9999999m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(1.0000001m), Is.EqualTo(1));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(9.9999999m), Is.EqualTo(1));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(10.0000001m), Is.EqualTo(2));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(99.9999999m), Is.EqualTo(2));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(decimal.MaxValue), Is.EqualTo(29));

                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(-0.0000001m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(-0.9999999m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(-1.0000001m), Is.EqualTo(1));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(-9.9999999m), Is.EqualTo(1));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(-10.0000001m), Is.EqualTo(2));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(-99.9999999m), Is.EqualTo(2));
                Assert.That(DecimalRuleHelper.CalcIntegerPartLength(decimal.MinValue), Is.EqualTo(29));
            });
        }

        [Test]
        public void CalcDecimalPlaces()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(0m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(0.0000001m), Is.EqualTo(7));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(0.0001000m), Is.EqualTo(4));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(1.0000001m), Is.EqualTo(7));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(450.678m), Is.EqualTo(3));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(1500m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(decimal.MaxValue), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(long.MaxValue + 1.0001m), Is.EqualTo(4));
            });

            Assert.Multiple(() =>
            {
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(0m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(-0.0000001m), Is.EqualTo(7));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(-0.0001000m), Is.EqualTo(4));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(-1.0000001m), Is.EqualTo(7));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(-450.678m), Is.EqualTo(3));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(-1500m), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(decimal.MinValue), Is.EqualTo(0));
                Assert.That(DecimalRuleHelper.CalcFractionalPartLength(long.MinValue - 1.0001m), Is.EqualTo(4));
            });
        }

        [Test]
        public void CheckMaxDigits()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DecimalRuleHelper.CheckMaxDigits(0m, 5), Is.True);
                Assert.That(DecimalRuleHelper.CheckMaxDigits(12345m, 5), Is.True);
                Assert.That(DecimalRuleHelper.CheckMaxDigits(123.45m, 5), Is.True);
                Assert.That(DecimalRuleHelper.CheckMaxDigits(1.2345m, 5), Is.True);

                Assert.That(DecimalRuleHelper.CheckMaxDigits(123456m, 5), Is.False);
                Assert.That(DecimalRuleHelper.CheckMaxDigits(123.456m, 5), Is.False);
                Assert.That(DecimalRuleHelper.CheckMaxDigits(1.23456m, 5), Is.False);
            });
        }

        [Test]
        public void CheckDecimalPlaces()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DecimalRuleHelper.CheckDecimalPlaces(0m, 2), Is.True);
                Assert.That(DecimalRuleHelper.CheckDecimalPlaces(1.1m, 2), Is.True);
                Assert.That(DecimalRuleHelper.CheckDecimalPlaces(1.12m, 2), Is.True);
                Assert.That(DecimalRuleHelper.CheckDecimalPlaces(1.123m, 2), Is.False);
                Assert.That(DecimalRuleHelper.CheckDecimalPlaces(1.1234m, 2), Is.False);
            });
        }
    }
}
