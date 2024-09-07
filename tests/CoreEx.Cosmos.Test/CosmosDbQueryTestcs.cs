using CoreEx.Data.Querying;
using CoreEx.Entities;

namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    [Category("WithCosmos")]
    public class CosmosDbQueryTest
    {
        private CosmosDb _db;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync().ConfigureAwait(false);
            _db = new CosmosDb(auth: false);
        }

        [Test]
        public async Task Query_NoPaging1()
        {
            var v = await _db.Persons1.Query().ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(5));

            v = await _db.Persons1.Query(q => q.Where(x => x.Name == "Greg")).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(1));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));

            v = await _db.Persons1.Query(q => q.Where(x => x.Name == "GREG")).ToArrayAsync();
            Assert.That(v, Is.Empty);
        }

        [Test]
        public async Task Query_Paging1()
        {
            var pr = new Entities.PagingResult(Entities.PagingArgs.CreateSkipAndTake(1, 2, true));
            var v = await _db.Persons1.Query(q => q.OrderBy(x => x.Id)).WithPaging(pr).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(pr.TotalCount, Is.EqualTo(5));

            v = await _db.Persons1.Query(q => q.OrderBy(x => x.Name)).WithPaging(1, 2).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Name, Is.EqualTo("Mike"));

            var vr = await _db.Persons1.Query(q => q.OrderBy(x => x.Name).Where(x => !x.Locked)).WithPaging(Entities.PagingArgs.CreateSkipAndTake(1, 2, true)).SelectResultAsync<Person1CollectionResult, Person1Collection>();
            Assert.That(vr.Items, Has.Count.EqualTo(2));
            Assert.That(vr.Items[0].Name, Is.EqualTo("Mike"));
            Assert.That(vr.Items[1].Name, Is.EqualTo("Rebecca"));
            Assert.That(vr.Paging, Is.Not.Null);
            Assert.That(vr.Paging.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task Query_Wildcards1()
        {
            var v = await _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "g*").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = await _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "*Y").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = await _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "*e*").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _db.Persons1.Query(q => q.WhereWildcard(x => x.Name, "*m*e")).ToArrayAsync());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }

        [Test]
        public async Task Query_NoPaging2()
        {
            var v = await _db.Persons2.Query().ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(5));

            v = await _db.Persons2.Query(q => q.Where(x => x.Name == "Greg")).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(1));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));

            v = await _db.Persons2.Query(q => q.Where(x => x.Name == "GREG")).ToArrayAsync();
            Assert.That(v, Is.Empty);
        }

        [Test]
        public async Task Query_Paging2()
        {
            var pr = new Entities.PagingResult(Entities.PagingArgs.CreateSkipAndTake(1, 2, true));
            var v = await _db.Persons2.Query(q => q.OrderBy(x => x.Id)).WithPaging(pr).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(pr.TotalCount, Is.EqualTo(5));

            v = await _db.Persons2.Query(q => q.OrderBy(x => x.Name)).WithPaging(1, 2).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Name, Is.EqualTo("Mike"));

            var vr = await _db.Persons2.Query(q => q.OrderBy(x => x.Name).Where(x => !x.Locked)).WithPaging(Entities.PagingArgs.CreateSkipAndTake(1, 2, true)).SelectResultAsync<Person2CollectionResult, Person2Collection>();
            Assert.That(vr.Items, Has.Count.EqualTo(2));
            Assert.That(vr.Items[0].Name, Is.EqualTo("Mike"));
            Assert.That(vr.Items[1].Name, Is.EqualTo("Rebecca"));
            Assert.That(vr.Paging, Is.Not.Null);
            Assert.That(vr.Paging.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task Query_Wildcards2()
        {
            var v = await _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "g*").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = await _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "*Y").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = await _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "*e*").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _db.Persons2.Query(q => q.WhereWildcard(x => x.Name, "*m*e")).ToArrayAsync());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }

        [Test]
        public async Task Query_NoPaging3()
        {
            var v = await _db.Persons3.Query().ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(5));

            v = await _db.Persons3.Query(q => q.Where(x => x.Value.Name == "Greg")).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(1));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));

            v = await _db.Persons3.Query(q => q.Where(x => x.Value.Name == "GREG")).ToArrayAsync();
            Assert.That(v, Is.Empty);
        }

        [Test]
        public async Task Query_Paging3()
        {
            var pr = new Entities.PagingResult(Entities.PagingArgs.CreateSkipAndTake(1, 2, true));
            var v = await _db.Persons3.Query(q => q.OrderBy(x => x.Id)).WithPaging(pr).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(pr.TotalCount, Is.EqualTo(5));

            v = await _db.Persons3.Query(q => q.OrderBy(x => x.Value.Name)).WithPaging(1, 2).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Name, Is.EqualTo("Mike"));

            var vr = await _db.Persons3.Query(q => q.OrderBy(x => x.Value.Name).Where(x => !x.Value.Locked)).WithPaging(Entities.PagingArgs.CreateSkipAndTake(1, 2, true)).SelectResultAsync<Person3CollectionResult, Person3Collection>();
            Assert.That(vr.Items, Has.Count.EqualTo(2));
            Assert.That(vr.Items[0].Name, Is.EqualTo("Mike"));
            Assert.That(vr.Items[1].Name, Is.EqualTo("Rebecca"));
            Assert.That(vr.Paging, Is.Not.Null);
            Assert.That(vr.Paging.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task Query_Wildcards3()
        {
            var v = await _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "g*").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));

            v = await _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "*Y").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Name, Is.EqualTo("Sally"));

            v = await _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "*e*").OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(3));
            Assert.That(v[0].Name, Is.EqualTo("Rebecca"));
            Assert.That(v[1].Name, Is.EqualTo("Greg"));
            Assert.That(v[2].Name, Is.EqualTo("Mike"));

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _db.Persons3.Query(q => q.WhereWildcard(x => x.Value.Name, "*m*e")).ToArrayAsync());
            Assert.That(ex.Message, Is.EqualTo("Wildcard selection text is not supported."));
        }

        [Test]
        public async Task ModelQuery_Paging3()
        {
            var pr = new Entities.PagingResult(Entities.PagingArgs.CreateSkipAndTake(1, 2, true));
            var v = await _db.Persons3.ModelContainer.Query(q => q.OrderBy(x => x.Id)).WithPaging(pr).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Value.Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Value.Name, Is.EqualTo("Greg"));
            Assert.That(pr.TotalCount, Is.EqualTo(5));

            v = await _db.Persons3.ModelContainer.Query(q => q.OrderBy(x => x.Value.Name)).WithPaging(1, 2).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Value.Name, Is.EqualTo("Greg"));
            Assert.That(v[1].Value.Name, Is.EqualTo("Mike"));
        }

        [Test]
        public async Task ModelQuery_WithFilter()
        {
            var qac = QueryArgsConfig.Create()
                .WithFilter(f => f
                    .AddField<string>("Name", "Value.Name", c => c.SupportKinds(QueryFilterTokenKind.AllStringOperators).UseUpperCase())
                    .AddField<bool>("Birthday", "Value.Birthday"));

            var v = await _db.Persons3.ModelContainer.Query(q => q.Where(qac, QueryArgs.Create("endswith(name, 'Y')")).OrderBy(x => x.Id)).ToArrayAsync();
            Assert.That(v, Has.Length.EqualTo(2));
            Assert.That(v[0].Value.Name, Is.EqualTo("Gary"));
            Assert.That(v[1].Value.Name, Is.EqualTo("Sally"));
        }
    }
}