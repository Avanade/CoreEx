using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class MultiValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task MultiError()
        {
            var v1 = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10))
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10));

            var r = await MultiValidator.Create()
                .Add(v1, new TestData { CountB = 0 })
                .Add(1.Validate("value").Configure(c => c.Between(10, 20)))
                .ValidateAsync().ConfigureAwait(false);
            
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);

                Assert.That(r.Messages!, Has.Count.EqualTo(3));

                Assert.That(r.Messages[0].Text, Is.EqualTo("Text is required."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));

                Assert.That(r.Messages[1].Text, Is.EqualTo("Count B must be greater than 10."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("CountB"));

                Assert.That(r.Messages[2].Text, Is.EqualTo("Value must be between 10 and 20."));
                Assert.That(r.Messages[2].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[2].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task MultiError2()
        {
            var v1 = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10))
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10));

            var r = await MultiValidator.Create()
                .Add(new TestData { CountB = 0 }.Validate("value").Configure(c => c.Entity(v1)))
                .Add(1.Validate("id").Configure(c => c.Between(10, 20)))
                .ValidateAsync().ConfigureAwait(false);

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);

                Assert.That(r.Messages!, Has.Count.EqualTo(3));

                Assert.That(r.Messages[0].Text, Is.EqualTo("Text is required."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value.Text"));

                Assert.That(r.Messages[1].Text, Is.EqualTo("Count B must be greater than 10."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("value.CountB"));

                Assert.That(r.Messages[2].Text, Is.EqualTo("Identifier must be between 10 and 20."));
                Assert.That(r.Messages[2].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[2].Property, Is.EqualTo("id"));
            });

            Assert.Throws<ValidationException>(() => r.ThrowOnError());
        }

        [Test]
        public async Task MultiSuccess()
        {
            var v1 = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10))
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10));

            var r = await MultiValidator.Create()
                .Add(new TestData { Text = "XXXXXXXXXX", CountB = 11 }.Validate("value").Configure(c => c.Entity(v1)))
                .Add(15.Validate("id").Configure(c => c.Between(10, 20)))
                .ValidateAsync().ConfigureAwait(false);

            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);
            r.ThrowOnError();
        }
    }
}