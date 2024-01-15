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
            Assert.That(v1.HasErrors, Is.False);
            
            v1 = await 123.Validate("value").Must(x => false).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").Must(() => true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 123.Validate("value").Must(() => false).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}
