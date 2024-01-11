using CoreEx.Entities;
using CoreEx.RefData;
using CoreEx.Test.Framework.RefData;
using CoreEx.Validation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    internal class ReferenceDataRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_RefData()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var v1 = await ((RefDataEx)"Aaa").Validate("value").IsValid().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((RefDataEx)"Abc").Validate("value").IsValid().ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Code()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var v1 = await "Aaa".Validate("value").RefDataCode().As<RefDataEx>().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "Abc".Validate("value").RefDataCode().As<RefDataEx>().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task SidList_Validate()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator<RefDataProvider>();
            var sp = sc.BuildServiceProvider();

            ReferenceDataOrchestrator.SetCurrent(sp.GetRequiredService<ReferenceDataOrchestrator>());

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var sids = new ReferenceDataCodeList<RefDataEx>("Aaa", "Abc");
            var v1 = await sids.Validate("value").AreValid().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains one or more invalid items."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            sids = new ReferenceDataCodeList<RefDataEx>("Aaa", "Aaa");
            v1 = await sids.Validate("value").AreValid().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains duplicates; Code 'AAA' specified more than once."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await sids.Validate("value").AreValid(allowDuplicates: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await sids.Validate("value").AreValid(true, 5).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must have at least 5 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await sids.Validate("value").AreValid(true, maxCount: 1).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not exceed 1 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}