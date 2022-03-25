using CoreEx.Events;
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
            var es = new CoreEx.Text.Json.EventDataSerializer(new Text.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
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

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3, "Value");
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ed3.Value.ToString());
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
            var es = new CoreEx.Newtonsoft.Json.EventDataSerializer(new Newtonsoft.Json.JsonSerializer()) { SerializeValueOnly = false } as IEventSerializer;
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
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ((Nsj.Linq.JToken)ed3.Value).ToString(Nsj.Formatting.None));
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
    }
}