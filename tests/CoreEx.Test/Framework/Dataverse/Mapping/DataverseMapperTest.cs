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

            Assert.Multiple(() =>
            {
                Assert.That(entity.Id, Is.EqualTo(1.ToGuid()));
                Assert.That(entity.KeyAttributes, Is.Empty);
                Assert.That(entity.Attributes, Has.Count.EqualTo(4));
                Assert.That(entity.GetAttributeValue<string>("name"), Is.EqualTo("Bob"));
                Assert.That(entity.GetAttributeValue<string>("Street"), Is.EqualTo("sss"));
                Assert.That(entity.GetAttributeValue<string>("town"), Is.EqualTo("ccc"));
                Assert.That(entity.GetAttributeValue<string>("Address2"), Is.EqualTo("{\"street\":\"ttt\",\"city\":\"ddd\"}"));
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(p.Id, Is.EqualTo(1.ToGuid().ToString()));
                Assert.That(p.Name, Is.EqualTo("Bob"));
                Assert.That(p.Address?.Street, Is.EqualTo("sss"));
                Assert.That(p.Address?.City, Is.EqualTo("ccc"));
                Assert.That(p.Address2?.Street, Is.EqualTo("ttt"));
                Assert.That(p.Address2?.City, Is.EqualTo("ddd"));
            });
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