namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    public class CosmosDbQueryTestcs
    {
        private CosmosDb _db;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync().ConfigureAwait(false);
            _db = new CosmosDb();
        }

        [Test]
        public void Query_NoPaging1()
        {
            var v = _db.Persons1.Query().ToArray();
            Assert.That(v, Has.Length.EqualTo(5));

            v = _db.Persons1.Query(q => q.Where(x => x.Name == "Greg")).ToArray();
            Assert.That(v, Has.Length.EqualTo(1));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));

            v = _db.Persons1.Query(q => q.Where(x => x.Name == "GREG")).ToArray();
            Assert.That(v, Is.Empty);
        }

        [Test]
        public void Query_Paging1()
        {
            var pr = new Entities.PagingResult(Entities.PagingArgs.CreateSkipAndTake(1, 2, true));
            var v = _db.Persons1.Query().WithPaging(pr).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(pr.TotalCount, Is.EqualTo(5));

            v = _db.Persons1.Query(q => q.OrderBy(x => x.Name)).WithPaging(1, 2).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Name, Is.EqualTo("Mike"));
        }

        [Test]
        public void Query_Wildcards1()
        {
            var v = _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "g*")).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "*Y")).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "*e*")).ToArray();
            Assert.That(v, Has.Length.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.Throws<InvalidOperationException>(() => _db.Persons1.AsQueryable().WhereWildcard(x => x.Name, "*m*e").ToArray());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }

        [Test]
        public void AsQueryable_NoPaging2()
        {
            var v = _db.Persons2.AsQueryable().ToArray();
            Assert.That(v, Has.Length.EqualTo(5));

            v = _db.Persons2.AsQueryable().Where(x => x.Name == "Greg").ToArray();
            Assert.That(v, Has.Length.EqualTo(1));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));

            v = _db.Persons2.AsQueryable().Where(x => x.Name == "GREG").ToArray();
            Assert.That(v, Is.Empty);
        }

        [Test]
        public void AsQueryable_Paging2()
        {
            var pr = new Entities.PagingResult(Entities.PagingArgs.CreateSkipAndTake(1, 2, true));
            var v = _db.Persons2.Query().WithPaging(pr).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(pr.TotalCount, Is.EqualTo(5));

            v = _db.Persons2.Query(q => q.OrderBy(x => x.Name)).WithPaging(1, 2).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Name, Is.EqualTo("Mike"));
        }

        [Test]
        public void AsQueryable_Wildcards2()
        {
            var v = _db.Persons2.AsQueryable().WhereWildcard(x => x.Name, "g*").ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = _db.Persons2.AsQueryable().WhereWildcard(x => x.Name, "*Y").ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = _db.Persons2.AsQueryable().WhereWildcard(x => x.Name, "*e*").ToArray();
            Assert.That(v, Has.Length.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.Throws<InvalidOperationException>(() => _db.Persons2.AsQueryable().WhereWildcard(x => x.Name, "*m*e").ToArray());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }

        [Test]
        public void Query_NoPaging3()
        {
            var v = _db.Persons3.Query().ToArray();
            Assert.That(v, Has.Length.EqualTo(5));

            v = _db.Persons3.Query(q => q.Where(x => x.Value.Name == "Greg")).ToArray();
            Assert.That(v, Has.Length.EqualTo(1));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));

            v = _db.Persons3.Query(q => q.Where(x => x.Value.Name == "GREG")).ToArray();
            Assert.That(v, Is.Empty);
        }

        [Test]
        public void AsQueryable_Paging3()
        {
            var v = _db.Persons3.AsQueryable().WithPaging(1, 2).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Value.Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Value.Name, Is.EqualTo("Greg"));

            v = _db.Persons3.AsQueryable().OrderBy(x => x.Value.Name).WithPaging(1, 2).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Value.Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Value.Name, Is.EqualTo("Mike"));
        }

        [Test]
        public void Query_Wildcards3()
        {
            var v = _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "g*")).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "*Y")).ToArray();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "*e*")).ToArray();
            Assert.That(v, Has.Length.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.Throws<InvalidOperationException>(() => _db.Persons3.AsQueryable().WhereWildcard(x => x.Value.Name, "*m*e").ToArray());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }

        [Test]
        public void Query_Wildcards2()
        {
            var v = _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "g*")).SelectQuery<List<Person2>>();
            Assert.That(v, Has.Count.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "*Y")).SelectQuery<List<Person2>>();
            Assert.That(v, Has.Count.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "*e*")).SelectQuery<List<Person2>>();
            Assert.That(v, Has.Count.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.Throws<InvalidOperationException>(() => _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "*m*e")).SelectQuery<List<Person2>>());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }
    }
}