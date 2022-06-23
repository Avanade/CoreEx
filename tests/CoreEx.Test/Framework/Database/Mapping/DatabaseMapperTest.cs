using CoreEx.Database.Mapping;
using CoreEx.Database.SqlServer;
using CoreEx.Mapping.Converters;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Database.Mapping
{
    [TestFixture]
    internal class DatabaseMapperTest
    {
        [Test]
        public void MapToDb()
        {
            var p = new Person { Id = 88, Name = "Bob", Address = new Address { Street = "sss", City = "ccc" }, Address2 = new Address { Street = "ttt", City = "ddd" } };

            var pdm = new PersonDatabaseMapper();
            var dpc = new CoreEx.Database.DatabaseParameterCollection(new SqlServerDatabase(() => null!));
            pdm.MapToDb(p, dpc);

            Assert.AreEqual(5, dpc.Count);
            Assert.AreEqual("@PersonId", dpc[0].ParameterName);
            Assert.AreEqual(88, dpc[0].Value);
            Assert.AreEqual("@firstname", dpc[1].ParameterName);
            Assert.AreEqual("Bob", dpc[1].Value);
            Assert.AreEqual("@Street", dpc[2].ParameterName);
            Assert.AreEqual("sss", dpc[2].Value);
            Assert.AreEqual("@City", dpc[3].ParameterName);
            Assert.AreEqual("ccc", dpc[3].Value);
            Assert.AreEqual("@Address2", dpc[4].ParameterName);
            Assert.AreEqual("{\"street\":\"ttt\",\"city\":\"ddd\"}", dpc[4].Value);

            Assert.AreEqual("name", pdm[x => x.Name].ColumnName);
        }
    }

    public class PersonDatabaseMapper : DatabaseMapper<Person>
    {
        private readonly DatabaseMapper<Address> _addressMapper = DatabaseMapper.CreateAuto<Address>("City")
            .HasProperty(x => x.City);

        public PersonDatabaseMapper()
        {
            InheritPropertiesFrom(DatabaseMapper.CreateAuto<PersonBase>().HasProperty(x => x.Id, null, "@PersonId"));
            Property(x => x.Name, "name", "@firstname");
            Property(x => x.Address).SetMapper(_addressMapper);
            Property(x => x.Address2).SetConverter(new ObjectToJsonConverter<Address>());
        }
    }

    public class PersonBase
    {
        public int Id { get; set; }
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