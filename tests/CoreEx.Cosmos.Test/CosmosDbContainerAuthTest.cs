namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    [Category("WithCosmos")]
    public class CosmosDbContainerAuthTest
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private CosmosDb _db;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync().ConfigureAwait(false);
            _db = new CosmosDb(auth: true);
        }

        [Test]
        public void AsQueryable1() => Assert.That(_db.Persons1.Query().AsQueryable().Count(), Is.EqualTo(3));

        [Test]
        public void AsQueryable2() => Assert.That(_db.Persons2.Query().AsQueryable().Count(), Is.EqualTo(3));

        [Test]
        public void AsQueryable3() => Assert.That(_db.Persons3.Query().AsQueryable().Count(), Is.EqualTo(3));

        [Test]
        public async Task Get1Async()
        {
            Assert.That(await _db.Persons1.GetAsync(404.ToGuid().ToString()), Is.Null);

            var v = await _db.Persons1.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v!.Id, Is.EqualTo(1.ToGuid().ToString()));

            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.GetAsync(2.ToGuid().ToString()));
        }

        [Test]
        public async Task Get2Async()
        {
            Assert.That(await _db.Persons2.GetAsync(404.ToGuid().ToString()), Is.Null);

            var v = await _db.Persons2.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v!.Id, Is.EqualTo(1.ToGuid().ToString()));

           Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.GetAsync(2.ToGuid().ToString()));
        }

        [Test]
        public async Task Get3Async()
        {
            Assert.That(await _db.Persons3.GetAsync(404.ToGuid()), Is.Null);

            var v = await _db.Persons3.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v!.Id, Is.EqualTo(1.ToGuid()));

            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.GetAsync(2.ToGuid()));
        }

        [Test]
        public async Task Create1Async()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _db.Persons1.CreateAsync(null!));

            var id = Guid.NewGuid().ToString();
            var v = new Person1 { Id = id, Name = "Michelle", Birthday = new DateTime(1979, 08, 12), Salary = 181000m };
            v = await _db.Persons1.CreateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(id));

            v = new Person1 { Id = Guid.NewGuid().ToString(), Name = "Harry", Birthday = new DateTime(1999, 07, 21), Salary = 181000m, Locked = true };
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.CreateAsync(v));
        }

        [Test]
        public async Task Create2Async()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _db.Persons2.CreateAsync(null!));

            var id = Guid.NewGuid().ToString();
            var v = new Person2 { Id = id, Name = "Michelle", Birthday = new DateTime(1979, 08, 12), Salary = 181000m };
            v = await _db.Persons2.CreateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(id));

            v = new Person2 { Id = Guid.NewGuid().ToString(), Name = "Harry", Birthday = new DateTime(1999, 07, 21), Salary = 181000m, Locked = true };
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.CreateAsync(v));
        }

        [Test]
        public async Task Create3Async()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _db.Persons3.CreateAsync(null!));

            var id = Guid.NewGuid();
            var v = new Person3 { Id = id, Name = "Michelle", Birthday = new DateTime(1979, 08, 12), Salary = 181000m };
            v = await _db.Persons3.CreateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(id));

            v = new Person3 { Id = Guid.NewGuid(), Name = "Harry", Birthday = new DateTime(1999, 07, 21), Salary = 181000m, Locked = true };
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.CreateAsync(v));
        }

        [Test]
        public async Task Update1Async()
        {
            // Update where not auth.
            var v = (await _db.Persons1.CosmosContainer.ReadItemAsync<Person1>(4.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None).ConfigureAwait(false)).Resource;
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.UpdateAsync(v));

            // Update to something not auth.
            v = (await _db.Persons1.CosmosContainer.ReadItemAsync<Person2>(5.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None).ConfigureAwait(false)).Resource;
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            v.Locked = true;
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.UpdateAsync(v));

            v.Locked = false;
            await _db.Persons1.UpdateAsync(v);
        }

        [Test]
        public async Task Update2Async()
        {
            // Update where not auth.
            var v = (await _db.Persons2.CosmosContainer.ReadItemAsync<Person2>(4.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None).ConfigureAwait(false)).Resource;
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.UpdateAsync(v));

            // Update to something not auth.
            v = (await _db.Persons2.CosmosContainer.ReadItemAsync<Person2>(5.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None).ConfigureAwait(false)).Resource;
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            v.Locked = true;
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.UpdateAsync(v));

            v.Locked = false;
            await _db.Persons2.UpdateAsync(v);
        }

        [Test]
        public async Task Update3Async()
        {
            // Update where not auth.
            var v = (await _db.Persons3.CosmosContainer.ReadItemAsync<Person3>(4.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None).ConfigureAwait(false)).Resource;
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.UpdateAsync(v));

            // Update to something not auth.
            v = (await _db.Persons3.CosmosContainer.ReadItemAsync<Person3>(5.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None).ConfigureAwait(false)).Resource;
            Assert.That(v, Is.Not.Null);

            v.Name += "X";
            v.Locked = true;
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.UpdateAsync(v));

            v.Locked = false;
            await _db.Persons3.UpdateAsync(v);
        }

        [Test]
        public async Task Delete1Async()
        {
            await _db.Persons1.DeleteAsync(3.ToGuid().ToString());
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons1.DeleteAsync(4.ToGuid().ToString()));
        }

        [Test]
        public async Task Delete2Async()
        {
            await _db.Persons2.DeleteAsync(3.ToGuid().ToString());
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons2.DeleteAsync(4.ToGuid().ToString()));
        }

        [Test]
        public async Task Delete3Async()
        {
            await _db.Persons3.DeleteAsync(3.ToGuid());
            Assert.ThrowsAsync<AuthorizationException>(() => _db.Persons3.DeleteAsync(4.ToGuid()));
        }
    }
}