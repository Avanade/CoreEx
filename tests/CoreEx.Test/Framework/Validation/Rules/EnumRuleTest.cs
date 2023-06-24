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
            var v1 = await ((AbcOption)1).Validate("value").Enum().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await ((AbcOption)88).Validate("value").Enum().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_NonNullable()
        {
            var ac = new AbcClass();
            var v = new Validator<AbcClass>().HasProperty(x => x.Option, p => p.Enum());

            var v1 = await v.ValidateAsync(ac);
            Assert.IsFalse(v1.HasErrors);

            ac.Option = AbcOption.B;
            v1 = await v.ValidateAsync(ac);
            Assert.IsFalse(v1.HasErrors);

            ac.Option = ((AbcOption)404);
            v1 = await v.ValidateAsync(ac);
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Option is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Option", v1.Messages[0].Property);
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
            Assert.IsFalse(v1.HasErrors);

            ac.Option = AbcOption.B;
            v1 = await v.ValidateAsync(ac);
            Assert.IsFalse(v1.HasErrors);

            ac.Option = ((AbcOption)404);
            v1 = await v.ValidateAsync(ac);
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Option is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Option", v1.Messages[0].Property);
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