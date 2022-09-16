using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    [Category("WithCosmos")]
    public class CosmosDbQueryPartitioningTest
    {
        private CosmosDb _db;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync("/filter", "/value/filter").ConfigureAwait(false);
            _db = new CosmosDb(auth: false);
        }

        [SetUp]
        public void Setup()
        {
            Console.Error.WriteLineAsync("waiting 1 sec before starting test");
            Thread.Sleep(TestSetUp.TestDelayMs);
        }

        [Test]
        public async Task Query1()
        {
            var v = await _db.Persons1.Query().ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(5));

            v = await _db.Persons1.Query(new PartitionKey("A")).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(3));
        }

        [Test]
        public async Task Query2()
        {
            var v = await _db.Persons2.Query().ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(5));

            v = await _db.Persons2.Query(new PartitionKey("A")).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(3));
        }

        [Test]
        public async Task Query3()
        {
            var v = await _db.Persons3.Query().ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(5));

            v = await _db.Persons3.Query(new PartitionKey("A")).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(3));
        }
    }
}