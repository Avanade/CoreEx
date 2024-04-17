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
            var v1 = await td.Validate("value").Configure(c => c.Entity(v)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Property, Is.EqualTo("value.Text"));
            });

            td = new TestData { Text = "xxx", CountA = 88 };
            v1 = await td.Validate("value").Configure(c => c.Entity(v)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Property, Is.EqualTo("value.CountA"));
            });

            td = new TestData { Text = "xxx", CountA = 5 };
            v1 = await td.Validate("value").Configure(c => c.Entity(v)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }
    }
}