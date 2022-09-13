using CoreEx.Entities;
using CoreEx.Json.Data;
using CoreEx.RefData.Models;
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
            Assert.IsTrue(ex!.Message.StartsWith("JSON root element must be an Object."));
        }

        [Test]
        public void Parse_JsonNoArray()
        {
            var ex = Assert.Throws<ArgumentException>(() => JsonDataReader.ParseJson("{\"fruit\":\"apples\"}"));
            Assert.IsTrue(ex!.Message.StartsWith("JSON root element must be an Object with an underlying array."));
        }

        [Test]
        public void Deserialize_NotFound()
        {
            var jdr = JsonDataReader.ParseYaml(data);
            Assert.IsFalse(jdr.TryDeserialize<Person>("Bananas", out var coll));
            Assert.IsNull(coll);
        }

        [Test]
        public void Deserialize_Single()
        {
            var jdr = JsonDataReader.ParseYaml(
@"data:
 - Person:
   - { id: ^1, first: Bob, last: Smith, age: 23 }", new JsonDataReaderArgs(null, "XXXX", new DateTime(2000, 01, 01)));

            Assert.IsTrue(jdr.TryDeserialize<Person>("Person", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual(new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), coll[0].Id);
            Assert.AreEqual("Bob", coll[0].First);
            Assert.AreEqual("Smith", coll[0].Last);
            Assert.AreEqual(23, coll[0].Age);

            Assert.IsNotNull(coll[0].ChangeLog);
            Assert.AreEqual("XXXX", coll[0].ChangeLog!.CreatedBy);
            Assert.AreEqual(new DateTime(2000, 01, 01), coll[0].ChangeLog!.CreatedDate);
            Assert.IsNull(coll[0].ChangeLog!.UpdatedBy);
            Assert.IsNull(coll[0].ChangeLog!.UpdatedDate);
        }

        [Test]
        public void Deserialize_Multi()
        {
            var jdr = JsonDataReader.ParseYaml(data);
            Assert.IsTrue(jdr.TryDeserialize<Person>("Person", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(2, coll!.Count);
            Assert.AreEqual(new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), coll[0].Id);
            Assert.AreEqual("Bob", coll[0].First);
            Assert.AreEqual("Smith", coll[0].Last);
            Assert.AreEqual(23, coll[0].Age);
            Assert.AreEqual(new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), coll[1].Id);
            Assert.AreEqual("Jenny", coll[1].First);
            Assert.AreEqual("Browne", coll[1].Last);
            Assert.AreEqual(51, coll[1].Age);
        }

        [Test]
        public void Deserialize_GenerateIdentifier()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Person\":[{\"first\":\"Bob\"}]}]}");
            Assert.IsTrue(jdr.TryDeserialize<Person>("Person", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual("Bob", coll[0].First);
            Assert.AreNotEqual(Guid.Empty, coll[0].Id);
        }

        [Test]
        public void Deserialize_RuntimeParameter1()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Person\":[{\"first\":\"^(System.Environment.UserName)\"}]}]}");
            Assert.IsTrue(jdr.TryDeserialize<Person>("Person", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual(System.Environment.UserName, coll[0].First);
        }

        [Test]
        public void Deserialize_RuntimeParameter2()
        {
            var args = new JsonDataReaderArgs();
            args.Parameters.Add("fruit", "banana");
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Person\":[{\"first\":\"^(fruit)\"}]}]}", args);
            Assert.IsTrue(jdr.TryDeserialize<Person>("Person", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual("banana", coll[0].First);
        }

        [Test]
        public void Deserialize_RefData()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Gender\":[{\"F\":\"Female\"},{\"code\":\"M\",\"text\":\"Male\"},{\"code\":\"O\",\"text\":\"Other\",\"isActive\":false,\"sortOrder\":99}]}]}", new JsonDataReaderArgs(new ReferenceDataContentJsonSerializer()));
            Assert.IsTrue(jdr.TryDeserialize<Gender>("Gender", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(3, coll!.Count);
            Assert.IsNotNull(coll[0].Id);
            Assert.AreEqual("F", coll[0].Code);
            Assert.AreEqual("Female", coll[0].Text);
            Assert.AreEqual(1, coll[0].SortOrder);
            Assert.AreEqual(true, coll[0].IsActive);
            Assert.AreEqual("M", coll[1].Code);
            Assert.AreEqual("Male", coll[1].Text);
            Assert.AreEqual(2, coll[1].SortOrder);
            Assert.AreEqual(true, coll[1].IsActive);
            Assert.AreEqual("O", coll[2].Code);
            Assert.AreEqual("Other", coll[2].Text);
            Assert.AreEqual(99, coll[2].SortOrder);
            Assert.AreEqual(false, coll[2].IsActive);
        }

        [Test]
        public void Deserialize_NumberToStringIdentifier()
        {
            var jdr = JsonDataReader.ParseJson("{\"data\":[{\"Contact\":[{\"id\":123456}]}]}");
            Assert.IsTrue(jdr.TryDeserialize<Contact>("Contact", out var coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual("123456", coll[0].Id);

            jdr = JsonDataReader.ParseYaml(
@"data:
 - Contact:
   - { id: 0123456, first: Bob }");

            Assert.IsTrue(jdr.TryDeserialize<Contact>("Contact", out coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual("0123456", coll[0].Id);

            jdr = JsonDataReader.ParseJson("{\"data\":[{\"Contact\":[{\"id\":123.456}]}]}");
            Assert.IsTrue(jdr.TryDeserialize<Contact>("Contact", out coll));
            Assert.IsNotNull(coll);
            Assert.AreEqual(1, coll!.Count);
            Assert.AreEqual("123.456", coll[0].Id);

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