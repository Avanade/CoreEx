using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    [Category("WithCosmos")]
    public class CosmosDbContainerPartitioningTest
    {
        private CosmosDb _db;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync("/filter", "/value/filter", dbName: "CoreEx.Cosmos.Test2").ConfigureAwait(false);
            _db = new CosmosDb(auth: false, partitioning: true);
            _db.UsePartitionKey(new PartitionKey("A"));
            _db.Persons1.UsePartitionKey(p => new PartitionKey(p.Filter));
            _db.Persons2.UsePartitionKey(p => new PartitionKey(p.Filter));
            _db.Persons3.UsePartitionKey(p => new PartitionKey(p.Filter));
        }

        [SetUp]
        public async Task Setup()
        {
            await Console.Error.WriteLineAsync($"waiting {TestSetUp.TestDelayMs / 1000} s before starting test");
            await Task.Delay(TestSetUp.TestDelayMs);
        }

        [Test]
        public async Task Get1Async()
        {
            var v = await _db.Persons1.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Null);

            v = await _db.Persons1.GetAsync(4.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("Sally"));
        }

        [Test]
        public async Task Create1Async()
        {
            var id = Guid.NewGuid().ToString();
            var v = new Person1 { Id = id, Name = "Michelle", Birthday = new DateTime(1979, 08, 12), Salary = 181000m, Filter = "B" };
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.CreateAsync(v));

            v.Filter = "A";
            await _db.Persons1.CreateAsync(v);

            v = await _db.Persons1.GetAsync(id);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("Michelle"));
        }

        [Test]
        public async Task Update1Async()
        {
            var v = await _db.Persons1.GetAsync(4.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            v.Filter = "B";
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.UpdateAsync(v));

            v.Filter = "A";
            v = await _db.Persons1.UpdateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("SallyX"));
        }

        [Test]
        public async Task Delete1Async()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons1.DeleteAsync(1.ToGuid().ToString()));
            var ir = await _db.Persons1.Container.ReadItemAsync<Person1>(1.ToGuid().ToString(), new PartitionKey("B")).ConfigureAwait(false);
            Assert.That(ir, Is.Not.Null);
            Assert.That(ir.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

            await _db.Persons1.DeleteAsync(5.ToGuid().ToString());
            var v = await _db.Persons1.GetAsync(5.ToGuid().ToString());
            Assert.That(v, Is.Null);
        }

        [Test]
        public async Task Get2Async()
        {
            var v = await _db.Persons2.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Null);

            v = await _db.Persons2.GetAsync(4.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("Sally"));
        }

        [Test]
        public async Task Create2Async()
        {
            var id = Guid.NewGuid().ToString();
            var v = new Person2 { Id = id, Name = "Michelle", Birthday = new DateTime(1979, 08, 12), Salary = 181000m, Filter = "B" };
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.CreateAsync(v));

            v.Filter = "A";
            await _db.Persons2.CreateAsync(v);

            v = await _db.Persons2.GetAsync(id);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("Michelle"));
        }

        [Test]
        public async Task Update2Async()
        {
            var v = await _db.Persons2.GetAsync(4.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            v.Filter = "B";
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.UpdateAsync(v));

            v.Filter = "A";
            v = await _db.Persons2.UpdateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("SallyX"));
        }

        [Test]
        public async Task Delete2Async()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons2.DeleteAsync(1.ToGuid().ToString()));
            var ir = await _db.Persons2.Container.ReadItemAsync<Person2>(1.ToGuid().ToString(), new PartitionKey("B")).ConfigureAwait(false);
            Assert.That(ir, Is.Not.Null);
            Assert.That(ir.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

            await _db.Persons2.DeleteAsync(5.ToGuid().ToString());
            var v = await _db.Persons2.GetAsync(5.ToGuid().ToString());
            Assert.That(v, Is.Null);
        }

        [Test]
        public async Task Get3Async()
        {
            var v = await _db.Persons3.GetAsync(1.ToGuid());
            Assert.That(v, Is.Null);

            v = await _db.Persons3.GetAsync(4.ToGuid());
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("Sally"));
        }

        [Test]
        public async Task Create3Async()
        {
            var id = Guid.NewGuid();
            var v = new Person3 { Id = id, Name = "Michelle", Birthday = new DateTime(1979, 08, 12), Salary = 181000m, Filter = "B" };
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.CreateAsync(v));

            v.Filter = "A";
            await _db.Persons3.CreateAsync(v);

            v = await _db.Persons3.GetAsync(id);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("Michelle"));
        }

        [Test]
        public async Task Update3Async()
        {
            var v = await _db.Persons3.GetAsync(4.ToGuid());
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            v.Filter = "B";
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.UpdateAsync(v));

            v.Filter = "A";
            v = await _db.Persons3.UpdateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Name, Is.EqualTo("SallyX"));
        }

        [Test]
        public async Task Delete3Async()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons3.DeleteAsync(1.ToGuid()));
            var ir = await _db.Persons3.Container.ReadItemAsync<Person2>(1.ToGuid().ToString(), new PartitionKey("B")).ConfigureAwait(false);
            Assert.That(ir, Is.Not.Null);
            Assert.That(ir.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

            await _db.Persons3.DeleteAsync(5.ToGuid());
            var v = await _db.Persons3.GetAsync(5.ToGuid());
            Assert.That(v, Is.Null);
        }
    }
}