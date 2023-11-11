using NUnit.Framework;
using CoreEx.Dataverse.Mapping;
using Microsoft.Xrm.Sdk;
using System;
using CoreEx.Entities;
using CoreEx.Mapping.Converters;

namespace CoreEx.Test.Framework.Dataverse.Mapping
{
    [TestFixture]

    internal class DataverseMapperTest
    {
        [Test]
        public void MapToDataverse()
        {
            var p = new Person { Id = 1.ToGuid().ToString(), Name = "Bob", Address = new Address { Street = "sss", City = "ccc" }, Address2 = new Address { Street = "ttt", City = "ddd" } };

            var pdm = new PersonDataverseMapper();
            var entity = new Entity("Person");
            pdm.MapToDataverse(p, entity);

            Assert.AreEqual(1.ToGuid(), entity.Id);
            Assert.AreEqual(0, entity.KeyAttributes.Count);
            Assert.AreEqual(4, entity.Attributes.Count);
            Assert.AreEqual("Bob", entity.GetAttributeValue<string>("name"));
            Assert.AreEqual("sss", entity.GetAttributeValue<string>("Street"));
            Assert.AreEqual("ccc", entity.GetAttributeValue<string>("town"));
            Assert.AreEqual("{\"street\":\"ttt\",\"city\":\"ddd\"}", entity.GetAttributeValue<string>("Address2"));

            //OrganizationRequest req = new OrganizationRequest();
            //PublicClientApplicationBuilder appBuilder = PublicClientApplicationBuilder.Create("clientId");
        }

        [Test]
        public void MapFromDataverse()
        {
            var pdm = new PersonDataverseMapper();
            var entity = new Entity("Person", 1.ToGuid());
            entity["name"] = "Bob";
            entity["Street"] = "sss";
            entity["town"] = "ccc";
            entity["Address2"] = "{\"street\":\"ttt\",\"city\":\"ddd\"}";

            var p = pdm.MapFromDataverse(entity)!;

            Assert.AreEqual(1.ToGuid().ToString(), p.Id);
            Assert.AreEqual("Bob", p.Name);
            Assert.AreEqual("sss", p.Address?.Street);
            Assert.AreEqual("ccc", p.Address?.City);
            Assert.AreEqual("ttt", p.Address2?.Street);
            Assert.AreEqual("ddd", p.Address2?.City);
        }
    }

    public class PersonDataverseMapper : DataverseMapper<Person>
    {
        private readonly DataverseMapper<Address> _addressMapper = DataverseMapper.CreateAuto<Address>("City").HasProperty(x => x.City, "town");

        public PersonDataverseMapper()
        {
            InheritPropertiesFrom(DataverseMapper.CreateAuto<PersonBase>());
            Property(x => x.Name, "name");
            Property(x => x.Address).SetMapper(_addressMapper);
            Property(x => x.Address2).SetConverter(new ObjectToJsonConverter<Address>());
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
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }
}