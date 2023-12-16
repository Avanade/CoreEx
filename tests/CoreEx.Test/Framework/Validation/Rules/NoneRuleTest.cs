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
            var v1 = await "XXX".Validate("value").None().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not be specified.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await ((string?)null).Validate("value").None().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await (string.Empty).Validate("value").None().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
        }

        [Test]
        public async Task Validate_Int32()
        {
            var v1 = await (123).Validate("value").None().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not be specified.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await (0).Validate("value").None().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var v2 = await ((int?)123).Validate("value").None().ValidateAsync();
            Assert.IsTrue(v2.HasErrors);

            v2 = await ((int?)0).Validate("value").None().ValidateAsync();
            Assert.IsTrue(v2.HasErrors);

            v2 = await ((int?)null).Validate("value").None().ValidateAsync();
            Assert.IsFalse(v2.HasErrors);
        }

        public class Foo
        {
            public string? Bar { get; set; }
        }

        [Test]
        public async Task Validate_Entity()
        {
            Foo? foo = new Foo();
            var v1 = await foo.Validate("value").None().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not be specified.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            foo = null;
            v1 = await foo.Validate("value").None().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }
    }
}
