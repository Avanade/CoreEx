using Azure.Data.Tables;
using CoreEx.Azure.Storage;
using CoreEx.Configuration;
using CoreEx.Hosting.Work;
using CoreEx.Test.Framework.Json.Mapping;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnitTestEx.NUnit;


namespace CoreEx.Test.Framework.Hosting.Work
{
    [TestFixture]
    internal class WorkStateOrchestratorTest
    {
        private static WorkStateOrchestrator CreateOrchestrator(string persistType)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>(FileWorkStatePersistence.ConfigKey, Path.GetTempPath()) });
            var s = new DefaultSettings(config.Build());
            IWorkStatePersistence p = persistType switch
            {
                "fs" => new FileWorkStatePersistence(s),
                "at" => CreateTableWorkStatePersistence(),
                _ => new InMemoryWorkStatePersistence()
            };

            if (p is null)
                return null!;

            return new WorkStateOrchestrator(p, s);
        }

        private static TableWorkStatePersistence CreateTableWorkStatePersistence()
        {
            var csn = $"{nameof(BlobAttachmentStorage)}ConnectionString";
            var cs = Environment.GetEnvironmentVariable(csn);
            if (string.IsNullOrEmpty(cs))
                return null!;

            var tsc = new TableServiceClient(cs);
            return new TableWorkStatePersistence(tsc);
        }

        [Test]
        public async Task Orchestrate_With_FileSystem() => await Orchestrate(CreateOrchestrator("fs"));

        [Test]
        public async Task Orchestrate_With_InMemory() => await Orchestrate(CreateOrchestrator("im"));

        [Test]
        public async Task Orchestrate_With_AzureTable() => await Orchestrate(CreateOrchestrator("at"));

        private static async Task Orchestrate(WorkStateOrchestrator o)
        {
            if (o is null)
            {
                Assert.Inconclusive("Test cannot run as the environment variable 'TableWorkStatePersistenceConnectionString' is not defined.");
                return;
            }

            ExecutionContext.Reset();
            ExecutionContext.SetCurrent(new ExecutionContext { UserName = ExecutionContext.EnvironmentUserName });

            // Clean up before we begin.
            await o.Persistence.DeleteAsync("abc", default);

            // Get where not found.
            var wr = await o.GetAsync<Person>("abc");
            Assert.That(wr, Is.Null);

            // Create work and assert state.
            wr = await o.CreateAsync(WorkStateArgs.Create<Person>().Adjust(x => x.Id = "abc").Adjust(x => x.Key = "bananas"));
            Assert.That(wr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(wr.Id, Is.EqualTo("abc"));
                Assert.That(wr.TypeName, Is.EqualTo(typeof(Person).FullName));
                Assert.That(wr.Status, Is.EqualTo(WorkStatus.Created));
            });

            ObjectComparer.Assert(wr, await o.GetAsync<Person>("abc"));
            Assert.That(await o.GetDataAsync("abc", default), Is.Null);

            // Start work and assert state.
            wr = await o.StartAsync("abc");
            Assert.That(wr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(wr.Id, Is.EqualTo("abc"));
                Assert.That(wr.Status, Is.EqualTo(WorkStatus.Started));
            });

            ObjectComparer.Assert(wr, await o.GetAsync<Person>("abc"));
            Assert.That(await o.GetDataAsync("abc", default), Is.Null);

            // Set to indeterminate and assert state.
            wr = await o.IndeterminateAsync("abc", "Not sure!");
            Assert.That(wr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(wr.Id, Is.EqualTo("abc"));
                Assert.That(wr.Status, Is.EqualTo(WorkStatus.Indeterminate));
                Assert.That(wr.Reason, Is.EqualTo("Not sure!"));
            });

            ObjectComparer.Assert(wr, await o.GetAsync<Person>("abc"));
            Assert.That(await o.GetDataAsync("abc", default), Is.Null);

            // Set the data and assert by getting and comparing.
            var p = new Person { Id = "xyz", Name = "John" };
            await o.SetDataAsync("abc", p);

            var p2 = await o.GetDataAsync<Person>("abc", default);
            Assert.That(p2, Is.Not.Null);
            ObjectComparer.Assert(p, await o.GetDataAsync<Person>("abc"));

            // Set to complete and assert state.
            wr = await o.CompleteAsync("abc");
            Assert.That(wr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(wr.Id, Is.EqualTo("abc"));
                Assert.That(wr.Status, Is.EqualTo(WorkStatus.Completed));
                Assert.That(wr.Reason, Is.Null);
            });

            ObjectComparer.Assert(wr, await o.GetAsync<Person>("abc"));

            // Check different type; but same id. Confirms same type as extra pre-caution!
            wr = await o.GetAsync("other", "abc");
            Assert.That(wr, Is.Null);

            // Check different username; but same id. Confirms same user as extra/extra pre-caution!
            ExecutionContext.Reset();
            ExecutionContext.SetCurrent(new ExecutionContext { UserName = "other" });
            wr = await o.GetAsync<Person>("abc");
            Assert.That(wr, Is.Null);

            // Check no username set; but same id. Confirms same user as extra/extra/extra pre-caution!
            ExecutionContext.Reset();
            wr = await o.GetAsync<Person>("abc");
            Assert.That(wr, Is.Null);
        }

        [Test]
        public async Task Orchestrate_LargeData_With_FileSystem() => await Orchestrate_LargeData(CreateOrchestrator("fs"));

        [Test]
        public async Task Orchestrate_LargeData_With_InMemory() => await Orchestrate_LargeData(CreateOrchestrator("im"));

        [Test]
        public async Task Orchestrate_LargeData_With_AzureTable() => await Orchestrate_LargeData(CreateOrchestrator("at"));

        private static async Task Orchestrate_LargeData(WorkStateOrchestrator o)
        {
            if (o is null)
            {
                Assert.Inconclusive("Test cannot run as the environment variable 'TableWorkStatePersistenceConnectionString' is not defined.");
                return;
            }

            // Clean up before we begin.
            await o.Persistence.DeleteAsync("large", default);

            // Create the work.
            var wr = await o.CreateAsync(WorkStateArgs.Create<Person>().Adjust(x => x.Id = "large"));
            Assert.That(wr, Is.Not.Null);

            // Set large data and assert by getting and comparing.
            var ls = new string('x', 959998);
            await o.SetDataAsync("large", ls);

            var ls2 = await o.GetDataAsync<string>("large", default);
            Assert.That(ls2, Is.EqualTo(ls));
        }
    }
}