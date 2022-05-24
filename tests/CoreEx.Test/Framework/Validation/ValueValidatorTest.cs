using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class ValueValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public void Run_ErrorWithException()
        {
            Assert.ThrowsAsync<ValidationException>(async () => await new ValueValidator<TestData, int>(x => x.CountA, 0).Mandatory().RunAsync(true));
        }

        [Test]
        public async Task Run_ErrorWithResult()
        {
            var r = await new ValueValidator<TestData, int>(x => x.CountA, 0).Mandatory().RunAsync();
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasError);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Count A is required.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("CountA", r.Messages[0].Property);
        }

        [Test]
        public async Task Run_NoError()
        {
            var r = await new ValueValidator<TestData, int>(x => x.CountA, 1).Mandatory().RunAsync();
            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasError);
        }

        [Test]
        public async Task Run_NoError_String()
        {
            var r = await new ValueValidator<TestData, string?>(x => x.Text, "abc").Mandatory().RunAsync();
            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasError);
        }

        private class TestData2 { public string Text2 { get; set; } = "a"; }

        [Test]
        public async Task Run_NoError_String2()
        {
            var r = await new ValueValidator<TestData2, string>(x => x.Text2, "abc").Mandatory().RunAsync();
            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasError);
        }

        [Test]
        public async Task Run_Common_Error()
        {
            var cv = CommonValidator.Create<int>(v => v.Mandatory());

            var r = await new ValueValidator<TestData, int>(x => x.CountA, 0).Common(cv).RunAsync();
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasError);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Count A is required.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("CountA", r.Messages[0].Property);
        }

        [Test]
        public async Task Run_With_NotNull()
        {
            string name = "George";
            var r = await name.Validate().Mandatory().String(50).RunAsync();
            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasError);
        }

        [Test]
        public async Task Run_With_Null()
        {
            string? name = null;
            var r = await name.Validate().Mandatory().String(50).RunAsync();
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasError);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Value is required.", r.Messages[0].Text);
        }
    }
}