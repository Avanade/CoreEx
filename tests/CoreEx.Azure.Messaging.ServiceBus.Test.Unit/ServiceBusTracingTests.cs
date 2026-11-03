namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[TestFixture]
public class ServiceBusTracingTests
{
    private const string TraceParent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";
    private const string OtherTraceParent = "00-fedcba9876543210fedcba9876543210-fedcba9876543210-01";

    [TestCase(false)]
    [TestCase(true)]
    public async Task EnvelopeAndTransportBaggage_ShareOnePolicyAndPreserveExistingKeys(bool enabled)
    {
        using var activity = new Activity("host").Start();
        activity.AddBaggage("existing", "local");
        var formatter = new EventFormatter();
        var cloudEvent = new CloudNative.CloudEvents.CloudEvent { Id = "event", Type = "example", Source = new Uri("urn:test") };
        formatter.AddTracing(cloudEvent, TraceParent, traceBaggage: [new("region", "envelope"), new("existing", "remote")]);
        var outgoing = cloudEvent.ToServiceBusMessage();
        outgoing.ApplicationProperties["baggage"] = "region=transport,existing=transport,transportonly=value";
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: outgoing.Body, contentType: outgoing.ContentType, properties: outgoing.ApplicationProperties);
        var subscriber = new CloudEventSubscriber { PropagateBaggage = enabled };
        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        activity.GetBaggageItem("existing").Should().Be("local");
        activity.GetBaggageItem("region").Should().Be(enabled ? "transport" : null);
        activity.GetBaggageItem("transportonly").Should().Be(enabled ? "value" : null);
        activity.Baggage.Should().HaveCount(enabled ? 3 : 1);
        activity.Links.Should().ContainSingle();
    }

    [Test]
    public async Task CloudEventAndTransportContext_DoNotDuplicateLinks()
    {
        using var activity = new Activity("host").Start();
        var cloudEvent = new CloudNative.CloudEvents.CloudEvent
        {
            Id = "event",
            Type = "example",
            Source = new Uri("urn:test"),
            Data = new { Name = "example" }
        };
        new EventFormatter().AddTracing(cloudEvent, TraceParent);
        var outgoing = cloudEvent.ToServiceBusMessage();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: outgoing.Body, contentType: outgoing.ContentType,
            properties: outgoing.ApplicationProperties);

        (await new CloudEventSubscriber().ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        activity.Links.Should().ContainSingle();
    }

    [TestCase("traceparent")]
    [TestCase("Diagnostic-Id")]
    public void TransportContext_LinksRemoteProducerWithoutReparenting(string name)
    {
        using var activity = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).Start();
        var originalId = activity.Id;
        var originalParent = activity.ParentId;
        var message = Message(new() { [name] = TraceParent, ["tracestate"] = "vendor=value" });
        message.EnrichReceiveActivity();
        message.EnrichReceiveActivity();

        activity.Id.Should().Be(originalId);
        activity.ParentId.Should().Be(originalParent);
        activity.Links.Should().ContainSingle();
        var context = activity.Links.Single().Context;
        context.TraceId.ToString().Should().Be("0123456789abcdef0123456789abcdef");
        context.SpanId.ToString().Should().Be("0123456789abcdef");
        context.TraceState.Should().Be("vendor=value");
        context.IsRemote.Should().BeTrue();
        context.TraceFlags.Should().Be(ActivityTraceFlags.Recorded);
    }

    [TestCase(TraceParent, TraceParent)]
    [TestCase("invalid", OtherTraceParent)]
    public void TraceParent_HasPrecedenceWithValidDiagnosticFallback(string traceParent, string expected)
    {
        using var activity = new Activity("host").Start();
        Message(new() { ["traceparent"] = traceParent, ["Diagnostic-Id"] = OtherTraceParent }).EnrichReceiveActivity();
        ActivityContext.TryParse(expected, null, out var context).Should().BeTrue();
        activity.Links.Single().Context.TraceId.Should().Be(context.TraceId);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("invalid")]
    [TestCase("00-00000000000000000000000000000000-0123456789abcdef-01")]
    public void InvalidContext_DoesNotAddLinks(string? traceParent)
    {
        using var activity = new Activity("host").Start();
        var properties = new Dictionary<string, object> { ["tracestate"] = "vendor=value", ["Diagnostic-Id"] = 42 };
        if (traceParent is not null)
            properties["traceparent"] = traceParent;

        Message(properties).EnrichReceiveActivity();
        activity.Links.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ContextAlreadyInAncestry_DoesNotAddLink(bool localChild)
    {
        using var host = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).SetParentId(TraceParent).Start();
        using var child = localChild ? new Activity("subscriber").Start() : null;
        Message(new() { ["traceparent"] = TraceParent }).EnrichReceiveActivity();
        host.Links.Should().BeEmpty();
        child?.Links.Should().BeEmpty();
    }

    [Test]
    public void ContextAlreadyLinkedByHost_DoesNotAddChildLink()
    {
        using var host = new Activity("host").Start();
        ActivityContext.TryParse(TraceParent, null, isRemote: true, out var context).Should().BeTrue();
        host.AddLink(new ActivityLink(context));
        using var child = new Activity("subscriber").Start();
        Message(new() { ["traceparent"] = TraceParent }).EnrichReceiveActivity();
        child.Links.Should().BeEmpty();
        host.Links.Should().ContainSingle();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Baggage_IsOptInParsedAndDoesNotOverwriteExistingKeys(bool enabled)
    {
        using var activity = new Activity("host").Start();
        activity.AddBaggage("existing", "local");
        var message = Message(new() { ["baggage"] = "region=west%20coast;property=value,existing=remote,broken,feature=enabled" });
        message.EnrichReceiveActivity(enabled);
        message.EnrichReceiveActivity(enabled);
        activity.GetBaggageItem("existing").Should().Be("local");
        activity.GetBaggageItem("region").Should().Be(enabled ? "west coast" : null);
        activity.GetBaggageItem("feature").Should().Be(enabled ? "enabled" : null);
        activity.Baggage.Should().HaveCount(enabled ? 3 : 1);
    }

    [Test]
    public void Baggage_LimitsHeaderBytesAndImportedEntries()
    {
        using var activity = new Activity("host").Start();
        Message(new() { ["baggage"] = "large=" + new string('x', 8192) }).EnrichReceiveActivity(true);
        activity.Baggage.Should().BeEmpty();
        Message(new() { ["baggage"] = "large=" + new string('\u00e9', 4096) }).EnrichReceiveActivity(true);
        activity.Baggage.Should().BeEmpty();
        Message(new() { ["baggage"] = string.Join(",", Enumerable.Range(0, 70).Select(i => $"key{i}=value")) }).EnrichReceiveActivity(true);
        activity.Baggage.Should().HaveCount(64);
    }

    [Test]
    public void TransportMetadata_IsDiagnosticOnly()
    {
        using var activity = new Activity("host").Start();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "message", correlationId: "business-correlation",
            subject: "subject", sessionId: "session", deliveryCount: 2, enqueuedTime: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        message.EnrichReceiveActivity();
        activity.GetTagItem("messaging.message.id").Should().Be("message");
        activity.GetTagItem("messaging.message.conversation_id").Should().Be("business-correlation");
        activity.GetTagItem("messaging.subject").Should().Be("subject");
        activity.GetTagItem("messaging.servicebus.message.session_id").Should().Be("session");
        activity.GetTagItem("messaging.servicebus.message.delivery_count").Should().Be(2);
        activity.GetTagItem("messaging.servicebus.message.enqueued_time").Should().Be("2026-01-01T00:00:00.0000000+00:00");
        activity.Links.Should().BeEmpty();
    }

    [Test]
    public void NoHostActivity_DoesNotCreateOne()
    {
        var previous = Activity.Current;
        try
        {
            Activity.Current = null;
            Message(new() { ["traceparent"] = TraceParent, ["baggage"] = "region=west" }).EnrichReceiveActivity(true);
            Activity.Current.Should().BeNull();
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static ServiceBusReceivedMessage Message(Dictionary<string, object> properties)
        => ServiceBusModelFactory.ServiceBusReceivedMessage(properties: properties);

    private sealed class CloudEventSubscriber() : ServiceBusSubscriberBase(new EventFormatter(), NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken) => Result.SuccessTask;
    }
}
