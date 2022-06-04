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
            var v1 = await 1.Validate().Between(2, 10).When(true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 1.Validate().Between(2, 10).When(false).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate().Between(2, 10).When(() => true).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 1.Validate().Between(2, 10).When(() => false).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate().Between(2, 10).When(x => x.Value == 1).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 1.Validate().Between(2, 10).When(x => x.Value != 1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var v = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.CountA, p => p.Between(1, 10).When(x => x.Text == "xxx"));

            var td = new TestData { Text = "xxx", CountA = 88 };
            var v2 = await td.Validate().Entity(v).ValidateAsync();
            Assert.IsTrue(v2.HasErrors);

            td = new TestData { Text = "yyy", CountA = 88 };
            v2 = await td.Validate().Entity(v).ValidateAsync();
            Assert.IsFalse(v2.HasErrors);
        }

        [Test]
        public async Task WhenValue()
        {
            var v1 = await 1.Validate().Between(2, 10).WhenValue(v => v == 1).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 1.Validate().Between(2, 10).WhenValue(v => v == 2).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var v = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.CountA, p => p.Between(1, 10).WhenValue(v => v == 88));

            var td = new TestData { Text = "xxx", CountA = 88 };
            var v2 = await td.Validate().Entity(v).ValidateAsync();
            Assert.IsTrue(v2.HasErrors);

            td = new TestData { Text = "xxx", CountA = 99 };
            v2 = await td.Validate().Entity(v).ValidateAsync();
            Assert.IsFalse(v2.HasErrors);
        }

        [Test]
        public async Task WhenHasValue()
        {
            var v1 = await 1.Validate().Immutable().WhenHasValue().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 0.Validate().Immutable().WhenHasValue().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task WhenOperation()
        {
            ExecutionContext.Current.OperationType = OperationType.Update;

            var v1 = await 1.Validate().Immutable().WhenOperation(OperationType.Update).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 1.Validate().Immutable().WhenOperation(OperationType.Create).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task WhenNotOperation()
        {
            ExecutionContext.Current.OperationType = OperationType.Update;

            var v1 = await 1.Validate().Immutable().WhenNotOperation(OperationType.Create).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);

            v1 = await 1.Validate().Immutable().WhenNotOperation(OperationType.Update).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }
    }
}