using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class BetweenRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await 1.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 5.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate("value").Between(2, 10).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between 2 and 10.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 10.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 11.Validate("value").Between(1, 10, "One", "Ten").ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between One and Ten.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 2.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 5.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between 1 and 10 (exclusive).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 9.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 10.Validate("value").Between(1, 10, "One", "Ten", exclusiveBetween: true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between One and Ten (exclusive).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Nullable()
        {
            int? v = null;
            var v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between 1 and 10.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v = 1;
            v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v = 5;
            v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v = 1;
            v1 = await v.Validate("value").Between(2, 10).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between 2 and 10.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v = 10;
            v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v = 11;
            v1 = await v.Validate("value").Between(1, 10, "One", "Ten").ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between One and Ten.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v = 2;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v = 5;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v = 1;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between 1 and 10 (exclusive).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v = 9;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v = 10;
            v1 = await v.Validate("value").Between(1, 10, "One", "Ten", exclusiveBetween: true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be between One and Ten (exclusive).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}
