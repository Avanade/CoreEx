using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class MustRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_Value()
        {
            var v1 = await 123.Validate("value").Must(x => true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
            
            v1 = await 123.Validate("value").Must(x => false).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 123.Validate("value").Must(() => true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 123.Validate("value").Must(() => false).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}
