using NUnit.Framework;
using CoreEx.Entities;
using CoreEx.Validation;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class AbstractValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(null);

        [Test]
        public async Task Abstract_And_RuleFor()
        {
            var r = await new TestDataValidator().ValidateAsync(new TestData());
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(2));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Please specify a text value."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
                Assert.That(r.Messages![1].Text, Is.EqualTo("Date B is required."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("DateB"));
            });
        }

        [Test]
        public async Task With_Message()
        {
            var r = await new TestDataValidator2().ValidateAsync(new TestData());
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(2));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Text is wonky."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
                Assert.That(r.Messages![1].Text, Is.EqualTo("Date B null-as."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("DateB"));
            });
        }

        [Test]
        public async Task IncludeSameType()
        {
            var r = await new TestDataValidator3().ValidateAsync(new TestData());
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(2));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Please specify a text value."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
                Assert.That(r.Messages![1].Text, Is.EqualTo("Date B is required."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("DateB"));
            });
        }
    }

    public class TestDataValidator : AbstractValidator<TestData>
    {
        public TestDataValidator()
        {
            RuleFor(x => x.Text).NotEmpty().WithMessage("Please specify a text value.");
            RuleFor(x => x.DateB).NotNull();
        }
    }

    public class TestDataValidator2 : AbstractValidator<TestData>
    {
        public TestDataValidator2()
        {
            RuleFor(x => x.Text).WithMessage("Text is wonky.").NotEmpty();
            RuleFor(x => x.DateB).WithMessage("Data B is wonky").NotNull().WithMessage("Date B null-as.");
        }
    }

    public class TestDataValidator3 : AbstractValidator<TestData>
    {
        public TestDataValidator3()
        {
            Include(new TestDataValidator());
        }
    }
}