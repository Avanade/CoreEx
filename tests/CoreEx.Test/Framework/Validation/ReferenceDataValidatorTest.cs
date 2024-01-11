using CoreEx.Entities;
using CoreEx.RefData.Extended;
using CoreEx.Validation;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class ReferenceDataValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        public class Gender : ReferenceDataBaseEx<int, Gender> { }

        public class GenderValidator : ReferenceDataValidatorBase<Gender, GenderValidator> { }

        [Test]
        public async Task Validate_Null()
        {
            var r = await new ReferenceDataValidator<Gender>().ValidateAsync(null!);
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Value is required."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Empty()
        {
            var r = await GenderValidator.Default.ValidateAsync(new Gender());
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(3));
                Assert.That(r.Messages![0].Property, Is.EqualTo("Id"));
                Assert.That(r.Messages[1].Property, Is.EqualTo("Code"));
                Assert.That(r.Messages[2].Property, Is.EqualTo("Text"));
            });
        }

        [Test]
        public async Task Validate_Dates()
        {
            var r = await GenderValidator.Default.ValidateAsync(new Gender { Id = 1, Code = "X", Text = "XX", StartDate = new DateTime(2000, 01, 01), EndDate = new DateTime(1950, 01, 01) });
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("End Date must be greater than or equal to Start Date."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("EndDate"));
            });
        }
    }
}