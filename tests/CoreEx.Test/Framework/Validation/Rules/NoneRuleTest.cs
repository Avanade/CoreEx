using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class NoneRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_String()
        {
            var v1 = await "XXX".Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not be specified."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await ((string?)null).Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (string.Empty).Validate("value").Configure(c => c.Validate()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "  ".Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Int32()
        {
            var v1 = await (123).Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not be specified."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await (0).Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var v2 = await ((int?)123).Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v2.HasErrors, Is.True);

            v2 = await ((int?)0).Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v2.HasErrors, Is.True);

            v2 = await ((int?)null).Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);
        }

        public class Foo
        {
            public string? Bar { get; set; }
        }

        [Test]
        public async Task Validate_Entity()
        {
            Foo? foo = new();
            var v1 = await foo.Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not be specified."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            foo = null;
            v1 = await foo.Validate("value").Configure(c => c.None()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }
    }
}
