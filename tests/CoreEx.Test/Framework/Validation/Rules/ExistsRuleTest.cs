using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class ExistsRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_Value()
        {
            var v1 = await 123.Validate().Exists(x => true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
            
            v1 = await 123.Validate().Exists(x => false).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not found; a valid value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 123.Validate().Exists(true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 123.Validate().Exists(false).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not found; a valid value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 123.Validate().Exists((_, __) => Task.FromResult(true)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 123.Validate().Exists((_, __) => Task.FromResult(false)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not found; a valid value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 123.Validate().Exists((_, __) => Task.FromResult<object?>(new object())).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 123.Validate().Exists((_, __) => Task.FromResult((object?)null)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not found; a valid value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await 123.Validate().AgentExists((_, __) => CoreEx.Http.HttpResult.CreateAsync(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK))).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 123.Validate().AgentExists((_, __) => CoreEx.Http.HttpResult.CreateAsync(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound))).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not found; a valid value is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }
    }
}