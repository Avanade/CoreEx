using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class NumericRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_AllowNegatives()
        {
            var v1 = await (123f).Validate("value").Numeric().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (-123f).Validate("value").Numeric().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not be negative."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await (-123f).Validate("value").Numeric(true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var v2 = await (123d).Validate("value").Numeric().ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await (-123d).Validate("value").Numeric().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v2.HasErrors, Is.True);
                Assert.That(v2.Messages!, Has.Count.EqualTo(1));
                Assert.That(v2.Messages![0].Text, Is.EqualTo("Value must not be negative."));
                Assert.That(v2.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v2.Messages[0].Property, Is.EqualTo("value"));
            });

            v2 = await (-123d).Validate("value").Numeric(true).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);
        }
    }
}
