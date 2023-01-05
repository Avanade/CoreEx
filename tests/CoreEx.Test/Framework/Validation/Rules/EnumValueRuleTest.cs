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
            var v1 = await "A".Validate().Enum().As<AbcOption>().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "a".Validate().Enum().As<AbcOption>().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await "a".Validate().Enum().As<AbcOption>(true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "X".Validate().Enum().As<AbcOption>().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await ((string?)null).Validate().Enum().As<AbcOption>().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task ValidateAndOverride()
        {
            var v1 = await "someoptionwithcasing".Validate().Enum().As<CaseOption>(true, false).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual("someoptionwithcasing", v1.Value);

            var ec = new EnumClass { Option = "someoptionwithcasing" };
            var v2 = await new Validator<EnumClass>().HasProperty(x => x.Option, p => p.Enum().As<CaseOption>(true, false)).ValidateAsync(ec);
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual("someoptionwithcasing", ec.Option);

            v2 = await new Validator<EnumClass>().HasProperty(x => x.Option, p => p.Enum().As<CaseOption>(true, true)).ValidateAsync(ec);
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual("SomeOptionWithCasing", ec.Option);

            ec.Option = null;
            v2 = await new Validator<EnumClass>().HasProperty(x => x.Option, p => p.Enum().As<CaseOption>(true, true)).ValidateAsync(ec);
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual(null, ec.Option);
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