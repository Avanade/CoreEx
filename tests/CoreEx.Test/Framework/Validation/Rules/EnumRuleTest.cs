using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class EnumRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await ((AbcOption)1).Validate("value").Configure(c => c.Enum()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((AbcOption)88).Validate("value").Configure(c => c.Enum()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_NonNullable()
        {
            var ac = new AbcClass();
            var v = new Validator<AbcClass>().HasProperty(x => x.Option, p => p.Enum());

            var v1 = await v.ValidateAsync(ac);
            Assert.That(v1.HasErrors, Is.False);

            ac.Option = AbcOption.B;
            v1 = await v.ValidateAsync(ac);
            Assert.That(v1.HasErrors, Is.False);

            ac.Option = ((AbcOption)404);
            v1 = await v.ValidateAsync(ac);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Option is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Option"));
            });
        }

        public class AbcClass
        {
            public AbcOption Option { get; set; }
        }

        [Test]
        public async Task Validate_Nullable()
        {
            var ac = new AbcClassN();
            var v = new Validator<AbcClassN>().HasProperty(x => x.Option, p => p.Enum());

            var v1 = await v.ValidateAsync(ac);
            Assert.That(v1.HasErrors, Is.False);

            ac.Option = AbcOption.B;
            v1 = await v.ValidateAsync(ac);
            Assert.That(v1.HasErrors, Is.False);

            ac.Option = ((AbcOption)404);
            v1 = await v.ValidateAsync(ac);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Option is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Option"));
            });
        }

        public class AbcClassN
        {
            public AbcOption? Option { get; set; }
        }
    }

    public enum AbcOption
    {
        A,
        B,
        C
    }
}