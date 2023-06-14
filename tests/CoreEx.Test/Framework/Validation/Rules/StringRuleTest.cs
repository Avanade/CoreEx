using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class StringRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_MinLength()
        {
            var v1 = await "Abc".Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "Ab".Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "A".Validate("value").String(2, 5).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must be at least 2 characters in length.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await string.Empty.Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await ((string?)null).Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_MaxLength()
        {
            var v1 = await "Abc".Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "Abcde".Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "Abcdef".Validate("value").String(2, 5).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not exceed 5 characters in length.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await string.Empty.Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await ((string?)null).Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_Regex()
        {
            var r = new Regex("[a-zA-Z]$");
            var v1 = await "Abc".Validate("value").String(r).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "123".Validate("value").String(r).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await string.Empty.Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await ((string?)null).Validate("value").String(2, 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }
    }
}
