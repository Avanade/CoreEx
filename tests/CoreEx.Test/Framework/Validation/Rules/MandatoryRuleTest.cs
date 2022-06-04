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
            var v1 = await "XXX".Validate().Mandatory().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await ((string?)null).Validate().Mandatory().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await (string.Empty).Validate().Mandatory().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Int32()
        {
            var v1 = await (123).Validate().Mandatory().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await (0).Validate().Mandatory().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            var v2 = await ((int?)123).Validate().Mandatory().ValidateAsync();
            Assert.IsFalse(v2.HasErrors);

            v2 = await ((int?)0).Validate().Mandatory().ValidateAsync();
            Assert.IsFalse(v2.HasErrors);

            v2 = await ((int?)null).Validate().Mandatory().ValidateAsync();
            Assert.IsTrue(v2.HasErrors);
            Assert.AreEqual(1, v2.Messages!.Count);
            Assert.AreEqual("Value is required.", v2.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v2.Messages[0].Type);
            Assert.AreEqual("value", v2.Messages[0].Property);
        }

        public class Foo
        {
            public string? Bar { get; set; }
        }

        [Test]
        public async Task Validate_Entity()
        {
            Foo? foo = new Foo();
            var v1 = await foo.Validate().Mandatory().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            foo = null;
            v1 = await foo.Validate().Mandatory().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}
