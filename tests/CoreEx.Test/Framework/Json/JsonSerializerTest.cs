using CoreEx.Entities;
using CoreEx.Json;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Nsj = Newtonsoft.Json;
using Stj = System.Text.Json;

namespace CoreEx.Test.Framework.Json
{
    [TestFixture]
    public class JsonSerializerTest
    {
        #region SystemTextJson

        [Test]
        public void SystemTextJson_Serialize_Deserialize()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X", ETag = "xxx" };
            var json = js.Serialize(p);
            Assert.AreEqual(json, "{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99,\"etag\":\"xxx\"}");

            p = js.Deserialize<BackendProduct>(json);
            Assert.NotNull(p);
            Assert.AreEqual("A", p!.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);
            Assert.AreEqual("xxx", p.ETag);

            p = (BackendProduct)js.Deserialize(json, typeof(BackendProduct))!;
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);
            Assert.AreEqual("xxx", p.ETag);

            json = js.Serialize(p, JsonWriteFormat.Indented);
            json.Should().Be("{\n  \"code\": \"A\",\n  \"DESCRIPTION\": \"B\",\n  \"retailPrice\": 1.99,\n  \"etag\": \"xxx\"\n}"
                .Replace("\n", Environment.NewLine), because: "Line breaks should be preserved in indented JSON with different line endings on Linux and Windows");
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize_BinaryData()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var bs = js.SerializeToBinaryData(p);

            p = js.Deserialize<BackendProduct>(bs);
            Assert.NotNull(p);
            Assert.AreEqual("A", p!.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            p = (BackendProduct)js.Deserialize(bs, typeof(BackendProduct))!;
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize_Dynamic()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var json = js.Serialize(p);

            var o = js.Deserialize(json);
            Assert.NotNull(o);
            Assert.IsInstanceOf<System.Text.Json.JsonElement>(o);
            Assert.AreEqual(o!.ToString(), "{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}");
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize_Dynamic_BinaryData()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var bs = js.SerializeToBinaryData(p);

            var o = js.Deserialize(bs);
            Assert.NotNull(o);
            Assert.IsInstanceOf<System.Text.Json.JsonElement>(o);
            Assert.AreEqual(o!.ToString(), "{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}");
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonString()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out string json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(json, "{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(json, "{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(json, "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(json, "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonObject()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out object json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");

            p.Address = new Address { Street = "One", City = "First" };
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "address" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"address\":{\"street\":\"One\",\"city\":\"First\"}}");
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize_Exceptions()
        {
            // Arrange
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Exception realException;

            try { throw new Exception("Test"); }
            catch (Exception ex) { realException = ex; }

            // Act
            var serialized = js.Serialize(realException);
            var deserialized = js.Deserialize<Exception>(serialized)!;

            // Assert
            deserialized.Data.Should().BeEquivalentTo(realException.Data);
            deserialized.Message.Should().BeEquivalentTo(realException.Message, because: "Custom converter only handles Message on deserialization");
        }

        [Test]
        public void SystemTextJson_TryGetJsonName()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var t = typeof(Other);

            Assert.IsTrue(js.TryGetJsonName(t.GetProperty(nameof(Other.FirstName))!, out string? jn));
            Assert.AreEqual("first-name", jn);

            Assert.IsFalse(js.TryGetJsonName(t.GetProperty(nameof(Other.MiddleName))!, out jn));
            Assert.AreEqual(null, jn);

            Assert.IsTrue(js.TryGetJsonName(t.GetProperty(nameof(Other.LastName))!, out jn));
            Assert.AreEqual("lastName", jn);
        }

        [Test]
        public void SystemTextJson_Serialize_DictionaryKeys()
        {
            var d = new Dictionary<string, string> { { "ABC", "XXX" }, { "Efg", "Xxx" }, { "hij", "xxx" }, { "AbEfg", "xxXxx" }, { "ETag", "ETAG" } };
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var json = js.Serialize(d);
            Assert.AreEqual("{\"abc\":\"XXX\",\"efg\":\"Xxx\",\"hij\":\"xxx\",\"abEfg\":\"xxXxx\",\"etag\":\"ETAG\"}", json);
        }

        [Test]
        public void SystemTextJson_Serialize_CollectionResult()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;

            // Null object.
            var pcr = (PersonCollectionResult?)null;

            var json = js.Serialize(pcr);
            Assert.AreEqual("null", json);

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.IsNull(pcr);

            // Empty collection.
            pcr = new PersonCollectionResult();

            json = js.Serialize(pcr);
            Assert.AreEqual("[]", json);

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.IsNotNull(pcr);
            Assert.IsNotNull(pcr!.Items);
            Assert.AreEqual(0, pcr.Items.Count);
            Assert.IsNull(pcr.Paging);

            // Items in collection.
            pcr.Items.Add(new Person { FirstName = "Jane" });
            pcr.Items.Add(new Person { FirstName = "John" });

            json = js.Serialize(pcr);
            Assert.AreEqual("[{\"firstName\":\"Jane\"},{\"firstName\":\"John\"}]", json);

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.IsNotNull(pcr);
            Assert.IsNotNull(pcr!.Items);
            Assert.AreEqual(2, pcr.Items.Count);
            Assert.IsNull(pcr.Paging);
            Assert.AreEqual("Jane", pcr.Items[0].FirstName);
            Assert.AreEqual("John", pcr.Items[1].FirstName);
        }

        #endregion

        #region NewtonsoftJson

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X", ETag = "xxx" };
            var json = js.Serialize(p);
            Assert.AreEqual(json, "{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99,\"etag\":\"xxx\"}");

            p = js.Deserialize<BackendProduct>(json);
            Assert.NotNull(p);
            Assert.AreEqual("A", p!.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);
            Assert.AreEqual("xxx", p.ETag);

            p = (BackendProduct)js.Deserialize(json, typeof(BackendProduct))!;
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);
            Assert.AreEqual("xxx", p.ETag);

