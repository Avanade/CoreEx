using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class CompareValueRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await 1.Validate("value").CompareValue(CompareOperator.Equal, 1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").CompareValue(CompareOperator.Equal, 2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be equal to 2."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 1.Validate("value").CompareValue(CompareOperator.GreaterThan, 0).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").CompareValue(CompareOperator.GreaterThan, 2, "Two").ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be greater than Two."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Nullable()
        {
            int? v = 1;
            var v1 = await v.Validate("value").CompareValue(CompareOperator.Equal, 1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await v.Validate("value").CompareValue(CompareOperator.Equal, 2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be equal to 2."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Equal()
        {
            var v1 = await 1.Validate("value").Equal(1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").Equal(2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be equal to 2."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_NotEqual()
        {
            var v1 = await 1.Validate("value").NotEqual(2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").NotEqual(1).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not be equal to 1."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_LessThan()
        {
            var v1 = await 1.Validate("value").LessThan(2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").LessThan(1).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be less than 1."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_LessThanOrEqualTo()
        {
            var v1 = await 1.Validate("value").LessThanOrEqualTo(1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 2.Validate("value").LessThanOrEqualTo(1).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be less than or equal to 1."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_GreaterThan()
        {
            var v1 = await 2.Validate("value").GreaterThan(1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").GreaterThan(2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be greater than 2."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_GreaterThanOrEqualTo()
        {
            var v1 = await 2.Validate("value").GreaterThanOrEqualTo(2).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").GreaterThanOrEqualTo(2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be greater than or equal to 2."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}