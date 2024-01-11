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

            Assert.That(dpc, Has.Count.EqualTo(5));
            Assert.Multiple(() =>
            {
                Assert.That(dpc[0].ParameterName, Is.EqualTo("@PersonId"));
                Assert.That(dpc[0].Value, Is.EqualTo(88));
                Assert.That(dpc[1].ParameterName, Is.EqualTo("@firstname"));
                Assert.That(dpc[1].Value, Is.EqualTo("Bob"));
                Assert.That(dpc[2].ParameterName, Is.EqualTo("@Street"));
                Assert.That(dpc[2].Value, Is.EqualTo("sss"));
                Assert.That(dpc[3].ParameterName, Is.EqualTo("@City"));
                Assert.That(dpc[3].Value, Is.EqualTo("ccc"));
                Assert.That(dpc[4].ParameterName, Is.EqualTo("@Address2"));
                Assert.That(dpc[4].Value, Is.EqualTo("{\"street\":\"ttt\",\"city\":\"ddd\"}"));

                Assert.That(pdm[x => x.Name].ColumnName, Is.EqualTo("name"));
            });
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