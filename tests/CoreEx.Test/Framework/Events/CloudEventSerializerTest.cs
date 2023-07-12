using CoreEx.Events;
using CoreEx.Events.Attachments;
using CoreEx.TestFunction.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Events
{
    [TestFixture]
    public class CloudEventSerializerTest
    {
        [Test]
        public async Task SystemTextJson_Serialize_Deserialize1()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer() as IEventSerializer;
            var ed = CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            ed2 = (EventData<Product>)await es.DeserializeAsync(bd, typeof(Product)).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize2()
        {
            var ef = new EventDataFormatter { SourceDefault = _ => new Uri("null", UriKind.RelativeOrAbsolute) };
            var es = new CoreEx.Text.Json.CloudEventSerializer(ef) as IEventSerializer;
            var ed = CreateProductEvent2();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent2, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2, "Type", "Source");
            Assert.AreEqual(new Uri("null", UriKind.Relative), ed2.Source);
            Assert.AreEqual("coreex.testfunction.models.product", ed2.Type);
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize1_WithAttachment()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer() { AttachmentStorage = CreateEventStorage("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", 10) } as IEventSerializer;
            var ed = CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1Attachement, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            ed2 = (EventData<Product>)await es.DeserializeAsync(bd, typeof(Product)).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);

            var ed3 = await es.DeserializeAsync(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed3, "Value");
            Assert.AreEqual("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", ed3.Value?.ToString());
        }

        [Test]
        public async Task SystemTextJson_Serialize_Deserialize1_WithNoAttachment()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer() { AttachmentStorage = CreateEventStorage("{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}", 1000) } as IEventSerializer;
            var ed = CreateProductEvent1();
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
        public async Task NewtonsoftJson_Serialize_Deserialize1()
        {
            var es = new CoreEx.Newtonsoft.Json.CloudEventSerializer() as IEventSerializer;
            var ed = CreateProductEvent1();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent1, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2);
        }

        [Test]
        public async Task NewtonsoftJson_Serialize_Deserialize2()
        {
            var ef = new EventDataFormatter { SourceDefault = _ => new Uri("null", UriKind.RelativeOrAbsolute) };
            var es = new CoreEx.Newtonsoft.Json.CloudEventSerializer(ef) as IEventSerializer;
            var ed = CreateProductEvent2();
            var bd = await es.SerializeAsync(ed).ConfigureAwait(false);
            Assert.IsNotNull(bd);
            Assert.AreEqual(CloudEvent2, bd.ToString());

            var ed2 = await es.DeserializeAsync<Product>(bd).ConfigureAwait(false);
            ObjectComparer.Assert(ed, ed2, "Type", "Source");
            Assert.AreEqual(new Uri("null", UriKind.Relative), ed2.Source);
            Assert.AreEqual("coreex.testfunction.models.product", ed2.Type);
        }

        internal static EventData<Product> CreateProductEvent1() => new()
        {
            Id = "id",
            Type = "product.created",
            Source = new Uri("product/a", UriKind.Relative),
            Subject = "product",
            Action = "created",
            CorrelationId = "cid",
            TenantId = "tid",
            Timestamp = new DateTime(2022, 02, 22, 22, 02, 22, DateTimeKind.Utc),
            PartitionKey = "pid",
            ETag = "etag",
            Attributes = new Dictionary<string, string> { { "fruit", "bananas" } },
            Value = new Product { Id = "A", Name = "B", Price = 1.99m }
        };

        internal static EventData<Product> CreateProductEvent2() => new()
        {
            Id = "id",
            Timestamp = new DateTime(2022, 02, 22, 22, 02, 22, DateTimeKind.Utc),
            Value = new Product { Id = "A", Name = "B", Price = 1.99m },
            CorrelationId = "cid"
        };

        private const string CloudEvent1 = "{\"specversion\":\"1.0\",\"id\":\"id\",\"time\":\"2022-02-22T22:02:22Z\",\"type\":\"product.created\",\"source\":\"product/a\",\"subject\":\"product\",\"action\":\"created\",\"correlationid\":\"cid\",\"partitionkey\":\"pid\",\"tenantid\":\"tid\",\"etag\":\"etag\",\"fruit\":\"bananas\",\"datacontenttype\":\"application/json\",\"data\":{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}}";

        private const string CloudEvent2 = "{\"specversion\":\"1.0\",\"id\":\"id\",\"time\":\"2022-02-22T22:02:22Z\",\"type\":\"coreex.testfunction.models.product\",\"source\":\"null\",\"correlationid\":\"cid\",\"datacontenttype\":\"application/json\",\"data\":{\"id\":\"A\",\"name\":\"B\",\"price\":1.99}}";

        private const string CloudEvent1Attachement = "{\"specversion\":\"1.0\",\"id\":\"id\",\"time\":\"2022-02-22T22:02:22Z\",\"type\":\"product.created\",\"source\":\"product/a\",\"subject\":\"product\",\"action\":\"created\",\"correlationid\":\"cid\",\"partitionkey\":\"pid\",\"tenantid\":\"tid\",\"etag\":\"etag\",\"fruit\":\"bananas\",\"datacontenttype\":\"application/json\",\"data\":{\"contentType\":\"application/json\",\"attachment\":\"bananas.json\"}}";

        internal static EventStorage CreateEventStorage(string? data = null, int? max = null) => new(data) { MaxDataSize = max ?? 100000 };

        internal class EventStorage : IAttachmentStorage
        {
            private readonly BinaryData _data;

            public EventStorage(string? data)
                => _data = data is null ? BinaryData.Empty : new BinaryData(data);

            public int MaxDataSize { get; set; } = 10;

            public Task<BinaryData> ReadAync(EventAttachment attachment, CancellationToken cancellationToken) => Task.FromResult(_data);

            public Task<EventAttachment> WriteAsync(EventData @event, BinaryData attachmentData, CancellationToken cancellationToken)
            {
                Assert.AreEqual(_data.ToString(), attachmentData.ToString());
                return Task.FromResult(new EventAttachment { Attachment = "bananas.json", ContentType = "application/json" });
            }
        }
    }
}