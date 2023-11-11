﻿using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.OData;
using NUnit.Framework;
using System;
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
            Assert.IsTrue(response.IsSuccessStatusCode);

            _personUrl = response.RequestMessage!.RequestUri!.ToString();
        }

        public static IMapper GetMapper()
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

        public static ODataClient GetPersonClient(Soc.ODataClientSettings? settings = null)
        {
            settings ??= new Soc.ODataClientSettings();
            settings.BaseUri = new Uri(_personUrl!);
            return new ODataClient(new Soc.ODataClient(settings), GetMapper());
        }

        [Test]
        public async Task A010_Get_NotFound()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "invalid_key", default);
            Assert.IsNull(result.Value);
        }

        [Test]
        public async Task A020_Get_NotFound_IgnoreResourceNotFoundException()
        {
            var odata = GetPersonClient(new Soc.ODataClientSettings { IgnoreResourceNotFoundException = true });
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "invalid_key", default);
            Assert.IsNull(result.Value);
        }

        [Test]
        public async Task A030_Get_Success()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual("russellwhyte", result.Value!.Id);
            Assert.AreEqual("Russell", result.Value.FirstName);
            Assert.AreEqual("Whyte", result.Value.LastName);
        }

        [Test]
        public async Task B010_SelectSingle()
        {
            var odata = GetPersonClient();
            var result = await odata.Query<Person, MPerson>("People", q => q.Filter(p => p.UserName == "russellwhyte")).SelectSingleAsync(default);
            Assert.IsNotNull(result);
            Assert.AreEqual("russellwhyte", result!.Id);
            Assert.AreEqual("Russell", result.FirstName);
            Assert.AreEqual("Whyte", result.LastName);
        }

        [Test]
        public async Task C010_Create_Success()
        {
            var odata = GetPersonClient();
            var result = await odata.CreateWithResultAsync<Person, MPerson>("People", new Person { Id = "bobsmith", FirstName = "Bob", LastName = "Smith" }, default);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual("bobsmith", result.Value!.Id);
            Assert.AreEqual("Bob", result.Value.FirstName);
            Assert.AreEqual("Smith", result.Value.LastName);

            var result2 = await odata.GetWithResultAsync<Person, MPerson>("People", "bobsmith", default);
            Assert.IsNotNull(result2.Value);
            Assert.AreEqual("bobsmith", result2.Value!.Id);
            Assert.AreEqual("Bob", result2.Value.FirstName);
            Assert.AreEqual("Smith", result2.Value.LastName);
        }

        [Test]
        public async Task D010_Update_NotFound()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);

            result.Value!.Id = "russellwhyteeeee";
            result.Value.FirstName = "Russell2";

            var ex = Assert.ThrowsAsync<Soc.WebRequestException>(async () => await odata.UpdateWithResultAsync<Person, MPerson>("People", result.Value, default));
            Assert.AreEqual(HttpStatusCode.InternalServerError, ex!.Code);
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

            Assert.IsTrue(result2.IsFailure);
            Assert.IsInstanceOf<NotFoundException>(result2.Error);
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
            Assert.AreEqual(HttpStatusCode.InternalServerError, ex!.Code);
        }

        [Test]
        public async Task D040_Update_Success()
        {
            var odata = GetPersonClient();
            var result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);

            result.Value!.FirstName = "Russell2";
            var result2 = await odata.UpdateWithResultAsync<Person, MPerson>("People", result.Value, default);
            Assert.IsNotNull(result2.Value);
            Assert.AreEqual("russellwhyte", result2.Value!.Id);
            Assert.AreEqual("Russell2", result2.Value.FirstName);
            Assert.AreEqual("Whyte", result2.Value.LastName);

            result = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual("russellwhyte", result.Value!.Id);
            Assert.AreEqual("Russell2", result.Value.FirstName);
            Assert.AreEqual("Whyte", result.Value.LastName);
        }

        [Test]
        public async Task E010_Delete_PreRead()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnDelete = true };
            var result = await odata.DeleteWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.IsTrue(result.IsSuccess);

            var result2 = await odata.GetWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.IsNull(result2.Value);

            // Idempotent :-)
            result = await odata.DeleteWithResultAsync<Person, MPerson>("People", "russellwhyte", default);
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public async Task E020_Delete_NoPreRead()
        {
            var odata = GetPersonClient();
            odata.Args = new ODataArgs { PreReadOnDelete = false };
            var result = await odata.DeleteWithResultAsync<Person, MPerson>("People", "ronaldmundy", default);
            Assert.IsTrue(result.IsSuccess);

            var result2 = await odata.GetWithResultAsync<Person, MPerson>("People", "ronaldmundy", default);
            Assert.IsNull(result2.Value);

            // Idempotent, but alas :-(
            var ex = Assert.ThrowsAsync<Soc.WebRequestException>(async () => await odata.DeleteWithResultAsync<Person, MPerson>("People", "ronaldmundy", default));
            Assert.AreEqual(HttpStatusCode.InternalServerError, ex!.Code);
        }
    }

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