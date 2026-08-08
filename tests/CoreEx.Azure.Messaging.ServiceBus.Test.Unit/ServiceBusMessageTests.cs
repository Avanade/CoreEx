using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CoreEx.Entities;
using CoreEx.Events;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class ServiceBusMessageTests
{
    [Test]
    public void CloudEventToServiceBusMessage_Structured()
    {
        var p = new Product { Id = 1, Sku = "SKU-001" };

        var ed = EventData.CreateEventWith(p, EventAction.Published).WithTitle("unit.test.title").WithSource(new Uri("http://unit.test.source"));
        var ce = new EventFormatter().ConvertToCloudEvent(ed);

        var sbm = ce.ToServiceBusMessage(ContentMode.Structured);
        sbm.Should().NotBeNull();
        sbm.ContentType.Should().Be("application/cloudevents+json; charset=utf-8");

        var sbrm = ConvertToReceivedMessage(sbm);
        var ce2 = sbrm.ToCloudEvent();  // Inferred

        var jce = ce.EncodeToJsonElement();
        var jce2 = ce2.EncodeToJsonElement();
        ObjectComparer.Assert(jce, jce2);
    }

    [Test]
    public void CloudEventToServiceBusMessage_Binary()
    {
        var p = new Product { Id = 1, Sku = "SKU-001" };

        var ed = EventData.CreateEventWith(p, EventAction.Published).WithTitle("unit.test.title").WithSource(new Uri("http://unit.test.source"));
        var ce = new EventFormatter().ConvertToCloudEvent(ed);

        var sbm = ce.ToServiceBusMessage(ContentMode.Binary);
        sbm.Should().NotBeNull();

        var sbrm = ConvertToReceivedMessage(sbm);
        var ce2 = sbrm.ToCloudEvent();  // Inferred

        var jce = ce.EncodeToJsonElement();
        var jce2 = ce2.EncodeToJsonElement();

        Console.WriteLine(jce);
        Console.WriteLine(jce2);

        ObjectComparer.Assert(jce, jce2);
    }

    [Test]
    public void CloudEventToServiceBusMessage_Binary_ExcludeAttributes_PreservesTraceContext()
    {
        // Regression: trace context (traceparent/tracestate) must survive Binary-mode round-trip even when
        // includeAttributes (the bulk ce_-prefixed properties) is excluded.
        var p = new Product { Id = 1, Sku = "SKU-001" };
        var ed = EventData.CreateEventWith(p, EventAction.Published).WithTitle("unit.test.title").WithSource(new Uri("http://unit.test.source"));
        ed.TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
        ed.TraceState = "vendor=prop";

        var ce = new EventFormatter().ConvertToCloudEvent(ed);

        var sbm = ce.ToServiceBusMessage(ContentMode.Binary, includeAttributes: false);
        sbm.ApplicationProperties["traceparent"].Should().Be(ed.TraceParent);
        sbm.ApplicationProperties["tracestate"].Should().Be(ed.TraceState);

        var sbrm = ConvertToReceivedMessage(sbm);
        var ce2 = sbrm.ToCloudEvent();

        ce2.TryGetExtensionAttribute("traceparent", out string? traceParent).Should().BeTrue();
        traceParent.Should().Be(ed.TraceParent);

        ce2.TryGetExtensionAttribute("tracestate", out string? traceState).Should().BeTrue();
        traceState.Should().Be(ed.TraceState);
    }

    [Test]
    public void CloudEventToServiceBusMessage_Binary_ExcludeAttributes_PreservesCoreIdentity()
    {
        // Regression: Id/Type (and the ce_specversion marker that makes the message recognizable as a CloudEvent
        // at all) must survive Binary-mode round-trip even when includeAttributes is excluded.
        var p = new Product { Id = 1, Sku = "SKU-001" };
        var ed = EventData.CreateEventWith(p, EventAction.Published).WithTitle("unit.test.title").WithSource(new Uri("http://unit.test.source"));
        var ce = new EventFormatter().ConvertToCloudEvent(ed);

        var sbm = ce.ToServiceBusMessage(ContentMode.Binary, includeAttributes: false);
        var sbrm = ConvertToReceivedMessage(sbm);

        sbrm.IsCloudEvent().Should().BeTrue();

        var ce2 = sbrm.ToCloudEvent();
        ce2.Id.Should().Be(ce.Id);
        ce2.Type.Should().Be(ce.Type);
    }

    public class Product : IIdentifier<int>
    {
        public int Id { get; set; }
        public string? Sku { get; set; }
    }

    internal static ServiceBusReceivedMessage ConvertToReceivedMessage(ServiceBusMessage m)
    {
        // Copy application properties
        var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in m.ApplicationProperties)
            props[kvp.Key] = kvp.Value;

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: m.Body,
            messageId: m.MessageId,
            partitionKey: m.PartitionKey,
            sessionId: m.SessionId,
            replyToSessionId: m.ReplyToSessionId,
            timeToLive: m.TimeToLive,
            correlationId: m.CorrelationId,
            subject: m.Subject,
            to: m.To,
            contentType: m.ContentType,
            replyTo: m.ReplyTo,
            scheduledEnqueueTime: m.ScheduledEnqueueTime,
            properties: props,
            deliveryCount: 1,
            sequenceNumber: DateTimeOffset.UtcNow.Ticks,
            enqueuedTime: DateTimeOffset.UtcNow
        );
    }
}