using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Clauses
{
    [TestFixture]
    public class WhenClauseTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task When()
        {
            var v1 = await 1.Validate(c => c.Between(2, 10).When(true)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 1.Validate(c => c.Between(2, 10).When(false)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate(c => c.Between(2, 10).When(() => true)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 1.Validate(c => c.Between(2, 10).When(() => false)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate(c => c.Between(2, 10).When(x => x.Value == 1)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 1.Validate(c => c.Between(2, 10).When(x => x.Value != 1)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var v = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.CountA, p => p.Between(1, 10).When(x => x.Text == "xxx"));

            var td = new TestData { Text = "xxx", CountA = 88 };
            var v2 = await td.Validate().Configure(c => c.Entity(v)).ValidateAsync();
            Assert.That(v2.HasErrors, Is.True);

            td = new TestData { Text = "yyy", CountA = 88 };
            v2 = await td.Validate().Configure(c => c.Entity(v)).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);
        }

        [Test]
        public async Task WhenValue()
        {
            var v1 = await 1.Validate(c => c.Between(2, 10).WhenValue(v => v == 1)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 1.Validate(c => c.Between(2, 10).WhenValue(v => v == 2)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var v = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.CountA, p => p.Between(1, 10).WhenValue(v => v == 88));

            var td = new TestData { Text = "xxx", CountA = 88 };
            var v2 = await td.Validate().Configure(c => c.Entity(v)).ValidateAsync();
            Assert.That(v2.HasErrors, Is.True);

            td = new TestData { Text = "xxx", CountA = 99 };
            v2 = await td.Validate().Configure(c => c.Entity(v)).ValidateAsync();
            Assert.That(v2.HasErrors, Is.False);
        }

        [Test]
        public async Task WhenHasValue()
        {
            var v1 = await 1.Validate(c => c.Immutable().WhenHasValue()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 0.Validate(c => c.Immutable().WhenHasValue()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task WhenOperation()
        {
            ExecutionContext.Current.OperationType = OperationType.Update;

            var v1 = await 1.Validate(c => c.Immutable().WhenOperation(OperationType.Update)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 1.Validate(c => c.Immutable().WhenOperation(OperationType.Create)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task WhenNotOperation()
        {
            ExecutionContext.Current.OperationType = OperationType.Update;

            var v1 = await 1.Validate(c => c.Immutable().WhenNotOperation(OperationType.Create)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await 1.Validate(c => c.Immutable().WhenNotOperation(OperationType.Update)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }
    }
}