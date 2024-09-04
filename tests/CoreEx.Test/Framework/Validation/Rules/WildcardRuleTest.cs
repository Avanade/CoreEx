using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;
using CoreEx.Wildcards;

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
            var v1 = await "xxxx".Validate("value").Configure(c => c.Wildcard()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "*xxxx".Validate("value").Configure(c => c.Wildcard()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "xxxx*".Validate("value").Configure(c => c.Wildcard()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "*xxxx*".Validate("value").Configure(c => c.Wildcard()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "x*x".Validate("value").Configure(c => c.Wildcard(wildcard: Wildcard.MultiAll)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "x?x".Validate("value").Configure(c => c.Wildcard(wildcard: Wildcard.MultiAll)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains invalid or non-supported wildcard selection."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}