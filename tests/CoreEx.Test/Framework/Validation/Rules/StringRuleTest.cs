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
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "Ab".Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "A".Validate("value").String(2, 5).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be at least 2 characters in length."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await string.Empty.Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((string?)null).Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_MaxLength()
        {
            var v1 = await "Abc".Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "Abcde".Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "Abcdef".Validate("value").String(2, 5).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not exceed 5 characters in length."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await string.Empty.Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((string?)null).Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Regex()
        {
            var r = new Regex("[a-zA-Z]$");
            var v1 = await "Abc".Validate("value").String(r).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "123".Validate("value").String(r).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await string.Empty.Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((string?)null).Validate("value").String(2, 5).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_ExactLength()
        {
            var v1 = await "Abc".Validate("value").String(3, 3).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "A".Validate("value").String(3, 3).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be exactly 3 characters in length."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await "AAAA".Validate("value").Length(3).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be exactly 3 characters in length."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}
