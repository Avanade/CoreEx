using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.OData;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.Test.Framework.ODatax
{
    [TestFixture]
    internal class ODataTest
    {
        private static string? _personUrl; //https://www.odata.org/odata-services/service-usages/request-key-tutorial/

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            using var hc = new HttpClient();
            var response = hc.GetAsync("https://services.odata.org/TripPinRESTierService/").Result;
            Assert.That(response.IsSuccessStatusCode, Is.True);

            _personUrl = response.RequestMessage!.RequestUri!.ToString();
        }

        internal static IMapper GetMapper()
        {
            var mapper = new Mapper();
            mapper.Register(new Mapper<Product, MProduct>()
                .Map((s, d) => d.ID = s.Id)
                .Map((s, d) => d.Name = s.Name)
                .Map((s, d) => d.Description = s.Description));

            mapper.Register(new Mapper<MProduct, Product>()
                .Map((s, d) => d.Id = s.ID)
                .Map((s, d) => d.Name = s.Name)
                .Map((s, d) => d.Description = s.Description));

            mapper.Register(new Mapper<Person, MPerson>()
                .Map((s, d) => d.UserName = s.Id)
                .Map((s, d) => d.FirstName = s.FirstName)
                .Map((s, d) => d.LastName = s.LastName));

            mapper.Register(new Mapper<MPerson, Person>()
                .Map((s, d) => d.Id = s.UserName)
                .Map((s, d) => d.FirstName = s.FirstName)
                .Map((s, d) => d.LastName = s.LastName));

            return mapper;
        }

        internal static ODataClient GetPersonClient(Soc.ODataClientSettings? settings = null)
        {
            settings ??= new Soc.ODataClientSettings();
            settings.BaseUri = new Uri(_personUrl!);
            settings.BeforeRequest = r => Console.WriteLine($"{r.Method} {r.RequestUri}");
            return new ODataClient(new Soc.ODataClient(settings), GetMapper());
        }

        [Test]
        public async Task A010_Get_NotFound()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "invalid_key", default);
            Assert.That(result.Value, Is.Null);
        }

        [Test]
        public async Task A020_Get_NotFound_IgnoreResourceNotFoundException()
        {
            var odata = GetPersonClient(new Soc.ODataClientSettings { IgnoreResourceNotFoundException = true });
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "invalid_key", default);
            Assert.That(result.Value, Is.Null);
        }

        [Test]
        public async Task A030_Get_Success()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.That(result.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result.Value.FirstName, Is.EqualTo("Russell"));
                Assert.That(result.Value.LastName, Is.EqualTo("Whyte"));
            });
        }

        [Test]
        public async Task B010_SelectSingle()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.UserName == "russellwhyte")).SelectSingleAsync(default);
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result.FirstName, Is.EqualTo("Russell"));
                Assert.That(result.LastName, Is.EqualTo("Whyte"));
            });

            var result2 = (await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.UserName == "russellwhyte")).SelectSingleWithResultAsync(default)).Value;
            Assert.That(result2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result2!.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result2.FirstName, Is.EqualTo("Russell"));
                Assert.That(result2.LastName, Is.EqualTo("Whyte"));
            });
        }

        [Test]
        public async Task B020_SelectFirstOrDefault()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.FirstName == "Russell")).SelectFirstAsync(default);
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result.FirstName, Is.EqualTo("Russell"));
                Assert.That(result.LastName, Is.EqualTo("Whyte"));
            });

            var result2 = (await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.FirstName == "Russell")).SelectFirstWithResultAsync(default)).Value;
            Assert.That(result2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result2!.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result2.FirstName, Is.EqualTo("Russell"));
                Assert.That(result2.LastName, Is.EqualTo("Whyte"));
            });

            var result3 = await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.FirstName == "does-not-exist")).SelectFirstOrDefaultAsync(default);
            Assert.That(result3, Is.Null);

            var result4 = (await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.FirstName == "does-not-exist")).SelectFirstOrDefaultWithResultAsync(default)).Value;
            Assert.That(result4, Is.Null);
        }

        [Test]
        public async Task B030_SelectQueryWithResultAsync()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People").WithPaging(2, 3).SelectQueryWithResultAsync<PersonCollection>(default);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Has.Count.EqualTo(3));
                Assert.That(result.Value.Select(x => x.FirstName).ToArray(), Is.EqualTo(new string[] { "Elaine", "Genevieve", "Georgina" }));
            });
        }

        [Test]
        public async Task B030_SelectResultWithResultAsync()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People").WithPaging(PagingArgs.CreateSkipAndTake(2, 3, true)).SelectResultWithResultAsync<PersonCollectionResult, PersonCollection>(default);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.Items, Has.Count.EqualTo(3));
                Assert.That(result.Value.Items.Select(x => x.FirstName).ToArray(), Is.EqualTo(new string[] { "Elaine", "Genevieve", "Georgina" }));
                Assert.That(result.Value.Paging!.TotalCount, Is.EqualTo(20));
            });
        }

        [Test]
        public async Task B040_SelectResultWithResultAsync_WildCards()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People", q => q.FilterWildcard(x => x.FirstName, "*s*")).WithPaging(PagingArgs.CreateSkipAndTake(2, 3, true)).SelectResultWithResultAsync<PersonCollectionResult, PersonCollection>(default);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.Items, Has.Count.EqualTo(3));
                Assert.That(result.Value.Items.Select(x => x.FirstName).ToArray(), Is.EqualTo(new string[] { "Russell", "Sallie", "Sandy" }));
                Assert.That(result.Value.Paging!.TotalCount, Is.EqualTo(7));
            });

            // Weird how Scott comes last as it is first when no paging is requested, but that is what is returned from the service <shrug_emoji/>.
            result = await odata.Query<Person, MPerson>("People", q => q.FilterWildcard(x => x.FirstName, "s*")).WithPaging(PagingArgs.CreateSkipAndTake(2, 3, true)).SelectResultWithResultAsync<PersonCollectionResult, PersonCollection>(default);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.Items, Has.Count.EqualTo(1));
                Assert.That(result.Value.Items.Select(x => x.FirstName).ToArray(), Is.EqualTo(new string[] { "Scott" }));
                Assert.That(result.Value.Paging!.TotalCount, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task B050_SelectQuery()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People", q => q.FilterWith((string)null!, x => x.FirstName == "Scott")).SelectQueryAsync<PersonCollection>();
            Assert.That(result, Has.Count.EqualTo(20));

            result = await odata.Query<Person, MPerson>("People", q => q.FilterWith("abc", x => x.FirstName == "Scott")).SelectQueryAsync<PersonCollection>();
            Assert.That(result, Has.Count.EqualTo(1));

            odata = GetPersonClient();
            result = await odata.Query<Person, MPerson>("People", q => q.FilterWith((string)null!, x => x.FirstName == "Scott")).SelectQueryAsync<PersonCollection>();
            Assert.That(result, Has.Count.EqualTo(20));

            result = await odata.Query<Person, MPerson>("People", q => q.FilterWith("abc", x => x.FirstName == "Scott")).SelectQueryAsync<PersonCollection>();
            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task C010_Create_Success()
        {
            var odata = GetPersonClient();
            var result = await odata.CreateWithResultAsync<Person, MPerson>("People", new Person { Id = "bobsmith", FirstName = "Bob", LastName = "Smith" }, default);
            Assert.That(result.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.Id, Is.EqualTo("bobsmith"));
                Assert.That(result.Value.FirstName, Is.EqualTo("Bob"));
                Assert.That(result.Value.LastName, Is.EqualTo("Smith"));
            });

            var result2 = await odata.GetWithResultAsync<Person, MPerson>("People", "bobsmith", default);
            Assert.That(result2.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result2.Value.Id, Is.EqualTo("bobsmith"));
                Assert.That(result2.Value.FirstName, Is.EqualTo("Bob"));
                Assert.That(result2.Value.LastName, Is.EqualTo("Smith"));
            });
        }

        [Test]
        public async Task D010_Update_NotFound()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnUpdate = false };
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);

            result.Value!.Id = "russellwhyteeeee";
            result.Value.FirstName = "Russell2";

            var ex = Assert.ThrowsAsync<Soc.WebRequestException>(async () => await odata.UpdateWithResultAsync<Person, MPerson>("People", result.Value, default));
            Assert.That(ex!.Code, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task D020_Update_NotFound_PreRead()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnUpdate = true };
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);

            result.Value!.Id = "russellwhyteeeee";
            result.Value.FirstName = "Russell2";
            var result2 = await odata.UpdateWithResultAsync<Person, MPerson>("People", result.Value, default);

            Assert.Multiple(() =>
            {
                Assert.That(result2.IsFailure, Is.True);
                Assert.That(result2.Error, Is.InstanceOf<NotFoundException>());
            });
        }

        [Test]
        public async Task D030_Update_NotFound_NoPreRead()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnUpdate = false };
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);

            result.Value!.Id = "russellwhyteeeee";
            result.Value.FirstName = "Russell2";

            var ex = Assert.ThrowsAsync<Soc.WebRequestException>(async () => await odata.UpdateWithResultAsync<Person, MPerson>("People", result.Value, default));
            Assert.That(ex!.Code, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task D040_Update_Success()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);

            result.Value!.FirstName = "Russell2";
            var result2 = await odata.UpdateWithResultAsync<Person, MPerson>("People", result.Value, default);
            Assert.That(result2.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result2.Value!.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result2.Value.FirstName, Is.EqualTo("Russell2"));
                Assert.That(result2.Value.LastName, Is.EqualTo("Whyte"));
            });

            result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.That(result.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value!.Id, Is.EqualTo("russellwhyte"));
                Assert.That(result.Value.FirstName, Is.EqualTo("Russell2"));
                Assert.That(result.Value.LastName, Is.EqualTo("Whyte"));
            });
        }

        [Test]
        public async Task E010_Delete_PreRead()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnDelete = true };
            var result = await odata.DeleteWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.That(result.IsSuccess, Is.True);

            var result2 = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.That(result2.Value, Is.Null);

            // Pre-read will determine not found :-)
            result = await odata.DeleteWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.Multiple(() =>
            {
                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error, Is.InstanceOf<NotFoundException>());
            });
        }

        [Test]
        public async Task E020_Delete_NoPreRead()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnDelete = false };
            var result = await odata.DeleteWithResultAsync<Person, MPerson>("People", "ronaldmundy", default);
            Assert.That(result.IsSuccess, Is.True);

            var result2 = await odata.GetWithResultAsync<Person, MPerson>("People", "ronaldmundy", default);
            Assert.That(result2.Value, Is.Null);

            // Alas, arguably this endpoint should return not found :-(
            var ex = Assert.ThrowsAsync<Soc.WebRequestException>(async () => await odata.DeleteWithResultAsync<Person, MPerson>("People", "ronaldmundy", default));
            Assert.That(ex!.Code, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task F010_Collection_CRD()
        {
            var odata = GetPersonClient();
            var ocoll = odata.CreateItemCollection("People", new PersonMapper());
            var result = await ocoll.CreateWithResultAsync(new Person { Id = "barbsmith", FirstName = "Barbara", LastName = "Smith" });
            Assert.That(result.IsSuccess, Is.True);

            var result2 = await ocoll.GetWithResultAsync("barbsmith");
            Assert.That(result2.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result2.Value.Id, Is.EqualTo("barbsmith"));
                Assert.That(result2.Value.FirstName, Is.EqualTo("Barbara"));
                Assert.That(result2.Value.LastName, Is.EqualTo("Smith"));
            });

            result2 = await ocoll.GetWithResultAsync("barbsmith");
            Assert.That(result2.Value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result2.Value.Id, Is.EqualTo("barbsmith"));
                Assert.That(result2.Value.FirstName, Is.EqualTo("Barbara"));
                Assert.That(result2.Value.LastName, Is.EqualTo("Smith"));
            });

            var result3 = await ocoll.DeleteWithResultAsync("barbsmith");
            Assert.That(result3.IsSuccess, Is.True);

            result2 = await ocoll.GetWithResultAsync("barbsmith");
            Assert.That(result2.Value, Is.Null);
        }
    }

    public class PersonCollectionResult : CollectionResult<PersonCollection, Person> { }

    public class PersonCollection : System.Collections.Generic.List<Person> { }

    public class Person : IIdentifier<string>
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class MPerson
    {
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class PersonMapper : OData.Mapping.ODataMapper<Person>
    {
        public PersonMapper()
        {
            Map(x => x.Id, "UserName").SetPrimaryKey();
            Map(x => x.FirstName);
            Map(x => x.LastName);
        }
    }

    public class Product : IIdentifier<int>
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }
    }

    public class MProduct
    {
        public int ID { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }
    }
}