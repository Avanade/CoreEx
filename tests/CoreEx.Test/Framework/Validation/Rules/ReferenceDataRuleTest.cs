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
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp, new MemoryCache(new MemoryCacheOptions())).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var v1 = await ((RefDataEx)"Aaa").Validate().IsValid().RunAsync();
            Assert.IsFalse(v1.HasError);

            v1 = await ((RefDataEx)"Abc").Validate().IsValid().RunAsync();

            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Code()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp, new MemoryCache(new MemoryCacheOptions())).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var v1 = await "Aaa".Validate().RefDataCode().As<RefDataEx>().RunAsync();
            Assert.IsFalse(v1.HasError);

            v1 = await "Abc".Validate().RefDataCode().As<RefDataEx>().RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);
        }

        [Test]
        public async Task SidList_Validate()
        {
            IServiceCollection sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddJsonSerializer();
            sc.AddExecutionContext();
            sc.AddScoped<RefDataProvider>();
            sc.AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp, new MemoryCache(new MemoryCacheOptions())).Register<RefDataProvider>());
            var sp = sc.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var ec = scope.ServiceProvider.GetService<ExecutionContext>();

            var sids = new ReferenceDataSidList<string, RefDataEx>("Aaa", "Abc");
            var v1 = await sids.Validate().AreValid().RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains one or more invalid items.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);

            sids = new ReferenceDataSidList<string, RefDataEx>("Aaa", "Aaa");
            v1 = await sids.Validate().AreValid().RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains duplicates; Code 'AAA' specified more than once.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);

            v1 = await sids.Validate().AreValid(allowDuplicates: true).RunAsync();
            Assert.IsFalse(v1.HasError);

            v1 = await sids.Validate().AreValid(true, 5).RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must have at least 5 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);

            v1 = await sids.Validate().AreValid(true, maxCount: 1).RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not exceed 1 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);
        }
    }
}