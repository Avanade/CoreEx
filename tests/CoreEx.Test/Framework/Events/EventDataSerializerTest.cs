﻿using CoreEx.Events;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnitTestEx.NUnit;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Test.Framework.Events
{
    [TestFixture]
    public class EventDataSerializerTest
    {
        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData1()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData2()
        {
            var ef = new EventDataFormatter { SourceDefault = _ => new Uri("null", UriKind.RelativeOrAbsolute) };
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer(), ef) { SerializeValueOnly = false } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent2();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent2, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2, "Type", "Source");
            Assert.AreEqual(new Uri("null", UriKind.Relative), ed2.Source);
            Assert.AreEqual("coreex.testfunction.models.product", ed2.Type);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData3()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped) { Value = ped.Value };
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            ed2 = (EventData<Product>)await es.DeserializeAsync(bd, typeof(Product)).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3, "Value");
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ed3.Value?.ToString());
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData4()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped);
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_ValueOnly()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = true } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(new EventData<Product> { Value = ed.Value }, ed2);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_ValueOnly2()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = true } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped);
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(new EventData<Product>(), ed2);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData1_WithAttachment()
        {
            // Serialized length is > 10, so it will be stored in attachment
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false, AttachmentStorage = CloudEventSerializerTest.CreateEventStorage("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", 10) } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1Attachment, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData1_WithNoAttachment()
        {
            // Serialized length is < 100, so it will _not_ be stored in attachment
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false, AttachmentStorage = CloudEventSerializerTest.CreateEventStorage("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", 100) } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData3_WithAttachment()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false, AttachmentStorage = CloudEventSerializerTest.CreateEventStorage("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", 10) } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped) { Value = ped.Value };
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1Attachment, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            ed2 = (EventData<Product>)await es.DeserializeAsync(bd, typeof(Product)).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3, "Value");
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ed3.Value?.ToString());
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize_EventData3_WithNoAttachment()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false, AttachmentStorage = CloudEventSerializerTest.CreateEventStorage("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", 100) } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped) { Value = ped.Value };
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            ed2 = (EventData<Product>)await es.DeserializeAsync(bd, typeof(Product)).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3, "Value");
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ed3.Value?.ToString());
        }

        [Test]
        public async Task NewtonsoftJson_Serialize_Deserialize_EventData1()
        {
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);
        }

        [Test]
        public async Task NewtonsoftJson_Serialize_Deserialize_EventData2()
        {
            var ef = new EventDataFormatter { SourceDefault = _ => new Uri("null", UriKind.RelativeOrAbsolute) };
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer(), ef) { SerializeValueOnly = false } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent2();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent2, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2, "Type", "Source");
            Assert.AreEqual(new Uri("null", UriKind.Relative), ed2.Source);
            Assert.AreEqual("coreex.testfunction.models.product", ed2.Type);
        }

        [Test]
        public async Task NewtonsoftJson_Serialize_Deserialize_EventData3()
        {
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped) { Value = ped.Value };
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3, "Value");
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ((Nsj.Linq.JToken)ed3.Value!).ToString(Nsj.Formatting.None));
        }

        [Test]
        public async Task NewtonsoftJson_Serialize_Deserialize_EventData4()
        {
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped);
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3);
        }

        [Test]
        public async Task NewtonsoftJson_Serialize_Deserialize_ValueOnly()
        {
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer()) { SerializeValueOnly = true } as IEventSerializer;
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(new EventData<Product> { Value = ed.Value }, ed2);
        }

        [Test]
        public async Task NewtonsoftText_Serialize_Deserialize_ValueOnly2()
        {
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer()) { SerializeValueOnly = true } as IEventSerializer;
            var ped = CloudEventSerializerTest.CreateProductEvent1();
            var ed = new EventData(ped);
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(new EventData<Product>(), ed2);
        }

        private const string CloudEvent1 = "{\"value\":{\"id\":\"A\",\"name\":\"B\",\"price\":1.99},\"id\":\"id\",\"subject\":\"product\",\"action\":\"created\",\"type\":\"product.created\",\"source\":\"product/a\",\"timestamp\":\"2022-02-22T22:02:22+00:00\",\"correlationId\":\"cid\",\"tenantId\":\"tid\",\"partitionKey\":\"pid\",\"etag\":\"etag\",\"attributes\":{\"fruit\":\"bananas\"}}";

        private const string CloudEvent2 = "{\"value\":{\"id\":\"A\",\"name\":\"B\",\"price\":1.99},\"id\":\"id\",\"type\":\"coreex.testfunction.models.product\",\"source\":\"null\",\"timestamp\":\"2022-02-22T22:02:22+00:00\",\"correlationId\":\"cid\"}";

        private const string CloudEvent1Attachment = "{\"value\":{\"contentType\":\"application/json\",\"attachment\":\"bananas.json\"},\"id\":\"id\",\"subject\":\"product\",\"action\":\"created\",\"type\":\"product.created\",\"source\":\"product/a\",\"timestamp\":\"2022-02-22T22:02:22+00:00\",\"correlationId\":\"cid\",\"tenantId\":\"tid\",\"partitionKey\":\"pid\",\"etag\":\"etag\",\"attributes\":{\"fruit\":\"bananas\"}}";

    }
}