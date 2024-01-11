using Azure.Core.Amqp;
using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Az = Azure.Messaging.ServiceBus;

namespace CoreEx.Test.Framework.Azure.ServiceBus
{
    [TestFixture]
    public class EventDataToServiceBusConverterTest
    {
        [Test]
        public async Task Convert_NoValue_Using_TextJsonEventSerialization_ToAndFrom()
        {
            var c = new EventDataToServiceBusConverter();
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY" });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage());

            var edc = new ServiceBusReceivedMessageEventDataConverter();
            var e = await edc.ConvertFromAsync(rm, null, default);
            AssertEventData(e);
        }

        [Test]
        public async Task Convert_WithValue_Using_TextJsonEventSerialization_ToAndFrom()
        {
            var c = new EventDataToServiceBusConverter();
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", Value = new Product { Id = "X", Name = "Xxx", Price = 9.99m } });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage());

            var edc = new ServiceBusReceivedMessageEventDataConverter();
            var e = await edc.ConvertFromAsync(rm, null, default);
            AssertEventData(e);

            var ep = await edc.ConvertFromAsync<Product>(rm, default);
            AssertEventData(ep);

            ep = (EventData<Product>)await edc.ConvertFromAsync(rm, typeof(Product), default);
            AssertEventData(ep);
        }

        [Test]
        public async Task Convert_NoValue_Using_TextJsonEventSerialization_All_ToAndFrom()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer { SerializeValueOnly = false };

            var c = new EventDataToServiceBusConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY" });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage());

            var edc = new ServiceBusReceivedMessageEventDataConverter(es);
            var e = await edc.ConvertFromAsync(rm, null, default);
            AssertEventData(e);
        }

        [Test]
        public async Task Convert_WithValue_Using_TextJsonEventSerialization_All_ToAndFrom()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer { SerializeValueOnly = false };

            var c = new EventDataToServiceBusConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", Value = new Product { Id = "X", Name = "Xxx", Price = 9.99m } });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage());

            var edc = new ServiceBusReceivedMessageEventDataConverter(es);
            var e = await edc.ConvertFromAsync(rm, null, default);
            AssertEventData(e);

            var ep = await edc.ConvertFromAsync<Product>(rm, default);
            AssertEventData(ep);

            ep = (EventData<Product>)await edc.ConvertFromAsync(rm, typeof(Product), default);
            AssertEventData(ep);
        }

        [Test]
        public async Task Convert_NoValue_Using_TextJsonCloudEventSerialization_All_ToAndFrom()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer();

            var c = new EventDataToServiceBusConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Source = new Uri("xxx", UriKind.Relative), Action = "YYY" });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage());

            var edc = new ServiceBusReceivedMessageEventDataConverter(es);
            var e = await edc.ConvertFromAsync(rm, null, default);
            AssertEventData(e);
        }

        [Test]
        public async Task Convert_As_PlainText()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer();

            var c = new EventDataToServiceBusConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Source = new Uri("xxx", UriKind.Relative), Action = "YYY" });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage(), c => { c.Properties.ContentType = MediaTypeNames.Text.Plain; c.Body = new AmqpMessageBody(new ReadOnlyMemory<byte>[] { new BinaryData("Blah").ToMemory()[..] }); });

            var edc = new ServiceBusReceivedMessageEventDataConverter(es);
            var e = await edc.ConvertFromAsync(rm, null, default);
            Assert.Multiple(() =>
            {
                Assert.That(e.Value, Is.Not.Null.And.EqualTo("Blah"));
                Assert.That(m.Subject, Is.EqualTo("xxx"));
            });
        }

        [Test]
        public async Task Convert_Value_As_PlainText()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer();

            var c = new EventDataToServiceBusConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Source = new Uri("xxx", UriKind.Relative), Action = "YYY" });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage(), c => { c.Properties.ContentType = MediaTypeNames.Text.Plain; c.Body = new AmqpMessageBody(new ReadOnlyMemory<byte>[] { new BinaryData("Blah").ToMemory()[..] }); });

            var edc = new ServiceBusReceivedMessageEventDataConverter(es);
            var e = await edc.ConvertFromAsync(rm, typeof(string), default);
            Assert.Multiple(() =>
            {
                Assert.That(e.Value, Is.Not.Null.And.EqualTo("Blah"));
                Assert.That(m.Subject, Is.EqualTo("xxx"));
            });
        }

        [Test]
        public async Task Convert_WithValue_Using_TextJsonCloudEventSerialization_All_ToAndFrom()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer();

            var c = new EventDataToServiceBusConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", Source = new Uri("xxx", UriKind.Relative), Value = new Product { Id = "X", Name = "Xxx", Price = 9.99m } });
            AssertServiceBusMessage(m);

            var rm = CreateServiceBusReceivedMessageFromAmqp(m.GetRawAmqpMessage());

            var edc = new ServiceBusReceivedMessageEventDataConverter(es);
            var e = await edc.ConvertFromAsync(rm, null, default);
            AssertEventData(e);

            var ep = await edc.ConvertFromAsync<Product>(rm, default);
            AssertEventData(ep);

            ep = (EventData<Product>)await edc.ConvertFromAsync(rm, typeof(Product), default);
            AssertEventData(ep);
        }

        private static Az.ServiceBusReceivedMessage CreateServiceBusReceivedMessageFromAmqp(AmqpAnnotatedMessage message, Action<AmqpAnnotatedMessage>? config = null)
        {
            if (message == null) throw new ArgumentNullException("message");

            config?.Invoke(message);

            message.Header.DeliveryCount = 1;
            message.Header.Durable = true;
            message.Header.Priority = 1;
            message.Header.TimeToLive = TimeSpan.FromSeconds(60);

            var t = typeof(Az.ServiceBusReceivedMessage);
            var c = t.GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(AmqpAnnotatedMessage) }, null);
            return c == null
                ? throw new InvalidOperationException($"'{typeof(Az.ServiceBusReceivedMessage).Name}' constructor that accepts Type '{typeof(AmqpAnnotatedMessage).Name}' parameter was not found.")
                : (Az.ServiceBusReceivedMessage)c.Invoke(new object?[] { message });
        }

        private static void AssertServiceBusMessage(Az.ServiceBusMessage? m)
        {
            Assert.That(m, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(m!.MessageId, Is.EqualTo("123"));
                Assert.That(m.Subject, Is.EqualTo("xxx"));
                Assert.That(m.ApplicationProperties.TryGetValue(nameof(EventData.Action), out var a), Is.True);
                Assert.That(a, Is.EqualTo("yyy"));
            });
        }

        private static void AssertEventData(EventData e)
        {
            Assert.That(e, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(e.Id, Is.EqualTo("123"));
                Assert.That(e.Subject, Is.EqualTo("xxx"));
                Assert.That(e.Action, Is.EqualTo("yyy"));
            });
        }

        private static void AssertEventData(EventData<Product> ep)
        {
            Assert.That(ep, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ep.Id, Is.EqualTo("123"));
                Assert.That(ep.Subject, Is.EqualTo("xxx"));
                Assert.That(ep.Action, Is.EqualTo("yyy"));
                Assert.That(ep.Value, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(ep.Value.Id, Is.EqualTo("X"));
                Assert.That(ep.Value.Name, Is.EqualTo("Xxx"));
                Assert.That(ep.Value.Price, Is.EqualTo(9.99m));
            });
        }
    }
}