            json = js.Serialize(p, JsonWriteFormat.Indented);
            json.Should().Be("{\n  \"code\": \"A\",\n  \"DESCRIPTION\": \"B\",\n  \"retailPrice\": 1.99,\n  \"etag\": \"xxx\"\n}"
                .Replace("\n", Environment.NewLine), because: "Line breaks should be preserved in indented JSON with different line endings on Linux and Windows");
        }

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize_BinaryData()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var bs = js.SerializeToBinaryData(p);

            p = js.Deserialize<BackendProduct>(bs);
            Assert.NotNull(p);
            Assert.AreEqual("A", p!.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            p = (BackendProduct)js.Deserialize(bs, typeof(BackendProduct))!;
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);
        }

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize_Dynamic()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var json = js.Serialize(p);

            var o = js.Deserialize(json);
            Assert.NotNull(o);
            Assert.IsInstanceOf<Nsj.Linq.JObject>(o);
            Assert.AreEqual(((Nsj.Linq.JObject)o!).ToString(Nsj.Formatting.None), "{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}");
        }

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize_Dynamic_BinaryData()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var bs = js.SerializeToBinaryData(p);

            var o = js.Deserialize(bs);
            Assert.NotNull(o);
            Assert.IsInstanceOf<Nsj.Linq.JObject>(o);
            Assert.AreEqual(((Nsj.Linq.JObject)o!).ToString(Nsj.Formatting.None), "{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}");
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonString()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out string json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(json, "{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(json, "{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(json, "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(json, "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonObject()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out object json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"firstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}");

            p.Address = new Address { Street = "One", City = "First" };
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "address" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"address\":{\"street\":\"One\",\"city\":\"First\"}}");
        }

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize_Exceptions()
        {
            // Arrange
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Exception realException;

            try { throw new Exception("Test"); }
            catch (Exception ex) { realException = ex; }

            // Act
            var serialized = js.Serialize(realException);
            var deserialized = js.Deserialize<Exception>(serialized)!;

            // Assert
            deserialized.Data.Should().BeEquivalentTo(realException.Data);
            deserialized.Message.Should().BeEquivalentTo(realException.Message);
            deserialized.StackTrace.Should().BeEquivalentTo(realException.StackTrace);
        }

        [Test]
        public void NewtonsoftJson_TryGetJsonName()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var t = typeof(Other);

            Assert.IsTrue(js.TryGetJsonName(t.GetProperty(nameof(Other.FirstName))!, out string? jn));
            Assert.AreEqual("first_name", jn);

            Assert.IsTrue(js.TryGetJsonName(t.GetProperty(nameof(Other.MiddleName))!, out jn));
            Assert.AreEqual("middleName", jn);

            Assert.IsFalse(js.TryGetJsonName(t.GetProperty(nameof(Other.LastName))!, out jn));
            Assert.AreEqual(null, jn);

            // Verify JsonObject usage.
            t = typeof(Other2);
            Assert.IsTrue(js.TryGetJsonName(t.GetProperty(nameof(Other2.FirstName))!, out jn));
            Assert.AreEqual("firstName", jn);

            Assert.IsFalse(js.TryGetJsonName(t.GetProperty(nameof(Other2.LastName))!, out jn));
            Assert.AreEqual(null, jn);

            // Verify ContractResolver STJ marked-up objects.
            t = typeof(CoreEx.WebApis.ExtendedContentResult);
            Assert.IsFalse(js.TryGetJsonName(t.GetProperty(nameof(CoreEx.WebApis.ExtendedContentResult.AfterExtension))!, out jn));
            Assert.AreEqual(null, jn);

            t = typeof(CoreEx.Entities.ChangeLog);
            Assert.IsTrue(js.TryGetJsonName(t.GetProperty(nameof(CoreEx.Entities.ChangeLog.CreatedBy))!, out jn));
            Assert.AreEqual("createdBy", jn);
        }

        [Test]
        public void NewtonsoftJson_Serialize_DictionaryKeys()
        {
            var d = new Dictionary<string, string> { { "ABC", "XXX" }, { "Efg", "Xxx" }, { "hij", "xxx" }, { "AbEfg", "xxXxx" }, { "ETag", "ETAG" } };
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var json = js.Serialize(d);
            Assert.AreEqual("{\"abc\":\"XXX\",\"efg\":\"Xxx\",\"hij\":\"xxx\",\"abEfg\":\"xxXxx\",\"etag\":\"ETAG\"}", json);
        }

        [Test]
        public void NewtonsoftJson_Serialize_CollectionResult()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;

            // Null object.
            var pcr = (PersonCollectionResult?)null;

            var json = js.Serialize(pcr);
            Assert.AreEqual("null", json);

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.IsNull(pcr);

            // Empty collection.
            pcr = new PersonCollectionResult();

            json = js.Serialize(pcr);
            Assert.AreEqual("[]", json);

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.IsNotNull(pcr);
            Assert.IsNotNull(pcr!.Items);
            Assert.AreEqual(0, pcr.Items.Count);
            Assert.IsNull(pcr.Paging);

            // Items in collection.
            pcr.Items.Add(new Person { FirstName = "Jane" });
            pcr.Items.Add(new Person { FirstName = "John" });

            json = js.Serialize(pcr);
            Assert.AreEqual("[{\"firstName\":\"Jane\"},{\"firstName\":\"John\"}]", json);

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.IsNotNull(pcr);
            Assert.IsNotNull(pcr!.Items);
            Assert.AreEqual(2, pcr.Items.Count);
            Assert.IsNull(pcr.Paging);
            Assert.AreEqual("Jane", pcr.Items[0].FirstName);
            Assert.AreEqual("John", pcr.Items[1].FirstName);
        }

        #endregion

        public class Person
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public List<Address>? Addresses { get; set; }
            public string? SSN { get; set; }
            public decimal NetWorth { get; set; }
            public bool? IsAwesome { get; set; }
            public Address? Address { get; set; }
        }

        public class Address
        {
            public string? Street { get; set; }
            public string? City { get; set; }
        }

        public class PersonCollection : List<Person> { }

        public class PersonCollectionResult : CollectionResult<PersonCollection, Person> { }

        public class Other
        {
            [Stj.Serialization.JsonPropertyName("first-name")]
            [Nsj.JsonProperty("first_name")]
            public string? FirstName { get; set; }
            [Stj.Serialization.JsonIgnore]
            public string? MiddleName { get; set; }
            [Nsj.JsonIgnore]
            public string? LastName { get; set; }
        }

        [Nsj.JsonObject(MemberSerialization = Nsj.MemberSerialization.OptIn)]
        public class Other2
        {
            [Nsj.JsonProperty("firstName")]
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
        }

        public class BackendProduct
        {
            [Nsj.JsonProperty("code")]
            [Stj.Serialization.JsonPropertyName("code")]
            public string? Code { get; set; }

            [Nsj.JsonProperty("DESCRIPTION")]
            [Stj.Serialization.JsonPropertyName("DESCRIPTION")]
            public string? Description { get; set; }

            [Nsj.JsonProperty("retailPrice")]
            [Stj.Serialization.JsonPropertyName("retailPrice")]
            public decimal RetailPrice { get; set; }

            [Nsj.JsonIgnore]
            [Stj.Serialization.JsonIgnore]
            public string? Secret { get; set; }

            [Nsj.JsonIgnore]
            [Stj.Serialization.JsonIgnore]
            public CompositeKey PrimaryKey => new CompositeKey(Code);

            public string? ETag { get; set; }
        }
    }
}