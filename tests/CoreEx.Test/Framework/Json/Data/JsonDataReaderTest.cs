using CoreEx.Entities;
using CoreEx.Json.Data;
using CoreEx.RefData;
using CoreEx.Text.Json;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Json.Data
{
    [TestFixture]
    public class JsonDataReaderTest
    {
        private readonly string data =
@"data:
 - Person:
   - { id: ^1, first: Bob, last: Smith, age: 23 }
   - { id: ^2, first: Jenny, last: Browne, age: 51 }
 - Contact:
   - { id: ^1, first: Bob, last: Smith }
   - { id: ^2, first: Jenny, last: Browne }";

        [Test]
        public void Parse_NonJsonObject()
        {
            var ex = Assert.Throws<ArgumentException>(() => JsonDataReader.ParseYaml("apples"));
            Assert.That(ex!.Message, Does.StartWith("JSON root element must be an Object."));
        }

        [Test]
        public void Parse_JsonNoArray()
        {
            var ex = Assert.Throws<ArgumentException>(() => JsonDataReader.ParseJson("{\"fruit\":\"apples\"}"));
            Assert.That(ex!.Message, Does.StartWith("JSON root element must be an Object with an underlying array."));
        }

        [Test]
        public void Deserialize_NotFound()
        {
            var jdr = JsonDataReader.ParseYaml(data);
            Assert.Multiple(() =>
            {
                Assert.That(jdr.TryDeserialize<Person>("Bananas", out var coll), Is.False);
                Assert.That(coll, Is.Null);
            });
        }

        [Test]
        public void Deserialize_Single()
        {
            var jdr = JsonDataReader.ParseYaml(
@"data:
 - Person:
   - { id: ^1, first: Bob, last: Smith, age: 23 }", new JsonDataReaderArgs(null, "XXXX", new DateTime(2000, 01, 01)));

            Assert.That(jdr.TryDeserialize<Person>("Person", out var coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll![0].Id, Is.EqualTo(new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
                Assert.That(coll[0].First, Is.EqualTo("Bob"));
                Assert.That(coll[0].Last, Is.EqualTo("Smith"));
                Assert.That(coll[0].Age, Is.EqualTo(23));
                Assert.That(coll[0].ChangeLog, Is.Not.Null);
            });

            Assert.Multiple(() =>
            {
                Assert.That(coll[0].ChangeLog!.CreatedBy, Is.EqualTo("XXXX"));
                Assert.That(coll[0].ChangeLog!.CreatedDate, Is.EqualTo(new DateTime(2000, 01, 01)));
                Assert.That(coll[0].ChangeLog!.UpdatedBy, Is.Null);
                Assert.That(coll[0].ChangeLog!.UpdatedDate, Is.Null);
            });
        }

        [Test]
        public void Deserialize_Multi()
        {
            var jdr = JsonDataReader.ParseYaml(data);
            Assert.That(jdr.TryDeserialize<Person>("Person", out var coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(2));
                Assert.That(coll![0].Id, Is.EqualTo(new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
                Assert.That(coll[0].First, Is.EqualTo("Bob"));
                Assert.That(coll[0].Last, Is.EqualTo("Smith"));
                Assert.That(coll[0].Age, Is.EqualTo(23));
                Assert.That(coll[1].Id, Is.EqualTo(new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
                Assert.That(coll[1].First, Is.EqualTo("Jenny"));
                Assert.That(coll[1].Last, Is.EqualTo("Browne"));
                Assert.That(coll[1].Age, Is.EqualTo(51));
            });
        }

        [Test]
        public void Deserialize_GenerateIdentifier()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Person\":[{\"first\":\"Bob\"}]}]}");
            Assert.That(jdr.TryDeserialize<Person>("Person", out var coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll![0].First, Is.EqualTo("Bob"));
                Assert.That(coll[0].Id, Is.Not.EqualTo(Guid.Empty));
            });
        }

        [Test]
        public void Deserialize_RuntimeParameter1()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Person\":[{\"first\":\"^(System.Environment.UserName)\"}]}]}");
            Assert.Multiple(() =>
            {
                Assert.That(jdr.TryDeserialize<Person>("Person", out var coll), Is.True);
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll![0].First, Is.EqualTo(System.Environment.UserName));
            });
        }

        [Test]
        public void Deserialize_RuntimeParameter2()
        {
            var args = new JsonDataReaderArgs();
            args.Parameters.Add("fruit", "banana");
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Person\":[{\"first\":\"^(fruit)\"}]}]}", args);
            Assert.Multiple(() =>
            {
                Assert.That(jdr.TryDeserialize<Person>("Person", out var coll), Is.True);
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll![0].First, Is.EqualTo("banana"));
            });
        }

        [Test]
        public void Deserialize_RefData()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Gender\":[{\"F\":\"Female\"},{\"code\":\"M\",\"text\":\"Male\"},{\"code\":\"O\",\"text\":\"Other\",\"isActive\":false,\"sortOrder\":99}]}]}", new JsonDataReaderArgs(new ReferenceDataContentJsonSerializer()));
            Assert.That(jdr.TryDeserialize<Gender>("Gender", out var coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(3));
                Assert.That(coll![0].Id, Is.Not.Null);
                Assert.That(coll[0].Code, Is.EqualTo("F"));
                Assert.That(coll[0].Text, Is.EqualTo("Female"));
                Assert.That(coll[0].SortOrder, Is.EqualTo(1));
                Assert.That(coll[0].IsActive, Is.EqualTo(true));
                Assert.That(coll[1].Code, Is.EqualTo("M"));
                Assert.That(coll[1].Text, Is.EqualTo("Male"));
                Assert.That(coll[1].SortOrder, Is.EqualTo(2));
                Assert.That(coll[1].IsActive, Is.EqualTo(true));
                Assert.That(coll[2].Code, Is.EqualTo("O"));
                Assert.That(coll[2].Text, Is.EqualTo("Other"));
                Assert.That(coll[2].SortOrder, Is.EqualTo(99));
                Assert.That(coll[2].IsActive, Is.EqualTo(false));
            });
        }

        [Test]
        public void Deserialize_RefData2()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Gender\":[{\"F\":\"Female\"},{\"Code\":\"M\",\"Text\":\"Male\"},{\"Code\":\"O\",\"Text\":\"Other\",\"IsActive\":false,\"SortOrder\":99}]}]}", new JsonDataReaderArgs(new ReferenceDataContentJsonSerializer()));
            Assert.That(jdr.TryDeserialize<Gender>("Gender", out var coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(3));
                Assert.That(coll![0].Id, Is.Not.Null);
                Assert.That(coll[0].Code, Is.EqualTo("F"));
                Assert.That(coll[0].Text, Is.EqualTo("Female"));
                Assert.That(coll[0].SortOrder, Is.EqualTo(1));
                Assert.That(coll[0].IsActive, Is.EqualTo(true));
                Assert.That(coll[1].Code, Is.EqualTo("M"));
                Assert.That(coll[1].Text, Is.EqualTo("Male"));
                Assert.That(coll[1].SortOrder, Is.EqualTo(2));
                Assert.That(coll[1].IsActive, Is.EqualTo(true));
                Assert.That(coll[2].Code, Is.EqualTo("O"));
                Assert.That(coll[2].Text, Is.EqualTo("Other"));
                Assert.That(coll[2].SortOrder, Is.EqualTo(99));
                Assert.That(coll[2].IsActive, Is.EqualTo(false));
            });
        }

        [Test]
        public void Deserialize_NumberToStringIdentifier()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Contact\":[{\"id\":123456}]}]}");
            Assert.That(jdr.TryDeserialize<Contact>("Contact", out var coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll![0].Id, Is.EqualTo("123456"));
            });

            jdr = JsonDataReader.ParseYaml(
@"data:
 - Contact:
   - { id: 0123456, first: Bob }");

            Assert.That(jdr.TryDeserialize<Contact>("Contact", out coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll![0].Id, Is.EqualTo("0123456"));
            });

            jdr = JsonDataReader.ParseJson("{\"data\":[{\"Contact\":[{\"id\":123.456}]}]}");
            Assert.That(jdr.TryDeserialize<Contact>("Contact", out coll), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(coll, Is.Not.Null);
                Assert.That(coll!, Has.Count.EqualTo(1));
                Assert.That(coll[0].Id, Is.EqualTo("123.456"));
            });

            jdr = JsonDataReader.ParseJson("{\"data\":[{\"Contact\":[{\"id\":true}]}]}");
            Assert.Throws<System.Text.Json.JsonException>(() => jdr.TryDeserialize<Contact>("Contact", out coll));
        }

        public class Person : IIdentifier<Guid>, IChangeLog
        {
            public Guid Id { get; set; }
            public string? First { get; set; }
            public string? Last { get; set; }
            public int? Age { get; set; }
            public ChangeLog? ChangeLog { get; set; }
        }

        public class Contact : IIdentifier<string>
        {
            public string? Id { get; set; }
            public string? First { get; set; }
        }

        public class Gender : ReferenceDataBase<string> { }
    }
}