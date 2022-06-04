using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class WildcardRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task ValidateWildcard()
        {
            var v1 = await "xxxx".Validate().Wildcard().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "*xxxx".Validate().Wildcard().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "xxxx*".Validate().Wildcard().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "*xxxx*".Validate().Wildcard().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "x*x".Validate().Wildcard().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "x?x".Validate().Wildcard().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains invalid or non-supported wildcard selection.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}