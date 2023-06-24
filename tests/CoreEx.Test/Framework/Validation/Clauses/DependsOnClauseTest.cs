using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Clauses
{
    [TestFixture]
    public class DependsOnClauseTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task DependsOn()
        {
            var v = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.CountA, p => p.Between(1, 10).DependsOn(x => x.Text));

            var td = new TestData { Text = null, CountA = 88 };
            var v1 = await td.Validate("value").Entity(v).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("value.Text", v1.Messages[0].Property);

            td = new TestData { Text = "xxx", CountA = 88 };
            v1 = await td.Validate("value").Entity(v).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("value.CountA", v1.Messages[0].Property);

            td = new TestData { Text = "xxx", CountA = 5 };
            v1 = await td.Validate("value").Entity(v).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }
    }
}