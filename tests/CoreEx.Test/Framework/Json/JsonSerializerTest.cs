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
            Assert.That(json, Is.EqualTo("{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99,\"etag\":\"xxx\"}"));

            p = js.Deserialize<BackendProduct>(json);
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p!.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
                Assert.That(p.ETag, Is.EqualTo("xxx"));
            });

            p = (BackendProduct)js.Deserialize(json, typeof(BackendProduct))!;
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
                Assert.That(p.ETag, Is.EqualTo("xxx"));
            });

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
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p!.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
            });

            p = (BackendProduct)js.Deserialize(bs, typeof(BackendProduct))!;
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
            });
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize_Dynamic()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var json = js.Serialize(p);

            var o = js.Deserialize(json);
            Assert.That(o, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(o, Is.InstanceOf<System.Text.Json.JsonElement>());
                Assert.That(o!.ToString(), Is.EqualTo("{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}"));
            });
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize_Dynamic_BinaryData()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var bs = js.SerializeToBinaryData(p);

            var o = js.Deserialize(bs);
            Assert.That(o, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(o, Is.InstanceOf<System.Text.Json.JsonElement>());
                Assert.That(o!.ToString(), Is.EqualTo("{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}"));
            });
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonString()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new() { Street = "One", City = "First" }, new() { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out string json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json), Is.True);
                Assert.That(json, Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(json, Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "firstName" }, out json), Is.True);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json), Is.True);
                Assert.That(json, Is.EqualTo("{}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude), Is.False);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));
            });
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonObject()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new() { Street = "One", City = "First" }, new() { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out object json, JsonPropertyFilter.Exclude), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "firstName" }, out json), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude), Is.False);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "addresses[1].city" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\"}]}"));
            });

            p.Address = new Address { Street = "One", City = "First" };
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "address" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonObject)json).ToJsonString(), Is.EqualTo("{\"address\":{\"street\":\"One\",\"city\":\"First\"}}"));
            });
        }

        [Test]
        public void SystemTextJson_TryApplyFilter_JsonArray()
        {
            var p = new int[] { 11, 22, 333 };
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            object json;
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "[2]" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonArray)json).ToJsonString(), Is.EqualTo("[333]"));
            });

            js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "[2]" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((System.Text.Json.Nodes.JsonArray)json).ToJsonString(), Is.EqualTo("[11,22]"));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other.FirstName))!, out string? jn), Is.True);
                Assert.That(jn, Is.EqualTo("first-name"));

                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other.MiddleName))!, out jn), Is.False);
                Assert.That(jn, Is.EqualTo(null));

                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other.LastName))!, out jn), Is.True);
                Assert.That(jn, Is.EqualTo("lastName"));

            });
        }

        [Test]
        public void SystemTextJson_Serialize_DictionaryKeys()
        {
            var d = new Dictionary<string, string> { { "ABC", "XXX" }, { "Efg", "Xxx" }, { "hij", "xxx" }, { "AbEfg", "xxXxx" }, { "ETag", "ETAG" } };
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;
            var json = js.Serialize(d);
            Assert.That(json, Is.EqualTo("{\"abc\":\"XXX\",\"efg\":\"Xxx\",\"hij\":\"xxx\",\"abEfg\":\"xxXxx\",\"etag\":\"ETAG\"}"));
        }

        [Test]
        public void SystemTextJson_Serialize_CollectionResult()
        {
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;

            // Null object.
            var pcr = (PersonCollectionResult?)null;

            var json = js.Serialize(pcr);
            Assert.That(json, Is.EqualTo("null"));

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.That(pcr, Is.Null);

            // Empty collection.
            pcr = new PersonCollectionResult();

            json = js.Serialize(pcr);
            Assert.That(json, Is.EqualTo("[]"));

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.That(pcr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(pcr!.Items, Is.Not.Null);
                Assert.That(pcr.Items, Is.Empty);
                Assert.That(pcr.Paging, Is.Null);
            });

            // Items in collection.
            pcr.Items.Add(new Person { FirstName = "Jane" });
            pcr.Items.Add(new Person { FirstName = "John" });

            json = js.Serialize(pcr);
            Assert.That(json, Is.EqualTo("[{\"firstName\":\"Jane\"},{\"firstName\":\"John\"}]"));

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.That(pcr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(pcr!.Items, Is.Not.Null);
                Assert.That(pcr.Items, Has.Count.EqualTo(2));
                Assert.That(pcr.Paging, Is.Null);
                Assert.That(pcr.Items[0].FirstName, Is.EqualTo("Jane"));
                Assert.That(pcr.Items[1].FirstName, Is.EqualTo("John"));
            });
        }

        #endregion

        #region NewtonsoftJson

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X", ETag = "xxx" };
            var json = js.Serialize(p);
            Assert.That(json, Is.EqualTo("{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99,\"etag\":\"xxx\"}"));

            p = js.Deserialize<BackendProduct>(json);
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p!.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
                Assert.That(p.ETag, Is.EqualTo("xxx"));
            });

            p = (BackendProduct)js.Deserialize(json, typeof(BackendProduct))!;
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
                Assert.That(p.ETag, Is.EqualTo("xxx"));
            });

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
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p!.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
            });

            p = (BackendProduct)js.Deserialize(bs, typeof(BackendProduct))!;
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p.Code, Is.EqualTo("A"));
                Assert.That(p.Description, Is.EqualTo("B"));
                Assert.That(p.RetailPrice, Is.EqualTo(1.99m));
                Assert.That(p.Secret, Is.Null);
            });
        }

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize_Dynamic()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var json = js.Serialize(p);

            var o = js.Deserialize(json);
            Assert.That(o, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(o, Is.InstanceOf<Nsj.Linq.JObject>());
                Assert.That(((Nsj.Linq.JObject)o!).ToString(Nsj.Formatting.None), Is.EqualTo("{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}"));
            });
        }

        [Test]
        public void NewtonsoftJson_Serialize_Deserialize_Dynamic_BinaryData()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var p = new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m, Secret = "X" };
            var bs = js.SerializeToBinaryData(p);

            var o = js.Deserialize(bs);
            Assert.That(o, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(o, Is.InstanceOf<Nsj.Linq.JObject>());
                Assert.That(((Nsj.Linq.JObject)o!).ToString(Nsj.Formatting.None), Is.EqualTo("{\"code\":\"A\",\"DESCRIPTION\":\"B\",\"retailPrice\":1.99}"));
            });
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonString()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new() { Street = "One", City = "First" }, new() { Street = "Two", City = "Second" } } };

            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out string json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json), Is.True);
                Assert.That(json, Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(json, Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "firstName" }, out json), Is.True);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json), Is.True);
                Assert.That(json, Is.EqualTo("{}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude), Is.False);
                Assert.That(json, Is.EqualTo("{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));
            });
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonObject()
        {
            var p = new Person { FirstName = "John", LastName = "Smith", Addresses = new List<Address> { new() { Street = "One", City = "First" }, new() { Street = "Two", City = "Second" } } };
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            object json;

            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"firstName\":\"John\",\"addresses\":[{\"street\":\"One\"},{\"street\":\"Two\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "LastName", "Addresses.City", "LastName" }, out json), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"city\":\"First\"},{\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "lastname", "addresses" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "firstName" }, out json), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"firstName\":\"John\"}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "middlename" }, out json, JsonPropertyFilter.Exclude), Is.False);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\",\"city\":\"Second\"}]}"));

                Assert.That(js.TryApplyFilter(p, new string[] { "addresses[1].city" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"firstName\":\"John\",\"lastName\":\"Smith\",\"addresses\":[{\"street\":\"One\",\"city\":\"First\"},{\"street\":\"Two\"}]}"));
            });

            p.Address = new Address { Street = "One", City = "First" };
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "address" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("{\"address\":{\"street\":\"One\",\"city\":\"First\"}}"));
            });
        }

        [Test]
        public void NewtonsoftJson_TryApplyFilter_JsonOArray()
        {
            var p = new int[] { 11, 22, 333 };
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            object json;

            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "[2]" }, out json, JsonPropertyFilter.Include), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("[333]"));
            });

            js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            Assert.Multiple(() =>
            {
                Assert.That(js.TryApplyFilter(p, new string[] { "[2]" }, out json, JsonPropertyFilter.Exclude), Is.True);
                Assert.That(((Nsj.Linq.JToken)json).ToString(Nsj.Formatting.None), Is.EqualTo("[11,22]"));
            });
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

            Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other.FirstName))!, out string? jn), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(jn, Is.EqualTo("first_name"));

                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other.MiddleName))!, out jn), Is.False);
                Assert.That(jn, Is.EqualTo(null));

                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other.LastName))!, out jn), Is.False);
                Assert.That(jn, Is.EqualTo(null));
            });

            // Verify JsonObject usage.
            t = typeof(Other2);
            Assert.Multiple(() =>
            {
                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other2.FirstName))!, out jn), Is.True);
                Assert.That(jn, Is.EqualTo("firstName"));

                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(Other2.LastName))!, out jn), Is.False);
                Assert.That(jn, Is.EqualTo(null));
            });

            // Verify ContractResolver STJ marked-up objects.
            t = typeof(CoreEx.AspNetCore.WebApis.ExtendedContentResult);
            Assert.Multiple(() =>
            {
                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(CoreEx.AspNetCore.WebApis.ExtendedContentResult.AfterExtension))!, out jn), Is.False);
                Assert.That(jn, Is.EqualTo(null));
            });

            t = typeof(CoreEx.Entities.ChangeLog);
            Assert.Multiple(() =>
            {
                Assert.That(js.TryGetJsonName(t.GetProperty(nameof(CoreEx.Entities.ChangeLog.CreatedBy))!, out jn), Is.True);
                Assert.That(jn, Is.EqualTo("createdBy"));
            });
        }

        [Test]
        public void NewtonsoftJson_Serialize_DictionaryKeys()
        {
            var d = new Dictionary<string, string> { { "ABC", "XXX" }, { "Efg", "Xxx" }, { "hij", "xxx" }, { "AbEfg", "xxXxx" }, { "ETag", "ETAG" } };
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;
            var json = js.Serialize(d);
            Assert.That(json, Is.EqualTo("{\"abc\":\"XXX\",\"efg\":\"Xxx\",\"hij\":\"xxx\",\"abEfg\":\"xxXxx\",\"etag\":\"ETAG\"}"));
        }

        [Test]
        public void NewtonsoftJson_Serialize_CollectionResult()
        {
            var js = new CoreEx.Newtonsoft.Json.JsonSerializer() as IJsonSerializer;

            // Null object.
            var pcr = (PersonCollectionResult?)null;

            var json = js.Serialize(pcr);
            Assert.That(json, Is.EqualTo("null"));

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.That(pcr, Is.Null);

            // Empty collection.
            pcr = new PersonCollectionResult();

            json = js.Serialize(pcr);
            Assert.That(json, Is.EqualTo("[]"));

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.That(pcr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(pcr!.Items, Is.Not.Null);
                Assert.That(pcr.Items, Is.Empty);
                Assert.That(pcr.Paging, Is.Null);
            });

            // Items in collection.
            pcr.Items.Add(new Person { FirstName = "Jane" });
            pcr.Items.Add(new Person { FirstName = "John" });

            json = js.Serialize(pcr);
            Assert.That(json, Is.EqualTo("[{\"firstName\":\"Jane\"},{\"firstName\":\"John\"}]"));

            pcr = js.Deserialize<PersonCollectionResult>(json);
            Assert.That(pcr, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(pcr!.Items, Is.Not.Null);
                Assert.That(pcr.Items, Has.Count.EqualTo(2));
                Assert.That(pcr.Paging, Is.Null);
                Assert.That(pcr.Items[0].FirstName, Is.EqualTo("Jane"));
                Assert.That(pcr.Items[1].FirstName, Is.EqualTo("John"));
            });
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
            public CompositeKey PrimaryKey => new(Code);

            public string? ETag { get; set; }
        }
    }
}