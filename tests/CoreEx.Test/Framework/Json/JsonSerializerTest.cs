using CoreEx.Json;
using CoreEx.TestFunction.Models;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Nsj = Newtonsoft.Json;

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
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var json = js.Serialize(p);
            Assert.AreEqual(json, "{\"code\":\"A\",\"description\":\"B\",\"retailPrice\":1.99}");

            p = js.Deserialize<BackendProduct>(json);
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            p = (BackendProduct)js.Deserialize(json, typeof(BackendProduct));
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            json = js.Serialize(p, JsonWriteFormat.Indented);
            json.Should().Be("{\n  \"code\": \"A\",\n  \"description\": \"B\",\n  \"retailPrice\": 1.99\n}"
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
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            p = (BackendProduct)js.Deserialize(bs, typeof(BackendProduct));
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
            Assert.AreEqual(o.ToString(), "{\"code\":\"A\",\"description\":\"B\",\"retailPrice\":1.99}");
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
            Assert.AreEqual(o.ToString(), "{\"code\":\"A\",\"description\":\"B\",\"retailPrice\":1.99}");
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonString()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out string json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"FirstName\":\"John\",\"Addresses\":[{\"Street\":\"One\"},{\"Street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(json, "{\"LastName\":\"Smith\",\"Addresses\":[{\"City\":\"First\"},{\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(json, "{\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(json, "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(json, "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"FirstName\":\"John\",\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonObject()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out object json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"FirstName\":\"John\",\"Addresses\":[{\"Street\":\"One\"},{\"Street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"LastName\":\"Smith\",\"Addresses\":[{\"City\":\"First\"},{\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), "{\"FirstName\":\"John\",\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");
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
            var deserialized = js.Deserialize<Exception>(serialized);

            // Assert
            deserialized.Data.Should().BeEquivalentTo(realException.Data);
            deserialized.Message.Should().BeEquivalentTo(realException.Message, because: "Custom converter only handles Message on deserialization");
        }

        #endregion

        #region NewtonsoftJson

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var json = js.Serialize(p);
            Assert.AreEqual(json, "{\"code\":\"A\",\"description\":\"B\",\"retailPrice\":1.99}");

            p = js.Deserialize<BackendProduct>(json);
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            p = (BackendProduct)js.Deserialize(json, typeof(BackendProduct));
            Assert.NotNull(p);
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            json = js.Serialize(p, JsonWriteFormat.Indented);
            json.Should().Be("{\n  \"code\": \"A\",\n  \"description\": \"B\",\n  \"retailPrice\": 1.99\n}"
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
            Assert.AreEqual("A", p.Code);
            Assert.AreEqual("B", p.Description);
            Assert.AreEqual(1.99m, p.RetailPrice);
            Assert.IsNull(p.Secret);

            p = (BackendProduct)js.Deserialize(bs, typeof(BackendProduct));
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
            Assert.AreEqual(((Nsj.Linq.JObject)o).ToString(Nsj.Formatting.None), "{\"code\":\"A\",\"description\":\"B\",\"retailPrice\":1.99}");
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
            Assert.AreEqual(((Nsj.Linq.JObject)o).ToString(Nsj.Formatting.None), "{\"code\":\"A\",\"description\":\"B\",\"retailPrice\":1.99}");
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonString()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out string json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"FirstName\":\"John\",\"Addresses\":[{\"Street\":\"One\"},{\"Street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(json, "{\"LastName\":\"Smith\",\"Addresses\":[{\"City\":\"First\"},{\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(json, "{\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(json, "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(json, "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(json, "{\"FirstName\":\"John\",\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonObject()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new Address { Street = "One", City = "First" }, new Address { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out object json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"FirstName\":\"John\",\"Addresses\":[{\"Street\":\"One\"},{\"Street\":\"Two\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"LastName\":\"Smith\",\"Addresses\":[{\"City\":\"First\"},{\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "firstName" }, out json));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"FirstName\":\"John\"}");

            Assert.IsTrue(js.TryApplyFilter(p, new string[] { "middlename" }, out json));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{}");

            Assert.IsFalse(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude));
            Assert.AreEqual(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), "{\"FirstName\":\"John\",\"LastName\":\"Smith\",\"Addresses\":[{\"Street\":\"One\",\"City\":\"First\"},{\"Street\":\"Two\",\"City\":\"Second\"}]}");
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
            var deserialized = js.Deserialize<Exception>(serialized);

            // Assert
            deserialized.Data.Should().BeEquivalentTo(realException.Data);
            deserialized.Message.Should().BeEquivalentTo(realException.Message);
            deserialized.StackTrace.Should().BeEquivalentTo(realException.StackTrace);
        }

        #endregion

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public List<Address> Addresses { get; set; }
            public string SSN { get; set; }
            public decimal NetWorth { get; set; }
            public bool? IsAwesome { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
        }
    }
}