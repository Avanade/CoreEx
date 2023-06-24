using CoreEx.Events;
using CoreEx.Solace.PubSub;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using SolaceSystems.Solclient.Messaging;
using System;

namespace CoreEx.Test.Framework.Solace.PubSub
{
    [TestFixture]
    public class EventDataToPubSubConverterTest
    {
        [Test]
        public void Convert_NoValue_Using_EventDataToPubSubMessageConverter()
        {
            var c = new EventDataToPubSubMessageConverter();
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY" });
            AssertPubSubMessage(m);
        }

        [Test]
        public void Convert_NoValue_WithPartitionKey_Using_EventDataToPubSubMessageConverter()
        {
            var c = new EventDataToPubSubMessageConverter();
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", PartitionKey = "ZZZ" });
            AssertPubSubMessage(m);
        }

        [Test]
        public void Convert_WithValue_Using_EventDataToPubSubMessageConverter()
        {
            var c = new EventDataToPubSubMessageConverter();
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", Value = new Product { Id = "X", Name = "Xxx", Price = 9.99m } });
            AssertPubSubMessage(m);
        }

        [Test]
        public void Convert_NoValue_Using_TextJsonEventSerialization()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer { SerializeValueOnly = false };

            var c = new EventDataToPubSubMessageConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY" });
            AssertPubSubMessage(m);
        }

        [Test]
        public void Convert_WithValue_Using_TextJsonEventSerialization()
        {
            var es = new CoreEx.Text.Json.EventDataSerializer { SerializeValueOnly = false };

            var c = new EventDataToPubSubMessageConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", Value = new Product { Id = "X", Name = "Xxx", Price = 9.99m } });
            AssertPubSubMessage(m);
        }

        [Test]
        public void Convert_NoValue_Using_TextJsonCloudEventSerialization()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer();

            var c = new EventDataToPubSubMessageConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Source = new Uri("xxx", UriKind.Relative), Action = "YYY" });
            AssertPubSubMessage(m);
        }

        [Test]
        public void Convert_WithValue_Using_TextJsonCloudEventSerialization()
        {
            var es = new CoreEx.Text.Json.CloudEventSerializer();

            var c = new EventDataToPubSubMessageConverter(es);
            var m = c.Convert(new EventData { Id = "123", Subject = "XXX", Action = "YYY", Source = new Uri("xxx", UriKind.Relative), Value = new Product { Id = "X", Name = "Xxx", Price = 9.99m } });
            AssertPubSubMessage(m);
        }

        private static void AssertPubSubMessage(IMessage m)
        {
            Assert.That(m, Is.Not.Null);
            Assert.That(m!.ApplicationMessageId, Is.EqualTo("123"));
            Assert.That(m.UserPropertyMap.GetString("Subject"), Is.EqualTo("xxx"));
            Assert.That(m.UserPropertyMap.GetString("Action"), Is.EqualTo("yyy"));
        }
    }
}