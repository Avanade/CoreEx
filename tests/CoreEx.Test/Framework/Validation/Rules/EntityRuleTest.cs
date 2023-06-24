using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;
using static CoreEx.Test.Framework.Validation.ValidatorTest;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class EntityRuleTest
    {
        private static readonly IValidatorEx _tiv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());
        private static readonly IValidatorEx _tev = Validator.Create<TestEntity>().HasProperty(x => x.Item, p => p.Entity(_tiv));

        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var te = new TestEntity { Item = new TestItem() };
            var v1 = await te.Validate("value").Entity(_tev).ValidateAsync();

            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Identifier is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value.Item.Id", v1.Messages[0].Property);
        }
    }
}