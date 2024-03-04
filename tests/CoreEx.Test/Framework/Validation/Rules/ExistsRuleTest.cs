using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;
using CoreEx.Results;

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
            var v1 = await 123.Validate("value").Exists(x => true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
            
            v1 = await 123.Validate("value").Exists(x => false).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is not found; a valid value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").Exists(true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 123.Validate("value").Exists(false).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is not found; a valid value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").ExistsAsync((_, __) => Task.FromResult(true)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 123.Validate("value").ExistsAsync((_, __) => Task.FromResult(false)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is not found; a valid value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").ValueExistsAsync((_, __) => Task.FromResult<object?>(new object())).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 123.Validate("value").ValueExistsAsync((_, __) => Task.FromResult((object?)null)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is not found; a valid value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").AgentExistsAsync((_, __) => CoreEx.Http.HttpResult.CreateAsync(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK), __)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 123.Validate("value").AgentExistsAsync((_, __) => CoreEx.Http.HttpResult.CreateAsync(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound), __)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is not found; a valid value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").AgentExistsAsync((_, __) => CoreEx.Http.HttpResult.CreateAsync<int>(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK), cancellationToken: __)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 123.Validate("value").AgentExistsAsync((_, __) => CoreEx.Http.HttpResult.CreateAsync<int>(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound), cancellationToken: __)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is not found; a valid value is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await 123.Validate("value").ValueExistsAsync(async (_, __) => await GetBlahAsync()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.True); // Result.Success is not a valid value
        }

        private static Task<Result> GetBlahAsync() => Task.FromResult(Result.Success);
    }
}