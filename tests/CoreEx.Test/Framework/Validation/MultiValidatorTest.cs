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
                .Add(1.Validate("value").Between(10, 20))
                .ValidateAsync().ConfigureAwait(false);
            
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);

            Assert.AreEqual(3, r.Messages!.Count);
            Assert.AreEqual("Text is required.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("Text", r.Messages[0].Property);

            Assert.AreEqual("Count B must be greater than 10.", r.Messages[1].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[1].Type);
            Assert.AreEqual("CountB", r.Messages[1].Property);

            Assert.AreEqual("Value must be between 10 and 20.", r.Messages[2].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[2].Type);
            Assert.AreEqual("value", r.Messages[2].Property);
        }

        [Test]
        public async Task MultiError2()
        {
            var v1 = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10))
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10));

            var r = await MultiValidator.Create()
                .Add(new TestData { CountB = 0 }.Validate("value").Entity(v1))
                .Add(1.Validate("id").Between(10, 20))
                .ValidateAsync().ConfigureAwait(false);

            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);

            Assert.AreEqual(3, r.Messages!.Count);
            Assert.AreEqual("Text is required.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("value.Text", r.Messages[0].Property);

            Assert.AreEqual("Count B must be greater than 10.", r.Messages[1].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[1].Type);
            Assert.AreEqual("value.CountB", r.Messages[1].Property);

            Assert.AreEqual("Identifier must be between 10 and 20.", r.Messages[2].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[2].Type);
            Assert.AreEqual("id", r.Messages[2].Property);

            Assert.Throws<ValidationException>(() => r.ThrowOnError());
        }

        [Test]
        public async Task MultiSuccess()
        {
            var v1 = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10))
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10));

            var r = await MultiValidator.Create()
                .Add(new TestData { Text = "XXXXXXXXXX", CountB = 11 }.Validate("value").Entity(v1))
                .Add(15.Validate("id").Between(10, 20))
                .ValidateAsync().ConfigureAwait(false);

            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasErrors);
            r.ThrowOnError();
        }
    }
}