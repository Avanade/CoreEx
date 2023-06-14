using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class CompareValueRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await 1.Validate("value").CompareValue(CompareOperator.Equal, 1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate("value").CompareValue(CompareOperator.Equal, 2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be equal to 2.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 1.Validate("value").CompareValue(CompareOperator.GreaterThan, 0).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate("value").CompareValue(CompareOperator.GreaterThan, 2, "Two").ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be greater than Two.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Nullable()
        {
            int? v = 1;
            var v1 = await v.Validate("value").CompareValue(CompareOperator.Equal, 1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await v.Validate("value").CompareValue(CompareOperator.Equal, 2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be equal to 2.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}