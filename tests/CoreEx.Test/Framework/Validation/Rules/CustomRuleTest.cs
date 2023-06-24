using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;
using CoreEx.Results;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class CustomRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await "Abc".Validate("value").Custom(x => { x.CreateErrorMessage("Test"); return Result.Success; }).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Test", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}
