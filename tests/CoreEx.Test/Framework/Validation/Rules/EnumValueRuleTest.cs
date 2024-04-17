using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class EnumValueRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await "A".Validate("value").Configure(c => c.Enum().As<AbcOption>()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "a".Validate("value").Configure(c => c.Enum().As<AbcOption>()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await "a".Validate("value").Configure(c => c.Enum().As<AbcOption>(true)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "X".Validate("value").Configure(c => c.Enum().As<AbcOption>()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await ((string?)null).Validate("value").Configure(c => c.Enum().As<AbcOption>()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task ValidateAndOverride()
        {
            var v1 = await "someoptionwithcasing".Validate("value").Configure(c => c.Enum().As<CaseOption>(true, false)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(v1.Value, Is.EqualTo("someoptionwithcasing"));
            });

            var ec = new EnumClass { Option = "someoptionwithcasing" };
            var v2 = await new Validator<EnumClass>().HasProperty(x => x.Option, p => p.Enum().As<CaseOption>(true, false)).ValidateAsync(ec);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(ec.Option, Is.EqualTo("someoptionwithcasing"));
            });

            v2 = await new Validator<EnumClass>().HasProperty(x => x.Option, p => p.Enum().As<CaseOption>(true, true)).ValidateAsync(ec);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(ec.Option, Is.EqualTo("SomeOptionWithCasing"));
            });

            ec.Option = null;
            v2 = await new Validator<EnumClass>().HasProperty(x => x.Option, p => p.Enum().As<CaseOption>(true, true)).ValidateAsync(ec);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(ec.Option, Is.EqualTo(null));
            });
        }

        public class EnumClass
        { 
            public string? Option { get; set; }
        }
    }

    public enum CaseOption
    {
        SomeOptionWithCasing
    }
}