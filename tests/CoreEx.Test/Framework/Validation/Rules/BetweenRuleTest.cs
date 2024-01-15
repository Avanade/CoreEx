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
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 5.Validate("value").Between(1, 10).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").Between(2, 10).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between 2 and 10."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 10.Validate("value").Between(1, 10).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 11.Validate("value").Between(1, 10, "One", "Ten").ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between One and Ten."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 2.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 5.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between 1 and 10 (exclusive)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 9.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 10.Validate("value").Between(1, 10, "One", "Ten", exclusiveBetween: true).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between One and Ten (exclusive)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Nullable()
        {
            int? v = null;
            var v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between 1 and 10."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v = 1;
            v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v = 5;
            v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v = 1;
            v1 = await v.Validate("value").Between(2, 10).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between 2 and 10."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v = 10;
            v1 = await v.Validate("value").Between(1, 10).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v = 11;
            v1 = await v.Validate("value").Between(1, 10, "One", "Ten").ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between One and Ten."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v = 2;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v = 5;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v = 1;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between 1 and 10 (exclusive)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v = 9;
            v1 = await v.Validate("value").Between(1, 10, exclusiveBetween: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v = 10;
            v1 = await v.Validate("value").Between(1, 10, "One", "Ten", exclusiveBetween: true).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be between One and Ten (exclusive)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}