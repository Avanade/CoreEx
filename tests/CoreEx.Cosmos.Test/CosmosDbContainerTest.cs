namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    [Category("WithCosmos")]
    public class CosmosDbContainerTest
    {
        private CosmosDb _db;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync().ConfigureAwait(false);
            _db = new CosmosDb(auth: false);
        }

        [Test]
        public async Task Get1Async()
        {
            Assert.That(await _db.Persons1.GetAsync(404.ToGuid().ToString()), Is.Null);

            var v = await _db.Persons1.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(1.ToGuid().ToString()));
            Assert.That(v.Name, Is.EqualTo("Rebecca"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1990, 08, 07, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(v.Salary, Is.EqualTo(150000m));
        }

        [Test]
        public async Task Get2Async()
        {
            Assert.That(await _db.Persons2.GetAsync(404.ToGuid().ToString()), Is.Null);

            var v = await _db.Persons2.GetAsync(1.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(1.ToGuid().ToString()));
            Assert.That(v.Name, Is.EqualTo("Rebecca"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1990, 08, 07, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(v.Salary, Is.EqualTo(150000m));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));
        }

        [Test]
        public async Task Get3Async()
        {
            var ex = Assert.ThrowsAsync<NotSupportedException>(() => _db.Persons3.GetAsync(DateTime.UtcNow));
            Assert.That(ex.Message, Does.StartWith("An identifier must be one of the following Types: string, int, long, or Guid."), ex.Message);

            Assert.That(await _db.Persons3.GetAsync(404.ToGuid()), Is.Null);

            var v = await _db.Persons3.GetAsync(1.ToGuid());
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(1.ToGuid()));
            Assert.That(v.Name, Is.EqualTo("Rebecca"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1990, 08, 07, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(v.Salary, Is.EqualTo(150000m));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Null);
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));

            // Different type.
            Assert.That(await _db.Persons3.GetAsync(100.ToGuid()), Is.Null);
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
            Assert.That(v.Name, Is.EqualTo("Michelle"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1979, 08, 12)));
            Assert.That(v.Salary, Is.EqualTo(181000m));

            v = await _db.Persons1.GetAsync(v.Id);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(id));
            Assert.That(v.Name, Is.EqualTo("Michelle"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1979, 08, 12)));
            Assert.That(v.Salary, Is.EqualTo(181000m));
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
            Assert.That(v.Name, Is.EqualTo("Michelle"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1979, 08, 12)));
            Assert.That(v.Salary, Is.EqualTo(181000m));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Null); 
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));

            v = await _db.Persons2.GetAsync(v.Id);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(id));
            Assert.That(v.Name, Is.EqualTo("Michelle"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1979, 08, 12)));
            Assert.That(v.Salary, Is.EqualTo(181000m));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Null); 
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));
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
            Assert.That(v.Name, Is.EqualTo("Michelle"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1979, 08, 12)));
            Assert.That(v.Salary, Is.EqualTo(181000m));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Null); 
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));

            v = await _db.Persons3.GetAsync(v.Id);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(id));
            Assert.That(v.Name, Is.EqualTo("Michelle"));
            Assert.That(v.Birthday, Is.EqualTo(new DateTime(1979, 08, 12)));
            Assert.That(v.Salary, Is.EqualTo(181000m));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Null); 
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));
        }

        [Test]
        public async Task Update1Async()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _db.Persons1.UpdateAsync(null!));

            // Get previous.
            var v = await _db.Persons1.GetAsync(5.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);

            // Update testing.
            v.Id = 404.ToGuid().ToString();
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons1.UpdateAsync(v));

            v.Id = 5.ToGuid().ToString();
            v.Name += "X";
            v = await _db.Persons1.UpdateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(5.ToGuid().ToString()));
            Assert.That(v.Name, Is.EqualTo("MikeX"));
        }

        [Test]
        public async Task Update2Async()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _db.Persons2.UpdateAsync(null!));

            // Get previous.
            var v = await _db.Persons2.GetAsync(5.ToGuid().ToString());
            Assert.That(v, Is.Not.Null);

            // Update testing.
            v.Id = 404.ToGuid().ToString();
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons2.UpdateAsync(v));

            v.Id = 5.ToGuid().ToString();
            v.Name += "X";
            v = await _db.Persons2.UpdateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(5.ToGuid().ToString()));
            Assert.That(v.Name, Is.EqualTo("MikeX"));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Not.Null);
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));

            v.ETag = "ZZZZZ";
            v.Name += "X";
            Assert.ThrowsAsync<ConcurrencyException>(() => _db.Persons2.UpdateAsync(v));
        }

        [Test]
        public async Task Update3Async()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _db.Persons3.UpdateAsync(null!));

            // Get previous.
            var v = await _db.Persons3.GetAsync(5.ToGuid());
            Assert.That(v, Is.Not.Null);

            // Update testing.
            v.Id = 404.ToGuid();
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons3.UpdateAsync(v));

            v.Id = 5.ToGuid();
            v.Name += "X";
            v = await _db.Persons3.UpdateAsync(v);
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Id, Is.EqualTo(5.ToGuid()));
            Assert.That(v.Name, Is.EqualTo("MikeX"));
            Assert.That(v.ChangeLog, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.CreatedDate, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedBy, Is.Not.Null);
            Assert.That(v.ChangeLog.UpdatedDate, Is.Not.Null);
            Assert.That(v.ETag, Is.Not.Null);
            Assert.That(v.ETag, Does.Not.StartsWith("\""));

            v.ETag = "ZZZZZ";
            v.Name += "X";
            Assert.ThrowsAsync<ConcurrencyException>(() => _db.Persons3.UpdateAsync(v));
        }

        [Test]
        public async Task Delete1Async()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons1.DeleteAsync(404.ToGuid().ToString()));

            await _db.Persons1.DeleteAsync(4.ToGuid().ToString());

            using (var r = await _db.Persons1.Container.ReadItemStreamAsync(4.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None))
            {
                Assert.That(r, Is.Not.Null);
                Assert.That(r.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
            };
        }

        [Test]
        public async Task Delete2Async()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons2.DeleteAsync(404.ToGuid().ToString()));

            await _db.Persons2.DeleteAsync(4.ToGuid().ToString());

            using (var r = await _db.Persons2.Container.ReadItemStreamAsync(4.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None))
            {
                Assert.That(r, Is.Not.Null);
                Assert.That(r.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
            };
        }

        [Test]
        public async Task Delete3Async()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _db.Persons3.DeleteAsync(404.ToGuid()));

            await _db.Persons3.DeleteAsync(4.ToGuid());

            using (var r = await _db.Persons3.Container.ReadItemStreamAsync(4.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None))
            {
                Assert.That(r, Is.Not.Null);
                Assert.That(r.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
            };

            using (var r = await _db.Persons3.Container.ReadItemStreamAsync(100.ToGuid().ToString(), Microsoft.Azure.Cosmos.PartitionKey.None))
            {
                Assert.That(r, Is.Not.Null);
                Assert.That(r.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            };
        }
    }
}