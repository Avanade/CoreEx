using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class DuplicateRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_Value()
        {
            var v1 = await 123.Validate().Duplicate(x => false).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
            
            v1 = await 123.Validate().Duplicate(x => true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value already exists and would result in a duplicate.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);

            v1 = await 123.Validate().Duplicate(() => false).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 123.Validate().Duplicate(() => true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value already exists and would result in a duplicate.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);
        }
    }
}
