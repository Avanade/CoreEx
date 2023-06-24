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
            Assert.IsFalse(v1.HasErrors);

            v1 = await (-123f).Validate("value").Numeric().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not be negative.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await (-123f).Validate("value").Numeric(true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var v2 = await (123d).Validate("value").Numeric().ValidateAsync();
            Assert.IsFalse(v2.HasErrors);

            v2 = await (-123d).Validate("value").Numeric().ValidateAsync();
            Assert.IsTrue(v2.HasErrors);
            Assert.AreEqual(1, v2.Messages!.Count);
            Assert.AreEqual("Value must not be negative.", v2.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v2.Messages[0].Type);
            Assert.AreEqual("value", v2.Messages[0].Property);

            v2 = await (-123d).Validate("value").Numeric(true).ValidateAsync();
            Assert.IsFalse(v2.HasErrors);
        }
    }
}
