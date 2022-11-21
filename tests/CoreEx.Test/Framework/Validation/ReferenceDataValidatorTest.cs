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
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Value is required.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("value", r.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Empty()
        {
            var r = await GenderValidator.Default.ValidateAsync(new Gender());
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(3, r.Messages!.Count);
            Assert.AreEqual("Id", r.Messages[0].Property);
            Assert.AreEqual("Code", r.Messages[1].Property);
            Assert.AreEqual("Text", r.Messages[2].Property);
        }

        [Test]
        public async Task Validate_Dates()
        {
            var r = await GenderValidator.Default.ValidateAsync(new Gender { Id = 1, Code = "X", Text = "XX", StartDate = new DateTime(2000, 01, 01), EndDate = new DateTime(1950, 01, 01) });
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("End Date must be greater than or equal to Start Date.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("EndDate", r.Messages[0].Property);
        }
    }
}