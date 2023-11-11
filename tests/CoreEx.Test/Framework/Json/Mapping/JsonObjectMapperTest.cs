using NUnit.Framework;
using System;
using CoreEx.Entities;
using CoreEx.Mapping.Converters;
using CoreEx.Json.Mapping;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace CoreEx.Test.Framework.Json.Mapping
{
    [TestFixture]
    internal class JsonObjectMapperTest
    {
        [Test]
        public void MapToJson()
        {
            var p = new Person { Id = 1.ToGuid().ToString(), Name = "Bob", Address = new Address { Street = "sss", City = "ccc" }, Address2 = new Address { Street = "ttt", City = "ddd" }, Other = new DateTime(2000, 01, 01, 02, 58, 34) };

            var pdm = new PersonJsonMapper();
            var json = new JsonObject();
            pdm.MapToJson(p, json);

            Assert.AreEqual("{\"id\":\"00000001-0000-0000-0000-000000000000\",\"nname\":\"Bob\",\"street\":\"sss\",\"town\":\"ccc\",\"address2\":{\"street\":\"ttt\",\"city\":\"ddd\"},\"other\":\"01/01/2000 02:58:34\"}", json.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        }

        [Test]
        public void MapFromJson()
        {
            var json = JsonNode.Parse("{\"id\":\"00000001-0000-0000-0000-000000000000\",\"nname\":\"Bob\",\"street\":\"sss\",\"town\":\"ccc\",\"address2\":{\"street\":\"ttt\",\"city\":\"ddd\"},\"other\":\"01/01/2000 02:58:34\"}")!;

            var pdm = new PersonJsonMapper();
            var p = pdm.MapFromJson(json.AsObject())!;

            Assert.AreEqual(1.ToGuid().ToString(), p.Id);
            Assert.AreEqual("Bob", p.Name);
            Assert.AreEqual("sss", p.Address?.Street);
            Assert.AreEqual("ccc", p.Address?.City);
            Assert.AreEqual("ttt", p.Address2?.Street);
            Assert.AreEqual("ddd", p.Address2?.City);
            Assert.AreEqual(new DateTime(2000, 01, 01, 02, 58, 34), p.Other);
        }
    }

    public class PersonJsonMapper : JsonObjectMapper<Person>
    {
        private readonly JsonObjectMapper<Address> _addressMapper = JsonObjectMapper.CreateAuto<Address>("City").HasProperty(x => x.City, "town");

        public PersonJsonMapper()
        {
            InheritPropertiesFrom(JsonObjectMapper.CreateAuto<PersonBase>());
            Property(x => x.Name, "nname");
            Property(x => x.Address).SetMapper(_addressMapper);
            Property(x => x.Address2);
            Property(x => x.Other).SetConverter(new DateTimeToStringConverter());
        }
    }

    public class PersonBase : IIdentifier<string>
    {
        public string? Id { get; set; }
    }

    public class Person : PersonBase
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
        public Address? Address2 { get; set; }
        public DateTime? Other { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }
}