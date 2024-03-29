using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class MandatoryRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_String()
        {
            var v1 = await "XXX".Validate("value").Mandatory().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((string?)null).Validate("value").Mandatory().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await (string.Empty).Validate("value").Mandatory().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Int32()
        {
            var v1 = await (123).Validate("value").Mandatory().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (0).Validate("value").Mandatory().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            var v2 = await ((int?)123).Validate("value").Mandatory().ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await ((int?)0).Validate("value").Mandatory().ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await ((int?)null).Validate("value").Mandatory().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v2.HasErrors, Is.True);
                Assert.That(v2.Messages!, Has.Count.EqualTo(1));
                Assert.That(v2.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v2.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v2.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        public class Foo
        {
            public string? Bar { get; set; }
        }

        [Test]
        public async Task Validate_Entity()
        {
            Foo? foo = new();
            var v1 = await foo.Validate("value").Mandatory().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            foo = null;
            v1 = await foo.Validate("value").Mandatory().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_NotEmpty_String()
        {
            var v1 = await "XXX".Validate("value").NotEmpty().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((string?)null).Validate("value").NotEmpty().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await (string.Empty).Validate("value").NotEmpty().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_NotEmpty_Int32()
        {
            var v1 = await (123).Validate("value").NotEmpty().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await (0).Validate("value").NotEmpty().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            var v2 = await ((int?)123).Validate("value").NotEmpty().ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await ((int?)0).Validate("value").NotEmpty().ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);

            v2 = await ((int?)null).Validate("value").NotEmpty().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v2.HasErrors, Is.True);
                Assert.That(v2.Messages!, Has.Count.EqualTo(1));
                Assert.That(v2.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(v2.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v2.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}
