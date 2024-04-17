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
            var v1 = await te.Validate("entity", "EnTiTy").Configure(c => c.Entity(_tev)).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Identifier is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("entity.Item.Id"));
            });
        }
    }
}